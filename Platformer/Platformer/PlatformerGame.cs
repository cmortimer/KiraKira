#region File Description
//-----------------------------------------------------------------------------
// PlatformerGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input.Touch;


namespace Platformer
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class PlatformerGame : Microsoft.Xna.Framework.Game
    {
        // Resources for drawing.
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Global content.
        private SpriteFont hudFont;

        private Texture2D winOverlay;
        private Texture2D loseOverlay;
        private Texture2D diedOverlay;
        private Texture2D mainMenuScreen;

        // Meta-level game state.
        private int levelIndex = -1;
        private Level level;
        private bool wasContinuePressed;

        // When the time remaining is less than the warning time, it blinks on the hud
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(30);

        // We store our input states so that we only poll once per frame, 
        // then we use the same input state wherever needed
        private GamePadState gamePadState;
        private KeyboardState keyboardState;
        private TouchCollection touchState;
        private AccelerometerState accelerometerState;
        
        // The number of levels in the Levels directory of our content. We assume that
        // levels in our content are 0-based and that all numbers under this constant
        // have a level file present. This allows us to not need to check for the file
        // or handle exceptions, both of which can add unnecessary time to level loading.
        private const int numberOfLevels = 3;


        //GameStates
        private enum Gamestate
        {
            Menu, Game, Pause
        }

        private Gamestate state = Gamestate.Menu;

        public PlatformerGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1600;
            graphics.PreferredBackBufferHeight = 960;
            Content.RootDirectory = "Content";

#if WINDOWS_PHONE
            graphics.IsFullScreen = true;
            TargetElapsedTime = TimeSpan.FromTicks(333333);
#endif

            Accelerometer.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load fonts
            hudFont = Content.Load<SpriteFont>("Fonts/Hud");

            // Load overlay textures
            winOverlay = Content.Load<Texture2D>("Overlays/you_win");
            loseOverlay = Content.Load<Texture2D>("Overlays/you_lose");
            diedOverlay = Content.Load<Texture2D>("Overlays/you_died");
            mainMenuScreen = Content.Load<Texture2D>("Backgrounds/TitleScreen");

            //Known issue that you get exceptions if you use Media PLayer while connected to your PC
            //See http://social.msdn.microsoft.com/Forums/en/windowsphone7series/thread/c8a243d2-d360-46b1-96bd-62b1ef268c66
            //Which means its impossible to test this from VS.
            //So we have to catch the exception and throw it away
            try
            {
                MediaPlayer.IsRepeating = true;
                //MediaPlayer.Play(Content.Load<Song>("Sounds/Music"));
            }
            catch { }

            LoadNextLevel();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Handle polling for our input and handling high-level input
            HandleInput();

            if (state == Gamestate.Menu)
            {

            }
            else if (state == Gamestate.Game)
            {
                // update our level, passing down the GameTime along with all of our input states
                level.Update(gameTime, keyboardState, gamePadState, touchState,
                            accelerometerState, Window.CurrentOrientation);
            }

            base.Update(gameTime);
        }

        private void HandleInput()
        {
            // get all of our input states
            keyboardState = Keyboard.GetState();
            gamePadState = GamePad.GetState(PlayerIndex.One);
            touchState = TouchPanel.GetState();
            accelerometerState = Accelerometer.GetState();

            //Pause game when p is pressed
            if (keyboardState.IsKeyDown(Keys.P) && state == Gamestate.Game)
                state = Gamestate.Pause;

            //Reset Level if paused and r is pressed
            if (keyboardState.IsKeyDown(Keys.R) && state == Gamestate.Pause)
            {
                ReloadCurrentLevel();
                state = Gamestate.Game;
            }

            //Resume Game upon Enter
            if (keyboardState.IsKeyDown(Keys.Space) && state != Gamestate.Game)
                state = Gamestate.Game;

            // Exit the game when back is pressed.
            if (gamePadState.Buttons.Back == ButtonState.Pressed)
                Exit();

            bool continuePressed =
                keyboardState.IsKeyDown(Keys.Space) ||
                gamePadState.IsButtonDown(Buttons.A) ||
                touchState.AnyTouch();

            // Perform the appropriate action to advance the game and
            // to get the player back to playing.
            if (!wasContinuePressed && continuePressed)
            {
                if (!level.Player.IsAlive)
                {
                    level.StartNewLife();
                    ReloadCurrentLevel();
                }
                else if (level.TimeRemaining == TimeSpan.Zero)
                {
                    if (level.ReachedExit)
                        LoadNextLevel();
                    else
                        ReloadCurrentLevel();
                }
            }

            wasContinuePressed = continuePressed;
        }

        private void LoadNextLevel()
        {
            // move to the next level
            levelIndex = (levelIndex + 1) % numberOfLevels;

            // Unloads the content for the current level before loading the next one.
            if (level != null)
                level.Dispose();

            // Load the level.
            string levelPath = string.Format("Content/Levels/{0}.txt", levelIndex);
            using (Stream fileStream = TitleContainer.OpenStream(levelPath))
                level = new Level(Services, fileStream, levelIndex);
        }

        private void ReloadCurrentLevel()
        {
            --levelIndex;
            LoadNextLevel();
        }

        /// <summary>
        /// Draws the game from background to foreground.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            
            //For wireframe

            //spriteBatch.Begin(SpriteSortMode.Immediate,BlendState.Opaque);
            //RasterizerState state = new RasterizerState();
            //state.FillMode = FillMode.Solid;
            //spriteBatch.GraphicsDevice.RasterizerState = state;
            if (state == Gamestate.Menu)
            {
                level.DrawBackground(gameTime, spriteBatch);
                spriteBatch.Draw(mainMenuScreen, Vector2.Zero, Color.White);
            }
            else if (state == Gamestate.Game)
            {
                level.Draw(gameTime, spriteBatch);
                if (levelIndex == 0)
                {
                   // DrawShadowedString(hudFont, "Move and jump with WASD", new Vector2(10, Window.ClientBounds.Height / 4 * 3), Color.Yellow);
                   // DrawShadowedString(hudFont, "Possess Enemies with Space", new Vector2(400, Window.ClientBounds.Height / 4 * 3), Color.Yellow);
                   // DrawShadowedString(hudFont, "Depossess with V", new Vector2(750, Window.ClientBounds.Height / 4 * 3), Color.Yellow);
                   // DrawShadowedString(hudFont, "Pause the game with P", new Vector2(1000, Window.ClientBounds.Height / 4 * 3), Color.Yellow);
                   // DrawShadowedString(hudFont, "Get Here!", new Vector2(1450, Window.ClientBounds.Height / 4 * 3), Color.Yellow);
                }
                DrawHud();
            }

            else if (state == Gamestate.Pause)
            {
                level.Draw(gameTime, spriteBatch);
                DrawHud();
                Vector2 pausedSize = hudFont.MeasureString("Paused");
                Vector2 center = new Vector2(Window.ClientBounds.Width / 2 - pausedSize.X / 2, Window.ClientBounds.Height / 2 - pausedSize.Y / 2);
                DrawShadowedString(hudFont, "Paused", center, Color.Yellow);
                Vector2 instructSize = hudFont.MeasureString("Press Enter to continue");
                Vector2 unpause = new Vector2(Window.ClientBounds.Width / 2 - instructSize.X / 2, Window.ClientBounds.Height / 2 - instructSize.Y / 2 + 30);
                DrawShadowedString(hudFont, "Press Enter to continue", unpause, Color.Yellow);
                Vector2 resetSize = hudFont.MeasureString("Press R to reset the level");
                Vector2 resetPos = new Vector2(Window.ClientBounds.Width / 2 - instructSize.X / 2, Window.ClientBounds.Height / 2 - instructSize.Y / 2 + 60);
                DrawShadowedString(hudFont, "Press R to reset the level", resetPos, Color.Yellow);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawHud()
        {
            Rectangle titleSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            // Draw time remaining. Uses modulo division to cause blinking when the
            // player is running out of time.
            string timeString = "TIME: " + level.TimeRemaining.Minutes.ToString("00") + ":" + level.TimeRemaining.Seconds.ToString("00");
            Color timeColor;
            if (level.TimeRemaining > WarningTime ||
                level.ReachedExit ||
                (int)level.TimeRemaining.TotalSeconds % 2 == 0)
            {
                timeColor = Color.Yellow;
            }
            else
            {
                timeColor = Color.Red;
            }
            DrawShadowedString(hudFont, timeString, hudLocation, timeColor);

            // Draw score
            float timeHeight = hudFont.MeasureString(timeString).Y;
            DrawShadowedString(hudFont, "Score: " + level.Score.ToString(), hudLocation + new Vector2(0.0f, timeHeight * 1.2f), Color.Yellow);
           
            // Determine the status overlay message to show.
            Texture2D status = null;
            if (level.TimeRemaining == TimeSpan.Zero)
            {
                if (level.ReachedExit)
                {
                    status = winOverlay;
                }
                else
                {
                    status = loseOverlay;
                }
            }
            else if (!level.Player.IsAlive)
            {
                status = diedOverlay;
            }

            if (status != null)
            {
                // Draw status message.
                Vector2 statusSize = new Vector2(status.Width, status.Height);
                spriteBatch.Draw(status, center - statusSize / 2, Color.White);
            }
        }

        private void DrawShadowedString(SpriteFont font, string value, Vector2 position, Color color)
        {
            spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
            spriteBatch.DrawString(font, value, position, color);
        }
    }
}
