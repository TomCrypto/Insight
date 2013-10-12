using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using Microsoft.VisualBasic;

using SharpDX.Direct3D11;
using SharpDX;

namespace Fraunhofer
{
    public partial class MainForm : Form
    {
        private List<SpectralTerm> spectralTerms = new List<SpectralTerm>();
        private Boolean started = false;
        private Renderer renderer;

        public MainForm()
        {
            InitializeComponent();
            DoubleBuffered = true;

            renderer = new Renderer(RenderPanel.Handle, RenderPanel.Size);
            ExposureBar.Value = (int)(Math.Log(renderer.Exposure, 2) * 100);
            SpeedBar.Value = (int)(renderer.Speed * 10.0f);
            DistanceBar.Value = (int)(renderer.LensFilter.ApertureDefinition.observationDistance * DistanceBar.Maximum);

            AnimationList.SelectedIndex = 5;
            renderer.AnimationShader = animationShaders[AnimationList.SelectedIndex];

            SetDefaultSpectralTerms();
        }

        private void SetDefaultSpectralTerms()
        {
            spectralTerms.Clear();
            spectralTerms.Add(new SpectralTerm() { wavelength = 450, rgbFilter = new Color4(  0.0f / 255.0f,  51.0f / 255.0f, 255.0f / 255.0f, 1) });
            spectralTerms.Add(new SpectralTerm() { wavelength = 475, rgbFilter = new Color4(0.0f / 255.0f, 178.0f / 255.0f, 255.0f / 255.0f, 1) });
            spectralTerms.Add(new SpectralTerm() { wavelength = 500, rgbFilter = new Color4(0.0f / 255.0f, 255.0f / 255.0f, 127.0f / 255.0f, 1) });
            spectralTerms.Add(new SpectralTerm() { wavelength = 525, rgbFilter = new Color4(54.0f / 255.0f, 255.0f / 255.0f, 0.0f / 255.0f, 1) });
            spectralTerms.Add(new SpectralTerm() { wavelength = 550, rgbFilter = new Color4(145.0f / 255.0f, 255.0f / 255.0f, 0.0f / 255.0f, 1) });
            spectralTerms.Add(new SpectralTerm() { wavelength = 575, rgbFilter = new Color4(236.0f / 255.0f, 255.0f / 255.0f, 0.0f / 255.0f, 1) });
            spectralTerms.Add(new SpectralTerm() { wavelength = 600, rgbFilter = new Color4(255.0f / 255.0f, 176.0f / 255.0f, 0.0f / 255.0f, 1) });
            spectralTerms.Add(new SpectralTerm() { wavelength = 625, rgbFilter = new Color4(255.0f / 255.0f, 78.0f / 255.0f, 0.0f / 255.0f, 1) });
            spectralTerms.Add(new SpectralTerm() { wavelength = 650, rgbFilter = new Color4(255.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f, 1) });
            ApertureDefinition def = renderer.LensFilter.ApertureDefinition;
            def.spectralTerms = spectralTerms.ToArray();
            renderer.LensFilter.ApertureDefinition = def;

            SpectralTermsList.Items.Clear();

            foreach (SpectralTerm term in spectralTerms)
            {
                ListViewItem item = SpectralTermsList.Items.Add(((int)term.wavelength).ToString() + " nm");
                item.SubItems.Add("(" + term.rgbFilter.Red.ToString("0.0") + ", " + term.rgbFilter.Green.ToString("0.0") + ", " + term.rgbFilter.Blue.ToString("0.0") + ")");
            }
        }

        private void LoadApertureBtn_Click(object sender, EventArgs e)
        {
            if (OpenFileDlg.ShowDialog(this) == DialogResult.OK)
            {
                Texture2D apertureTexture = (Texture2D)Texture2D.FromFile(renderer.Device, OpenFileDlg.FileName);
                renderer.SetAperture(apertureTexture);
                apertureTexture.Dispose();
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            renderer.Dispose();
        }

        private void ApertureRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            renderer.DisplayState = DisplayState.APERTURE_TRANSMISSION_FUNCTION;
        }

        private void FilterRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            renderer.DisplayState = DisplayState.APERTURE_CONVOLUTION_FILTER;
        }

