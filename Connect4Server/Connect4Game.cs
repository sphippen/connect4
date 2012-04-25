// Written by Aric Parkinson and Spencer Phippen for CS 3500, October 2011
using System;
using System.Timers;

// TODO: fix the timer (6-4?)

namespace Connect4Server
{
    internal class Connect4Game
    {
        private enum BoardSquareState { empty, black, white };          //Used to keep track of the state of a particular square on the board.
                                                                            //empty indicates the square has not been taken
                                                                            //black indicates the square is taken by black
                                                                            //white indicates the square is taken by white

        /// <summary>
        /// Triggered when the game has ended. The Connect4Game parameter is this game.
        /// </summary>
        public event Action<Connect4Game> GameHasEnded;

        private Connect4ClientConnection black;                         //Player in the black position
        private Connect4ClientConnection white;                         //Player in the white position
        private bool blacksMove;                                        //True if it is black's turn, false if it is white's turn
        private Timer gameTimer;                                        //Timer used to manage the time limit for turns
        private BoardSquareState[][] gameBoard;                         //Represents the board. The position (relative to the PS7 specs)
                                                                        //is the first index * 7 plus the second index plus 1
                                                                        //(to compensate for 0-based indexing).
        private DateTime lastTime;                                      //Time since last tick or turn
        private long whiteTimeLeft;                                     //Time remaining for White's turns
        private long blackTimeLeft;                                     //Time remaining for Black's turns
        private bool itsOver;                                           //set when this game has ended
        private int timeLimit;

        private static Random r = new Random();                         //for all your random-number-generating needs


        /// <summary>
        /// Constructs a new game between two players. Randomly selects positions (black or white), and sets
        /// up a timer to manage time left for a turn.
        /// </summary>
        /// <param name="client1">Player 1</param>
        /// <param name="client2">Player 2</param>
        /// <param name="timeLimit">Time limit for this game</param>
        public Connect4Game(Connect4ClientConnection client1, Connect4ClientConnection client2, int _timeLimit, 
                        Connect4Service.WhoGoesFirst choosingMethod)
        {
            switch (choosingMethod)
            {
                case Connect4Service.WhoGoesFirst.first:
                    black = client1;
                    white = client2;
                    break;
                case Connect4Service.WhoGoesFirst.second:
                    black = client2;
                    white = client1;
                    break;
                case Connect4Service.WhoGoesFirst.random:
                    int tmp;
                    lock (r)
                    {
                        tmp = r.Next() % 2;
                    }
                    if (tmp == 0)
                    {
                        black = client1;
                        white = client2;
                    }
                    else
                    {
                        black = client2;
                        white = client1;
                    }
                    break;
                default:
                    break;
            }


            black.MoveRequest += MoveRequest;
            white.MoveRequest += MoveRequest;
            black.Resign += Resigned;
            white.Resign += Resigned;
            black.Disconnected += Disconnected;
            white.Disconnected += Disconnected;

            timeLimit = _timeLimit;

            whiteTimeLeft = _timeLimit * 1000;
            blackTimeLeft = _timeLimit * 1000;
            blacksMove = true;
            gameBoard = new BoardSquareState[6][];
            for (int i = 0; i < 6; i++)
            {
                gameBoard[i] = new BoardSquareState[7];
                for (int j = 0; j < 7; j++)
                {
                    gameBoard[i][j] = BoardSquareState.empty;
                }
            }

            itsOver = false;

            black.SendPlay(white.Name, _timeLimit, "black");
            white.SendPlay(black.Name, _timeLimit, "white");

            gameTimer = new Timer(1000); // tick every second
            Tick(null, null);
            gameTimer.Elapsed += Tick;
            gameTimer.Start();
        }

        /// <summary>
        /// Used to request that this game shut down.
        /// </summary>
        public void PrepareToDie()
        {
            EndGame();
        }

