using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Connect4GUI
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Connect4 : Microsoft.Xna.Framework.Game
    {
        public const int screenWidth = 384;
        public const int screenHeight = 288;

        // 800x600 is almost always supported
        private const int windowWidth = 800;
        private const int windowHeight = 600;

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        //private GameplayState gameplayManager;

        private IGameState currentState;

        // all other classes render to this target
        private RenderTarget2D screen;

        public Connect4()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = windowWidth;
            graphics.PreferredBackBufferHeight = windowHeight;
            graphics.ApplyChanges();

            currentState = new PreConnectionState(this);
            currentState.Initialize();

            // call other classes' Initialize methods here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            screen = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight);

            // call other classes' LoadContent methods here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // call other classes' UnloadContent methods here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            currentState.Update(gameTime);

            // call other classes' Update methods here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(screen);
            GraphicsDevice.Clear(new Color(0, 50, 0));

            currentState.Draw(gameTime);

            // call other classes' Draw methods here

            GraphicsDevice.SetRenderTarget(null);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null);
            spriteBatch.Draw(screen, GraphicsDevice.Viewport.Bounds, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        public void ChangeState(IGameState state)
        {
            currentState.Uninitialize();
            state.Initialize();
            currentState = state;
        }
    }
}
