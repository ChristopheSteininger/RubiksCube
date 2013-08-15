using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace RubiksCube
{
    enum HUDDrawState
    {
        None,
        Minimal,
        Performance,
        All
    }

    class HUD : DrawableGameComponent
    {
        private Cube cube;
        private GraphicsDeviceManager graphics;

        private HUDDrawState drawState = HUDDrawState.Minimal;
        private Color color;

        private SpriteFont largeFont;
        private SpriteFont smallFont;
        private SpriteBatch spriteBatch;

        private Texture2D pixel;
        private Texture2D largePixel;
        
        private int frameRate = 0;
        private int frameCounter = 0;
        private TimeSpan elapsedTime = TimeSpan.Zero;

        private bool isCameraRotating = false;
        private Random random = new Random();

        public HUD(Game game, Cube cube, GraphicsDeviceManager graphics, Color color)
            : base(game)
        {
            this.graphics = graphics;

            this.cube = cube;
            this.color = color;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            largeFont = Game.Content.Load<SpriteFont>("largefont");
            smallFont = Game.Content.Load<SpriteFont>("smallfont");

            pixel = Game.Content.Load<Texture2D>("pixel");
            largePixel = Game.Content.Load<Texture2D>("pixel_larger");

            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            UpdateFPSCounter(gameTime);
            CheckKeyboard();
            UpdateCamera(gameTime);

            base.Update(gameTime);
        }

        private void UpdateFPSCounter(GameTime gameTime)
        {
            elapsedTime += gameTime.ElapsedGameTime;
            if (elapsedTime >= TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                frameRate = frameCounter;
                frameCounter = 0;
            }
        }

        private void CheckKeyboard()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape))
            {
                Game.Exit();
            }

            if (IsKeyPressed(Keys.F))
            {
                graphics.SynchronizeWithVerticalRetrace = !graphics.SynchronizeWithVerticalRetrace;
                Game.IsFixedTimeStep = !Game.IsFixedTimeStep;

                graphics.ApplyChanges();
            }

            if (IsKeyPressed(Keys.M))
            {
                graphics.PreferMultiSampling = !graphics.PreferMultiSampling;

                graphics.ApplyChanges();
            }

            if (IsKeyPressed(Keys.F11))
            {
                graphics.IsFullScreen = !graphics.IsFullScreen;

                graphics.ApplyChanges();
            }

            if (IsKeyPressed(Keys.H))
            {
                drawState = (HUDDrawState)(((int)drawState + 1) % (int)(HUDDrawState.All + 1));
            }

            if (IsKeyPressed(Keys.X))
            {
                cube.Rotate(new Vector3(1, -1, -1), 1);
                cube.Rotate(new Vector3(2, -1, -1), 1);
            }

            if (IsKeyPressed(Keys.Y))
            {
                cube.Rotate(new Vector3(-1, 1, -1), 1);
                cube.Rotate(new Vector3(-1, 2, -1), 1);
            }

            if (IsKeyPressed(Keys.Z))
            {
                cube.Rotate(new Vector3(-1, -1, 1), 1);
                cube.Rotate(new Vector3(-1, -1, 2), 1);
            }

            if (IsKeyPressed(Keys.O))
            {
                cube.RandomRotation(random);
            }

            if (IsKeyPressed(Keys.P))
            {
                for (int i = 0; i < 20; i++)
                {
                    cube.RandomRotation(random);
                }
            }
        }

        private bool IsKeyPressed(Keys key)
        {
            return Keyboard.GetState().IsKeyDown(key) && OldInputStates.KeyboardState.IsKeyUp(key);
        }

        private void UpdateCamera(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            if (keyboardState.IsKeyDown(Keys.Space))
            {
                if (mouseState.LeftButton == ButtonState.Pressed && !isCameraRotating)
                {
                    isCameraRotating = true;
                }

                if (mouseState.LeftButton == ButtonState.Released && isCameraRotating)
                {
                    isCameraRotating = false;
                }

                if (isCameraRotating)
                {
                    float xDifference = mouseState.X - OldInputStates.MouseState.X;
                    float yDifference = mouseState.Y - OldInputStates.MouseState.Y;

                    float rotationAmount = 0.005f;

                    Camera.RotateAroundTarget(new Vector3(
                        xDifference * rotationAmount, yDifference * rotationAmount, 0));
                }
            }

            int scrollValue = mouseState.ScrollWheelValue - OldInputStates.MouseState.ScrollWheelValue;

            Camera.Zoom(0.02f * scrollValue);
        }

        public override void Draw(GameTime gameTime)
        {
            using (new PerformanceStopwatch("mvDrawHUD"))
            {
                frameCounter++;

                spriteBatch.Begin();
                DrawSelectedQuad(gameTime);

                if (drawState >= HUDDrawState.Minimal)
                {
                    DrawCubeText();
                }

                if (drawState >= HUDDrawState.Performance)
                {
                    DrawPerformanceText();
                }

                if (drawState == HUDDrawState.All)
                {
                    DrawAxes();
                    Draw2DCube();
                }
                spriteBatch.End();
            }
            
            base.Draw(gameTime);
        }

        private void DrawSelectedQuad(GameTime gameTime)
        {
            if (cube.SelectedQuad == null)
            {
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                DrawLine(cube.SelectedQuad[i], cube.SelectedQuad[(i + 1) % 4], Color.Violet);
            }

            for (int i = 0; i < cube.ArrowVectors.Length; i++)
            {
                DrawLine(cube.ArrowVectors[i], cube.ArrowVectors[i] + new Vector2(0, 2), Color.Black);
            }
        }

        private void DrawCubeText()
        {
            DrawShadowedText(largeFont, "FPS: " + frameRate.ToString()
                + (graphics.SynchronizeWithVerticalRetrace ? " (Limited to 60)" : " (Unlimited)"),
                new Vector2(10, 10), color);

            DrawShadowedText(largeFont, cube.CubeSize + " Cube: " + AddCommaSeperators(cube.CubeletCount)
                + " cubelets, " + AddCommaSeperators(cube.Vertices) + " vertices in buffer",
                new Vector2(10, 40), color);

            DrawShadowedText(largeFont, "Rotations in left queue: " + cube.CommandsInQueue.ToString(),
                new Vector2(10, 70), color);
        }

        private void DrawPerformanceText()
        {
            DrawShadowedText(smallFont, "Performance:", new Vector2(10, 120), color);

            DrawShadowedText(smallFont, "Load Content: " + Performance.Timers["tmLoadContent"].ToString(),
                new Vector2(10, 150), color);

            DrawShadowedText(smallFont, "Selection: " + Performance.Timers["tmSelection"].ToString(),
                new Vector2(10, 180), color);

            DrawShadowedText(smallFont, "Average Update: " + Performance.Timers["mvUpdate"].ToString(),
                new Vector2(10, 210), color);

            DrawShadowedText(smallFont, "Average Draw: " + Performance.Timers["mvDraw"].ToString(),
                new Vector2(10, 240), color);

            DrawShadowedText(smallFont, "Average Draw Cube: " + Performance.Timers["mvDrawCube"].ToString(),
                new Vector2(30, 270), color);

            DrawShadowedText(smallFont, "Average Draw HUD: " + Performance.Timers["mvDrawHUD"].ToString(),
                new Vector2(30, 300), color);

            DrawShadowedText(smallFont, "Selection Calls: " + Performance.Counters["coSelection"].ToString(),
                new Vector2(10, 330), color);

            DrawShadowedText(smallFont, "Step Calls: " + Performance.Counters["coStep"].ToString(),
                new Vector2(10, 360), color);

            DrawShadowedText(smallFont, "Last Step: " + Performance.Timers["tmStep"].ToString(),
                new Vector2(10, 390), color);
        }

        private void DrawShadowedText(SpriteFont font, String text, Vector2 position, Color foreColor)
        {
            spriteBatch.DrawString(font, text, position + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, text, position, foreColor);
        }

        private void DrawAxes()
        {
            Vector3 origin = GraphicsDevice.Viewport.Project(Vector3.Zero, Camera.ProjectionMatrix,
                Camera.ViewMatrix, Matrix.Identity);
            Vector3 xAxes = GraphicsDevice.Viewport.Project(Vector3.Right, Camera.ProjectionMatrix,
                Camera.ViewMatrix, Matrix.Identity);
            Vector3 yAxes = GraphicsDevice.Viewport.Project(Vector3.Up, Camera.ProjectionMatrix,
                Camera.ViewMatrix, Matrix.Identity);
            Vector3 zAxes = GraphicsDevice.Viewport.Project(Vector3.Forward, Camera.ProjectionMatrix,
                Camera.ViewMatrix, Matrix.Identity);

            float scale = 3.0f;
            Vector2 start = new Vector2(80, 500);

            Vector2 xDirection = new Vector2(origin.X, origin.Y) - new Vector2(xAxes.X, xAxes.Y);
            Vector2 yDirection = new Vector2(origin.X, origin.Y) - new Vector2(yAxes.X, yAxes.Y);
            Vector2 zDirection = new Vector2(origin.X, origin.Y) - new Vector2(zAxes.X, zAxes.Y);

            DrawLine(start, start + xDirection * scale, Color.Blue);
            DrawLine(start, start + yDirection * scale, Color.Green);
            DrawLine(start, start + zDirection * scale, Color.Red);

            DrawShadowedText(largeFont, "1", start + xDirection * scale, Color.Blue);
            DrawShadowedText(largeFont, "2", start + yDirection * scale, Color.Green);
            DrawShadowedText(largeFont, "3", start + zDirection * scale, Color.Red);
        }

        private void Draw2DCube()
        {
            int x = 50;
            int y = 700;
            int size = cube.CubeSize * largePixel.Width + 3;

            DrawGrid(cube.GetFaceColors(new Vector3(-1, -1, 0)), new Vector2(x + size, y + size));      // Front
            DrawGrid(cube.GetFaceColors(new Vector3(2, -1, -1)), new Vector2(x, y + size));             // Left
            DrawGrid(cube.GetFaceColors(new Vector3(0, -1, -1)), new Vector2(x + 2 * size, y + size));  // Right
            DrawGrid(cube.GetFaceColors(new Vector3(-1, 2, -1)), new Vector2(x + size, y));             // Top
            DrawGrid(cube.GetFaceColors(new Vector3(-1, 0, -1)), new Vector2(x + size, y + 2 * size));  // Bottom
            DrawGrid(cube.GetFaceColors(new Vector3(-1, -1, 2)), new Vector2(x + 3 * size, y + size));  // Back
        }

        private void DrawGrid(Color[,] grid, Vector2 position)
        {
            for (int y = 0; y < grid.GetLength(0); y++)
            {
                for (int x = 0; x < grid.GetLength(1); x++)
                {
                    spriteBatch.Draw(largePixel, new Vector2(largePixel.Width * x, largePixel.Height * y)
                        + position, grid[y, x]);
                }
            }
        }

        private void DrawLine(Vector2 a, Vector2 b, Color color)
        {
            float rotation = (float)Math.Atan((b - a).Y / (b - a).X);
            rotation += b.X < a.X ? MathHelper.Pi : 0;

            spriteBatch.Draw(pixel, new Rectangle((int)a.X, (int)a.Y, (int)(b - a).Length(), 2),
                null, color, rotation, Vector2.Zero, SpriteEffects.None, 0);
        }

        private string AddCommaSeperators(int number)
        {
            string sNumber = number.ToString();
            int correction = 0;
            for (int i = sNumber.Length - 3; i > correction; i -= 2)
            {
                sNumber = sNumber.Insert(i - correction, ",");
                correction++;
            }

            return sNumber;
        }
    }
}