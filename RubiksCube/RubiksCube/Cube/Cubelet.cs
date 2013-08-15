using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RubiksCube
{
    enum Orientation { Front = 1, Back = 2, Right = 4, Left = 8, Bottom = 16, Top = 32 };

    class Cubelet
    {
        private readonly Orientation startingOrientation;
        public Orientation StartingOrientation { get { return startingOrientation; } }

        private readonly int faceCode;
        public int FaceCode { get { return faceCode; } }

        private Vector3[] originalColorVerticesDirection;
        private VertexPositionColor[] colorVertices;
        public VertexPositionColor[] ColorVertices { get { return colorVertices; } }

        private Vector3 currentRotation;
        private Vector3 rotationModifier;
        private Matrix previousRotations = Matrix.Identity;

        private Vector3 position;
        public Vector3 Position { get { return position; } }

        private Vector3 cubePosition;
        public Vector3 CubePosition { get { return cubePosition; } }

        private readonly Vector3 center;
        private readonly Vector3 cubeCenter;
        private readonly Vector3 originalDirection;

        public bool IsRotating { get { return rotationModifier != Vector3.Zero; } }

        private Matrix worldMatrix = Matrix.Identity;
        public Matrix WorldMatrix { get { return worldMatrix; } }

        public Cubelet(Vector3 cubePosition, Vector3 center, Vector3 cubeCenter, float scale,
            float offset, Orientation startingOrientation)
        {
            this.cubePosition = cubePosition;
            this.center = center;
            this.cubeCenter = cubeCenter;
            this.startingOrientation = startingOrientation;

            faceCode = GetFaceCode(startingOrientation);
            position = (scale + offset) * cubePosition;
            originalDirection = position - center;

            SetupColorVertices();
            UpdateWorldMatrix();
        }

        private void SetupColorVertices()
        {
            int colorVerticesCount = 0;
            for (int i = 0; i < 6; i++)
            {
                colorVerticesCount += ((int)startingOrientation & 1 << i) >> i;
            }

            int start = 0;
            colorVertices = new VertexPositionColor[colorVerticesCount];

            if ((startingOrientation & Orientation.Front) == Orientation.Front)
            {
                colorVertices[start++] = new VertexPositionColor(new Vector3(1, 1, 0), Color.Red);
            }

            if ((startingOrientation & Orientation.Back) == Orientation.Back)
            {
                colorVertices[start++] = new VertexPositionColor(new Vector3(1, 1, 2), Color.Orange);
            }

            if ((startingOrientation & Orientation.Right) == Orientation.Right)
            {
                colorVertices[start++] = new VertexPositionColor(new Vector3(0, 1, 1), Color.Blue);
            }

            if ((startingOrientation & Orientation.Left) == Orientation.Left)
            {
                colorVertices[start++] = new VertexPositionColor(new Vector3(2, 1, 1), Color.Green);
            }

            if ((startingOrientation & Orientation.Bottom) == Orientation.Bottom)
            {
                colorVertices[start++] = new VertexPositionColor(new Vector3(1, 0, 1), Color.Yellow);
            }

            if ((startingOrientation & Orientation.Top) == Orientation.Top)
            {
                colorVertices[start++] = new VertexPositionColor(new Vector3(1, 2, 1), Color.White);
            }

            originalColorVerticesDirection = new Vector3[colorVerticesCount];

            for (int i = 0; i < colorVertices.Length; i++)
            {
                originalColorVerticesDirection[i] = colorVertices[i].Position - new Vector3(1);
            }
        }

        public void Update(TimeSpan elapsedGameTime)
        {
            // If the cubelet is still rotating.
            if (rotationModifier != Vector3.Zero)
            {
                // Use rotationModifier to adjust rotation.
                Vector3 oldRotation = currentRotation;
                float rotationAmount = 0.005f * (float)elapsedGameTime.TotalMilliseconds;

                currentRotation.X += MathHelper.Clamp(rotationModifier.X, -rotationAmount, rotationAmount);
                currentRotation.Y += MathHelper.Clamp(rotationModifier.Y, -rotationAmount, rotationAmount);
                currentRotation.Z += MathHelper.Clamp(rotationModifier.Z, -rotationAmount, rotationAmount);

                // Subtract the amount by which rotation was adjusted from rotationModifier.
                Vector3 rotationDifference = currentRotation - oldRotation;
                rotationModifier -= rotationDifference;

                // If any component of rotationModifer is within 0.0001 of 0 set to 0 to stop hunting.
                rotationModifier.X = rotationModifier.X < 0.0001 && rotationModifier.X > -0.0001 ? 0 : rotationModifier.X;
                rotationModifier.Y = rotationModifier.Y < 0.0001 && rotationModifier.Y > -0.0001 ? 0 : rotationModifier.Y;
                rotationModifier.Z = rotationModifier.Z < 0.0001 && rotationModifier.Z > -0.0001 ? 0 : rotationModifier.Z;

                // Update the position.
                Matrix rotationMatrix = Matrix.CreateFromYawPitchRoll(
                    currentRotation.Y, currentRotation.X, currentRotation.Z);

                position = center + Vector3.Transform(originalDirection, previousRotations * rotationMatrix);

                // If the cubelet has finished rotating, update currentRotation, previousRotation and colorVertices.
                if (rotationModifier == Vector3.Zero)
                {
                    previousRotations *= rotationMatrix;
                    currentRotation = Vector3.Zero;

                    for (int i = 0; i < colorVertices.Length; i++)
                    {
                        colorVertices[i].Position = new Vector3(1) +
                            Vector3.Transform(originalColorVerticesDirection[i], previousRotations);

                        colorVertices[i].Position.X = (int)Math.Round(colorVertices[i].Position.X);
                        colorVertices[i].Position.Y = (int)Math.Round(colorVertices[i].Position.Y);
                        colorVertices[i].Position.Z = (int)Math.Round(colorVertices[i].Position.Z);
                    }
                }

                UpdateWorldMatrix();
            }
        }

        public void Rotate(Vector3 rotationMultiple)
        {
            rotationMultiple *= MathHelper.PiOver2;
            CubeRotation(rotationMultiple);

            rotationModifier += rotationMultiple;
        }

        private void CubeRotation(Vector3 rotation)
        {
            Vector3 cubeRotationCenter = new Vector3(
                (rotation.X != 0 ? cubePosition.X : cubeCenter.X),
                (rotation.Y != 0 ? cubePosition.Y : cubeCenter.Y),
                (rotation.Z != 0 ? cubePosition.Z : cubeCenter.Z));

            Matrix rotationMatrix = Matrix.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);

            cubePosition = cubeRotationCenter
                + Vector3.Transform(cubePosition - cubeRotationCenter, rotationMatrix);

            cubePosition = new Vector3((int)Math.Round(cubePosition.X),
                (int)Math.Round(cubePosition.Y), (int)Math.Round(cubePosition.Z));
        }

        private void UpdateWorldMatrix()
        {
            worldMatrix = previousRotations
                * Matrix.CreateFromYawPitchRoll(currentRotation.Y, currentRotation.X, currentRotation.Z)
                * Matrix.CreateTranslation(position);
        }

        private int GetFaceCode(Orientation orientation)
        {
            int[] possibleCombinations = new int[] { 1, 2, 4, 5, 6, 8, 9, 10, 16, 17, 18, 20, 21, 22, 24, 25,
                26, 32, 33, 34, 36, 37, 38, 40, 41, 42 };
            for (int i = 0; i < possibleCombinations.Length; i++)
            {
                if ((int)orientation == possibleCombinations[i])
                {
                    return i;
                }
            }

            return -1;
        }
    }
}