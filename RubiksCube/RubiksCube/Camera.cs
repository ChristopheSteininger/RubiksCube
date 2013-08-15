using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace RubiksCube
{
    static class Camera
    {
        private static Matrix viewMatrix;
        public static Matrix ViewMatrix
        {
            get { return viewMatrix; }
        }

        private static Matrix projectionMatrix;
        public static Matrix ProjectionMatrix
        {
            get { return projectionMatrix; }
        }

        public delegate void ViewMatrixUpdatedEvent();
        private static ViewMatrixUpdatedEvent viewMatrixUpdated;
        public static ViewMatrixUpdatedEvent ViewMatrixUpdated
        {
            get { return viewMatrixUpdated; }
            set { viewMatrixUpdated = value; }
        }

        public delegate void ProjectionMatrixUpdatedEvent();
        private static ProjectionMatrixUpdatedEvent projectionMatrixUpdated;
        public static ProjectionMatrixUpdatedEvent ProjectionMatrixUpdated
        {
            get { return projectionMatrixUpdated; }
            set { projectionMatrixUpdated = value; }
        }

        private static Vector3 position = new Vector3(0, 0, -50);
        private static Vector3 target = Vector3.Zero;
        public static Vector3 Target { get { return target; } }
        private static Vector3 up = Vector3.Up;
        private static Vector3 right = Vector3.Right;

        public static void SetupCamera(float aspectRatio)
        {
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, aspectRatio, 1.0f, 1000.0f);

            if (projectionMatrixUpdated != null)
            {
                projectionMatrixUpdated();
            }

            UpdateViewMatrix();
        }

        public static void UpdateViewMatrix(Vector3 newPosition, Vector3 newTarget, Vector3 newUp)
        {
            position = newPosition;
            target = newTarget;
            up = newUp;

            UpdateViewMatrix();
        }

        public static void UpdateLookAt(Vector3 newTarget)
        {
            target = newTarget;

            UpdateViewMatrix();
        }

        public static void RotateAroundTarget(Vector3 rotation)
        {
            if (rotation == Vector3.Zero)
            {
                return;
            }

            float distanceToTarget = (position - target).Length();
            
            right = Vector3.Transform(right, Matrix.CreateRotationY(rotation.X));
            up = Vector3.Transform(up, Matrix.CreateRotationX(rotation.Y));

            Vector3 direction = Vector3.Cross(up, right);
            direction.Normalize();

            position = target + direction * distanceToTarget;

            UpdateViewMatrix();
        }

        public static void Zoom(float amount)
        {
            if (amount == 0)
            {
                return;
            }

            Vector3 direction = position - target;
            Vector3 normalizedDirection = direction;
            normalizedDirection.Normalize();

            position = target + (direction - normalizedDirection * amount);

            UpdateViewMatrix();
        }

        private static void UpdateViewMatrix()
        {
            viewMatrix = Matrix.CreateLookAt(position, target, up);

            if (viewMatrixUpdated != null)
            {
                viewMatrixUpdated();
            }
        }
    }
}