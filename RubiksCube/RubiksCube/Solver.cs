using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace RubiksCube
{
    class Solver : GameComponent
    {
        private Cube cube;

        private bool isSolved = true;

        private Color[,] whiteFace;
        private Color[,] yellowFace;
        private Color[,] redFace;
        private Color[,] orangeFace;
        private Color[,] blueFace;
        private Color[,] greenFace;

        public Solver(Game game, Cube cube)
            : base(game)
        {
            this.cube = cube;
        }

        public override void Update(GameTime gameTime)
        {
            if (cube.CubeSize == 3 && Keyboard.GetState().IsKeyDown(Keys.S)
                && OldInputStates.KeyboardState.IsKeyUp(Keys.S))
            {
                cube.RotationFinished += Step;

                Step();
            }
            
            base.Update(gameTime);
        }

        private void Step()
        {
            Performance.Counters["coStep"].Increment();

            using (new PerformanceStopwatch("tmStep"))
            {
                UpdateFaceColors();

                if (isSolved)
                {
                    cube.RotationFinished -= Step;

                    return;
                }

                WhiteCross();
            }
        }

        private void UpdateFaceColors()
        {
            Vector3[] faces = new Vector3[] { new Vector3(-1, 2, -1), new Vector3(-1, 0, -1),
                new Vector3(-1, -1, 0), new Vector3(-1, -1, 2), new Vector3(0, -1, -1), new Vector3(2, -1, -1)};

            isSolved = true;
            for (int i = 0; i < 6; i++)
            {
                Color[,] faceColors = cube.GetFaceColors(faces[i]);

                for (int y = 0; isSolved && y < faceColors.GetLength(0); y++)
                {
                    for (int x = 0; isSolved && x < faceColors.GetLength(1); x++)
                    {
                        if (x != 0 || y != 0)
                        {
                            isSolved = faceColors[x, y] == faceColors[0, 0];
                        }
                    }
                }

                if (faceColors[1, 1] == Color.White)
                {
                    whiteFace = faceColors;
                }
                if (faceColors[1, 1] == Color.Yellow)
                {
                    yellowFace = faceColors;
                }
                if (faceColors[1, 1] == Color.Red)
                {
                    redFace = faceColors;
                }
                if (faceColors[1, 1] == Color.Orange)
                {
                    orangeFace = faceColors;
                }
                if (faceColors[1, 1] == Color.Blue)
                {
                    blueFace = faceColors;
                }
                if (faceColors[1, 1] == Color.Green)
                {
                    greenFace = faceColors;
                }
            }
        }

        private void WhiteCross()
        {
            Cubelet redWhite = GetCubelet(Color.Red, Color.White);
            if (redWhite.CubePosition.Z == 0 && redWhite.CubePosition.Y == 2)
            {
                return;
            }

            if (redWhite.CubePosition.Z == 0)
            {
                int rotation = (int)redWhite.CubePosition.X - 1;
                cube.Rotate(new Vector3(-1, -1, 0), rotation == 0 ? 2 : rotation);

                return;
            }
        }

        private Cubelet GetCubelet(params Color[] colors)
        {
            for (int i = 0; i < cube.Cubelets.Length; i++)
            {
                bool colorInArray = cube.Cubelets[i].ColorVertices.Length == colors.Length;
                for (int j = 0; colorInArray && j < cube.Cubelets[i].ColorVertices.Length; j++)
                {
                    colorInArray = false;
                    for (int k = 0; !colorInArray && k < colors.Length; k++)
                    {
                        colorInArray = cube.Cubelets[i].ColorVertices[j].Color == colors[k];
                    }
                }

                if (colorInArray)
                {
                    return cube.Cubelets[i];
                }
            }

            return null;
        }
    }
}