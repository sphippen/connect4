using System;
using System.Collections.Generic;
using System.Linq;
using Connect4Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Connect4GUI
{
    public class GameplayState : IGameState
    {
        private const float boardDepth = 0.2f;
        private const float textDepth = 0.3f;
        private const float behindCheckerDepth = 0.9f;
        private const float checkerDepth = 0.8f;
        private const float logoDepth = 1.0f;

        private const int columnWidth = 32;
        private const int rowHeight = 32;

        private const float checkerVelocity = 600f;

        private const double pulsePeriod = 1000;

        private static readonly Color textColor = Color.Yellow;

        private SpriteBatch spriteBatch;
        private Connect4 owner;

        private Texture2D boardTexture;
        private Texture2D blackChecker;
        private Texture2D whiteChecker;
        private Texture2D redChecker;
        private Texture2D log;
        private Texture2D sider;
        private SpriteFont font;

        private Sprite board;
        private Sprite logo;
        private Sprite leftSider;
        private Sprite rightSider;
        private List<Sprite> checkers;
        private Sprite selectionChecker;

        private TextSprite youText;
        private TextSprite vs;
        private TextSprite opponentText;
        private TextSprite generalMessage;

        private string userName;
        private string hostName;
        private int port;
        
        private string opponentsName;

        private int selectedCol;

        private Connect4ClientGame c4Game;

        private KeyboardState? oldKeyboardState;

        private bool isOver;
        private bool youGoFirst;
        private bool yourTurn;

        public GameplayState(Connect4 game, string userName, string hostName, int port)
        {
            owner = game;
            checkers = new List<Sprite>();
            c4Game = new Connect4ClientGame(userName);
            c4Game.Notify += ((msg) => Console.WriteLine(msg));
            this.hostName = hostName;
            this.userName = userName;
            this.port = port;
        }

        public void Initialize()
        {
            oldKeyboardState = null;
            selectedCol = 0;
            isOver = false;
            youGoFirst = false;
            opponentsName = null;

            c4Game.ConnectionEstablished += ConnectionEstablishedHandler;
            c4Game.Disconnected += DisconnectedHandler;
            c4Game.GameStarted += GameStartedHandler;
            c4Game.IllegalMove += IllegalMoveHandler;
            c4Game.MoveMade += MoveMadeHandler;
            c4Game.Tick += TickHandler;

            // load all our content now
            spriteBatch = new SpriteBatch(owner.GraphicsDevice);

            boardTexture = owner.Content.Load<Texture2D>("board");
            blackChecker = owner.Content.Load<Texture2D>("black-ch");
            redChecker = owner.Content.Load<Texture2D>("red-ch");
            whiteChecker = owner.Content.Load<Texture2D>("white-ch");
            log = owner.Content.Load<Texture2D>("logo");
            sider = owner.Content.Load<Texture2D>("side");

            board = new Sprite(boardTexture, new Vector2(
                columnWidth,
                Connect4.screenHeight - boardTexture.Height - (rowHeight / 2)), boardDepth);

            logo = new Sprite(log, new Vector2(Connect4.screenWidth / 2 - log.Width / 2, 0), logoDepth);

            leftSider = new Sprite(sider, new Vector2(
                0,
                Connect4.screenHeight - sider.Height), boardDepth);
            rightSider = new Sprite(sider, new Vector2(
                board.Position.X + boardTexture.Width,
                Connect4.screenHeight - sider.Height), boardDepth, SpriteEffects.FlipHorizontally);

            //initialized just to ensure we don't get a null pointer exception in update/draw
            selectionChecker = new Sprite(blackChecker, new Vector2(-1000, -1000), checkerDepth);

            font = owner.Content.Load<SpriteFont>("kongtext2");

            float x = board.Position.X + boardTexture.Width + columnWidth;

            youText = new TextSprite("", font, new Vector2(x, board.Position.Y), textDepth);
            vs = new TextSprite("", font, new Vector2(x, board.Position.Y + (rowHeight * 3) / 2), textDepth);
            opponentText = new TextSprite("", font, new Vector2(x, board.Position.Y + rowHeight * 3), textDepth);
            generalMessage = new TextSprite("", font, new Vector2(x, board.Position.Y + (rowHeight * 9) / 2), textDepth);

            c4Game.StartConnection(hostName, port);
        }

        public void Uninitialize()
        {
            c4Game.ConnectionEstablished -= ConnectionEstablishedHandler;
            c4Game.Disconnected -= DisconnectedHandler;
            c4Game.GameStarted -= GameStartedHandler;
            c4Game.IllegalMove -= IllegalMoveHandler;
            c4Game.MoveMade -= MoveMadeHandler;
            c4Game.Tick -= TickHandler;
        }

        public void ChangeState(Connect4 game, IGameState state)
        {
            game.ChangeState(state);
        }

        public void Update(GameTime gameTime)
        {
            // update sprites
            board.Update(gameTime);
            logo.Update(gameTime);
            leftSider.Update(gameTime);
            rightSider.Update(gameTime);

            lock (checkers)
            {
                foreach (Sprite s in checkers)
                {
                    s.Update(gameTime);
                }
            }

            selectionChecker.Update(gameTime);

            // update text
            youText.Update(gameTime);
            opponentText.Update(gameTime);
            vs.Update(gameTime);
            generalMessage.Update(gameTime);

            // input stuff
            KeyboardState state = Keyboard.GetState();
            if (oldKeyboardState != null)
            {
                KeyboardState oldKBState = (KeyboardState)oldKeyboardState;
                if (!isOver && oldKBState.IsKeyUp(Keys.Left) && state.IsKeyDown(Keys.Left))
                {
                    ChangeSelection(false);
                }
                if (!isOver && oldKBState.IsKeyUp(Keys.Right) && state.IsKeyDown(Keys.Right))
                {
                    ChangeSelection(true);
                }
                if (!isOver && oldKBState.IsKeyUp(Keys.Enter) && state.IsKeyDown(Keys.Enter))
                {
                    c4Game.MakeMove(selectedCol + 1);
                }
                if (!isOver && oldKBState.IsKeyUp(Keys.R) && state.IsKeyDown(Keys.R))
                {
                    c4Game.Resign();
                }
                if (!isOver && oldKBState.IsKeyUp(Keys.C) && state.IsKeyDown(Keys.C))
                {
                    if (opponentsName == null) // we haven't been paired up yet
                    {
                        c4Game.CancelConnection();
                        generalMessage.Message = "Cancelling...";
                    }
                }
                if (isOver && oldKBState.IsKeyUp(Keys.Escape) && state.IsKeyDown(Keys.Escape))
                {
                    owner.ChangeState(new PreConnectionState(owner));
                }
            }
            oldKeyboardState = state;
        }

        public void Draw(GameTime gameTime)
        {
            spriteBatch.Begin(SpriteSortMode.BackToFront, null, SamplerState.PointClamp, null, null);

            // draw sprites
            board.Draw(gameTime, spriteBatch);
            logo.Draw(gameTime, spriteBatch);
            leftSider.Draw(gameTime, spriteBatch);
            rightSider.Draw(gameTime, spriteBatch);

            lock (checkers)
            {
                foreach (Sprite s in checkers)
                {
                    s.Draw(gameTime, spriteBatch);
                }
            }

            selectionChecker.Draw(gameTime, spriteBatch);

            // draw text
            youText.Draw(gameTime, spriteBatch);
            opponentText.Draw(gameTime, spriteBatch);
            vs.Draw(gameTime, spriteBatch);
            generalMessage.Draw(gameTime, spriteBatch);

            spriteBatch.End();
        }

        /// <summary>
        /// Changes your current column selection
        /// </summary>
        /// <param name="right">if you are going right</param>
        private void ChangeSelection(bool right)
        {
            int curCol = selectedCol;
            if (right)
            {
                selectedCol++;
                selectedCol %= 7;
                while (c4Game.GameBoard.ElementAt(5).ElementAt(selectedCol) != BoardSquareState.empty && curCol != selectedCol)
                {
                    selectedCol++;
                    selectedCol %= 7;
                }
                if (yourTurn)
                {
                    selectionChecker.Position = new Vector2(board.Position.X + selectedCol * columnWidth, selectionChecker.Position.Y);
                }
            }
            else
            {
                selectedCol--;
                selectedCol += 7;
                selectedCol %= 7;
                while (c4Game.GameBoard.ElementAt(5).ElementAt(selectedCol) != BoardSquareState.empty && curCol != selectedCol)
                {
                    selectedCol--;
                    selectedCol += 7;
                    selectedCol %= 7;
                }
                if (yourTurn)
                {
                    selectionChecker.Position = new Vector2(board.Position.X + selectedCol * columnWidth, selectionChecker.Position.Y);
                }
            }
        }

        private void ConnectionEstablishedHandler(bool success)
        {
            if (!success)
            {
                isOver = true;
                generalMessage.Message = "Connection\nfailed.\n\nEsc to exit.";
            }
            else
            {
                generalMessage.Message = "Connected.";
            }
        }

        private void DisconnectedHandler(DisconnectReason reason)
        {
            selectionChecker.Position = new Vector2(-columnWidth, selectionChecker.Position.Y);
            string message = "";
            switch (reason)
            {
                case DisconnectReason.draw:
                    message = "It was a\ndraw.";

                    lock (checkers)
                    {
                        foreach (Sprite s in checkers)
                        {
                            s.PulseColor(Color.Black, pulsePeriod);
                        }
                    }

                    break;
                case DisconnectReason.opponentDisconnected:
                    message = "Your\nopponent\ndisconnected.";
                    break;
                case DisconnectReason.opponentOutOfTime:
                    message = "Your\nopponent\nran out of\ntime.";
                    break;
                case DisconnectReason.opponentResigned:
                    message = "Your\nopponent\nresigned.";
                    break;
                case DisconnectReason.opponentWon:
                    message = "You lost!";

                    lock (checkers)
                    {
                        List<Sprite> newCheckers = new List<Sprite>();
                        foreach (Sprite s in checkers)
                        {
                            if (s.Texture == (youGoFirst ? whiteChecker : redChecker))
                            {
                                newCheckers.Add(new Sprite(blackChecker, s.Position, behindCheckerDepth));
                                s.MoveToWithVelocity(new Vector2(s.Position.X, Connect4.screenHeight + rowHeight), checkerVelocity);
                            }
                        }
                        foreach (Sprite s in newCheckers)
                        {
                            checkers.Add(s);
                        }
                        newCheckers.Clear();
                    }

                    break;
                case DisconnectReason.serverDisconnected:
                    message = "The server\ndied.";
                    break;
                case DisconnectReason.youOutOfTime:
                    message = "You ran out\nof time.";
                    break;
                case DisconnectReason.youResigned:
                    message = "You\nresigned.";
                    break;
                case DisconnectReason.youWon:
                    message = "You won!";

                    lock (checkers)
                    {
                        foreach (Sprite s in checkers)
                        {
                            if (s.Texture == (youGoFirst ? whiteChecker : redChecker))
                            {
                                s.Texture = whiteChecker;
                                s.AlternateColors(new Color[] { Color.Red, Color.Blue, Color.HotPink, Color.Green, Color.Black }, 60);
                            }
                        }
                    }

                    break;
                case DisconnectReason.youLeft:
                    message = "You left\nbefore\nplaying.";
                    break;
                default:
                    break;
            }
            isOver = true;
            message += "\n\nEsc to exit.";
            generalMessage.Message = message;
        }

        private void GameStartedHandler(string opponentName, int timeLimit, bool _youGoFirst)
        {
            youGoFirst = _youGoFirst;
            yourTurn = youGoFirst;
            if (youGoFirst)
            {
                selectionChecker = new Sprite(whiteChecker,
                                              new Vector2(selectedCol * columnWidth + board.Position.X, board.Position.Y - rowHeight),
                                              checkerDepth);
            }
            else
            {
                selectionChecker = new Sprite(redChecker,
                                              new Vector2(-columnWidth, board.Position.Y - rowHeight),
                                              checkerDepth);
            }
            opponentsName = opponentName;

            youText.Message = userName + "\n" + timeLimit;
            opponentText.Message = opponentsName + "\n" + timeLimit;
            vs.Message = " -VS - ";
            generalMessage.Message = youGoFirst ? "You\ngo first." : "Opponent\ngoes first.";
        }

        private void IllegalMoveHandler()
        {
            generalMessage.Message = "Illegal move.";
        }

        private void MoveMadeHandler(int row, int col, bool wasYou)
        {
            Texture2D color = (wasYou == youGoFirst) ? whiteChecker : redChecker;
            col--;
            row--;
            yourTurn = !yourTurn;

            if (row == 5 && col == selectedCol)
            {
                ChangeSelection(true);
            }

            Vector2 checkerSpot = new Vector2(
                board.Position.X + col * columnWidth,
                board.Position.Y + (5 - row) * rowHeight);

            Vector2 checkerStart = new Vector2(
                board.Position.X + col * columnWidth,
                (wasYou ? board.Position.Y - rowHeight : 0 - rowHeight));

            Sprite newChecker = new Sprite(color, checkerStart, checkerDepth);
            newChecker.MoveToWithVelocity(checkerSpot, checkerVelocity);

            lock (checkers)
            {
                checkers.Add(newChecker);
            }

            selectionChecker.Position = new Vector2((wasYou ? -columnWidth : (board.Position.X + selectedCol * columnWidth)),
                                                     selectionChecker.Position.Y);
            generalMessage.Message = (wasYou) ? "Opponent's\nturn." : "Your\nturn.";
        }

        private void TickHandler(bool isYou, int tick)
        {
            if (isYou)
            {
                youText.Message = userName + "\n" + tick;
            }
            else
            {
                opponentText.Message = opponentsName + "\n" + tick;
            }
        }
    }
}
