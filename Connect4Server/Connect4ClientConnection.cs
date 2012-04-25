// Written by Aric Parkinson and Spencer Phippen for CS 3500, October 2011
using System;
using System.Net.Sockets;
using System.Text;
using System.Linq;

namespace Connect4Server
{
    internal class Connect4ClientConnection
    {
        private enum ClientState { notPlaying, playing, gone };               //Status of this player
                                                                               //notPlaying indicates the player is not in a game
                                                                               //playing indicates the player is currently in a game
                                                                               //gone indicates the player disconnected weirdly

        /// <summary>
        /// Triggered when a move is requested by the player. Connect4ClientConnection parameter is this connnection
        /// object. Int paramter is the position of the requested move.
        /// </summary>
        public event Action<Connect4ClientConnection, int> MoveRequest;
        /// <summary>
        /// Triggered when this player's name is specified. Never called more than once. Connect4ClientConnection parameter
        /// is this connection object.
        /// </summary>
        public event Action<Connect4ClientConnection> NameSpecified;
        /// <summary>
        /// Triggered when this player requests to resign. Connect4ClientConnection parameter is this connection object
        /// </summary>
        public event Action<Connect4ClientConnection> Resign;
        /// <summary>
        /// Triggered when connection with the player ends. Never called more than once. Connect4ClientConnection paramter
        /// is this connection object.
        /// </summary>
        public event Action<Connect4ClientConnection> Disconnected;

        private Socket socket;                                          //Socket managing connection between the player and the server
        private string name;                                            //Name of the player. Null if unnamed.
        private ClientState state;                                      //Current state of the player
        private Connect4Service server;                                 //Main server managing this player connection
        private static UTF8Encoding encoding = new UTF8Encoding();      //Used to decode incoming byte[] buffers
        private byte[] outBuffer;                                       //Buffer of bytes going out to the client
        private byte[] inBuffer;                                        //Buffer of bytes coming in from the client
        private string pendingSends;                                    //Commands waiting to be sent
        private string incomingMessage;                                 //Message coming in from the client
        private bool isSending;                                         //Used to indicate if we are currently sending

        private string lastBadCommand;                                  //Last command sent by the client that was no good

        /// <summary>
        /// Constructs a new Connect4ClientConnection object to manage the connection
        /// between the player connecting to the given server through the given socket.
        /// </summary>
        /// <param name="_socket">Socket the player is connecting through</param>
        /// <param name="_server">Server the player is connecting to</param>
        public Connect4ClientConnection(Socket _socket, Connect4Service _server)
        {
            socket = _socket;
            Name = null;
            state = ClientState.notPlaying;
            server = _server;
            outBuffer = new byte[1024];
            inBuffer = new byte[1024];
            pendingSends = "";
            incomingMessage = "";
            isSending = false;
            lastBadCommand = null;

            try
            {
                socket.BeginReceive(inBuffer, 0, inBuffer.Length, SocketFlags.None, MessageReceived, null);
            }
            catch (Exception)
            {
                DisconnectHelper();
            }
        }

