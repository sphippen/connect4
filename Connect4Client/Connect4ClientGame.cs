// Written by Aric Parkinson and Spencer Phippen for CS 3500, October 2011
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Connect4Client
{

    public enum DisconnectReason
    {
        youResigned, opponentResigned, opponentDisconnected, youOutOfTime, opponentOutOfTime,
        youWon, opponentWon, draw, serverDisconnected, youLeft
    };

    /// <summary>
    /// Three states a square on the board may take
    /// </summary>
    public enum BoardSquareState { white, black, empty };

    public class Connect4ClientGame
    {
        /// <summary>
        /// Called when a message comes in on the underlying socket.
        /// </summary>
        public event Action<string> Notify;

        /// <summary>
        /// Triggers each time the timer ticks once. Int parameter represents the time remaining on the clock.
        /// </summary>
        public event Action<bool, int> Tick;
        /// <summary>
        /// Triggers each time a valid move is made by either player. First int represents is row, 2nd is column.
        /// Bool represents whether you made the move or not.
        /// </summary>
        public event Action<int, int, bool> MoveMade;
        /// <summary>
        /// Triggers when a successful connection is established with the server.
        /// </summary>
        public event Action<bool> ConnectionEstablished;
        /// <summary>
        /// Triggers when the game has started. String parameter is the name of your opponent. Int parameter is
        /// the time limit for each turn. Bool indicates whether you play first.
        /// </summary>
        public event Action<string, int, bool> GameStarted;
        /// <summary>
        /// Triggers each time an illegal move is made by you.
        /// </summary>
        public event Action IllegalMove;
        /// <summary>
        /// Triggers when you've disconnected from the server. DisconnectReason gives the reason disconnection
        /// occurred.
        /// </summary>
        public event Action<DisconnectReason> Disconnected;

        private BoardSquareState[][] gameBoard;                 //Represents the game board - position (relative to PS7 specs) is
                                                                    //the first index * 7 plus the second index + 1.
        private bool isBlack;                                   //True if you are black (playing first)
        private bool yourTurn;                                  //True if it is currently your turn
        private readonly string name;                           //This player's name
        private int moveCol;                                    //The last move you requested to make to the server
        private bool waitingForMoveReply;                       //whether or not we are waiting for a legality reply on a move we tried to make

        private Connect4Client client;                          //Client object managing communication between the server and the player

        /// <summary>
        /// Constructs a new Connect4ClientGame for a player with the given name.
        /// </summary>
        /// <param name="_name">Name of this player</param>
        public Connect4ClientGame(string _name)
        {
            if (_name.Contains("\n"))
            {
                throw new InvalidOperationException();
            }

            gameBoard = new BoardSquareState[6][];
            for (int i = 0; i < gameBoard.Length; i++)
            {
                gameBoard[i] = new BoardSquareState[7];
                for (int j = 0; j < gameBoard[i].Length; j++)
                {
                    gameBoard[i][j] = BoardSquareState.empty;
                }
            }

            name = _name;
            moveCol = -1;
            waitingForMoveReply = false;

            client = new Connect4Client(this);
            client.Disconnected += DisconnectedHandler;
            client.GameStarted += GameStartedHandler;
            client.IllegalMove += IllegalMoveHandler;
            client.LegalMove += LegalMoveHandler;
            client.MoveMade += MoveMadeHandler;
            client.Tick += TickHandler;
        }

        /// <summary>
        /// Starts a connection with a server on the provided Host name on the
        /// given port.
        /// </summary>
        /// <param name="hostName">Host name for the server</param>
        /// <param name="port">Port to connect to the server on</param>
        public void StartConnection(string hostName, int port)
        {
            client.BeginConnect(hostName, port, ConnectionCallback);
        }

        /// <summary>
        /// Cancels a connection with the server
        /// </summary>
        public void CancelConnection()
        {
            Task.Factory.StartNew(() => client.CancelConnection());
        }

        /// <summary>
        /// Attempts to make a move to the given column. If it is not your
        /// turn, this method will do nothing. If the move is illegal,
        /// the IllegalMove event will trigger.
        /// </summary>
        /// <param name="col">Column to attempt to move to</param>
        public void MakeMove(int col)
        {
            if (col <= 7 && col >= 1)
            {
                lock (this)
                {
                    if (yourTurn && !waitingForMoveReply)
                    {
                        moveCol = col;
                        waitingForMoveReply = true;
                        client.Move(col);
                    }
                }
            }
        }

        /// <summary>
        /// Resigns from the current game and disconnects you from the server
        /// </summary>
        public void Resign()
        {
            client.Resign();
        }

        internal void notify(string message)
        {
            if (Notify != null)
            {
                Notify(message);
            }
        }

        /// <summary>
        /// Handles ending a connection attempt, called from the Connect4Client.
        /// </summary>
        /// <param name="success">whether or not the connection attempt succeeded</param>
        private void ConnectionCallback(bool success)
        {
            if (success)
            {
                client.SendName(name);
            }

            if (ConnectionEstablished != null)
            {
                Task.Factory.StartNew(() => ConnectionEstablished(success));
            }
        }

        /// <summary>
        /// Handles the Disconnected event from the Connect4Client.
        /// </summary>
        /// <param name="dr">the reason for disconnecting</param>
        private void DisconnectedHandler(DisconnectReasonInside dr)
        {
            if (Disconnected != null)
            {
                // only initialized to make compiler happy
                DisconnectReason reason = DisconnectReason.serverDisconnected;
                switch (dr)
                {
                    case DisconnectReasonInside.blackOutOfTime:
                        reason = (isBlack) ? DisconnectReason.youOutOfTime : DisconnectReason.opponentOutOfTime;
                        break;
                    case DisconnectReasonInside.blackResigned:
                        reason = (isBlack) ? DisconnectReason.youResigned : DisconnectReason.opponentResigned;
                        break;
                    case DisconnectReasonInside.blackWon:
                        reason = (isBlack) ? DisconnectReason.youWon : DisconnectReason.opponentWon;
                        break;
                    case DisconnectReasonInside.draw:
                        reason = DisconnectReason.draw;
                        break;
                    case DisconnectReasonInside.opponentDisconnected:
                        reason = DisconnectReason.opponentDisconnected;
                        break;
                    case DisconnectReasonInside.serverDisconnected:
                        reason = DisconnectReason.serverDisconnected;
                        break;
                    case DisconnectReasonInside.whiteOutOfTime:
                        reason = (isBlack) ? DisconnectReason.opponentOutOfTime : DisconnectReason.youOutOfTime;
                        break;
                    case DisconnectReasonInside.whiteResigned:
                        reason = (isBlack) ? DisconnectReason.opponentResigned : DisconnectReason.youResigned;
                        break;
                    case DisconnectReasonInside.whiteWon:
                        reason = (isBlack) ? DisconnectReason.opponentWon : DisconnectReason.youWon;
                        break;
                    case DisconnectReasonInside.youLeft:
                        reason = DisconnectReason.youLeft;
                        break;
                    default:
                        break;
                }
                Task.Factory.StartNew(() => Disconnected(reason));
            }
        }

        /// <summary>
        /// Handles the GameStarted event from the Connect4Client.
        /// </summary>
        /// <param name="opponentName">the name of your opponent</param>
        /// <param name="timeLimit">the timelimit on the server</param>
        /// <param name="youGoFirst">whether or not you go first</param>
        private void GameStartedHandler(string opponentName, int timeLimit, bool youGoFirst)
        {
            yourTurn = isBlack = youGoFirst;
            if (GameStarted != null)
            {
                Task.Factory.StartNew(() => GameStarted(opponentName, timeLimit, youGoFirst));
            }
        }

        /// <summary>
        /// Handles the IllegalMove event from the Connect4Client.
        /// </summary>
        private void IllegalMoveHandler()
        {
            lock (this)
            {
                waitingForMoveReply = false;
            }
            if (IllegalMove != null)
            {
                Task.Factory.StartNew(() => IllegalMove());
            }
        }

        /// <summary>
        /// Handles the LegalMove event from the Connect4Client.
        /// </summary>
        private void LegalMoveHandler()
        {
            int row;
            lock (this)
            {
                waitingForMoveReply = false;
                yourTurn = false;
                moveCol--;
                for (row = 0; row < 6; row++)
                {
                    if (gameBoard[row][moveCol] == BoardSquareState.empty)
                    {
                        gameBoard[row][moveCol] = (isBlack) ? BoardSquareState.black : BoardSquareState.white;
                        break;
                    }
                }
                moveCol++;
            }
            if (MoveMade != null)
            {
                row++;
                Task.Factory.StartNew(() => MoveMade(row, moveCol, true));
            }
        }

        /// <summary>
        /// Handles the MoveMade event from the Connect4Client.
        /// </summary>
        /// <param name="col">the position being moved to</param>
        private void MoveMadeHandler(int col)
        {
            int row;
            lock (this)
            {
                yourTurn = true;
                col--;
                for (row = 0; row < 6; row++)
                {
                    if (gameBoard[row][col] == BoardSquareState.empty)
                    {
                        gameBoard[row][col] = (isBlack) ? BoardSquareState.white : BoardSquareState.black;
                        break;
                    }
                }
                col++;
            }
            if (MoveMade != null)
            {
                row++;
                Task.Factory.StartNew(() => MoveMade(row, col, false));
            }
        }

        /// <summary>
        /// Handles the Tick event from the Connect4Client.
        /// </summary>
        /// <param name="tick">the number of seconds left</param>
        private void TickHandler(string color, int tick)
        {
            bool you = (isBlack && color == "black" || !isBlack && color == "white");
            if (Tick != null)
            {
                Task.Factory.StartNew(() => Tick(you, tick));
            }
        }

        /// <summary>
        /// The name of the player on this client.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// An enumeration of the state of all spaces on this game board.
        /// </summary>
        public IEnumerable<IEnumerable<BoardSquareState>> GameBoard
        {
            get
            {
                lock (this)
                {
                    return gameBoard;
                }
            }
        }

    }
}
