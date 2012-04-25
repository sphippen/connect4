// Written by Aric Parkinson and Spencer Phippen for CS 3500, October 2011
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
namespace Connect4Server
{
    public class Connect4Service
    {
        public enum WhoGoesFirst {first, second, random};

        /// <summary>
        /// Triggered whenever a message is received. String parameter is the message received.
        /// </summary>
        public event Action<string> NotificationEvent;


        private List<Connect4Game> games;                           //List of all games currently running on the server
        private List<Connect4ClientConnection> needToIdentify;      //List of clients who have connected, but have not yet identified themselves
        private Connect4ClientConnection waiting;                  //Player 1 - put into a game as soon as Player 2 is identified. Set to null once the game starts
        private TcpListener server;                                 //Manages connection between the Server and the clients.
        private readonly int timeLimit;                            //Time limit established upon creation of the server.
        private bool isShuttingDown;                                //Whether or now we have been requested to shut down
        private WhoGoesFirst choosingMethod;

        /// <summary>
        /// Constructs a new Connect4Service object. Takes values to set the port it listens on,
        /// as well as a time limit for games held on this server.
        /// Throws InvalidOperationException if timelimit &lt; 1, or the port number is out of range
        /// or already being listened on.
        /// </summary>
        /// <param name="portNumber">Port for the server to listen on</param>
        /// <param name="timeLimit">Time limit for users in games on this server</param>
        public Connect4Service(int portNumber, int _timeLimit, WhoGoesFirst _choosingMethod)
        {
            if (_timeLimit <= 0)
            {
                throw new InvalidOperationException("time limit must be positive");
            }
            if (portNumber > IPEndPoint.MaxPort || portNumber < IPEndPoint.MinPort)
            {
                throw new InvalidOperationException("invalid port number");
            }
            choosingMethod = _choosingMethod;
            games = new List<Connect4Game>();
            needToIdentify = new List<Connect4ClientConnection>();
            waiting = null;
            timeLimit = _timeLimit;
            server = new TcpListener(IPAddress.Any, portNumber);
            try
            {
                server.Start();
            }
            catch (SocketException)
            {
                throw new InvalidOperationException("port already taken (or something like that)");
            }

            server.BeginAcceptSocket(ConnectionRequested, null);
            isShuttingDown = false;
        }

        /// <summary>
        /// Shuts down the server, closing all sockets currently open.
        /// </summary>
        public void Shutdown()
        {
            lock (this)
            {
                isShuttingDown = true;

                TcpListener tmp = server;
                server = null;
                tmp.Stop();

                foreach (Connect4Game g in games)
                {
                    Task.Factory.StartNew(() => g.PrepareToDie());
                }
                foreach (Connect4ClientConnection client in needToIdentify)
                {
                    client.NameSpecified -= NameSpecified;
                    client.Disconnected -= Disconnected;
                    client.CloseClient();
                }
                needToIdentify.Clear();
                if (waiting != null)
                {
                    waiting.Disconnected -= Disconnected;
                    waiting.CloseClient();
                    waiting = null;
                }
            }
        }

        /// <summary>
        /// Triggers the NotificationEvent, to be used external to this class.
        /// </summary>
        /// <param name="message">Message recieved</param>
        internal void notify(string message)
        {
            if (NotificationEvent != null)
            {
                NotificationEvent(message);
            }
        }

        /// <summary>
        /// Callback for whenever a socket is attempted to be opened with the server.
        /// </summary>
        /// <param name="ar"></param>
        private void ConnectionRequested(IAsyncResult ar)
        {
            try
            {
                if (server != null)
                {
                    Socket s = server.EndAcceptSocket(ar);

                    ////// Uncomment the following line if you want to test with telnet
                    //s.Send(new byte[1] { 60 });
                    ////// Leave it commented otherwise

                    server.BeginAcceptSocket(ConnectionRequested, null);

                    lock (this)
                    {
                        Connect4ClientConnection client = new Connect4ClientConnection(s, this);
                        needToIdentify.Add(client);
                        client.NameSpecified += NameSpecified;
                        client.Disconnected += Disconnected;

                        // just in case the client identified before we hooked up to its NameSpecified event
                        if (client.Name != null)
                        {
                            NameSpecified(client);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Called when a game is ended. Associated to an event in the Connect4Game class,
        /// so that when that game ends, we can remove it from the list (freeing up memory
        /// for future games).
        /// </summary>
        /// <param name="game">The game to be closed</param>
        private void GameFinished(Connect4Game game)
        {
            lock (this)
            {
                game.GameHasEnded -= GameFinished;
                games.Remove(game);
            }
        }

        /// <summary>
        /// Called when a player identifies him/herself. Will move into a waiting position, then
        /// if each waiting position is filled, starts a game and nulls each waiting position.
        /// </summary>
        /// <param name="player">Player that did gone and named him/herself</param>
        private void NameSpecified(Connect4ClientConnection player)
        {
            lock (this)
            {
                if (!isShuttingDown)
                {
                    needToIdentify.Remove(player);
                    player.NameSpecified -= NameSpecified;
                    if (waiting == null)
                    {
                        waiting = player;
                    }
                    else
                    {
                        Connect4ClientConnection player1 = waiting;
                        waiting = null;
                        player1.Disconnected -= Disconnected;
                        player.Disconnected -= Disconnected;
                        Connect4Game game = new Connect4Game(player1, player, timeLimit, choosingMethod);
                        game.GameHasEnded += GameFinished;
                        games.Add(game);
                    }
                }
            }
        }

        /// <summary>
        /// Called when a player has disconnected before joining a game.
        /// </summary>
        /// <param name="player">Player that disconnected</param>
        private void Disconnected(Connect4ClientConnection player)
        {
            lock (this)
            {
                player.Disconnected -= Disconnected;
                if (player == waiting)
                {
                    player.CloseClient();
                    waiting = null;
                }
                else
                {
                    player.NameSpecified -= NameSpecified;
                    needToIdentify.Remove(player);
                    player.CloseClient();
                }
            }
        }
    }
}
