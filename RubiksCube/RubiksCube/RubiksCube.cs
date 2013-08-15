//#define USE_SOLVER
#define DEBUGGING

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RubiksCube
{
    public class RubiksCube : Game
    {
        private GraphicsDeviceManager graphics;

        private Cube cube;
        private HUD hud;

#if USE_SOLVER
        private Solver solver;
#endif

        public RubiksCube()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferMultiSampling = true;
            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            graphics.IsFullScreen = true;
            Content.RootDirectory = "Content";

            // Remove the 60 fps limit.
            graphics.SynchronizeWithVerticalRetrace = false;
            this.IsFixedTimeStep = false;

            IsMouseVisible = true;

            cube = new Cube(this, 150);
            Components.Add(cube);

            hud = new HUD(this, cube, graphics, Color.Blue);
            Components.Add(hud);

#if USE_SOLVER
            solver = new Solver(this, cube);
            Components.Add(solver);
#endif

            // Renable the fps limit and windowed mode when debugging.
#if DEBUGGING
            graphics.IsFullScreen = false;

            graphics.SynchronizeWithVerticalRetrace = true;
            this.IsFixedTimeStep = true;
#endif
        }

        protected override void Initialize()
        {
            // Create the camera which can only rotate around the cube and zoom in and out.
            Camera.SetupCamera(GraphicsDevice.Viewport.AspectRatio);

            // Initialize all counters and timers.
            Performance.AddTimer("tmLoadContent", new Timer());
            Performance.AddTimer("tmSelection", new Timer());

            Performance.AddTimer("mvUpdate", new MovingAverage(120));

            Performance.AddTimer("mvDraw", new MovingAverage(120));
            Performance.AddTimer("mvDrawCube", new MovingAverage(120));
            Performance.AddTimer("mvDrawHUD", new MovingAverage(120));

            Performance.AddCounter("coSelection", new Counter());

            Performance.AddCounter("coStep", new Counter());
            Performance.AddTimer("tmStep", new Timer());

            base.Initialize();
        }

        protected override void LoadContent()
        {
            using (new PerformanceStopwatch("tmLoadContent"))
            {
                base.LoadContent();
            }
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            using (new PerformanceStopwatch("mvUpdate"))
            {
                base.Update(gameTime);

                OldInputStates.Update();
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            using (new PerformanceStopwatch("mvDraw"))
            {
                GraphicsDevice.Clear(Color.LightGray);

                base.Draw(gameTime);
            }
        }
    }

    // Stores the state of the keyboard and mouse on the last frame.
    static class OldInputStates
    {
        private static KeyboardState keyboardState;
        public static KeyboardState KeyboardState { get { return keyboardState; } }

        private static MouseState mouseState;
        public static MouseState MouseState { get { return mouseState; } }

        public static void Update()
        {
            keyboardState = Keyboard.GetState();
            mouseState = Mouse.GetState();
        }
    }
}