        private void FrameRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            renderer.DisplayState = DisplayState.ORIGINAL_FRAME;
        }

        private void ConvolvedFrameBtn_CheckedChanged(object sender, EventArgs e)
        {
            renderer.DisplayState = DisplayState.CONVOLVED_FRAME;
        }

        private void ExposureBar_Scroll(object sender, EventArgs e)
        {
            renderer.Exposure = (float)Math.Pow(2.0, ExposureBar.Value / 100.0);
        }

        private void SpeedBar_Scroll(object sender, EventArgs e)
        {
            renderer.Speed = (float)SpeedBar.Value / 10.0f;
        }

        private void DistanceBar_Scroll(object sender, EventArgs e)
        {
            ApertureDefinition def = renderer.LensFilter.ApertureDefinition;

            def.observationDistance = (float)DistanceBar.Value / DistanceBar.Maximum;

            renderer.LensFilter.ApertureDefinition = def;
        }

        private void AnimationList_SelectedIndexChanged(object sender, EventArgs e)
        {
            renderer.AnimationShader = animationShaders[AnimationList.SelectedIndex];
        }

        private void SpectralMenu_Opening(object sender, CancelEventArgs e)
        {
            RemoveWavelengthMenu.Enabled = (SpectralTermsList.SelectedItems.Count != 0);
        }

        private void NewWavelengthMenu_Click(object sender, EventArgs e)
        {
            String input = Microsoft.VisualBasic.Interaction.InputBox("Wavelength in nanometres (450 nm to 750 nm)", "Add new wavelength");
            if (input.Length == 0) return;
            double wavelength;

            if (Double.TryParse(input, out wavelength))
            {
                if ((wavelength < 450) || (wavelength > 750))
                {
                    Microsoft.VisualBasic.Interaction.MsgBox("Please enter a valid wavelength.", MsgBoxStyle.Exclamation);
                    return;
                }

                if (ColorDialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    Color4 color = new Color4((float)ColorDialog.Color.R / 255.0f,
                                              (float)ColorDialog.Color.G / 255.0f,
                                              (float)ColorDialog.Color.B / 255.0f,
                                              1);

                    spectralTerms.Add(new SpectralTerm() { wavelength = (float)wavelength, rgbFilter = color });

                    ListViewItem item = SpectralTermsList.Items.Add(((int)wavelength).ToString() + " nm");
                    item.SubItems.Add("(" + color.Red.ToString("0.0") + ", " + color.Green.ToString("0.0") + ", " + color.Blue.ToString("0.0") + ")");

                    ApertureDefinition def = renderer.LensFilter.ApertureDefinition;
                    def.spectralTerms = spectralTerms.ToArray();
                    renderer.LensFilter.ApertureDefinition = def;
                }
            }
            else Microsoft.VisualBasic.Interaction.MsgBox("Please enter a valid wavelength.", MsgBoxStyle.Exclamation);
        }

        private void RemoveWavelengthMenu_Click(object sender, EventArgs e)
        {
            int selected = SpectralTermsList.SelectedIndices[0]; // there should be only 1
            SpectralTermsList.Items.RemoveAt(selected);
            spectralTerms.RemoveAt(selected);

            ApertureDefinition def = renderer.LensFilter.ApertureDefinition;
            def.spectralTerms = spectralTerms.ToArray();
            renderer.LensFilter.ApertureDefinition = def;
        }

        private void ResetWavelengthsMenu_Click(object sender, EventArgs e)
        {
            SetDefaultSpectralTerms();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (!started)
            {
                LoadApertureBtn.PerformClick();
                started = true;
            }
        }

        private void RenderTimer_Tick(object sender, EventArgs e)
        {
            RedrawDisplay();
        }

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        const uint WM_PAINT = 0x000F; // Thanks AllEightUp for this trick (flickering gone).

        private void RedrawDisplay()
        {
            SendMessage(RenderPanel.Handle, WM_PAINT, 0, 0);
        }

        private void RenderPanel_Paint(object sender, PaintEventArgs e)
        {
            renderer.Render();
        }

        public String[] animationShaders =
        {
            /* No movement. */
            @"
            struct VS_IN
            {
	            float4 pos : POSITION;
	            float4 uv  : TEXCOORD;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float4 uv  : TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
                float u = input.uv.x * 2 - 1;
                float v = input.uv.y * 2 - 1;

                if (pow(u, 2) + pow(v, 2) < pow(0.25f, 2))
                    return float4(1, 1, 1, 1);
                else
                    return float4(0, 0, 0, 1);
            }
            ",
            /* Occluding rectangle. */
            @"
            cbuffer time : register(b0)
            {
                float t;
            }

            struct VS_IN
            {
	            float4 pos : POSITION;
	            float4 uv  : TEXCOORD;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float4 uv  : TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
                float u = input.uv.x * 2 - 1;
                float v = input.uv.y * 2 - 1;

                if ((u > -0.175f) && (u < 0.175f) && (v > -0.75f) && (v < 0.75f))
                    return float4(0.0005f, 0.0005f, 0.0005f, 1);

                float2 center = float2(cos(1.45f * t) + cos(0.82f * t),
                                       cos(0.62f * t) + cos(1.21f * t)) * 0.4f;

                if (pow(u - center.x, 2) + pow(v - center.y, 2) < pow(0.08f, 2))
                    return float4(1, 1, 1, 1);

                return float4(0, 0, 0, 1);
            }
            ",
            /* Two lights moving together. */
            @"
            cbuffer time : register(b0)
            {
                float t;
            }

            struct VS_IN
            {
	            float4 pos : POSITION;
	            float4 uv  : TEXCOORD;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float4 uv  : TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
                float u = input.uv.x * 2 - 1;
                float v = input.uv.y * 2 - 1;

                float2 c1 = float2(cos(1.45f * t) + cos(0.82f * t),
                                   cos(0.62f * t) + cos(1.21f * t)) * 0.4f;

                float2 c2 = float2(cos(0.25f * t) + cos(1.82f * t),
                                   cos(1.62f * t) + cos(0.21f * t)) * 0.4f;

                if (pow(u - c1.x, 2) + pow(v - c1.y, 2) < pow(0.09f, 2))
                    return float4(1, 1, 1, 1);

                if (pow(u - c2.x, 2) + pow(v - c2.y, 2) < pow(0.15f, 2))
                    return float4(0.1f, 0.1f, 0.1f, 1);

                return float4(0, 0, 0, 1);
            }
            ",
            /* A large stationary light source. */
            @"
            cbuffer time : register(b0)
            {
                float t;
            }

            struct VS_IN
            {
	            float4 pos : POSITION;
	            float4 uv  : TEXCOORD;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float4 uv  : TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
                float u = input.uv.x * 2 - 1;
                float v = input.uv.y * 2 - 1;

                float c = sin(t);

                if (abs(u - c) < 0.15f)
                    return float4(0, 0, 0, 1);

                if (u * u + v * v < 0.35f * 0.35f)
                    return float4(0.15f, 0.15f, 0.15f, 1.0f);

                return float4(0, 0, 0, 1);
            }
            ",
            /* Colored light. */
            @"
            cbuffer time : register(b0)
            {
                float t;
            }

            struct VS_IN
            {
	            float4 pos : POSITION;
	            float4 uv  : TEXCOORD;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float4 uv  : TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
                float u = input.uv.x * 2 - 1;
                float v = input.uv.y * 2 - 1;

                if ((u > -0.175f) && (u < 0.175f) && (v > -0.75f) && (v < 0.75f))
                    return float4(0.0005f, 0.0005f, 0.0005f, 1);

                float2 c1 = float2(cos(1.35f * t) + cos(0.72f * t),
                                   cos(2.62f * t) + cos(1.61f * t)) * 0.4f;

                if (pow(u - c1.x, 2) + pow(v - c1.y, 2) < pow(0.05f, 2))
                    return float4(0.25f, 0.75f, 0.35f, 1);

                return float4(0, 0, 0, 1);
            }
            ",
            /* Heavy occluding grating. */
            @"
            cbuffer time : register(b0)
            {
                float t;
            }

            struct VS_IN
            {
	            float4 pos : POSITION;
	            float4 uv  : TEXCOORD;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float4 uv  : TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
                float u = input.uv.x * 2 - 1;
                float v = input.uv.y * 2 - 1;

                if ((u + 1) % 0.1f < 0.08f)
                    return float4(0.0005f, 0.0005f, 0.0005f, 1);

                float2 center = float2(cos(1.45f * t) + cos(0.82f * t),
                                       cos(0.62f * t) + cos(1.21f * t)) * 0.4f;

                if (pow(u - center.x, 2) + pow(v - center.y, 2) < pow(0.10f, 2))
                    return float4(1, 1, 1, 1);

                return float4(0, 0, 0, 1);
            }
            "
        };
    }
}
