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
                using (AntRenderForm window = new AntRenderForm("Insight Library Sample"))
                {
                    using (Renderer renderer = new Renderer(window))
                    {
                        RenderLoop.Run(window, () =>
                        {
                            renderer.Update();
                            renderer.Render();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                MessageBox.Show(ex.Message, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
