using System;
using System.IO;
using System.Drawing;

using SharpDX;
using SharpDX.D3DCompiler;

using Insight;

namespace Sample
{
    /// <summary>
    /// Contains various application-wide settings. These are
    /// set to default, initial values, and may or may not be
    /// changeable by the sample. They are to be read by the
    /// renderer upon initialization.
    /// </summary>
    static class Settings
    {
        /// <summary> An include handler class for shader #includes. </summary>
        public class IncludeFX : Include
        {
            public void Close(Stream stream) { stream.Close(); stream.Dispose(); }
            public Stream Open(IncludeType type, string fileName, Stream stream)
            {
                return new FileStream(Settings.ShaderDir + fileName, FileMode.Open);
            }

            public void Dispose()
            {
                // TMP
            }

            public IDisposable Shadow { get; set; }
        }

        /// <summary> An include handler for shaders. </summary>
        public static IncludeFX includeFX = new IncludeFX();

        private static Size initialResolution = new Size(1280, 800);
        public static Size InitialResolution { get { return initialResolution; } }

        /// <summary> The far plane. </summary>
        static public float farPlane = 700;
        /// <summary> The near plane. </summary>
        static public float nearPlane = 0.005f;
        /// <summary> The movement sensitivity factor. </summary>
        static public float movementSensitivity = 0.05f;
        /// <summary> The rotation sensitivity factor. </summary>
        static public float rotationSensitivity = 0.9f;
        /// <summary> The initial camera position.</summary>
        public static Vector3 initialCameraPosition = new Vector3(6.840405f, -9.914818f, -0.217376f);
        /// <summary> The initial camera rotation.</summary>
        public static Vector2 initialCameraRotation = new Vector2(-1.628771f, 0.2387369f);

        public static string ShaderDir = "Shaders/";
        public static string ModelDir = "Models/";
        public static string TextureDir = "Textures/";
        public static string MaterialDir = "Materials/";

        public static string ModelExt = ".obj";
        public static string MaterialExt = ".mtl";
        public static string TextureExt = ".png";

        public static RenderQuality quality = RenderQuality.Medium;
    }
}
