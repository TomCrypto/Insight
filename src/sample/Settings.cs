using System;
using System.IO;
using System.Drawing;

using SharpDX;
using SharpDX.D3DCompiler;

namespace Sample
{
    /// <summary>
    /// Contains various application-wide settings.
    /// </summary>
    static class Settings
    {
        /// <summary> An include handler class for shader #includes. </summary>
        public class IncludeFX : Include
        {
            public void Close(Stream stream) { stream.Close(); stream.Dispose(); }
            public Stream Open(IncludeType type, string fileName, Stream stream)
            {
                return new FileStream(Settings.shaderDirectory + fileName, FileMode.Open);
            }

            public void Dispose()
            {
                // TMP
            }

            public IDisposable Shadow { get; set; }
        }

        /// <summary> An include handler for shaders. </summary>
        public static IncludeFX includeFX = new IncludeFX();

        private static Size initialResolution = new Size(600, 600);
        public static Size InitialResolution { get { return initialResolution; } }

        /// <summary> The far plane. </summary>
        static public float farPlane = 1000;
        /// <summary> The near plane. </summary>
        static public float nearPlane = 0.01f;
        /// <summary> The movement sensitivity factor. </summary>
        static public float movementSensitivity = 0.1f;
        /// <summary> The rotation sensitivity factor. </summary>
        static public float rotationSensitivity = 0.05f;
        /// <summary> The initial camera position.</summary>
        public static Vector3 initialCameraPosition = new Vector3(0, 0, 0);
        /// <summary> The initial camera rotation.</summary>
        public static Vector2 initialCameraRotation = new Vector2(0, 0);

        public static string shaderDirectory = "shaders/";
    }
}
