using System;
using System.Collections.Generic;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Iridium
{
    /// <summary>
    /// This class helps execute full-screen pixel shader
    /// passes on a surface, and supports UAV resources.
    /// </summary>
    public class SurfacePass : IDisposable
    {
        private Dictionary<String, PixelShader> shaders = new Dictionary<String, PixelShader>();
        private const ShaderFlags ShaderFlag = ShaderFlags.OptimizationLevel3;
        private const int ConstantBufferSize = 16384; /* bytes */
        private RasterizerState rasterizerState;
        private VertexShader quadVertexShader;
        private Buffer constantBuffer;

        private PixelShader CompilePixelShader(Device device, String shader)
        {
            if (shaders.ContainsKey(shader)) return shaders[shader]; /* This will enable inline shader code. */
            else using (ShaderBytecode bytecode = ShaderBytecode.Compile(shader, "main", "ps_5_0", ShaderFlag))
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
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
            });
        }

        private void SetupConstantBuffer(Device device)
        {
            constantBuffer = new Buffer(device, new BufferDescription()
            {
                StructureByteStride = 16,
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = ConstantBufferSize,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
            });
        }

        private void SetupVertexShader(Device device)
        {
            String vertexShader = @"
            struct VS_OUT
            {
                float4 pos : SV_POSITION;
                float2 tex :    TEXCOORD;
            };
 
            VS_OUT main(uint id : SV_VertexID)
            {
                VS_OUT output;
                output.tex = float2((id << 1) & 2, id & 2);
                output.pos = float4(output.tex * float2(2, -2) + float2(-1, 1), 0, 1);
                return output;
            }";

            using (ShaderBytecode bytecode = ShaderBytecode.Compile(vertexShader, "main", "vs_5_0", ShaderFlag))
            {
                quadVertexShader = new VertexShader(device, bytecode);
            }
        }

        private void ExecuteShaderPass(DeviceContext context)
        {
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.VertexShader.Set(quadVertexShader);
            context.Draw(3, 0);
        }

        /// <summary>
        /// Creates a SurfacePass instance.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        public SurfacePass(Device device)
        {
            SetupRasterizerState(device);
            SetupConstantBuffer(device);
            SetupVertexShader(device);
        }

        /// <summary>
        /// The most general shader pass method.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="shader">The pixel shader.</param>
        /// <param name="viewport">The render viewport.</param>
        /// <param name="rtv">A render target.</param>
        /// <param name="srv">Shader resources.</param>
        /// <param name="uav">Unordered access views.</param>
        /// <param name="cbuffer">A data stream to fill the constant buffer with.</param>
        public void Pass(Device device, String shader, ViewportF viewport, RenderTargetView rtv, ShaderResourceView[] srv, UnorderedAccessView[] uav, DataStream cbuffer)
        {
            if (shader == null) throw new ArgumentNullException("Shader code cannot be null.");
            device.ImmediateContext.PixelShader.Set(CompilePixelShader(device, shader));

            if (uav != null) device.ImmediateContext.OutputMerger.SetTargets(1, uav, new[] { rtv });
            else device.ImmediateContext.OutputMerger.SetTargets(new[] { rtv });

            if (srv != null) device.ImmediateContext.PixelShader.SetShaderResources(0, srv);
            device.ImmediateContext.PixelShader.SetConstantBuffer(0, constantBuffer);
            device.ImmediateContext.Rasterizer.SetViewports(new[] { viewport });

            if (cbuffer != null)
            {
                DataStream stream;
                device.ImmediateContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None, out stream);
                cbuffer.CopyTo(stream);
                device.ImmediateContext.UnmapSubresource(constantBuffer, 0);
                stream.Dispose();
            }

            ExecuteShaderPass(device.ImmediateContext);
        }

        /// <summary>
        /// Runs a shader pass over the entire render target.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="shader">The pixel shader.</param>
        /// <param name="rtv">The render target (must be a 2D texture).</param>
        /// <param name="srv">Shader resources.</param>
        /// <param name="uav">Unordered access views.</param>
        /// <param name="cbuffer">A data stream to fill the constant buffer with.</param>
        public void Pass(Device device, String shader, RenderTargetView rtv, ShaderResourceView[] srv, UnorderedAccessView[] uav, DataStream cbuffer)
        {
            int w = rtv.Resource.QueryInterface<Texture2D>().Description.Width;
            int h = rtv.Resource.QueryInterface<Texture2D>().Description.Height;

            ViewportF viewport = new ViewportF(0, 0, w, h);
            Pass(device, shader, viewport, rtv, srv, uav, cbuffer);
        }

        /// <summary>
        /// Runs a shader pass over the entire render target, with no UAV's.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="shader">The pixel shader.</param>
        /// <param name="rtv">The render target (must be a 2D texture).</param>
        /// <param name="srv">Shader resources.</param>
        /// <param name="cbuffer">A data stream to fill the constant buffer with.</param>
        public void Pass(Device device, String shader, RenderTargetView rtv, ShaderResourceView[] srv, DataStream cbuffer)
        {
            Pass(device, shader, rtv, srv, null, cbuffer);
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
                constantBuffer.Dispose();
                rasterizerState.Dispose();
                quadVertexShader.Dispose();
                foreach (PixelShader shader in shaders.Values) shader.Dispose();
            }
        }

        #endregion
    }
}
