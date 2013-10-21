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
            RenderForm window = new RenderForm("Insight Library Sample");
            //window.StartPosition = FormStartPosition.CenterScreen;
            window.FormBorderStyle = FormBorderStyle.FixedDialog;
            window.ClientSize = Settings.InitialResolution;
            window.Icon = Resources.ProgramIcon;

            using (Renderer renderer = new Renderer(window))
            {
                RenderLoop.Run(window, () =>
                {
                    renderer.Render();
                });
            }

            window.Dispose();
        }
    }
}
