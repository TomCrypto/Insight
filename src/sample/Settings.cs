using System;
using System.Drawing;

namespace Sample
{
    /// <summary>
    /// Contains various application-wide settings.
    /// </summary>
    static class Settings
    {
        private static Size initialResolution = new Size(600, 600);
        public static Size InitialResolution { get { return initialResolution; } }
    }
}
