using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RubiksCube
{
    partial class Cube : DrawableGameComponent
    {
        private Cubelet[] cubelets;
        public Cubelet[] Cubelets { get { return cubelets; } }
        public Cubelet SelectedCubelet { get { return cubelets[selectedCubelet]; } }

        private readonly int cubeSize;
        private const float scale = 1.7f;
        private const float offset = 0.05f;

        public int CubeSize { get { return cubeSize; } }
        public int CubeletCount { get { return cubelets.Length; } }
        public int Vertices { get { return vertexBuffer.VertexCount; } }

        private CommandQueueItem firstItem;
        private CommandQueueItem lastItem;

        private int commandsInQueue = 0;
        public int CommandsInQueue { get { return commandsInQueue; } }

        private int[] vertexStarts;
        private int[] indexStarts;

        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private RasterizerState state;
        private BasicEffect effect;

        private Vector3 rightFace = new Vector3(-1, 0, -1);
        private Vector3 bottomFace = new Vector3(-1, -1, 0);
        private Vector3 frontFace = new Vector3(0, -1, -1);

        private int selectedCubelet = -1;
        private Vector3[] cornerVertices;
        private Vector2[] selectedQuad;
        public Vector2[] SelectedQuad { get { return selectedQuad; } }

        private Vector2 mouseDrapStartPostion;
        private Vector2[] sliceRotationDirection;

        // REMOVE
        private Vector2[] arrowVectors;
        public Vector2[] ArrowVectors { get { return arrowVectors; } }

        private bool didCubeletsRotateLastFrame = false;
        public delegate void FinishedRotatingEvent();
        private FinishedRotatingEvent rotationFinished;
        public FinishedRotatingEvent RotationFinished
        {
            get { return rotationFinished; }
            set { rotationFinished = value; }
        }

        public Cube(Game game, int cubeSize)
            : base(game)
        {
            this.cubeSize = cubeSize;

            firstItem = new CommandQueueItem(new Vector3(-1), 0);
            lastItem = new CommandQueueItem(new Vector3(-1), 0);
            lastItem.LinkTo(firstItem);
        }

        protected override void LoadContent()
        {
            SetupCornerVertices();
            SetupCublelets();
            SetupBuffers();

            Camera.ViewMatrixUpdated += UpdateViewMatrices;

            state = new RasterizerState();
            state.CullMode = CullMode.CullClockwiseFace;
            GraphicsDevice.RasterizerState = state;
            
            base.LoadContent();
        }

        private void SetupCornerVertices()
        {
            cornerVertices = new Vector3[] {
                // Front
                new Vector3(0, 0, 0), new Vector3(scale, 0, 0),
                new Vector3(scale, scale, 0), new Vector3(0, scale, 0),
                // Back
                new Vector3(0, 0, scale), new Vector3(0, scale, scale),
                new Vector3(scale, scale, scale), new Vector3(scale, 0, scale),
                // Right
                new Vector3(0, 0, 0), new Vector3(0, scale, 0),
                new Vector3(0, scale, scale), new Vector3(0, 0, scale),
                // Left
                new Vector3(scale, 0, 0), new Vector3(scale, 0, scale),
                new Vector3(scale, scale, scale), new Vector3(scale, scale, 0),
                // Bottom
                new Vector3(0, 0, 0), new Vector3(0, 0, scale),
                new Vector3(scale, 0, scale), new Vector3(scale, 0, 0),
                // Top
                new Vector3(0, scale, 0), new Vector3(scale, scale, 0),
                new Vector3(scale, scale, scale), new Vector3(0, scale, scale)
            };
        }

        private void SetupCublelets()
        {
            // The number of cubelets is volume of a solid cube minus the volume of the inside
            // cubelets.
            cubelets = new Cubelet[(int)Math.Pow(cubeSize, 3) - (int)Math.Pow(cubeSize - 2, 3)];

            // Calculate the index of the center cubelet.
            Vector3 cubeCenter = new Vector3((cubeSize - 1) / 2.0f);

            // Calculate the actual center of the Rubiks cube.
            Vector3 center = new Vector3(((scale + offset) * cubeSize - offset) / 2.0f);

            // Point the camera at the actual center.
            Camera.UpdateLookAt(center);

            // Loop through the entire solid cube and create the cubelets at the edges.
            int index = 0;

            // Loop through each z slice.
            for (int z = 0; z < cubeSize; z++)
            {
                bool isFront = z == 0;
                bool isBack = z == cubeSize - 1;

                // Loop through each row in the current slice.
                for (int y = 0; y < cubeSize; y++)
                {
                    bool isBottom = y == 0;
                    bool isTop = y == cubeSize - 1;

                    // Loop throuhg each column in the current row.
                    for (int x = 0; x < cubeSize; x++)
                    {
                        bool isLeft = x == cubeSize - 1;
                        bool isRight = x == 0;

                        // If the current position is on the edge, create a new cubelet there.
                        if (isLeft || isRight || isBottom || isTop || isFront || isBack)
                        {
                            // Calculate the cubelet's orientation.
                            Orientation startingOrientation = 0;
                            startingOrientation |= isFront ? Orientation.Front : 0;
                            startingOrientation |= isBack ? Orientation.Back : 0;
                            startingOrientation |= isRight ? Orientation.Right : 0;
                            startingOrientation |= isLeft ? Orientation.Left : 0;
                            startingOrientation |= isBottom ? Orientation.Bottom : 0;
                            startingOrientation |= isTop ? Orientation.Top : 0;

                            // Create the cubelet.
                            cubelets[index++] = new Cubelet(
                                new Vector3(x, y, z), center, cubeCenter, scale,
                                offset, startingOrientation);
                        }
                    }
                }
            }
        }

        private void SetupBuffers()
        {
            effect = new BasicEffect(GraphicsDevice);
            effect.Projection = Camera.ProjectionMatrix;
            effect.View = Camera.ViewMatrix;
            effect.VertexColorEnabled = true;

            Color[] faceColors = new Color[] { Color.Green, Color.Blue, Color.Yellow,
                Color.White, Color.Red, Color.DarkOrange };
            Color unusedColor = Color.Black;

            vertexStarts = new int[26];
            indexStarts = new int[26];

            int vertexStart = 0;
            VertexPositionColor[] vertices = new VertexPositionColor[26 * 6 * 4];

            int indexStart = 0;
            short[] indices = new short[26 * 6 * 6];

            for (int i = 0; i < 26; i++)
            {
                bool cubeletFound = false;
                for (int j = 0; !cubeletFound && j < cubelets.Length; j++)
                {
                    if (cubelets[j].FaceCode == i)
                    {
                        vertexStarts[i] = vertexStart;
                        indexStarts[i] = indexStart;

                        CreateCubeletVertices(vertices, ref vertexStart, indices, ref indexStart,
                            cubelets[j].StartingOrientation, faceColors);

                        cubeletFound = true;
                    }
                }
            }

            vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor),
                vertices.Length, BufferUsage.None);
            vertexBuffer.SetData(vertices);

            indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits,
                indices.Length, BufferUsage.None);
            indexBuffer.SetData(indices);
        }

        public void CreateCubeletVertices(VertexPositionColor[] vertices, ref int vertexStart, short[] indices,
            ref int indexStart, Orientation visibleFaces, Color[] faceColors)
        {
            Vector3 origin = new Vector3(0, 0, 0) * scale;
            Vector3 up = new Vector3(0, 1, 0) * scale;
            Vector3 leftUp = new Vector3(1, 1, 0) * scale;
            Vector3 left = new Vector3(1, 0, 0) * scale;
            Vector3 back = new Vector3(0, 0, 1) * scale;
            Vector3 upBack = new Vector3(0, 1, 1) * scale;
            Vector3 leftUpBack = new Vector3(1, 1, 1) * scale;
            Vector3 leftBack = new Vector3(1, 0, 1) * scale;

            // Left Face.
            Color faceColor = (visibleFaces & Orientation.Left) == Orientation.Left ? faceColors[0] : Color.Black;

            vertices[vertexStart++] = new VertexPositionColor(leftUp, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(leftBack, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(left, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(leftUpBack, faceColor);

            indices[indexStart++] = (short)(vertexStart - 4);
            indices[indexStart++] = (short)(vertexStart - 3);
            indices[indexStart++] = (short)(vertexStart - 2);

            indices[indexStart++] = (short)(vertexStart - 4);
            indices[indexStart++] = (short)(vertexStart - 1);
            indices[indexStart++] = (short)(vertexStart - 3);

            // Right Face.
            faceColor = (visibleFaces & Orientation.Right) == Orientation.Right ? faceColors[1] : Color.Black;

            vertices[vertexStart++] = new VertexPositionColor(back, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(upBack, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(up, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(origin, faceColor);

            indices[indexStart++] = (short)(vertexStart - 4);
            indices[indexStart++] = (short)(vertexStart - 3);
            indices[indexStart++] = (short)(vertexStart - 2);

            indices[indexStart++] = (short)(vertexStart - 2);
            indices[indexStart++] = (short)(vertexStart - 1);
            indices[indexStart++] = (short)(vertexStart - 4);

            // Bottom Face.
            faceColor = (visibleFaces & Orientation.Bottom) == Orientation.Bottom ? faceColors[2] : Color.Black;

            vertices[vertexStart++] = new VertexPositionColor(origin, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(left, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(back, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(leftBack, faceColor);

            indices[indexStart++] = (short)(vertexStart - 4);
            indices[indexStart++] = (short)(vertexStart - 3);
            indices[indexStart++] = (short)(vertexStart - 2);

            indices[indexStart++] = (short)(vertexStart - 3);
            indices[indexStart++] = (short)(vertexStart - 1);
            indices[indexStart++] = (short)(vertexStart - 2);

            // Top Face.
            faceColor = (visibleFaces & Orientation.Top) == Orientation.Top ? faceColors[3] : Color.Black;

            vertices[vertexStart++] = new VertexPositionColor(upBack, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(leftUp, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(up, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(leftUpBack, faceColor);

            indices[indexStart++] = (short)(vertexStart - 4);
            indices[indexStart++] = (short)(vertexStart - 3);
            indices[indexStart++] = (short)(vertexStart - 2);

            indices[indexStart++] = (short)(vertexStart - 4);
            indices[indexStart++] = (short)(vertexStart - 1);
            indices[indexStart++] = (short)(vertexStart - 3);

            // Front Face.
            faceColor = (visibleFaces & Orientation.Front) == Orientation.Front ? faceColors[4] : Color.Black;

            vertices[vertexStart++] = new VertexPositionColor(origin, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(up, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(left, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(leftUp, faceColor);

            indices[indexStart++] = (short)(vertexStart - 4);
            indices[indexStart++] = (short)(vertexStart - 3);
            indices[indexStart++] = (short)(vertexStart - 2);

            indices[indexStart++] = (short)(vertexStart - 3);
            indices[indexStart++] = (short)(vertexStart - 1);
            indices[indexStart++] = (short)(vertexStart - 2);

            // Back Face.
            faceColor = (visibleFaces & Orientation.Back) == Orientation.Back ? faceColors[5] : Color.Black;

            vertices[vertexStart++] = new VertexPositionColor(leftUpBack, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(back, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(leftBack, faceColor);
            vertices[vertexStart++] = new VertexPositionColor(upBack, faceColor);

            indices[indexStart++] = (short)(vertexStart - 4);
            indices[indexStart++] = (short)(vertexStart - 3);
            indices[indexStart++] = (short)(vertexStart - 2);

            indices[indexStart++] = (short)(vertexStart - 4);
            indices[indexStart++] = (short)(vertexStart - 1);
            indices[indexStart++] = (short)(vertexStart - 3);
        }

        public override void Update(GameTime gameTime)
        {
            if (Mouse.GetState().LeftButton == ButtonState.Pressed
                && OldInputStates.MouseState.LeftButton == ButtonState.Released
                && Keyboard.GetState().IsKeyUp(Keys.Space))
            {
                CheckSelection();
            }

            if ((Mouse.GetState().LeftButton == ButtonState.Released
                && OldInputStates.MouseState.LeftButton == ButtonState.Pressed)
                || Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                selectedCubelet = -1;
                selectedQuad = null;

                Vector2 mouseDragDirection = new Vector2(Mouse.GetState().X, Mouse.GetState().Y)
                    - mouseDrapStartPostion;
            }
            
            CheckKeyboard();
            ExecuteRotation();

            bool isRotating = false;
            for (int i = 0; i < cubelets.Length; i++)
            {
                cubelets[i].Update(gameTime.ElapsedGameTime);
                isRotating = isRotating || cubelets[i].IsRotating;
            }

            if (didCubeletsRotateLastFrame && !isRotating && rotationFinished != null && commandsInQueue == 0)
            {
                rotationFinished();
            }

            base.Update(gameTime);

            didCubeletsRotateLastFrame = isRotating;
        }

        public void Rotate(Vector3 face, int rotationMultiple)
        {
            lastItem.NextItem.LinkTo(new CommandQueueItem(face, rotationMultiple));
            lastItem.LinkTo(lastItem.NextItem.NextItem);

            commandsInQueue++;
        }

        public void RandomRotation(Random random)
        {
            int face = random.Next(3);
            int slice = random.Next(cubeSize);
            int rotationMultiple = random.Next(3);
            rotationMultiple = rotationMultiple == 0 ? -1 : rotationMultiple;

            Rotate(new Vector3(
                face == 0 ? slice : -1,
                face == 1 ? slice : -1,
                face == 2 ? slice : -1), rotationMultiple);
        }

        private void CheckKeyboard()
        {
            if (selectedCubelet != -1)
            {
                KeyboardState keyboardState = Keyboard.GetState();
                if (keyboardState.IsKeyDown(Keys.D1))
                {
                    Rotate(new Vector3(cubelets[selectedCubelet].CubePosition.X, -1, -1), 1);
                }

                if (keyboardState.IsKeyDown(Keys.D2))
                {
                    Rotate(new Vector3(-1, cubelets[selectedCubelet].CubePosition.Y, -1), 1);
                }

                if (keyboardState.IsKeyDown(Keys.D3))
                {
                    Rotate(new Vector3(-1, -1, cubelets[selectedCubelet].CubePosition.Z), 1);
                }
            }
        }

        private void ExecuteRotation()
        {
            if (firstItem.NextItem != null)
            {
                Vector3 slice = firstItem.NextItem.Slice;
                int rotationMultiple = firstItem.NextItem.RotationMultiple;

                // Modify the slice by the orientation of the cube.
                Vector3 faceModifier = new Vector3(-1, -1, -1);
                float sliceModifier = 0;

                if (slice.X != -1)
                {
                    faceModifier = rightFace;
                    sliceModifier = slice.X;
                }

                if (slice.Y != -1)
                {
                    faceModifier = bottomFace;
                    sliceModifier = slice.Y;
                }

                if (slice.Z != -1)
                {
                    faceModifier = frontFace;
                    sliceModifier = slice.Z;
                }

                if (faceModifier.X != -1)
                {
                    slice = new Vector3(Math.Abs(faceModifier.X - sliceModifier), -1, -1);
                }

                if (faceModifier.Y != -1)
                {
                    slice = new Vector3(-1, Math.Abs(faceModifier.Y - sliceModifier), -1);
                }

                if (faceModifier.Z != -1)
                {
                    slice = new Vector3(-1, -1, Math.Abs(faceModifier.Z - sliceModifier));
                }

                if (NonNegativeDimension(slice) != 0)
                {
                    rotationMultiple *= -1;
                }

                // Check if any cubelets in the slice are rotating.
                bool isFaceRotating = false;
                for (int i = 0; i < cubelets.Length && !isFaceRotating; i++)
                {
                    if (!isFaceRotating && (slice.X == cubelets[i].CubePosition.X
                        || slice.Y == cubelets[i].CubePosition.Y || slice.Z == cubelets[i].CubePosition.Z))
                    {
                        isFaceRotating = cubelets[i].IsRotating;
                    }
                }

                // If no cubelets in the slice are rotating, then execute the current command.
                if (!isFaceRotating)
                {
                    for (int i = 0; i < cubelets.Length; i++)
                    {
                        if (slice.X == cubelets[i].CubePosition.X)
                        {
                            cubelets[i].Rotate(new Vector3(rotationMultiple, 0, 0));
                        }

                        else if (slice.Y == cubelets[i].CubePosition.Y)
                        {
                            cubelets[i].Rotate(new Vector3(0, rotationMultiple, 0));
                        }

                        else if (slice.Z == cubelets[i].CubePosition.Z)
                        {
                            cubelets[i].Rotate(new Vector3(0, 0, rotationMultiple));
                        }
                    }

                    firstItem.LinkTo(firstItem.NextItem.NextItem);
                    if (firstItem.NextItem == null)
                    {
                        lastItem.LinkTo(firstItem);
                    }

                    commandsInQueue--;

                    selectedCubelet = -1;
                    selectedQuad = null;
                }
            }
        }

        private float NonNegativeDimension(Vector3 vector)
        {
            if (vector.X >= 0)
            {
                return vector.X;
            }

            if (vector.Y >= 0)
            {
                return vector.Y;
            }

            if (vector.Z >= 0)
            {
                return vector.Z;
            }

            throw new ArgumentException("Cube.NonNegativeDimension(Vector3): No non-negative dimension");
        }

        public void UpdateViewMatrices()
        {
            effect.View = Camera.ViewMatrix;
        }

        public Color[,] GetFaceColors(Vector3 face)
        {
            Color[,] faceColors = new Color[cubeSize, cubeSize];
            for (int i = 0; i < cubelets.Length; i++)
            {
                for (int j = 0; j < cubelets[i].ColorVertices.Length; j++)
                {
                    Vector3 position = cubelets[i].ColorVertices[j].Position;
                    if (position.X == face.X)
                    {
                        faceColors[cubeSize - (int)cubelets[i].CubePosition.Y - 1, face.X == 2 ?
                            cubeSize - (int)cubelets[i].CubePosition.Z - 1 : (int)cubelets[i].CubePosition.Z]
                            = cubelets[i].ColorVertices[j].Color;
                    }

                    if (position.Y == face.Y)
                    {
                        faceColors[face.Y == 2 ? cubeSize - (int)cubelets[i].CubePosition.Z - 1
                            : (int)cubelets[i].CubePosition.Z, cubeSize - (int)cubelets[i].CubePosition.X - 1]
                            = cubelets[i].ColorVertices[j].Color;
                    }

                    if (position.Z == face.Z)
                    {
                        faceColors[cubeSize - (int)cubelets[i].CubePosition.Y - 1, face.Z == 0 ?
                            cubeSize - (int)cubelets[i].CubePosition.X - 1 : (int)cubelets[i].CubePosition.X]
                            = cubelets[i].ColorVertices[j].Color;
                    }
                }
            }

            return faceColors;
        }

        public override void Draw(GameTime gameTime)
        {
            using (new PerformanceStopwatch("mvDrawCube"))
            {
                GraphicsDevice.SetVertexBuffer(vertexBuffer);
                GraphicsDevice.Indices = indexBuffer;

                GraphicsDevice.BlendState = BlendState.Opaque;
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                GraphicsDevice.RasterizerState = state;

                for (int i = 0; i < cubelets.Length; i++)
                {
                    effect.World = cubelets[i].WorldMatrix;
                    effect.CurrentTechnique.Passes[0].Apply();

                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                        0, 0, vertexBuffer.VertexCount,
                        indexStarts[cubelets[i].FaceCode], 12);
                }
            }

            base.Draw(gameTime);
        }
    }

    class CommandQueueItem
    {
        private CommandQueueItem nextItem = null;
        public CommandQueueItem NextItem { get { return nextItem; } }

        private Vector3 slice;
        public Vector3 Slice { get { return slice; } }

        private int rotationMultiple;
        public int RotationMultiple { get { return rotationMultiple; } }

        public CommandQueueItem(Vector3 slice, int rotationMultiple)
        {
            this.slice = slice;
            this.rotationMultiple = rotationMultiple;
        }

        public void LinkTo(CommandQueueItem item)
        {
            nextItem = item;
        }
    }
}