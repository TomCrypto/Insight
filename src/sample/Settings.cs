using System.Drawing;

using SharpDX;
using SharpDX.DXGI;

using Insight;

namespace Sample
{
    /// <summary>
    /// Contains various application-wide settings. These are
    /// set to default, initial values, and may or may not be
    /// changeable by the sample. They are to be read by the
    /// renderer upon initialization.
    /// 
    /// Will be moved into a definition file later on.
    /// </summary>
    static class Settings
    {
        public static Size InitialResolution = new Size(1280, 800);

        /// <summary> The far plane. </summary>
        static public float farPlane = 700;
        /// <summary> The near plane. </summary>
        static public float nearPlane = 0.1f;
        /// <summary> The movement sensitivity factor. </summary>
        static public float movementSensitivity = 0.1f;
        /// <summary> The rotation sensitivity factor. </summary>
        static public float rotationSensitivity = 2.0f;
        /// <summary> The initial camera position.</summary>
        public static Vector3 initialCameraPosition = new Vector3(6.840405f, -9.914818f, -0.217376f);
        /// <summary> The initial camera rotation.</summary>
        public static Vector2 initialCameraRotation = new Vector2(-1.628771f, 0.2387369f);

        public static SampleDescription MultisamplingOptions = new SampleDescription(4, 3);

        public static RenderQuality quality = RenderQuality.Medium;
    }
}
