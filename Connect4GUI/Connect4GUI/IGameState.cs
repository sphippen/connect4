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
    public interface IGameState
    {
        void Initialize();

        void Uninitialize();

        void Draw(GameTime gameTime);

        void Update(GameTime gameTime);
    }
}
