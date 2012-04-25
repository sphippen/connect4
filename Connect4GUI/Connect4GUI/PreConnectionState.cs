using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using BufferInput;

namespace Connect4GUI
{
    public class PreConnectionState : IGameState
    {
        private enum SelectedString { userName, hostName, port };

        private const float everythingDepth = 0.5f;
        private const int textXPos = 32 * 3;

        private const double pulsePeriod = 1000;
        private static readonly Color pulse1 = Color.Yellow;
        private static readonly Color pulse2 = Color.Red;

        private static readonly Color textColor = Color.White;

        private static StringBuilder userName;
        private static StringBuilder hostName;
        private static StringBuilder port;
        private static bool eventInputInitialized = false;

        private static SelectedString selection = SelectedString.userName;

        private static float oneLineSize;

        private SpriteBatch spriteBatch;
        private SpriteFont font;

        private Sprite instructions;
        private Sprite logo;
        private Sprite selectionChecker;

        private TextSprite userNameEntry;
        private TextSprite hostNameEntry;
        private TextSprite portEntry;
        private TextSprite showMessage;

        private string generalMessage;

        private Connect4 owner;

        private KeyboardState? oldKeyboardState;

        public PreConnectionState(Connect4 game)
        {
            owner = game;
            if (userName == null)
            {
                userName = new StringBuilder();
                hostName = new StringBuilder("localhost");
                port = new StringBuilder("4000");
            }
        }

        public void Initialize()
        {
            oldKeyboardState = null;

            generalMessage = " ";

            font = owner.Content.Load<SpriteFont>("smallfont");
            spriteBatch = new SpriteBatch(owner.GraphicsDevice);

            Texture2D log = owner.Content.Load<Texture2D>("logo");
            logo = new Sprite(log, new Vector2(Connect4.screenWidth / 2 - log.Width / 2, 0), everythingDepth);

            Texture2D instr = owner.Content.Load<Texture2D>("instructions");
            instructions = new Sprite(instr, new Vector2(0, Connect4.screenHeight - instr.Height), everythingDepth);

            string userNameString = "username: " + userName.ToString();
            string hostNameString = "hostname: " + hostName.ToString();
            string portString = "port number: " + port.ToString();

            string toWrite = userNameString + "\n" + hostNameString + "\n" + portString + "\n" + generalMessage;

            oneLineSize = font.MeasureString(" ").Y;
            float textHeight = font.MeasureString(toWrite).Y;
            Vector2 curPos = new Vector2(textXPos,
                (instructions.Position.Y - logo.Texture.Height) / 2 - textHeight / 2 + logo.Texture.Height);

            Vector2 originalPos = curPos;

            userNameEntry = new TextSprite(userNameString, font, curPos, everythingDepth);
            curPos.Y += oneLineSize;
            hostNameEntry = new TextSprite(userNameString, font, curPos, everythingDepth);
            curPos.Y += oneLineSize;
            portEntry = new TextSprite(portString, font, curPos, everythingDepth);
            curPos.Y += oneLineSize;
            showMessage = new TextSprite(generalMessage, font, curPos, everythingDepth);

            Texture2D smallChecker = owner.Content.Load<Texture2D>("red-sch");

            // these may look like magic numbers: they basically are. Just some fine-tuning of the position, that's all.
            selectionChecker = new Sprite(smallChecker, new Vector2(originalPos.X - 18, originalPos.Y - 1), everythingDepth);

            switch (selection)
            {
                case SelectedString.userName:
                    userNameEntry.PulseColors(pulse1, pulse2, pulsePeriod);
                    break;
                case SelectedString.hostName:
                    selectionChecker.Position = new Vector2(selectionChecker.Position.X,
                        selectionChecker.Position.Y + oneLineSize);
                    hostNameEntry.PulseColors(pulse1, pulse2, pulsePeriod);
                    break;
                case SelectedString.port:
                    selectionChecker.Position = new Vector2(selectionChecker.Position.X,
                        selectionChecker.Position.Y + 2 * oneLineSize);
                    portEntry.PulseColors(pulse1, pulse2, pulsePeriod);
                    break;
                default:
                    break;
            }

            if (!eventInputInitialized)
            {
                EventInput.Initialize(owner.Window);
                eventInputInitialized = true;
            }
            EventInput.CharEntered += CharacterEntered;
        }

        public void Uninitialize()
        {
            EventInput.CharEntered -= CharacterEntered;
        }

        public void Draw(GameTime gameTime)
        {
            spriteBatch.Begin(SpriteSortMode.BackToFront, null);

            logo.Draw(gameTime, spriteBatch);

            userNameEntry.Draw(gameTime, spriteBatch);
            hostNameEntry.Draw(gameTime, spriteBatch);
            portEntry.Draw(gameTime, spriteBatch);
            showMessage.Draw(gameTime, spriteBatch);

            instructions.Draw(gameTime, spriteBatch);

            selectionChecker.Draw(gameTime, spriteBatch);

            spriteBatch.End();
        }

