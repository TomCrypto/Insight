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
            AntRenderForm window = new AntRenderForm("Insight Library Sample");
            //window.StartPosition   = FormStartPosition.CenterScreen;
            window.FormBorderStyle = FormBorderStyle.FixedDialog;
            window.Icon            = Resources.ProgramIcon;
            window.MaximizeBox     = false;

            window.Location = new Point(10, 10);
            Renderer renderer = null;

            RenderLoop.Run(window, () =>
            {
                if (window.ClientSize != DisplayResolution)
                {
                    if (renderer != null) renderer.Dispose();
                    window.ClientSize = DisplayResolution;
                    renderer = new Renderer(window);
                    TweakBar.UpdateWindow(window);
                }

                renderer.Render();
            });

            renderer.Dispose();
            window.Dispose();
        }

        static private Size resolution = Settings.InitialResolution;

        /// <summary>
        /// Gets or sets the display resolution (if changed, will
        /// fully reinitialize the renderer on the next frame).
        /// </summary>
        static public Size DisplayResolution
        {
            get { return resolution; }
            set { resolution = value; }
        }
    }
}
