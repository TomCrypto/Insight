using System.Drawing;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;

namespace Iridium
{
    public struct LensFilterDescription
    {
        public Size apertureSize;
        public Size frameSize;
    }

    public struct ApertureDefinition
    {
        public SpectralTerm[] spectralTerms;
        public float observationDistance;
    }

    public struct SpectralTerm
    {
        public float wavelength;
        public Color4 rgbFilter;

        public const float BLUE = 450.0f;
    }

    public class LensFilter : System.IDisposable
    {
        /// <summary>
        /// The graphics device used by this instance.
        /// </summary>
        /// <remarks>
        /// This instance does not own (and will not dispose of) this device.
        /// </remarks>
        public Device Device { get; private set; }

        /// <summary>
        /// The description for this lens filter instance.
        /// </summary>
        /// <remarks>
        /// This structure is used for storing any final attributes of this lens filter
        /// which can't be changed once the class is created (in other words, to change
        /// them would require reconstructing essentially the entire class instance).
        /// </remarks>
        public LensFilterDescription Description { get; private set; }

        /// <summary>
        /// The aperture transmission function of the lens.
        /// </summary>
        /// <remarks>
        /// This texture will contain the aperture transmission function, as a 2D array
        /// of real values. Its format is R32_Float - the contents of this texture will
        /// remain constant until a new aperture is set.
        /// </remarks>
        private readonly GraphicsResource aperture;

        /// <summary>
        /// The aperture transmission function at a given wavelength.
        /// </summary>
        /// <remarks>
        /// This texture contains a downsampled version of the original aperture padded
        /// with black to make it the same size as the aperture texture. This is to let
        /// the filter accumulate multiple diffraction patterns of the same aperture at
        /// different wavelengths (in order to accurately render colors). Physically, a
        /// larger aperture corresponds to a *lower* wavelength, thus by convention the
        /// original aperture corresponds to the lowest possible wavelength (blue). The
        /// format of this texture is the same as the aperture texture, R32_Float.
        /// 
        /// The decision of using "blue" as the base wavelength was made to ensure that
        /// the "spectral aperture" did not clip the borders of the aperture's texture,
        /// which would have required the use of a larger texture - and hence decreased
        /// performance and higher memory usage - to avoid huge graphical artifacts. In
        /// exchange, we just lose some detail at higher wavelengths, which in practice
        /// is unnoticeable and is a considerably better tradeoff than downsampling the
        /// diffraction pattern, which leaves significant aliasing and ringing effects.
        /// </remarks>
        private readonly GraphicsResource spectralAperture;

        /// <summary>
        /// The aperture convolution filter at a given wavelength.
        /// </summary>
        /// <remarks>
        /// The contents of this texture is equal to PowerSpectrum(spectralAperture).
        /// </remarks>
        private readonly GraphicsResource spectralFilter;

        /// <summary>
        /// The convolution filter, as a superposition of spectral filters.
        /// </summary>
        /// <remarks>
        /// That is, filter = sum[spectralFilter(wavelength) * wavelengthColor].
        /// </remarks>
        private readonly GraphicsResource filter;

        /// <summary>
        /// The normalization factor for the convolution filter.
        /// </summary>
        private readonly GraphicsResource filterNormalization;

        /// <summary>
        /// The frame to convolve the filter with.
        /// </summary>
        private readonly GraphicsResource frame;

        /// <summary>
        /// The Fourier Transform engine used for generating convolution filters.
        /// </summary>
        private readonly FourierTransform filterFFT;

        /// <summary>
        /// The Fourier Transform engine used for convolving frame and filter.
        /// </summary>
        private readonly FourierTransform convolutionFFT;

        public ShaderProcessor Processor { get; private set; }

        private SurfacePass pass;

        //public SpectralTerm[] spectralTerms { get; private set; }