        /// <summary>
        /// Tells each player that a second has gone by in this turn.
        /// </summary>
        private void Tick(object source, ElapsedEventArgs eea)
        {
            lastTime = DateTime.Now;
            lock (this)
            {
                if (!itsOver)
                {
                    gameTimer.Interval = 1000;
                    if (blacksMove)
                    {
                        if (source != null)
                        {
                            if (blackTimeLeft % 1000 == 0)
                            {
                                blackTimeLeft -= 1000;
                            }
                            else
                            {
                                blackTimeLeft -= (blackTimeLeft % 1000);
                            }
                        }

                        black.SendTick((int)(blackTimeLeft/1000), "black");
                        white.SendTick((int)(blackTimeLeft/1000), "black");

                        if (blackTimeLeft == 0)
                        {
                            black.SendTime("black");
                            white.SendTime("black");
                            EndGame();
                        }
                    }
                    else
                    {
                        if (source != null)
                        {
                            if (whiteTimeLeft % 1000 == 0)
                            {
                                whiteTimeLeft -= 1000;
                            }
                            else
                            {
                                whiteTimeLeft -= (whiteTimeLeft % 1000);
                            }
                        }

                        black.SendTick((int)(whiteTimeLeft/1000), "white");
                        white.SendTick((int)(whiteTimeLeft/1000), "white");

                        if (whiteTimeLeft == 0)
                        {
                            black.SendTime("white");
                            white.SendTime("white");
                            EndGame();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the move can be made, false otherwise.
        /// </summary>
        /// <param name="position">Column of move to be made</param>
        /// <param name="row">If the move is valid, will contain the row to put the piece. Undefined otherwise</param>
        /// <returns>True if valid, false if not</returns>
        private bool IsValidMove(int col, out int row)
        {
            if (col < 0 || col > 6)
            {
                row = -1;
                return false;
            }

            for (row = 0; row < 6; row++)
            {
                if (gameBoard[row][col] == BoardSquareState.empty)
                {
                    break;
                }
            }

            return row < 6;
        }

        /// <summary>
        /// Returns true if the specified move is a winning move for the specified player.
        /// </summary>
        /// <param name="col">the column to test</param>
        /// <param name="row">the row to test</param>
        /// <param name="check">the color to check</param>
        /// <returns>whether or not it is a winning move</returns>
        private bool IsWinningMove(int col, int row, BoardSquareState check)
        {
            return (PiecesInARow(row, col, 0, -1, check) + PiecesInARow(row, col, 0, 1, check) + 1) >= 4
                || (PiecesInARow(row, col, -1, 0, check) + 1) >= 4
                || (PiecesInARow(row, col, -1, -1, check) + PiecesInARow(row, col, 1, 1, check) + 1) >= 4
                || (PiecesInARow(row, col, 1, -1, check) + PiecesInARow(row, col, -1, 1, check) + 1) >= 4;
        }

        /// <summary>
        /// Returns the number of pieces traveled before encountering a space with a different
        /// state than the initial color. Requests parameters indicating direction to travel:
        /// ie: dx = 1, dy = 1 indicates up-left diagonal, dx = 0, dy = -1 indicates down.
        /// It is not recommended to send calls for |dx|, |dy| > 1, though function will still
        /// behave properly under those conditions.
        /// </summary>
        /// <param name="row">Starting position's row</param>
        /// <param name="col">Starting position's column</param>
        /// <param name="dx">Direction to travel in the rows</param>
        /// <param name="dy">Direction to travel in the columns</param>
        /// <param name="color">Initial color</param>
        /// <returns>Distance traveled before encountering a space in a different state than color</returns>
        private int PiecesInARow(int row, int col, int dx, int dy, BoardSquareState color)
        {
            int count = 0;

            while (true)
            {
                row += dx;
                col += dy;

                if (row > 5 || row < 0 || col > 6 || col < 0 || gameBoard[row][col] != color)
                {
                    break;
                }
                
                count++;
            }

            return count;
        }

        /// <summary>
        /// Returns a value that indicates whether the entire board is full.
        /// </summary>
        /// <returns></returns>
        private bool BoardIsFull()
        {
            for (int i = 0; i < 7; i++)
            {
                if (gameBoard[5][i] == BoardSquareState.empty)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Attempts to make the provided move by the provided player.
        /// If the move can't be made, and it is that player's turn, calls player.SendLegality(false).
        /// If the move can be made, and it is that player's turn, calls player.SendLegality(true), and otherPlayer.SendMove(position).
        /// If it is not that player's turn, ignores request.
        /// </summary>
        /// <param name="col">Column to be moved to</param>
        /// <param name="player">Player making the move</param>
        private void MoveRequest(Connect4ClientConnection player, int col)
        {
            DateTime tmp = System.DateTime.Now;
            lock (this)
            {
                if (!itsOver)
                {
                    Connect4ClientConnection mover = (blacksMove) ? black : white;
                    Connect4ClientConnection waiter = (blacksMove) ? white : black;
                    string color = (blacksMove) ? "black" : "white";
                    BoardSquareState moveState = (blacksMove) ? BoardSquareState.black : BoardSquareState.white;

                    if (player == mover)
                    {
                        col--;
                        int row;
                        if (IsValidMove(col, out row))
                        {
                            gameTimer.Stop();
                            if (blacksMove)
                            {
                                blackTimeLeft -= (int)(tmp.Subtract(lastTime).TotalMilliseconds + 0.5);
                            }
                            else
                            {
                                whiteTimeLeft -= (int)(tmp.Subtract(lastTime).TotalMilliseconds + 0.5);
                            }
                            lastTime = tmp;

                            mover.SendLegality(true);
                            blacksMove = !blacksMove;
                            gameBoard[row][col] = moveState;
                            waiter.SendMove(col + 1);
                            if (IsWinningMove(col, row, moveState))
                            {
                                black.SendWin(color);
                                white.SendWin(color);
                                EndGame();
                            }
                            else if (BoardIsFull())
                            {
                                black.SendDraw();
                                white.SendDraw();
                                EndGame();
                            }
                            else
                            {
                                // start the next timer
                                if (blacksMove)
                                {
                                    gameTimer.Interval = (blackTimeLeft % 1000 == 0) ? 1000 : (blackTimeLeft % 1000);
                                }
                                else
                                {
                                    if (whiteTimeLeft == timeLimit * 1000)
                                    {
                                        Tick(null, null);
                                    }
                                    gameTimer.Interval = (whiteTimeLeft % 1000 == 0) ? 1000 : (whiteTimeLeft % 1000);
                                }
                                gameTimer.Start();
                            }
                        }
                        else
                        {
                            mover.SendLegality(false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when a player has resigned from the game. Tells the other player that player left.
        /// </summary>
        /// <param name="player">Player that left</param>
        private void Resigned(Connect4ClientConnection player)
        {
            if (!itsOver)
            {
                string color = (player == white) ? "white" : "black";
                black.SendResigned(color);
                white.SendResigned(color);
                EndGame();
            }
        }

        /// <summary>
        /// Called if a player abruptly disconnects from the server. Informs the other player of this.
        /// </summary>
        /// <param name="player">Player that disconnected</param>
        private void Disconnected(Connect4ClientConnection player)
        {
            if (!itsOver)
            {
                if (player == white)
                {
                    black.SendDisconnected("white");
                    EndGame();
                }
                else
                {
                    white.SendDisconnected("black");
                    EndGame();
                }
            }
        }

        /// <summary>
        /// Helper method that drops event handlers on clients, sets itsOver stops the timer, closes them, and calls the GameHasEnded event.
        /// </summary>
        private void EndGame()
        {
            lock (this)
            {
                if (!itsOver)
                {
                    itsOver = true;
                    gameTimer.Stop();
                    gameTimer.Dispose();
                    black.Disconnected -= Disconnected;
                    white.Disconnected -= Disconnected;
                    black.CloseClient();
                    white.CloseClient();
                    if (GameHasEnded != null)
                    {
                        GameHasEnded(this);
                    }
                }
            }
        }
    }
}