        public void Update(GameTime gameTime)
        {
            // update sprites
            logo.Update(gameTime);

            userNameEntry.Update(gameTime);
            hostNameEntry.Update(gameTime);
            portEntry.Update(gameTime);
            showMessage.Update(gameTime);

            instructions.Update(gameTime);
            selectionChecker.Update(gameTime);

            // update text
            userNameEntry.Message = "username: " + userName.ToString();
            hostNameEntry.Message = "hostname: " + hostName.ToString();
            portEntry.Message = "port number: " + port.ToString();
            showMessage.Message = generalMessage;

            // get user input
            KeyboardState newState = Keyboard.GetState();

            if (oldKeyboardState != null)
            {
                KeyboardState oldKBState = (KeyboardState)oldKeyboardState;
                if (oldKBState.IsKeyUp(Keys.Up) && newState.IsKeyDown(Keys.Up))
                {
                    ChangeSelection(false);
                }
                if (oldKBState.IsKeyUp(Keys.Down) && newState.IsKeyDown(Keys.Down))
                {
                    ChangeSelection(true);
                }
                if (oldKBState.IsKeyUp(Keys.Enter) && newState.IsKeyDown(Keys.Enter))
                {
                    int portNumber;
                    if (hostName.ToString() != "")
                    {
                        if (Int32.TryParse(port.ToString(), out portNumber) && portNumber >= IPEndPoint.MinPort && portNumber <= IPEndPoint.MaxPort)
                        {
                            owner.ChangeState(new GameplayState(owner, userName.ToString(), hostName.ToString(), portNumber));
                        }
                        else
                        {
                            generalMessage = "Invalid port number.";
                        }
                    }
                    else
                    {
                        generalMessage = "Must enter a hostname.";
                    }
                }
                else if (oldKBState.IsKeyUp(Keys.Escape) && newState.IsKeyDown(Keys.Escape))
                {
                    owner.Exit();
                }
            }

            oldKeyboardState = newState;
        }

        private void ChangeSelection(bool down)
        {
            if (down)
            {
                switch (selection)
                {
                    case SelectedString.userName:
                        userNameEntry.StopPulsing();
                        selection = SelectedString.hostName;
                        selectionChecker.Position = new Vector2(selectionChecker.Position.X,
                                selectionChecker.Position.Y + oneLineSize);
                        hostNameEntry.PulseColors(pulse1, pulse2, pulsePeriod);
                        break;
                    case SelectedString.hostName:
                        hostNameEntry.StopPulsing();
                        selection = SelectedString.port;
                        selectionChecker.Position = new Vector2(selectionChecker.Position.X,
                                selectionChecker.Position.Y + oneLineSize);
                        portEntry.PulseColors(pulse1, pulse2, pulsePeriod);
                        break;
                    case SelectedString.port:
                        portEntry.StopPulsing();
                        selection = SelectedString.userName;
                        selectionChecker.Position = new Vector2(selectionChecker.Position.X,
                                selectionChecker.Position.Y - 2 * oneLineSize);
                        userNameEntry.PulseColors(pulse1, pulse2, pulsePeriod);
                        break;
                }
            }
            else
            {
                switch (selection)
                {
                    case SelectedString.userName:
                        userNameEntry.StopPulsing();
                        selection = SelectedString.port;
                        selectionChecker.Position = new Vector2(selectionChecker.Position.X,
                                selectionChecker.Position.Y + 2 * oneLineSize);
                        portEntry.PulseColors(pulse1, pulse2, pulsePeriod);
                        break;
                    case SelectedString.hostName:
                        hostNameEntry.StopPulsing();
                        selection = SelectedString.userName;
                        selectionChecker.Position = new Vector2(selectionChecker.Position.X,
                                selectionChecker.Position.Y - oneLineSize);
                        userNameEntry.PulseColors(pulse1, pulse2, pulsePeriod);
                        break;
                    case SelectedString.port:
                        portEntry.StopPulsing();
                        selection = SelectedString.hostName;
                        selectionChecker.Position = new Vector2(selectionChecker.Position.X,
                                selectionChecker.Position.Y - oneLineSize);
                        hostNameEntry.PulseColors(pulse1, pulse2, pulsePeriod);
                        break;
                }
            }
        }

        private void CharacterEntered(object sender, CharacterEventArgs e)
        {
            StringBuilder selected;
            switch (selection)
            {
                case SelectedString.userName:
                    selected = userName;
                    break;
                case SelectedString.hostName:
                    selected = hostName;
                    break;
                case SelectedString.port:
                    selected = port;
                    break;
                default:
                    selected = null;
                    break;
            }

            // Add key to text buffer. If not a symbol key. 
            if (!((int)e.Character < 32 || (int)e.Character > 126)) //From space to tilde
            {
                // Capitals are already supported in DLL so we don't have to worry about checking shift
                if (!(Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl)))
                {
                    selected.Append(e.Character);
                }
            }

            // Backspace - remove character if there are any
            if ((int)e.Character == 0x08 && selected.Length > 0)
            {
                selected.Remove(selected.Length - 1, 1);
            }
        }
    }
}
