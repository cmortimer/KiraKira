#region File Description
//-----------------------------------------------------------------------------
// Enemy.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer
{


    /// <summary>
    /// Facing direction along the X axis.
    /// </summary>
    enum FaceDirection
    {
        Left = -1, Right = 1, Idle = 0
    }

    enum MonsterType
    {
        Sushi, Lantern, Sandals
    }

    /// <summary>
    /// A monster who is impeding the progress of our fearless adventurer.
    /// </summary>
    class Enemy
    {
        //Possession variables
        private Player player;      //The player doing the possessing
        public bool possessed;
        public bool isDead;         //After depossession

        public MonsterType monsterType;

        public bool IsNearest
        {
            get { return isNearest; }
            set { isNearest = value; }
        }
        bool isNearest;

        public Level Level
        {
            get { return level; }
        }
        Level level;

        /// <summary>
        /// Position in world space of the bottom center of this enemy.
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
        }
        Vector2 position;

        private Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this enemy in world space.
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        // Animations
        private Animation runAnimation;
        private Animation idleAnimation;
        private Animation evilRunAnimation;
        private Animation evilRunAnimationGlow;
        private Animation evilIdleAnimation;
        private Animation evilIdleAnimationGlow;
        private Animation evilJumpAnimation;
        private AnimationPlayer sprite;
        private AnimationPlayer spriteGlow;

        /// <summary>
        /// The direction this enemy is facing and moving along the X axis.
        /// </summary>
        private FaceDirection direction = FaceDirection.Left;

        /// <summary>
        /// How long this enemy has been waiting before turning around.
        /// </summary>
        private float waitTime;

        /// <summary>
        /// How long to wait before turning around.
        /// </summary>
        private const float MaxWaitTime = 0.5f;

        /// <summary>
        /// The speed at which this enemy moves along the X axis.
        /// </summary>
        private const float MoveSpeed = 64.0f;

        /// <summary>
        /// Constructs a new Enemy.
        /// </summary>
        public Enemy(Level level, Vector2 position, string spriteSet)
        {
            this.level = level;
            this.position = position;
            this.isNearest = false;

            LoadContent(spriteSet);
        }

        /// <summary>
        /// Loads a particular enemy sprite sheet and sounds.
        /// </summary>
        public void LoadContent(string spriteSet)
        {
            // tag what monster it is
            switch (spriteSet)
            {
                case "Lantern":
                    monsterType = MonsterType.Lantern;
                    break;
                case "Sushi":
                    monsterType = MonsterType.Sushi;
                    break;
                case "Sandals":
                    monsterType = MonsterType.Sandals;
                    break;
                default:
                    break;
            }

            // Load animations.
            spriteSet = "Sprites/" + spriteSet + "/";
            runAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Good/Run"), 0.1f, true);
            idleAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Good/Idle"), 0.1f, true);
            evilRunAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Evil/Run"), 0.1f, true);
            evilRunAnimationGlow = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Evil/RunGlow"), 0.1f, true);
            evilIdleAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Evil/Idle"), 0.1f, true);
            evilIdleAnimationGlow = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Evil/IdleGlow"), 0.1f, true);

            if(monsterType == MonsterType.Lantern)
            {
                evilJumpAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Evil/Jump"), 0.1f, true);
            }

            sprite.PlayAnimation(idleAnimation);
            spriteGlow.PlayAnimation(evilIdleAnimationGlow);

            // Calculate bounds within texture size.
            int width = (int)(idleAnimation.FrameWidth * 0.35);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.7);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        /// <summary>
        /// Paces back and forth along a platform, waiting at either end.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            //Change orientation and position to player position
            if (possessed)
            {
                System.Diagnostics.Debug.WriteLine(this.direction);
                if (player.pubMove == 1)
                {
                    this.direction = FaceDirection.Right;
                }
                else if (player.pubMove == -1)
                {
                    this.direction = FaceDirection.Left;
                }
                else
                {
                    this.direction = FaceDirection.Idle;
                }
                this.position = player.Position;
            }
            else
            {
                float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

                // Calculate tile position based on the side we are walking towards.
                float posX = Position.X + localBounds.Width / 2 * (int)direction;
                int tileX = (int)Math.Floor(posX / Tile.Width) - (int)direction;
                int tileY = (int)Math.Floor(Position.Y / Tile.Height);

                if (waitTime > 0)
                {
                    // Wait for some amount of time.
                    waitTime = Math.Max(0.0f, waitTime - (float)gameTime.ElapsedGameTime.TotalSeconds);
                    if (waitTime <= 0.0f)
                    {
                        // Then turn around.
                        direction = (FaceDirection)(-(int)direction);
                    }
                }
                else
                {
                    // If we are about to run into a wall or off a cliff, start waiting.
                    if (Level.GetCollision(tileX + (int)direction, tileY - 1) == TileCollision.Impassable ||
                        Level.GetCollision(tileX + (int)direction, tileY) == TileCollision.Passable)
                    {
                        waitTime = MaxWaitTime;
                    }
                    else
                    {
                        // Move in the current direction.
                        Vector2 velocity = new Vector2((int)direction * MoveSpeed * elapsed, 0.0f);
                        position = position + velocity;
                    }
                }
            }
        }

        /// <summary>
        /// Draws the animated enemy.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Stop running when the game is paused, before turning around, or if the possessing player is idle.
            if (!Level.Player.IsAlive ||
                Level.ReachedExit ||
                Level.TimeRemaining == TimeSpan.Zero ||
                waitTime > 0 || this.direction == FaceDirection.Idle)
            {
                if (isNearest || possessed)
                {
                    sprite.PlayAnimation(evilIdleAnimation);
                    spriteGlow.PlayAnimation(evilIdleAnimationGlow);
                }
                else
                {
                    sprite.PlayAnimation(idleAnimation);
                }
            }
            else
            {
                if (isNearest || possessed)
                {
                    sprite.PlayAnimation(evilRunAnimation);
                    spriteGlow.PlayAnimation(evilRunAnimationGlow);
                }
                else
                {
                    sprite.PlayAnimation(runAnimation);
                }
            }

            if (!player.IsOnGround && monsterType == MonsterType.Lantern)
            {
                sprite.PlayAnimation(evilJumpAnimation);
            }

            // Draw facing the way the enemy is moving.
            SpriteEffects flip = direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            if (isNearest)
            {
                spriteGlow.Draw(gameTime, spriteBatch, Position, flip);
                sprite.Draw(gameTime, spriteBatch, Position, flip);
            }
            else
            {
                
                sprite.Draw(gameTime, spriteBatch, Position, flip);
            }
        }

        public void setPlayer(Player p)
        {
            this.player = p;
        }
    }
}