        /// <summary>
        /// Closes the socket connecting the player and the server. To be used externally only.
        /// </summary>
        public void CloseClient()
        {
            // we need to block until any pending messages go out
            while (pendingSends != "" && socket != null)
            {
                System.Threading.Thread.Sleep(100);
            }

            lock (this)
            {
                if (!(state == ClientState.gone))
                {
                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);

                        Socket tmp = socket;
                        socket = null;
                        tmp.Close();
                    }
                    catch (Exception)
                    {
                    }

                    state = ClientState.gone;
                }
            }
        }

        /// <summary>
        /// Sends a "play" command to the client.
        /// </summary>
        /// <param name="opponent">Name of the opponent this player is playing against</param>
        /// <param name="timeLimit">Time limit for this game</param>
        /// <param name="yourColor">Color of this player, black or white</param>
        public void SendPlay(string opponent, int timeLimit, string yourColor)
        {
            state = ClientState.playing;
            SendMessage("@play\r\n#" + opponent + "\r\n#" + timeLimit + "\r\n#" + yourColor + "\r\n");
        }

        /// <summary>
        /// Sends "legal" if the play is legal, "illegal" if it is not.
        /// </summary>
        /// <param name="isLegal">True = "legal", false = "illegal"</param>
        public void SendLegality(bool isLegal)
        {
            SendMessage(isLegal ? "@legal\r\n" : "@full\r\n");
        }

        /// <summary>
        /// Sends the "move" command with the position the move happened in.
        /// </summary>
        /// <param name="position">Location of the move</param>
        public void SendMove(int position)
        {
            SendMessage("@move\r\n#" + position + "\r\n");
        }

        /// <summary>
        /// Sends the "win" command to the client, with the color of the winning player.
        /// </summary>
        /// <param name="color">white if the white player won, black if the black player won.</param>
        public void SendWin(string color)
        {
            SendMessage("@win\r\n#" + color + "\r\n");
        }

        /// <summary>
        /// Sends the "draw" command to the client.
        /// </summary>
        public void SendDraw()
        {
            SendMessage("@draw\r\n");
        }

        /// <summary>
        /// Sends the "disconnected" command to the client, along with the color of the disconnected player.
        /// </summary>
        /// <param name="color">Color of the player that disconnected</param>
        public void SendDisconnected(string color)
        {
            SendMessage("@disconnected\r\n#" + color + "\r\n");
        }

        /// <summary>
        /// Sends the "resigned" message with the color of which player resigned.
        /// </summary>
        /// <param name="color">Color of the player that left the game</param>
        public void SendResigned(string color)
        {
            SendMessage("@resigned\r\n#" + color + "\r\n");
        }

        /// <summary>
        /// Sends the "tick" command along with the number of seconds remaining in the turn.
        /// </summary>
        /// <param name="tick">Seconds remaining in the turn</param>
        /// <param name="color">Color of player who's turn it is</param>
        public void SendTick(int tick, string color)
        {
            SendMessage("@tick\r\n#" + color + "\r\n#" + tick + "\r\n");
        }

        /// <summary>
        /// Sends the "time" command, along with the player that ran out of time.
        /// </summary>
        /// <param name="color">Color of the player that ran out of time on their turn</param>
        public void SendTime(string color)
        {
            SendMessage("@time\r\n#" + color + "\r\n");
        }

        /// <summary>
        /// Callback for when a message is recieved from the player
        /// </summary>
        /// <param name="r"></param>
        private void MessageReceived(IAsyncResult r)
        {
            int bytes = 0;

            try
            {
                if (socket != null)
                {
                    bytes = socket.EndReceive(r);
                }
            }
            catch (Exception)
            {
            }

            if (bytes == 0)
            {
                DisconnectHelper();
            }
            else
            {
                incomingMessage += encoding.GetString(inBuffer, 0, bytes);
                server.notify(incomingMessage);
                
                int index;

                while ((index = incomingMessage.IndexOf('\n')) >= 0)
                {
                    string firstLine = incomingMessage.Substring(0, index);
                    if (firstLine.Length < 1 || firstLine[0] != '@')
                    {
                        incomingMessage = incomingMessage.Substring(index + 1);
                        continue;
                    }

                    if (lastBadCommand != null)
                    {
                        SendMessage("@ignoring\r\n#" + lastBadCommand + "\r\n");
                        lastBadCommand = null;
                    }

                    //Strip out '@'
                    firstLine = firstLine.Substring(1);

                    if (firstLine.EndsWith("\r"))
                    {
                        firstLine = firstLine.Substring(0, firstLine.Length - 1);
                    }

                    int paramNumber = ExpectedLines(firstLine);

                    if (paramNumber == -1)
                    {
                        lastBadCommand = firstLine;
                        incomingMessage = incomingMessage.Substring(index + 1);
                    }
                    else
                    {
                        incomingMessage = SplitComments(incomingMessage);

                        int count;
                        if ((count = incomingMessage.Count((c) => c == '\n')) >= paramNumber + 1)
                        {
                            bool breakAll = false;
                            string[] lines = new string[paramNumber + 1];
                            string messageCopy = incomingMessage;
                            index = 0;
                            int curBreak = -1;
                            for (int i = 0; i <= paramNumber; i++)
                            {
                                curBreak = messageCopy.IndexOf('\n');
                                index += curBreak + 1;

                                lines[i] = messageCopy.Substring(0, curBreak);

                                if (i != 0 && lines[i][0] != '#')
                                {
                                    lastBadCommand = firstLine;
                                    incomingMessage = messageCopy;
                                    breakAll = true;
                                    break;
                                }

                                //Split out '#', or '@' on first one
                                lines[i] = lines[i].Substring(1);

                                if (lines[i].EndsWith("\r"))
                                {
                                    lines[i] = lines[i].Substring(0, lines[i].Length - 1);
                                }

                                messageCopy = messageCopy.Substring(curBreak + 1);
                            }

                            if(breakAll)
                            {
                                break;
                            }

                            if (TryExecuteCommand(lines))
                            {
                                incomingMessage = incomingMessage.Substring(index);
                            }
                            else
                            {
                                lastBadCommand = firstLine;
                                incomingMessage = incomingMessage.Substring(index);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                try
                {
                    if (socket != null)
                    {
                        socket.BeginReceive(inBuffer, 0, inBuffer.Length, SocketFlags.None, MessageReceived, null);
                    }
                }
                catch (Exception)
                {
                    DisconnectHelper();
                }
            }
        }

        /// <summary>
        /// Splits all comments from a given command/parameter chain.
        /// </summary>
        /// <param name="txt">Message to split comments from</param>
        /// <returns>remainder of the string</returns>
        private string SplitComments(string txt)
        {
            int curStart = 0;
            int curBreak = 0;

            while ((curBreak = txt.IndexOf('\n', curStart)) >= 0)
            {
                if (txt[curStart] != '#' && txt[curStart] != '@')
                {
                    txt = txt.Substring(0, curStart) + txt.Substring(curBreak + 1);
                }
                else
                {
                    curStart = curBreak + 1;
                }
            }

            return txt;
        }

        /// <summary>
        /// Attempts to execute a command provided by the client
        /// </summary>
        /// <param name="lines">Lines in the command</param>
        /// <returns>True if valid command and params, false otherwise</returns>
        private bool TryExecuteCommand(string[] lines)
        {
            switch (lines[0])
            {
                case "name":
                    if (Name == null)
                    {
                        Name = lines[1];
                        if (NameSpecified != null)
                        {
                            NameSpecified(this);
                        }
                        return true;
                    }
                    return false;
                case "move":
                    int toMove;
                    if (Int32.TryParse(lines[1], out toMove) && toMove >= 1 && toMove <= 7 && state == ClientState.playing)
                    {
                        if (MoveRequest != null)
                        {
                            MoveRequest(this, toMove);
                        }
                        return true;
                    }
                    return false;
                case "resign":
                    if (state == ClientState.playing)
                    {
                        if (Resign != null)
                        {
                            Resign(this);
                        }
                        return true;
                    }
                    return false;
                case "ignoring":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns the number of lines expected from a given command from the client.
        /// </summary>
        /// <param name="command">Command to parse</param>
        /// <returns>Number of additional lines - -1 if invalid command</returns>
        private int ExpectedLines(string command)
        {
            switch (command)
            {
                case "name":
                    return 1;
                case "move":
                    return 1;
                case "resign":
                    return 0;
                case "ignoring":
                    return 1;
                default:
                    return -1;
            }
        }

        //--- The next three methods are based on Prof. Zachary's ChatServer3's methods shown in class ---//

        /// <summary>
        /// Used to send a message to the player
        /// </summary>
        /// <param name="msg">Message to be sent</param>
        private void SendMessage(string msg)
        {
            lock (this)
            {
                pendingSends += msg;
                DoSendMessage();
            }
        }

        /// <summary>
        /// Sends the message if we are not currently waiting on a pending BeginSend, otherwise,
        /// doesn't.
        /// </summary>
        private void DoSendMessage()
        {
            lock (this)
            {
                if (!isSending)
                {
                    outBuffer = encoding.GetBytes(pendingSends);
                    isSending = true;
                    pendingSends = "";
                    try
                    {
                        if (socket != null)
                        {
                            socket.BeginSend(outBuffer, 0, outBuffer.Length, SocketFlags.None, SendCallback, 0);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Callback for when a message is sent to the client.
        /// </summary>
        /// <param name="r"></param>
        private void SendCallback(IAsyncResult r)
        {
            try
            {
                lock (this)
                {
                    if (socket != null)
                    {
                        int bytesSent = socket.EndSend(r);
                        int totalBytesSent = bytesSent + (int)r.AsyncState;
                        if (totalBytesSent == outBuffer.Length)
                        {
                            isSending = false;
                            if (pendingSends != "")
                            {
                                DoSendMessage();
                            }
                        }
                        else
                        {
                            socket.BeginSend(outBuffer, totalBytesSent, outBuffer.Length - totalBytesSent, SocketFlags.None, SendCallback, totalBytesSent);
                        }
                    }
                }
            }
            catch (Exception)
            { 
            }
        }

        /// <summary>
        /// Helper method used to notify the server that the socket connection ended, change the state of this object,
        /// close the socket, and call Disconnected
        /// </summary>
        private void DisconnectHelper()
        {
            lock (this)
            {
                if (state != ClientState.gone)
                {
                    server.notify("Socket closed");

                    Socket tmp = socket;
                    socket = null;
                    tmp.Close();
                    pendingSends = "";
                    state = ClientState.gone;
                }
            }
            if (Disconnected != null)
            {
                Disconnected(this);
            }
        }

        /// <summary>
        /// Name of this player
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }

            private set
            {
                name = value;
            }
        }
    }
}
