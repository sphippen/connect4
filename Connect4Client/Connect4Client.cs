// Written by Aric Parkinson and Spencer Phippen for CS 3500, October 2011
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Connect4Client
{
    /// <summary>
    /// The various ways in which the client may be disconnected.
    /// </summary>
    internal enum DisconnectReasonInside
    {
        blackResigned, whiteResigned, opponentDisconnected, blackOutOfTime, whiteOutOfTime,
        whiteWon, blackWon, draw, serverDisconnected, youLeft
    };

    internal class Connect4Client
    {
        /// <summary>
        /// Triggers when a game starts. String parameter is the name of your opponent. Int parameter
        /// is the time limit for turns in this game. The bool reprsents whether you are playing first.
        /// </summary>
        public event Action<string, int, bool> GameStarted;
        /// <summary>
        /// Triggers when a legal move is played by your opponent. Int parameter is the position on the
        /// board the move was made on.
        /// </summary>
        public event Action<int> MoveMade;
        /// <summary>
        /// Triggers when you make an illegal move.
        /// </summary>
        public event Action IllegalMove;
        /// <summary>
        /// Triggers when you make a legal move.
        /// </summary>
        public event Action LegalMove;
        /// <summary>
        /// Triggers when the "tick" message is recieved from the server. Int parameter is the time left
        /// in the current turn.
        /// </summary>
        public event Action<string, int> Tick;
        /// <summary>
        /// Triggers when the client is disconnected from the server for any reason. The DisconnectReason
        /// parameter represents why disconnection occurred.
        /// </summary>
        public event Action<DisconnectReasonInside> Disconnected;

        private enum NetworkState { connecting, unnamed, waiting, playing, gameOver };   //Indicates the state of connection
                                                                                            //connecting - connection with the server is not established
                                                                                            //waiting - connection established, but the game has not started yet
                                                                                            //playing - currently in game with another player
                                                                                            //gameOver - the game has ended

        private TcpClient client;                                               //Socket managing connection with the server
        private byte[] outBuffer;                                               //Buffer of bytes going out to the server
        private byte[] inBuffer;                                                //Buffer of bytes coming in from the server
        private string incomingMessage;                                         //Message coming in from the server
        private string pendingSends;                                            //List of commands queued to be sent to the server
        private NetworkState state;                                             //Current state of connection with the server
        private string lastBadCommand;                                          //the most recent command that came in that is not valid
        private Action<Boolean> connectionCallback;
        private Connect4ClientGame owner;

        private bool isSending;
        private static UTF8Encoding encoder = new UTF8Encoding();

        /// <summary>
        /// Constructs a new Connect4Client
        /// </summary>
        public Connect4Client(Connect4ClientGame ga)
        {
            client = new TcpClient();
            outBuffer = new byte[1024];
            inBuffer = new byte[1024];
            incomingMessage = "";
            pendingSends = "";
            isSending = false;
            lastBadCommand = null;
            state = NetworkState.gameOver;
            owner = ga;
        }

        /// <summary>
        /// Begins a connection with the server on the provided host name and port. Calls back to the
        /// given delegate after connection is achieved
        /// </summary>
        /// <param name="hostName">Host name for the server</param>
        /// <param name="port">Port for the server</param>
        /// <param name="callback">Function to call back after connection is made</param>
        public void BeginConnect(string hostName, int port, Action<Boolean> callback)
        {
            lock (this)
            {
                if (state == NetworkState.gameOver)
                {
                    connectionCallback = callback;
                    state = NetworkState.connecting;
                    client.BeginConnect(hostName, port, ConnectCallback, null);
                }
            }
        }


        //----PROTOCOL MESSAGE METHODS----//

        /// <summary>
        /// Sends the "name" command along with the name for this player. Throws
        /// exception if the given name has any '\n' characters in it.
        /// </summary>
        /// <param name="name">Name of this player</param>
        internal void SendName(string name)
        {
            lock (this)
            {
                if (state == NetworkState.unnamed)
                {
                    SendMessage("@name\n#" + name + "\n");
                    state = NetworkState.waiting;
                }
            }
        }

        /// <summary>
        /// Sends the "resign" command to the server.
        /// </summary>
        internal void Resign()
        {
            lock (this)
            {
                if (state == NetworkState.playing)
                {
                    SendMessage("@resign\n");
                }
            }
        }

        /// <summary>
        /// Sends the "move" command to the server, along with the column the
        /// move took.
        /// </summary>
        /// <param name="space">The space to move to</param>
        internal void Move(int column)
        {
            lock (this)
            {
                if (state == NetworkState.playing)
                {
                    SendMessage("@move\n#" + column + "\n");
                }
            }
        }

        /// <summary>
        /// Cancels connecting to the server - can be called any time before a game is started.
        /// </summary>
        internal void CancelConnection()
        {
            lock (this)
            {
                if (state == NetworkState.connecting)
                {
                    client.Close();
                }
                else if (state == NetworkState.unnamed || state == NetworkState.waiting)
                {
                    state = NetworkState.gameOver;
                    client.Client.Shutdown(SocketShutdown.Both);
                    TcpClient tmp = client;
                    client = null;
                    tmp.Close();
                    if (Disconnected != null)
                    {
                        Disconnected(DisconnectReasonInside.youLeft);
                    }
                }
            }

        }

        //----SOCKET CALLBACK METHODS----//

        /// <summary>
        /// Callback for socket.BeginConnect
        /// </summary>
        /// <param name="ar"></param>
        public void ConnectCallback(IAsyncResult ar)
        {
            lock (this)
            {
                try
                {
                    client.EndConnect(ar);
                }
                catch (Exception)
                {
                    state = NetworkState.gameOver;
                    if (connectionCallback != null)
                    {
                        connectionCallback(false);
                    }
                    return;
                }
                state = NetworkState.unnamed;
                if (connectionCallback != null)
                {
                    connectionCallback(true);
                }
            }

            try
            {
                client.Client.BeginReceive(inBuffer, 0, inBuffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception)
            {
                lock (this)
                {
                    if (state != NetworkState.gameOver)
                    {
                        Disconnect(DisconnectReasonInside.serverDisconnected);
                    }
                }
                TcpClient tmp = client;
                client = null;
                tmp.Close();
            }
        }

        /// <summary>
        /// Callback for socket.BeginRecieve
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            int bytes = 0;

            try
            {
                if (client != null)
                {
                    bytes = client.Client.EndReceive(ar);
                }
            }
            catch (Exception)
            {
            }

            if (bytes == 0)
            {
                if (client != null)
                {
                    lock (this)
                    {
                        if (state != NetworkState.gameOver)
                        {
                            Disconnect(DisconnectReasonInside.serverDisconnected);
                        }
                    }
                    TcpClient tmp = client;
                    client = null;
                    tmp.Close();
                }
            }
            else
            {
                incomingMessage += encoder.GetString(inBuffer, 0, bytes);

                if (owner != null)
                {
                    owner.notify(incomingMessage);
                }

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

                            if (breakAll)
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
                    if (client != null)
                    {
                        client.Client.BeginReceive(inBuffer, 0, inBuffer.Length, SocketFlags.None, ReceiveCallback, null);
                    }
                }
                catch (Exception)
                {
                    Disconnect(DisconnectReasonInside.serverDisconnected);
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
        /// Attempts to execute command provided by the server
        /// </summary>
        /// <param name="lines">Lines recieved from the server</param>
        /// <returns>True if executed, false otherwise</returns>
        private bool TryExecuteCommand(string[] lines)
        {
            lock (this)
            {
                switch (lines[0])
                {
                    case "play":
                        if (state == NetworkState.waiting)
                        {
                            int timeLimit;
                            if(Int32.TryParse(lines[2], out timeLimit) && (lines[3] == "black" || lines[3] == "white"))
                            {
                                state = NetworkState.playing;
                                if (GameStarted != null)
                                {
                                    GameStarted(lines[1], timeLimit, lines[3] == "black"); 
                                }
                                return true;
                            }
                        }
                        return false;
                    case "move":
                        if (state == NetworkState.playing)
                        {
                            int toMove;
                            if (Int32.TryParse(lines[1], out toMove) && toMove <= 7 && toMove >= 1)
                            {
                                if (MoveMade != null)
                                {
                                    MoveMade(toMove);
                                }
                                return true;
                            }
                        }
                        return false;
                    case "full":
                        if (state == NetworkState.playing)
                        {
                            if (IllegalMove != null)
                            {
                                IllegalMove();
                            }
                            return true;
                        }
                        return false;
                    case "legal":
                        if (state == NetworkState.playing)
                        {
                            if (LegalMove != null)
                            {
                                LegalMove();
                            }
                            return true;
                        }
                        return false;
                    case "win":
                        if (state == NetworkState.playing)
                        {
                            if (lines[1] == "white")
                            {
                                Disconnect(DisconnectReasonInside.whiteWon);
                                return true;
                            }
                            else if (lines[1] == "black")
                            {
                                Disconnect(DisconnectReasonInside.blackWon);
                                return true;
                            }
                        }
                        return false;
                    case "draw":
                        if (state == NetworkState.playing)
                        {
                            Disconnect(DisconnectReasonInside.draw);
                            return true;
                        }
                        return false;
                    case "disconnected":
                        if (state == NetworkState.playing)
                        {
                            Disconnect(DisconnectReasonInside.opponentDisconnected);
                            return true;
                        }
                        return false;
                    case "resigned":
                        if (state == NetworkState.playing)
                        {
                            if (lines[1] == "white")
                            {
                                Disconnect(DisconnectReasonInside.whiteResigned);
                                return true;
                            }
                            else if (lines[1] == "black")
                            {
                                Disconnect(DisconnectReasonInside.blackResigned);
                                return true;
                            }
                        }
                        return false;
                    case "tick":
                        if (state == NetworkState.playing)
                        {
                            int tickValue;
                            if ((lines[1] == "black" || lines[1] == "white" ) && 
                                Int32.TryParse(lines[2], out tickValue) && tickValue >= 0)
                            {
                                if (Tick != null)
                                {
                                    Tick(lines[1], tickValue);
                                }
                                return true;
                            }
                        }
                        return false;
                    case "time":
                        if (state == NetworkState.playing)
                        {
                            if (lines[1] == "white")
                            {
                                Disconnect(DisconnectReasonInside.whiteOutOfTime);
                                return true;
                            }
                            else if (lines[1] == "black")
                            {
                                Disconnect(DisconnectReasonInside.blackOutOfTime);
                                return true;
                            }
                        }
                        return false;
                    case "ignoring":
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Returns number of lines expected from a given command from the server.
        /// </summary>
        /// <param name="command">Command to be parsed</param>
        /// <returns>Number of parameters expected - -1 if invalid command</returns>
        private int ExpectedLines(string command)
        {
            switch (command)
            {
                case "play":
                    return 3;
                case "move":
                    return 1;
                case "full":
                    return 0;
                case "legal":
                    return 0;
                case "win":
                    return 1;
                case "draw":
                    return 0;
                case "disconnected":
                    return 1;
                case "resigned":
                    return 1;
                case "tick":
                    return 2;
                case "time":
                    return 1;
                case "ignoring":
                    return 1;
                default:
                    return -1;
            }
        }

        /// <summary>
        /// Disconnects from the server
        /// </summary>
        /// <param name="reason">Reason disconnection occurs</param>
        private void Disconnect(DisconnectReasonInside reason)
        {
            lock (this)
            {
                if (state != NetworkState.gameOver)
                {
                    state = NetworkState.gameOver;
                    if (Disconnected != null)
                    {
                        Disconnected(reason);
                    }
                }
            }
        }

        //--- The next three methods are based on Prof. Zachary's ChatServer3's methods shown in class ---//

        /// <summary>
        /// Used to send a message to the server.
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
                if (!isSending && pendingSends != "")
                {
                    outBuffer = encoder.GetBytes(pendingSends);
                    isSending = true;
                    pendingSends = "";
                    try
                    {
                        if (client != null)
                        {
                            client.Client.BeginSend(outBuffer, 0, outBuffer.Length, SocketFlags.None, SendCallback, 0);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Callback for when a message is sent to the server.
        /// </summary>
        /// <param name="r"></param>
        private void SendCallback(IAsyncResult r)
        {
            try
            {
                lock (this)
                {
                    if (client != null)
                    {
                        int bytesSent = client.Client.EndSend(r);
                        int totalBytesSent = bytesSent + (int)r.AsyncState;
                        if (totalBytesSent == outBuffer.Length)
                        {
                            isSending = false;
                            DoSendMessage();
                        }
                        else
                        {
                            client.Client.BeginSend(outBuffer, totalBytesSent, outBuffer.Length - totalBytesSent, SocketFlags.None, SendCallback, totalBytesSent);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }

}
