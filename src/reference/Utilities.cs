using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using Resource = SharpDX.Direct3D11.Resource;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace Iridium
{
    /// <summary>
    /// A graphics resource which can be bound for input and output (SRV/RT).
    /// </summary>
    public class GraphicsResource : IDisposable
    {
        /// <summary>
        /// The underlying resource (as a 2D texture).
        /// </summary>
        public Texture2D Resource { get; private set; }

        /// <summary>
        /// A render target view of this resource.
        /// </summary>
        /// <remarks>
        /// Will be null if the resource is not bound as RT.
        /// </remarks>
        public RenderTargetView RT { get; private set; }

        /// <summary>
        /// A shader resource view of this resource.
        /// </summary>
        /// <remarks>
        /// Will be null if the resource is not bound as SRV.
        /// </remarks>
        public ShaderResourceView SRV { get; private set; }

        /// <summary>
        /// Creates a new graphics resource.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="dimensions">The resource dimensions.</param>
        /// <param name="format">The resource's DXGI format.</param>
        /// <param name="renderTarget">Whether to bind as RT.</param>
        /// <param name="shaderResourceView">Whether to bind as SRV.</param>
        public GraphicsResource(Device device, Size dimensions, Format format, Boolean renderTarget = true, Boolean shaderResourceView = true, Boolean hasMipMaps = false, Boolean staging = false)
        {
            //if ((!renderTarget) && (!shaderResourceView))
            //    throw new ArgumentException("Requested resource cannot be bound at all.");

            if ((hasMipMaps) && ((!renderTarget) || (!shaderResourceView)))
                throw new ArgumentException("A resource with mipmaps must be bound as both input and output.");

            BindFlags bindFlags = (renderTarget ? BindFlags.RenderTarget : 0) | (shaderResourceView ? BindFlags.ShaderResource : 0);
            ResourceOptionFlags optionFlags = (hasMipMaps ? ResourceOptionFlags.GenerateMipMaps : 0);
            int mipLevels = (hasMipMaps ? GraphicsUtils.MipLevels(dimensions) : 1);

            Resource = new Texture2D(device, new Texture2DDescription()
            {
                Format = format,
                BindFlags = bindFlags,
                Width = dimensions.Width,
                Height = dimensions.Height,

                ArraySize = 1,
                MipLevels = mipLevels,
                OptionFlags = optionFlags,
                Usage = staging ? ResourceUsage.Staging : ResourceUsage.Default,
                CpuAccessFlags = (staging ? CpuAccessFlags.Read : CpuAccessFlags.None),
                SampleDescription = new SampleDescription(1, 0),
            });

            RT = (renderTarget ? new RenderTargetView(device, Resource) : null);
            SRV = (shaderResourceView ? new ShaderResourceView(device, Resource) : null);
        }

        #region IDisposable

        ~GraphicsResource()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (SRV != null) SRV.Dispose();
                if (RT != null) RT.Dispose();
                Resource.Dispose();
            }
        }

        #endregion
    }

    public class ShaderProcessor : IDisposable
    {
        /// <summary>
        /// Used to cache "clone" textures for use in readback shaders.
        /// </summary>
        /// <remarks>
        /// The resources stored in this dictionary are read-only (SRV).
        /// </remarks>
        Dictionary<Texture2DDescription, GraphicsResource> resourceCache = new Dictionary<Texture2DDescription, GraphicsResource>();

        Dictionary<String, PixelShader> shaderCache = new Dictionary<String, PixelShader>();

        SamplerState sampler;

        Buffer constantBuffer;

        VertexShader quadShader;

        InputLayout vertexLayout;

        RasterizerState rasterizerState;

        VertexBufferBinding quadVertices;

        private void SetupFullscreenQuad(Device device)
        {
            Buffer vertexBuffer = new Buffer(device, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 32 * 6,
                StructureByteStride = 32,
                Usage = ResourceUsage.Dynamic
            });

            DataStream stream;
            device.ImmediateContext.MapSubresource(vertexBuffer, MapMode.WriteDiscard, MapFlags.None, out stream);

            stream.Write<Vector4>(new Vector4(-1, -1, 0.5f, 1.0f));
            stream.Write<Vector4>(new Vector4(0, 1, 0, 1.0f));
            stream.Write<Vector4>(new Vector4(+1, -1, 0.5f, 1.0f));
            stream.Write<Vector4>(new Vector4(1, 1, 0, 1.0f));
            stream.Write<Vector4>(new Vector4(+1, +1, 0.5f, 1.0f));
            stream.Write<Vector4>(new Vector4(1, 0, 0, 1.0f));
            stream.Write<Vector4>(new Vector4(-1, -1, 0.5f, 1.0f));
            stream.Write<Vector4>(new Vector4(0, 1, 0, 1.0f));
            stream.Write<Vector4>(new Vector4(+1, +1, 0.5f, 1.0f));
            stream.Write<Vector4>(new Vector4(1, 0, 0, 1.0f));
            stream.Write<Vector4>(new Vector4(-1, +1, 0.5f, 1.0f));
            stream.Write<Vector4>(new Vector4(0, 0, 0, 1.0f));

            device.ImmediateContext.UnmapSubresource(vertexBuffer, 0);
            quadVertices = new VertexBufferBinding(vertexBuffer, 32, 0);
        }

        private PixelShader CompilePixelShader(Device device, String shader)
        {
            ShaderBytecode bytecode = ShaderBytecode.Compile(shader, "main", "ps_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None);
            PixelShader pixelShader = new PixelShader(device, bytecode);
            bytecode.Dispose();
            return pixelShader;
        }

        private void SetupQuadShader(Device device)
        {
            ShaderBytecode bytecode = ShaderBytecode.Compile(@"struct VS_IN
                                                              {
                                                              float4 pos : POSITION;
                                                              float4 uv  : TEXCOORD;
                                                              };

                                                              struct PS_IN
                                                              {
                                                              float4 pos : SV_POSITION;
                                                              float4 uv  : TEXCOORD;
                                                              };

                                                              PS_IN main(VS_IN input)
                                                              {
                                                                  PS_IN output = (PS_IN)0;
                                                                  output.pos = input.pos;
                                                                  output.uv = input.uv;
                                                                  return output;
                                                              }", "main", "vs_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None);

            quadShader = new VertexShader(device, bytecode);

            vertexLayout = new InputLayout(device, bytecode, new InputElement[]
                                                             {
                                                                 new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                                                                 new InputElement("TEXCOORD", 0, Format.R32G32B32A32_Float, 16, 0)
                                                             });

            bytecode.Dispose();
        }

        private void PreparePersistentShaderResources(Device device)
        {
            sampler = new SamplerState(device, new SamplerStateDescription
            {
                Filter = SharpDX.Direct3D11.Filter.MinMagMipLinear,
                ComparisonFunction = Comparison.Always,
                BorderColor = new Color4(0, 0, 0, 0),
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                MaximumAnisotropy = 16,
                MaximumLod = 0,
                MinimumLod = 0,
                MipLodBias = 0,
            });

            constantBuffer = new Buffer(device, new BufferDescription()
            {
                SizeInBytes = 1024,
                StructureByteStride = 16,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
            });

            rasterizerState = new RasterizerState(device, new RasterizerStateDescription
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid
            });

            device.ImmediateContext.VertexShader.Set(quadShader);
            device.ImmediateContext.Rasterizer.State = rasterizerState;
            device.ImmediateContext.InputAssembler.InputLayout = vertexLayout;
            device.ImmediateContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding[] { quadVertices });
        }

        private GraphicsResource AllocateReadbackTexture(Device device, Size size, Format format, Boolean hasMips)
        {
            return new GraphicsResource(device, size, format, true, true, hasMips, false);
        }

        private void RunShader(Device device, Size renderDimensions, PixelShader shader, RenderTargetView renderTarget, UnorderedAccessView[] uavTargets, ShaderResourceView readback, ShaderResourceView[] inputs)
        {
            ViewportF viewport = new ViewportF(0, 0, renderDimensions.Width, renderDimensions.Height);

            if (uavTargets == null) device.ImmediateContext.OutputMerger.SetTargets(new[] { renderTarget });
            else device.ImmediateContext.OutputMerger.SetTargets(1, uavTargets, new RenderTargetView[] { renderTarget });
            device.ImmediateContext.Rasterizer.SetViewports(new[] { viewport });

            device.ImmediateContext.PixelShader.Set(shader);
            device.ImmediateContext.PixelShader.SetSampler(0, sampler);
            device.ImmediateContext.PixelShader.SetShaderResource(0, readback);
            device.ImmediateContext.PixelShader.SetConstantBuffer(0, constantBuffer);
            if (inputs != null) device.ImmediateContext.PixelShader.SetShaderResources(1, inputs);

            PreparePersistentShaderResources(device);
            device.ImmediateContext.Draw(6, 0);
        }

        public ShaderProcessor(Device device)
        {
            SetupFullscreenQuad(device);
            SetupQuadShader(device);
            PreparePersistentShaderResources(device);
        }

        /// <summary>
        /// Executes a shader without readback.
        /// </summary>
        public void ExecuteShader(Device device, RenderTargetView renderTarget, ShaderResourceView[] inputs, DataStream bufferData, String shader)
        {
            if (bufferData != null)
            {
                DataStream stream;
                device.ImmediateContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None, out stream);
                bufferData.CopyTo(stream);
                device.ImmediateContext.UnmapSubresource(constantBuffer, 0);
            }

            Size targetSize = GraphicsUtils.TextureSize(renderTarget.Resource);
            Format targetFormat = GraphicsUtils.TextureFormat(renderTarget.Resource);

            PixelShader pixelShader;

            if (!shaderCache.TryGetValue(shader, out pixelShader))
            {
                pixelShader = CompilePixelShader(device, shader);
                shaderCache.Add(shader, pixelShader);
            }

            RunShader(device, targetSize, pixelShader, renderTarget, null, null, inputs);
        }

        /// <summary>
        /// Executes a shader with readback in t0.
        /// </summary>
        public void ExecuteReadbackShader(Device device, RenderTargetView renderTarget, ShaderResourceView[] inputs, DataStream bufferData, String shader)
        {
            if (bufferData != null)
            {
                DataStream stream;
                device.ImmediateContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None, out stream);
                bufferData.CopyTo(stream);
                device.ImmediateContext.UnmapSubresource(constantBuffer, 0);
            }

            Size targetSize = GraphicsUtils.TextureSize(renderTarget.Resource);
            Format targetFormat = GraphicsUtils.TextureFormat(renderTarget.Resource);
            Texture2DDescription key = renderTarget.Resource.QueryInterface<Texture2D>().Description;

            GraphicsResource readback;
            
            if (!resourceCache.TryGetValue(key, out readback))
            {
                readback = AllocateReadbackTexture(device, targetSize, targetFormat, key.MipLevels != 1);
                resourceCache.Add(key, readback);
            }

            PixelShader pixelShader;

            if (!shaderCache.TryGetValue(shader, out pixelShader))
            {
                pixelShader = CompilePixelShader(device, shader);
                shaderCache.Add(shader, pixelShader);
            }

            device.ImmediateContext.CopyResource(renderTarget.Resource, readback.Resource);
            RunShader(device, targetSize, pixelShader, renderTarget, null, readback.SRV, inputs);
        }

        /// <summary>
        /// Executes a shader without readback and with UAV.
        /// </summary>
        public void ExecuteShader(Device device, RenderTargetView renderTarget, UnorderedAccessView uavTarget, ShaderResourceView[] inputs, DataStream bufferData, String shader)
        {
            if (bufferData != null)
            {
                DataStream stream;
                device.ImmediateContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None, out stream);
                bufferData.CopyTo(stream);
                device.ImmediateContext.UnmapSubresource(constantBuffer, 0);
            }

            Size targetSize = GraphicsUtils.TextureSize(renderTarget.Resource);
            Format targetFormat = GraphicsUtils.TextureFormat(renderTarget.Resource);

            PixelShader pixelShader;

            if (!shaderCache.TryGetValue(shader, out pixelShader))
            {
                pixelShader = CompilePixelShader(device, shader);
                shaderCache.Add(shader, pixelShader);
            }

            RunShader(device, targetSize, pixelShader, renderTarget, new UnorderedAccessView[] { uavTarget }, null, inputs);
        }

        /// <summary>
        /// Executes a shader without readback and with multiple UAV's.
        /// </summary>
        public void ExecuteShader(Device device, RenderTargetView renderTarget, UnorderedAccessView[] uavTargets, ShaderResourceView[] inputs, DataStream bufferData, String shader)
        {
            if (bufferData != null)
            {
                DataStream stream;
                device.ImmediateContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None, out stream);
                bufferData.CopyTo(stream);
                device.ImmediateContext.UnmapSubresource(constantBuffer, 0);
            }

            Size targetSize = GraphicsUtils.TextureSize(renderTarget.Resource);
            Format targetFormat = GraphicsUtils.TextureFormat(renderTarget.Resource);

            PixelShader pixelShader;

            if (!shaderCache.TryGetValue(shader, out pixelShader))
            {
                pixelShader = CompilePixelShader(device, shader);
                shaderCache.Add(shader, pixelShader);
            }

            RunShader(device, targetSize, pixelShader, renderTarget, uavTargets, null, inputs);
        }

        public void UnbindShaderResources(Device device)
        {
            // do nothing for now
        }

        #region IDisposable

        ~ShaderProcessor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (GraphicsResource resource in resourceCache.Values)
                    resource.Dispose();

                foreach (PixelShader shader in shaderCache.Values)
                    shader.Dispose();

                sampler.Dispose();
                quadShader.Dispose();
                vertexLayout.Dispose();
                constantBuffer.Dispose();
                rasterizerState.Dispose();
                quadVertices.Buffer.Dispose();
            }
        }

        #endregion
    }

    public static class GraphicsUtils
    {
        public static Size TextureSize(Resource resource)
        {
            return new Size(resource.QueryInterface<Texture2D>().Description.Width,
                            resource.QueryInterface<Texture2D>().Description.Height);
        }

        public static Format TextureFormat(Resource resource)
        {
            return resource.QueryInterface<Texture2D>().Description.Format;
        }

        public static int MipLevels(Size size)
        {
            return (int)Math.Floor(Math.Log(Math.Max(size.Width, size.Height), 2));
        }
    }
}