        private ApertureDefinition apertureDefinition;
        public ApertureDefinition ApertureDefinition
        {
            get { return apertureDefinition; }
            set
            {
                apertureDefinition = value;
                GenerateConvolutionFilter();
            }
        }

        public LensFilter(Device device, LensFilterDescription description)
        {
            //pass = new SurfacePass(device);

            Description = description;
            Device = device;

            aperture            = new GraphicsResource(Device, Description.apertureSize, Format.R32_Float);
            spectralAperture    = new GraphicsResource(Device, Description.apertureSize, Format.R32_Float);
            spectralFilter      = new GraphicsResource(Device, Description.apertureSize, Format.R32_Float);
            filter              = new GraphicsResource(Device, Description.apertureSize, Format.R32G32B32A32_Float, true, true, true);
            filterNormalization = new GraphicsResource(Device, new Size(1, 1),           Format.R32G32B32A32_Float);
            frame               = new GraphicsResource(Device, Description.frameSize, Format.R32G32B32A32_Float);

            /* We start with the identity aperture (lets all light through, no diffraction). */
            Device.ImmediateContext.ClearRenderTargetView(aperture.RT, new Color4(1, 1, 1, 1));
            
            /* Begin with some sensible spectral terms. */
            apertureDefinition = new ApertureDefinition()
            {
                spectralTerms = new SpectralTerm[] // this is the most basic one, with just enough to represent 3 colors (the driver program sets better ones)
                {
                    new SpectralTerm() { wavelength = 450, rgbFilter = new Color4(0, 0, 1, 1) },
                    new SpectralTerm() { wavelength = 525, rgbFilter = new Color4(0, 1, 0, 1) },
                    new SpectralTerm() { wavelength = 650, rgbFilter = new Color4(1, 0, 0, 1) },
                },
                observationDistance = 1.0f
            };

            Processor = new ShaderProcessor(Device);
            filterFFT = new FourierTransform(Device, description.apertureSize, false);

            /* Technically, the minimum size required to avoid wraparound is apertureSize + frameSize - (1, 1) but we get
             * HUGE speed boosts if we align the convolution dimensions to nice round numbers for the FFT implementation. */

            //convolutionFFT = new FourierTransform(Device, description.apertureSize + description.frameSize - new Size(1, 1), true);
            convolutionFFT = new FourierTransform(Device, new Size(1280, 1280), true);
        }

        private float scale = 1;
        public float Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        private void GenerateSpectralAperture(SpectralTerm spectralTerm)
        {
            DataStream stream = new DataStream(4, true, true);
            stream.Write<float>(spectralTerm.wavelength / SpectralTerm.BLUE / apertureDefinition.observationDistance);
            stream.Position = 0;

            Processor.ExecuteShader(Device, spectralAperture.RT, new ShaderResourceView[] { aperture.SRV }, stream, @"
            texture2D aperture : register(t1);

            SamplerState texSampler;

            cbuffer params
            {
	            float inverseScale;
            };

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
	            return float4(aperture.Sample(texSampler, input.uv.xy * inverseScale).xyz, 1.0f);
            }
            ");

            /*pass.Pass(Device, @"
            texture2D aperture : register(t0);

            SamplerState texSampler
            {
                Filter = MIN_MAG_MIP_LINEAR;
                AddressU = Border;
                AddressV = Border;
                BorderColor = float4(0, 0, 0, 1);
            };

            cbuffer params
            {
	            float inverseScale;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
	            return float4(aperture.Sample(texSampler, input.tex.xy * inverseScale).xyz, 1.0f);
            }
            ", spectralAperture.RT, new[] { aperture.SRV }, stream);*/

            stream.Dispose();
        }

