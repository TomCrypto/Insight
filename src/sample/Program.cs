using System;
using System.Drawing;
using System.Windows.Forms;

using SharpDX.Windows;

namespace Sample
{
    static class Program
    {
        /// <summary>
        /// The sample's entry point.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                AntRenderForm window = new AntRenderForm("Insight Library Sample");
                Renderer renderer = null;

                RenderLoop.Run(window, () =>
                {
                    if (restartRenderer)
                    {
                        if (renderer != null) renderer.Dispose();
                        window.ClientSize = DisplayResolution;
                        renderer = new Renderer(window);
                        TweakBar.UpdateWindow(window);
                        restartRenderer = false;
                    }

                    renderer.Render();
                });

                renderer.Dispose();
                window.Dispose();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static private Size resolution = Settings.InitialResolution;
        static private Boolean restartRenderer = true;

        /// <summary>
        /// Gets or sets the display resolution (if changed, will
        /// fully reinitialize the renderer on the next frame).
        /// </summary>
        static public Size DisplayResolution
        {
            get
            {
                return resolution;
            }

            set
            {
                if (value != resolution)
                {
                    restartRenderer = true;
                    resolution = value;
                }
            }
        }

        /// <summary>
        /// Called when the renderer wishes to be reinitialized.
        /// </summary>
        public static void Restart()
        {
            restartRenderer = true;
        }
    }
}
