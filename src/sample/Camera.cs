using System;

using SharpDX;

namespace Sample
{
    /// <summary>
    /// Implements a perspective camera.
    /// </summary>
    class Camera
    {
        /// <summary>
        /// The current position of the camera, in world space.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// The current rotation of the camera, in spherical coordinates.
        /// </summary>
        public Vector2 Rotation { get; set; }

        /// <summary>
        /// The camera's projection matrix.
        /// </summary>
        public Matrix ProjectionMatrix
        {
            get
            {
                return Matrix.PerspectiveFovLH(FieldOfView * (float)(Math.PI / 180), AspectRatio, Settings.nearPlane, Settings.farPlane);
            }
        }

        /// <summary>
        /// The view matrix for the current camera position and rotation.
        /// </summary>
        public Matrix ViewMatrix
        {
            get
            {
                Matrix viewMatrix;
                Vector3 eyeVector;

                Recompute(out viewMatrix, out eyeVector);

                return viewMatrix;
            }
        }

        /// <summary>
        /// The (unit) direction in which the camera is facing.
        /// </summary>
        public Vector3 Direction
        {
            get
            {
                Matrix viewMatrix;
                Vector3 eyeVector;

                Recompute(out viewMatrix, out eyeVector);

                return eyeVector;
            }
        }

        /// <summary>
        /// The camera's field of view, in degrees.
        /// </summary>
        public float FieldOfView { get; set; }

        /// <summary>
        /// The camera's aspect ratio (width/height).
        /// </summary>
        public float AspectRatio { get; set; }

        private void Recompute(out Matrix viewMatrix, out Vector3 eyeVector)
        {
            Matrix rotationMatrix = Matrix.RotationX(Rotation.Y) * Matrix.RotationY(Rotation.X);

            Vector4 upVector = Vector3.Transform(Vector3.Up, rotationMatrix);
            Vector4 rotatedTarget = Vector3.Transform(Vector3.ForwardRH, rotationMatrix);
            Vector3 finalTarget = Position + new Vector3(rotatedTarget.X, rotatedTarget.Y, rotatedTarget.Z);

            viewMatrix = Matrix.LookAtLH(Position, finalTarget, new Vector3(upVector.X, upVector.Y, upVector.Z));
            eyeVector = Vector3.Normalize(finalTarget - Position);
        }

        /// <summary>
        /// Creates a new camera instance.
        /// </summary>
        /// <param name="position">Position of the camera.</param>
        /// <param name="rotation">Rotation of the camera.</param>
        /// <param name="fieldOfView">Field of view, in radians.</param>
        /// <param name="aspectRatio">Aspect ratio of the camera.</param>
        public Camera(Vector3 position, Vector2 rotation, float fieldOfView, float aspectRatio)
        {
            Position = position;
            Rotation = rotation;
            FieldOfView = fieldOfView;
            AspectRatio = aspectRatio;
        }

        /// <summary> Transforms a vector to the direction of the eye vector. </summary>
        public Vector3 EyeTransform(Vector2 vector)
        {
            Matrix rotationMatrix = Matrix.RotationX(Rotation.Y) * Matrix.RotationY(Rotation.X);
            return ToVector3(Vector3.Transform(new Vector3(vector.X, 0, vector.Y), rotationMatrix));
        }

        /// <summary>
        /// Moves the camera in a given direction.
        /// </summary>
        /// <param name="movement"> The movement vector to move across by, rotated towards the camera direction.</param>
        public void MoveCamera(Vector3 movement)
        {
            Matrix rotationMatrix = Matrix.RotationX(Rotation.Y) * Matrix.RotationY(Rotation.X);
            Vector4 rotatedTarget = Vector3.Transform(movement, rotationMatrix);
            
            Position += Vector3.Normalize(ToVector3(rotatedTarget)) * Settings.movementSensitivity;
        }

        /// <summary>
        /// Rotates the camera by a spherical angle.
        /// </summary>
        /// <param name="rotation">The rotation angle by which to rotate by.</param>
        public void RotateCamera(Vector2 rotation)
        {
            rotation *= Settings.rotationSensitivity;

            Rotation = new Vector2((float)(Rotation.X + rotation.X), /* Clamp vertical angle to avoid reversal. */
                                   (float)Math.Max(-Math.PI / 2, Math.Min(Math.PI / 2, (Rotation.Y + rotation.Y))));
        }

        /// <summary>
        /// Writes the camera's view matrix, projection matrix, position,
        /// and direction, to a data stream of the appropriate size.
        /// </summary>
        /// <param name="stream">The stream to write the camera to.</param>
        public void WriteTo(DataStream stream)
        {
            stream.Write<Matrix>(Matrix.Transpose(ViewMatrix * ProjectionMatrix));
            stream.Write<Vector4>(new Vector4(Position, 1));
            stream.Write<Vector4>(new Vector4(Direction, 1));
        }

        /// <summary>
        /// Returns the size of the camera as written to the stream.
        /// </summary>
        /// <returns>The size, in bytes, of the camera.</returns>
        public static int Size()
        {
            return 64 + 16 + 16;
        }

        /// <summary>
        /// Converts a Vector4 to a Vector3 by omitting the last component.
        /// </summary>
        /// <param name="v">The input Vector4.</param>
        /// <returns>The output Vector3.</returns>
        private static Vector3 ToVector3(Vector4 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }
    }
}