        private void AccumulateToFilter(SpectralTerm spectralTerm)
        {
            DataStream stream = new DataStream(16, true, true);
            stream.Write<Color4>(spectralTerm.rgbFilter);
            stream.Position = 0;

            Processor.ExecuteReadbackShader(Device, filter.RT, new ShaderResourceView[] { spectralFilter.SRV }, stream, @"
            texture2D filter : register(t0);
            texture2D spectralFilter : register(t1);

            SamplerState texSampler;

            cbuffer params
            {
	            float4 wavelengthColor;
            };

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
                /* This is the spectral diffraction energy coefficient for this pixel (R32_Float). */
                float spectralCoefficient = spectralFilter.Sample(texSampler, input.uv.xy).x;

                /* This is the original filter we are accumulating into (R32G32B32A32_Float). */
                float3 filterColor = filter.Sample(texSampler, input.uv.xy).xyz;

                return float4(filterColor + spectralCoefficient * wavelengthColor, 1.0f);
            }
            ");

            stream.Dispose();
        }

        private void BlurConvolutionFilter()
        {
            /* Apply a gentle blur to the convolution filter to remove some ringing artifacts. */

            DataStream stream = new DataStream(8, true, true);
            stream.Write<int>(Description.apertureSize.Width);
            stream.Write<int>(Description.apertureSize.Height);
            stream.Position = 0;

            Processor.ExecuteReadbackShader(Device, filter.RT, null, stream, @"
            texture2D filter : register(t0);

            SamplerState texSampler;

            cbuffer params
            {
	            uint width, height;
            };

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
                const float kernel[3][3] = {{1.0f / 16.0f, 2.0f / 16.0f, 1.0f / 16.0f},
                                            {2.0f / 16.0f, 4.0f / 16.0f, 2.0f / 16.0f},
                                            {1.0f / 16.0f, 2.0f / 16.0f, 1.0f / 16.0f}};

                float3 rgb = float3(0, 0, 0);

                float dx = 1.0f / width, dy = 1.0f / height;

                for (int y = -1; y < 2; ++y)
                    for (int x = -1; x < 2; ++x)
                    {
                        float u = input.uv.x + x * dx;
                        float v = input.uv.y + y * dy;

                        rgb += filter.Sample(texSampler, float2(u, v)).xyz * kernel[x + 1][y + 1];
                    }

                return float4(rgb, 1.0f);
            }
            ");

            stream.Dispose();
        }

