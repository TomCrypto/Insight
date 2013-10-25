using System;
using System.Windows.Forms;

using SharpDX.Windows;

namespace Sample
{
    static class Program
    {
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
                Console.WriteLine(ex); // TODO: better error reporting later on
                MessageBox.Show(ex.Message, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
