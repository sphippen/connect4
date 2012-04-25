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
    public class TextSprite
    {
        private string message;
        private SpriteFont font;

        private Vector2 position;
        private Vector2 positionToMoveTo;
        private TimeSpan timeLeft;
        private bool moving;
        private Color tint;
        private float depth;

        private bool isFading;
        private double timeSinceFadeStart;
        private double period;
        private double halfPeriod;
        private Color fadeTint1;
        private Color fadeTint2;
        private readonly Color originalTint;

        public TextSprite(string message, SpriteFont font, Vector2 position, float depth, Color tint)
        {
            this.message = message;
            this.font = font;
            this.position = position;
            this.depth = depth;
            this.moving = false;
            originalTint = tint;
            this.tint = tint;

            isFading = moving = false;
        }

        public TextSprite(string message, SpriteFont font, Vector2 position, float depth) : this(message, font, position, depth, Color.White)
        { }

        public void MoveToInTime(Vector2 position, TimeSpan time)
        {
            if (time.TotalSeconds == 0)
            {
                this.position = position;
                this.moving = false;
            }
            else if (time.TotalSeconds > 0)
            {
                this.positionToMoveTo = position;
                this.timeLeft = time;
                this.moving = true;
            }
        }

        public void MoveToWithVelocity(Vector2 position, float velocity)
        {
            this.positionToMoveTo = position;
            timeLeft = TimeSpan.FromSeconds((position - this.position).Length() / velocity);
            moving = true;
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(font, message, position, tint, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, depth);
        }

        public void Update(GameTime gameTime)
        {
            if (moving)
            {
                float scale;
                if (gameTime.ElapsedGameTime.TotalSeconds > timeLeft.TotalSeconds)
                {
                    scale = 1.0f;
                    moving = false;
                }
                else
                {
                    scale = (float)(gameTime.ElapsedGameTime.TotalSeconds / timeLeft.TotalSeconds);
                    timeLeft = timeLeft.Subtract(gameTime.ElapsedGameTime);
                }
                position = new Vector2(MathHelper.Lerp(position.X, positionToMoveTo.X, scale),
                                       MathHelper.Lerp(position.Y, positionToMoveTo.Y, scale));
            }

            if (isFading)
            {
                timeSinceFadeStart += gameTime.ElapsedGameTime.TotalMilliseconds;
                timeSinceFadeStart %= period;
                if (timeSinceFadeStart <= halfPeriod)
                {
                    float lerp = (float)(timeSinceFadeStart / halfPeriod);
                    tint.R = (byte)MathHelper.Lerp(fadeTint1.R, fadeTint2.R, lerp);
                    tint.G = (byte)MathHelper.Lerp(fadeTint1.G, fadeTint2.G, lerp);
                    tint.B = (byte)MathHelper.Lerp(fadeTint1.B, fadeTint2.B, lerp);
                }
                else // timeSinceFadeStart > halfPeriod
                {
                    float lerp = (float)((timeSinceFadeStart - halfPeriod) / halfPeriod);
                    tint.R = (byte)MathHelper.Lerp(fadeTint2.R, fadeTint1.R, lerp);
                    tint.G = (byte)MathHelper.Lerp(fadeTint2.G, fadeTint1.G, lerp);
                    tint.B = (byte)MathHelper.Lerp(fadeTint2.B, fadeTint1.B, lerp);
                }
            }


        }

        public Vector2 Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        public string Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
            }
        }

        /// <summary>
        /// Note: must be drawn with alpha blending for this to work.
        /// Alpha of passed color is ignored.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="period">amount of time to go from normal color -> target -> normal, in ms</param>
        public void PulseColors(Color target1, Color target2, double period)
        {
            isFading = true;
            this.period = period;
            halfPeriod = this.period / 2.0;
            timeSinceFadeStart = 0;
            fadeTint1 = target1;
            fadeTint2 = target2;
        }

        public void StopPulsing()
        {
            isFading = false;
            tint = originalTint;
        }
    }
}