        private void NormalizeConvolutionFilter()
        {
            /* We must use the triangle filter mode to avoid severe loss of accuracy, as the filter is mostly black. */
            //Result result = Texture2D.FilterTexture(Device.ImmediateContext, filter.Resource, 0, FilterFlags.Triangle);

            Device.ImmediateContext.GenerateMips(filter.SRV);

            /* Here we render to a 1x1 texture to fetch the lowest mip level's value (as an RGB value). */

            Processor.ExecuteShader(Device, filterNormalization.RT, new ShaderResourceView[] { filter.SRV }, null, @"
            texture2D filterMips : register(t1);

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
                uint w, h, m;

                filterMips.GetDimensions(0, w, h, m);
                return float4(filterMips.Load(int3(0, 0, m - 1)).xyz, 1.0f);
            }
            ");

            DataStream stream = new DataStream(16, true, true);
            stream.Write<uint>((uint)Description.apertureSize.Width);
            stream.Write<uint>((uint)Description.apertureSize.Height);
            stream.Position = 0;

            /* Now we simply scale every pixel in the filter by the obtained average multiplied by the pixel count. */

            Processor.ExecuteReadbackShader(Device, filter.RT, new ShaderResourceView[] { filterNormalization.SRV }, stream, @"
            texture2D filter : register(t0);
            texture2D normTex : register(t1);

            SamplerState texSampler;

            cbuffer dims
            {
                uint width;
                uint height;
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
                input.uv.x = (input.uv.x + 0.5f) % 1;
                input.uv.y = (input.uv.y + 0.5f) % 1;

                float3 normalization = normTex.Load(int3(0, 0, 0)).xyz * width * height;
                return float4(saturate(filter.Sample(texSampler, input.uv.xy).xyz / normalization - 1e-8f), 1.0f);
            }
            ");

            stream.Dispose();

            /* At this point, the filter should be very closely normalized (to +- 1%) */
        }

        public void GenerateConvolutionFilter()
        {
            Device.ImmediateContext.ClearRenderTargetView(filter.RT, new Color4(0, 0, 0, 0));

            /* STEP 1: accumulate all spectral filters (unnormalized). */

            foreach (SpectralTerm spectralTerm in apertureDefinition.spectralTerms)
            {
                GenerateSpectralAperture(spectralTerm);

                filterFFT.GeneratePowerSpectrum(Processor, spectralFilter.RT, spectralAperture.SRV);

                Device.ImmediateContext.PixelShader.SetShaderResource(0, null);
                Device.ImmediateContext.PixelShader.SetShaderResource(1, null);

                AccumulateToFilter(spectralTerm);

                Device.ImmediateContext.PixelShader.SetShaderResource(0, null);
                Device.ImmediateContext.PixelShader.SetShaderResource(1, null);
            }

            /* STEP 2: normalize the convolution filter through mipmapping. */

            NormalizeConvolutionFilter();
        }

        public void Convolve(RenderTargetView renderTarget, ShaderResourceView frame)
        {
            convolutionFFT.Convolve(Processor, renderTarget, frame, filter.SRV, 1);
        }

        public void SetAperture(ShaderResourceView aperture)
        {
            Processor.ExecuteShader(Device, this.aperture.RT, new ShaderResourceView[] { aperture }, null, @"
            texture2D aperture : register(t1);

            SamplerState texSampler;

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
                /* Treat average aperture color as float transmittance. */
	            float3 rgb = aperture.Sample(texSampler, input.uv.xy).xyz;
	            return float4((rgb.x + rgb.y + rgb.z) / 3.0f, 0, 0, 1.0f);
            }
            ");

            GenerateConvolutionFilter();
        }

        public void SetFrame(ShaderResourceView frame)
        {
            Processor.ExecuteShader(Device, this.frame.RT, new ShaderResourceView[] { frame }, null, @"
            texture2D frame : register(t1);

            SamplerState texSampler;

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
	            return float4(frame.Sample(texSampler, input.uv.xy).xyz, 1.0f);
            }
            ");
        }

        public void RenderAperture(RenderTargetView renderTarget)
        {
            Processor.ExecuteShader(Device, renderTarget, new ShaderResourceView[] { aperture.SRV }, null, @"
            texture2D aperture : register(t1);

            SamplerState texSampler;

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
	            float t = aperture.Sample(texSampler, input.uv.xy).x; /* Transmittance at this pixel. */
	            return float4(float3(1, 1, 1) * t, 1); /* Convert aperture to a grayscale 2D texture. */
            }
            ");
        }

        public void RenderFilter(RenderTargetView renderTarget)
        {
            Processor.ExecuteShader(Device, renderTarget, new ShaderResourceView[] { filter.SRV }, null, @"
            texture2D filter : register(t1);

            SamplerState texSampler;

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
	            return float4(filter.Sample(texSampler, input.uv.xy).xyz, 1.0f);
            }
            ");
        }

        public void RenderFrame(RenderTargetView renderTarget)
        {
            Processor.ExecuteShader(Device, renderTarget, new ShaderResourceView[] { frame.SRV }, null, @"
            texture2D frame : register(t1);

            SamplerState texSampler;

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
	            return float4(frame.Sample(texSampler, input.uv.xy).xyz, 1.0f);
            }
            ");
        }

        #region IDisposable

        ~LensFilter()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                filterNormalization.Dispose();
                spectralAperture.Dispose();
                spectralFilter.Dispose();
                aperture.Dispose();
                filter.Dispose();
                frame.Dispose();

                convolutionFFT.Dispose();
                filterFFT.Dispose();

                Processor.Dispose();
                //pass.Dispose();
            }
        }

        #endregion
    }
}
