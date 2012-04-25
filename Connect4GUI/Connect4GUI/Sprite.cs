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
    public class Sprite
    {
        private Texture2D image;

        private Vector2 position;
        private Vector2 positionToMoveTo;
        private TimeSpan movingTimeLeft;
        private bool isMoving;

        private int timePerChange;
        private Color[] colorsChanging;

        private bool isChanging;
        private double timeSinceLastColorChange;
        private int curColor;

        private bool isFading;
        private double timeSinceFadeStart;
        private double period;
        private double halfPeriod;
        private Color fadeTint;
        private readonly Color originalTint;

        private SpriteEffects effect;
        private float depth;

        private Color tint;

        public Sprite(Texture2D image, Vector2 position, float depth, SpriteEffects _effect, Color _tint)
        {
            this.image = image;
            this.position = position;
            this.depth = depth;
            this.isMoving = false;
            this.effect = _effect;

            isMoving = isFading = isChanging = false;

            originalTint = _tint;
            tint = originalTint;
        }

        public Sprite(Texture2D image, Vector2 position, float depth, SpriteEffects _effect) :
            this(image, position, depth, _effect, Color.White)
        { }

        public Sprite(Texture2D image, Vector2 position, float depth)
            : this(image, position, depth, SpriteEffects.None)
        { }

        public void MoveToInTime(Vector2 position, TimeSpan time)
        {
            if (time.TotalSeconds == 0)
            {
                this.position = position;
                this.isMoving = false;
            }
            else if (time.TotalSeconds > 0)
            {
                this.positionToMoveTo = position;
                this.movingTimeLeft = time;
                this.isMoving = true;
            }
        }

        public void MoveToWithVelocity(Vector2 position, float velocity)
        {
            this.positionToMoveTo = position;
            movingTimeLeft = TimeSpan.FromSeconds((position - this.position).Length() / velocity);
            isMoving = true;
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(image, position, null, tint, 0.0f, Vector2.Zero, 1.0f, effect, depth);
        }

        public void Update(GameTime gameTime)
        {
            if (isMoving)
            {
                float scale;
                if (gameTime.ElapsedGameTime.TotalSeconds > movingTimeLeft.TotalSeconds)
                {
                    scale = 1.0f;
                    isMoving = false;
                }
                else
                {
                    scale = (float)(gameTime.ElapsedGameTime.TotalSeconds / movingTimeLeft.TotalSeconds);
                    movingTimeLeft = movingTimeLeft.Subtract(gameTime.ElapsedGameTime);
                }
                position = new Vector2(MathHelper.Lerp(position.X, positionToMoveTo.X, scale),
                                       MathHelper.Lerp(position.Y, positionToMoveTo.Y, scale));
            }

            if (isChanging)
            {
                timeSinceLastColorChange += gameTime.ElapsedGameTime.TotalMilliseconds;
                if (timeSinceLastColorChange >= timePerChange)
                {
                    curColor++;
                    curColor %= colorsChanging.Length;
                    tint = colorsChanging[curColor];
                    timeSinceLastColorChange = 0;
                }
            }
            else if (isFading)
            {
                timeSinceFadeStart += gameTime.ElapsedGameTime.TotalMilliseconds;
                timeSinceFadeStart %= period;
                if (timeSinceFadeStart <= halfPeriod)
                {
                    float lerp = (float)(timeSinceFadeStart / halfPeriod);
                    tint.R = (byte)MathHelper.Lerp(originalTint.R, fadeTint.R, lerp);
                    tint.G = (byte)MathHelper.Lerp(originalTint.G, fadeTint.G, lerp);
                    tint.B = (byte)MathHelper.Lerp(originalTint.B, fadeTint.B, lerp);
                }
                else // timeSinceFadeStart > halfPeriod
                {
                    float lerp = (float)((timeSinceFadeStart - halfPeriod) / halfPeriod);
                    tint.R = (byte)MathHelper.Lerp(fadeTint.R, originalTint.R, lerp);
                    tint.G = (byte)MathHelper.Lerp(fadeTint.G, originalTint.G, lerp);
                    tint.B = (byte)MathHelper.Lerp(fadeTint.B, originalTint.B, lerp);
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

        public Texture2D Texture
        {
            get
            {
                return image;
            }
            set
            {
                image = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="timePerChange">Time, in milliseconds, for each change</param>
        public void AlternateColors(Color[] colors, int _timePerChange)
        {
            isFading = false;
            isChanging = true;
            colorsChanging = colors;
            timePerChange = _timePerChange;
            timeSinceLastColorChange = 0;
            tint = colorsChanging[0];
            curColor = 0;
        }

        /// <summary>
        /// Note: must be drawn with alpha blending for this to work.
        /// Alpha of passed color is ignored.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="period">amount of time to go from normal color -> target -> normal, in ms</param>
        public void PulseColor(Color target, double period)
        {
            isChanging = false;
            isFading = true;
            this.period = period;
            halfPeriod = this.period / 2.0;
            timeSinceFadeStart = 0;
            fadeTint = target;
        }
    }
}
