﻿using System;

using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Sample
{
    class SkyMaterial : Material
    {
        /// <summary>
        /// Size in bytes of the material buffer.
        /// </summary>
        private static int BufferSize = 16;

        public Double Brightness
        {
            get { return (Double)Bar[Prefix + "brightness"].Value; }
            set { Bar[Prefix + "brightness"].Value = value; }
        }

        public String ColorMap { get; set; }

        private Buffer constantBuffer;

        private PixelShader pixelShader;

        private SamplerState sampler;

        public SkyMaterial(Device device, TweakBar bar, String name)
            : base(device, bar, name)
        {
            bar.AddFloat(Prefix + "brightness", "Brightness", name, 0, 100, 30, 0.1, 2);

            pixelShader = Material.CompileShader(device, "skydome");

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
                stream.Write<float>((float)Brightness);
                Material.CopyStream(context, constantBuffer, stream);
            }

            context.PixelShader.Set(pixelShader);
            context.PixelShader.SetSampler(0, sampler);
            context.PixelShader.SetConstantBuffer(2, constantBuffer);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                sampler.Dispose();
                pixelShader.Dispose();
                constantBuffer.Dispose();

                Bar.RemoveVariable(Prefix + "brightness");
            }
        }
    }
}
