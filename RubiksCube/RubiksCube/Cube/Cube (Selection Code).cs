using System;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace RubiksCube
{
    partial class Cube : DrawableGameComponent
    {
        private void CheckSelection()
        {
            Vector2 mousePosition = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            if (selectedQuad != null
                && IsInQuad(mousePosition, selectedQuad[0], selectedQuad[1], selectedQuad[2], selectedQuad[3]))
            {
                return;
            }

            Performance.Counters["coSelection"].Increment();
            using (new PerformanceStopwatch("tmSelection"))
            {
                selectedQuad = new Vector2[4];

                for (int i = 0; i < cubelets.Length; i++)
                {
                    if (CheckCubeletSelection(mousePosition, i))
                    {
                        return;
                    }
                }

                selectedQuad = null;
                sliceRotationDirection = null;
            }
        }

        private bool CheckCubeletSelection(Vector2 mousePosition, int cubeletIndex)
        {
            for (int i = 0; !cubelets[cubeletIndex].IsRotating && i < 6; i++)
            {
                Orientation currentFace = (Orientation)(1 << i);
                if ((cubelets[cubeletIndex].StartingOrientation & currentFace) == currentFace)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        Vector3 projected = GraphicsDevice.Viewport.Project(cornerVertices[i * 4 + k],
                            Camera.ProjectionMatrix, Camera.ViewMatrix, cubelets[cubeletIndex].WorldMatrix);

                        selectedQuad[k] = new Vector2(projected.X, projected.Y);
                    }

                    if (IsInQuad(mousePosition, selectedQuad[0],
                        selectedQuad[1], selectedQuad[2], selectedQuad[3]))
                    {
                        float clockwiseError = ClockwiseError(selectedQuad[0], selectedQuad[1],
                            selectedQuad[2], selectedQuad[3]);
                        float counterClockwiseError = CounterClockwiseError(selectedQuad[0],
                            selectedQuad[1], selectedQuad[2], selectedQuad[3]);

                        if (clockwiseError < counterClockwiseError)
                        {
                            selectedCubelet = cubeletIndex;

                            Vector3 centerSelectedFace = (cornerVertices[i * 4] + cornerVertices[i * 4 + 2]) / 2;

                            Vector3 projectedCenterSelectedFace = GraphicsDevice.Viewport.Project(centerSelectedFace,
                                Camera.ProjectionMatrix, Camera.ViewMatrix, cubelets[cubeletIndex].WorldMatrix);
                            Vector3 projectedUpSelectedFace = GraphicsDevice.Viewport.Project(centerSelectedFace + Vector3.Up,
                                Camera.ProjectionMatrix, Camera.ViewMatrix, cubelets[cubeletIndex].WorldMatrix);
                            Vector3 projectedRightSelectedFace = GraphicsDevice.Viewport.Project(centerSelectedFace + Vector3.Right,
                                Camera.ProjectionMatrix, Camera.ViewMatrix, cubelets[cubeletIndex].WorldMatrix);

                            arrowVectors = new Vector2[3];
                            arrowVectors[0] = new Vector2(projectedCenterSelectedFace.X, projectedCenterSelectedFace.Y);
                            arrowVectors[1] = new Vector2(projectedUpSelectedFace.X, projectedUpSelectedFace.Y);
                            arrowVectors[2] = new Vector2(projectedRightSelectedFace.X, projectedRightSelectedFace.Y);

                            Vector2 center = new Vector2(projectedCenterSelectedFace.X, projectedCenterSelectedFace.Y);

                            sliceRotationDirection = new Vector2[2];
                            sliceRotationDirection[0] =
                                new Vector2(projectedUpSelectedFace.X, projectedUpSelectedFace.Y) - center;
                            sliceRotationDirection[0] =
                                new Vector2(projectedRightSelectedFace.X, projectedRightSelectedFace.Y) - center;

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private float ClockwiseError(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            Vector2[] diff = new Vector2[] { b - a, c - b, d - c, a - d };
            for (int i = 0; i < 4; i++)
            {
                int i0 = i;
                int i1 = (i + 1) % 4;
                int i2 = (i + 2) % 4;
                int i3 = (i + 3) % 4;

                if (diff[i0].X < 0 && diff[i1].Y < 0 && diff[i2].X > 0 && diff[i3].Y > 0)
                {
                    float error = 0;
                    float[] ratios = new float[] {diff[i0].Y / diff[i0].X, diff[i1].X / diff[i1].Y,
                        diff[i2].Y / diff[i2].X, diff[i3].X / diff[i3].Y};

                    // Return the average of ratios
                    for (int j = 0; j < 4; j++)
                    {
                        error += Math.Abs(ratios[j]);
                    }

                    return error / 4;
                }
            }

            return float.MaxValue;
        }

        private float CounterClockwiseError(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            Vector2[] diff = new Vector2[] { b - a, c - b, d - c, a - d };
            for (int i = 0; i < 4; i++)
            {
                int i0 = i;
                int i1 = (i + 1) % 4;
                int i2 = (i + 2) % 4;
                int i3 = (i + 3) % 4;

                if (diff[i0].Y < 0 && diff[i1].X < 0 && diff[i2].Y > 0 && diff[i3].X > 0)
                {
                    float factor = 0;
                    float[] ratios = new float[] {diff[i0].X / diff[i0].Y, diff[i1].Y / diff[i1].X,
                        diff[i2].X / diff[i2].Y, diff[i3].Y / diff[i3].X};

                    // Return the average of ratios
                    for (int j = 0; j < 4; j++)
                    {
                        factor += Math.Abs(ratios[j]);
                    }

                    return factor / 4;
                }
            }

            return float.MaxValue;
        }

        private bool IsInQuad(Vector2 point, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            return IsBetweenLines(point, a, b, d, c) && IsBetweenLines(point, a, d, b, c);
        }

        private bool IsBetweenLines(Vector2 point, Vector2 line1Start, Vector2 line1End,
            Vector2 line2Start, Vector2 line2End)
        {
            Vector2 foot1 = FindFoot(point, line1Start, line1End - line1Start);
            Vector2 intersection1 = Intersection(point, foot1, line2Start, line2End);

            if ((foot1 - point).Length() < (intersection1 - foot1).Length())
            {
                Vector2 foot2 = FindFoot(point, line2Start, line2End - line2Start);
                Vector2 intersection2 = Intersection(point, foot2, line1Start, line1End);

                return (foot2 - point).Length() < (intersection2 - foot2).Length();
            }

            return false;
        }

        private Vector2 FindFoot(Vector2 point, Vector2 start, Vector2 direction)
        {
            float s = (direction.X * point.X + direction.Y * point.Y
                - (direction.X * start.X + direction.Y * start.Y))
                / (float)(Math.Pow(direction.X, 2) + Math.Pow(direction.Y, 2));

            return start + s * direction;
        }

        private Vector2 Intersection(Vector2 line1Start, Vector2 line1End, Vector2 line2Start,
            Vector2 line2End)
        {
            float s =
                ((line2End.X - line2Start.X) * (line1Start.Y - line2Start.Y)
                - (line2End.Y - line2Start.Y) * (line1Start.X - line2Start.X))
                / ((line2End.Y - line2Start.Y) * (line1End.X - line1Start.X)
                - (line2End.X - line2Start.X) * (line1End.Y - line1Start.Y));

            return new Vector2(line1Start.X + s * (line1End.X - line1Start.X),
                line1Start.Y + s * (line1End.Y - line1Start.Y));
        }
    }
}