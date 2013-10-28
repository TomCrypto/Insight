using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Insight
{
    /// <summary>
    /// This class helps execute full-screen pixel shader
    /// passes on a surface, and supports UAV resources.
    /// </summary>
    public class SurfacePass : IDisposable
    {
        private class ShaderInclude : Include
        {
            public Stream Open(IncludeType type, string fileName, Stream stream)
            {
                if ((type == IncludeType.System) && (fileName == "pass"))
                {
                    /* Fetch the needed SurfacePass PixelDefinition. */
                    return new MemoryStream(Resources.PixelDefinition);
                }
                else
                {
                    /* Standard include, just get the correct shader file. */
                    String data = File.ReadAllText(fileName, Encoding.ASCII);
                    return new MemoryStream(Encoding.ASCII.GetBytes(data));
                }
            }

            public void Close(Stream stream)
            {
                stream.Close();
            }

            public IDisposable Shadow { get; set; }
            public void Dispose() { }
        }

        private static ShaderInclude includeHandler = new ShaderInclude();

        private Dictionary<String, PixelShader> shaders = new Dictionary<String, PixelShader>();
        private const ShaderFlags ShaderParams = ShaderFlags.OptimizationLevel3;
        private const int ConstantBufferSize = 1024; /* bytes */
        private RasterizerState rasterizerState;
        private VertexShader quadVertexShader;
        private Buffer constantBuffer;
        private SamplerState sampler;

        private PixelShader CompilePixelShader(Device device, String shader)
        {
            if (shaders.ContainsKey(shader)) return shaders[shader]; /* This will also enable inline shader code, by caching already compiled shaders. */
            else using (ShaderBytecode bytecode = ShaderBytecode.Compile(shader, "main", "ps_5_0", ShaderParams, EffectFlags.None, null, includeHandler))
            {
                PixelShader pixelShader = new PixelShader(device, bytecode);
                shaders.Add(shader, pixelShader);
                return pixelShader;
            }
        }

        private void SetupRasterizerState(Device device)
        {
            rasterizerState = new RasterizerState(device, new RasterizerStateDescription
            {
                CullMode = CullMode.Back,
                FillMode = FillMode.Solid,
            });
        }

        private void SetupConstantBuffer(Device device)
        {
            constantBuffer = new Buffer(device, new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = ConstantBufferSize,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
            });
        }

        private void SetupVertexShader(Device device)
        {
            using (ShaderBytecode bytecode = ShaderBytecode.Compile(Encoding.ASCII.GetString(Resources.PassVertexShader), "main",
                                                                    "vs_5_0", ShaderParams, EffectFlags.None, null, includeHandler))
            {
                quadVertexShader = new VertexShader(device, bytecode);
            }

            sampler = new SamplerState(device, new SamplerStateDescription()
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                Filter = Filter.Anisotropic,
                BorderColor = Color4.Black,
                MaximumAnisotropy = 16,
                MaximumLod = 15,
                MinimumLod = 0,
                MipLodBias = 0,
            });
        }

        private void ExecuteShaderPass(DeviceContext context)
        {
            context.Rasterizer.State = rasterizerState;
            context.VertexShader.Set(quadVertexShader);
            context.Draw(3, 0);
        }

        /// <summary>
        /// Gets the device which created this instance.
        /// </summary>
        public Device Device { get; private set; }

        /// <summary>
        /// Creates a SurfacePass instance.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        public SurfacePass(Device device)
        {
            SetupRasterizerState(device);
            SetupConstantBuffer(device);
            SetupVertexShader(device);
            Device = device;
        }

        /// <summary>
        /// The most general shader pass method.
        /// </summary>
        /// <param name="context">The device context to use.</param>
        /// <param name="shader">The pixel shader.</param>
        /// <param name="viewport">The render viewport.</param>
        /// <param name="rtv">A render target.</param>
        /// <param name="srv">Shader resources.</param>
        /// <param name="uav">Unordered access views.</param>
        /// <param name="cbuffer">A data stream to fill the constant buffer with.</param>
        public void Pass(DeviceContext context, String shader, ViewportF viewport, RenderTargetView rtv, ShaderResourceView[] srv, UnorderedAccessView[] uav, DataStream cbuffer)
        {
            if (shader == null) throw new ArgumentNullException("Shader code cannot be null.");
            context.PixelShader.Set(CompilePixelShader(Device, shader));

            if (uav != null) context.OutputMerger.SetTargets(1, uav, new[] { rtv });
            else context.OutputMerger.SetTargets(new[] { rtv });

            if (srv != null) context.PixelShader.SetShaderResources(0, srv);
            context.PixelShader.SetConstantBuffer(0, constantBuffer);
            context.Rasterizer.SetViewports(new[] { viewport });
            context.PixelShader.SetSampler(0, sampler);

            if (cbuffer != null)
            {
                DataStream stream;
                context.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None, out stream);
                cbuffer.CopyTo(stream);
                context.UnmapSubresource(constantBuffer, 0);
                stream.Dispose();
            }

            ExecuteShaderPass(context);
        }

        /// <summary>
        /// Runs a shader pass over the entire render target, with no UAV's.
        /// </summary>
        /// <param name="context">The device context to use.</param>
        /// <param name="shader">The pixel shader.</param>
        /// <param name="viewport">The render viewport.</param>
        /// <param name="rtv">The render target (must be a 2D texture).</param>
        /// <param name="srv">Shader resources.</param>
        /// <param name="cbuffer">A data stream to fill the constant buffer with.</param>
        public void Pass(DeviceContext context, String shader, ViewportF viewport, RenderTargetView rtv, ShaderResourceView[] srv, DataStream cbuffer)
        {
            Pass(context, shader, viewport, rtv, srv, null, cbuffer);
        }

        public void Pass(DeviceContext context, String shader, Size viewport, RenderTargetView rtv, ShaderResourceView[] srv, DataStream cbuffer)
        {
            Pass(context, shader, new ViewportF(0, 0, viewport.Width, viewport.Height), rtv, srv, null, cbuffer);
        }

        public void Pass(DeviceContext context, String shader, Size viewport, RenderTargetView rtv, ShaderResourceView[] srv, UnorderedAccessView[] uav, DataStream cbuffer)
        {
            Pass(context, shader, new ViewportF(0, 0, viewport.Width, viewport.Height), rtv, srv, uav, cbuffer);
        }

        #region IDisposable

        /// <summary>
        /// Destroys this SurfacePass instance.
        /// </summary>
        ~SurfacePass()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes of all used resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                sampler.Dispose();
                constantBuffer.Dispose();
                rasterizerState.Dispose();
                quadVertexShader.Dispose();
                foreach (PixelShader shader in shaders.Values) shader.Dispose();
            }
        }

        #endregion
    }
}
