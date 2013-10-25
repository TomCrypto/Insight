using System;

using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Sample
{
    class SibenikMaterial : Material
    {
        /// <summary>
        /// Size in bytes of the material buffer.
        /// </summary>
        private static int BufferSize = 64;

        public Color3 Diffuse
        {
            get { return (Color3)Bar[Prefix + "diffuse"].Value; }
            set { Bar[Prefix + "diffuse"].Value = value; }
        }

        public Color3 Specular
        {
            get { return (Color3)Bar[Prefix + "specular"].Value; }
            set { Bar[Prefix + "specular"].Value = value; }
        }

        public Double Shininess
        {
            get { return (Double)Bar[Prefix + "shininess"].Value; }
            set { Bar[Prefix + "shininess"].Value = value; }
        }

        public Double Brightness
        {
            get { return (Double)Bar[Prefix + "brightness"].Value; }
            set { Bar[Prefix + "brightness"].Value = value; }
        }

        public String ColorMap { get; set; }

        private Buffer constantBuffer;

        private PixelShader pixelShader;

        private SamplerState sampler;

        public SibenikMaterial(Device device, TweakBar bar, String name)
            : base(device, bar, name)
        {
            bar.AddColor(Prefix + "diffuse", "Diffuse", name, new Color3(1, 1, 1));
            bar.AddColor(Prefix + "specular", "Specular", name, new Color3(1, 1, 1));
            bar.AddFloat(Prefix + "shininess", "Shininess", name, 1, 256, 64, 0.1, 2);
            bar.AddFloat(Prefix + "brightness", "Brightness", name, 0, 25, 5, 0.1, 2);

            pixelShader = Material.CompileShader(device, "sibenik");

            constantBuffer = Material.AllocateMaterialBuffer(device, BufferSize);

            sampler = new SamplerState(device, new SamplerStateDescription()
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.Anisotropic,
                BorderColor = Color4.Black,
                MaximumAnisotropy = 16,
                MaximumLod = 15,
                MinimumLod = 0,
                MipLodBias = 0,
            });
        }

        public override void BindMaterial(DeviceContext context, ResourceProxy proxy)
        {
            using (DataStream stream = new DataStream(BufferSize, true, true))
            {
                stream.Write<Vector4>(new Vector4(Diffuse, 1));
                stream.Write<Vector4>(new Vector4(Specular, 1));
                stream.Write<Vector4>(new Vector4((float)Shininess, (float)Shininess, (float)Shininess, (float)Shininess));
                stream.Write<Vector4>(new Vector4((float)Brightness, (float)Brightness, (float)Brightness, (float)Brightness));
                Material.CopyStream(context, constantBuffer, stream);
            }

            context.PixelShader.Set(pixelShader);
            context.PixelShader.SetSampler(0, sampler);
            context.PixelShader.SetConstantBuffer(2, constantBuffer);
            context.PixelShader.SetShaderResource(1, proxy[ColorMap]);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                sampler.Dispose();
                pixelShader.Dispose();
                constantBuffer.Dispose();

                //Bar.RemoveVariable("albedo");
            }
        }
    }
}
