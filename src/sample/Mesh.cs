using System;
using System.Collections.Generic;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Sample
{
    /// <summary>
    /// Represents a mesh, with a material and collection of assets.
    /// </summary>
    class Mesh
    {
        /// <summary>
        /// A buffer containing the mesh vertices.
        /// </summary>
        private Buffer vertices;

        /// <summary>
        /// A vertex buffer binding for the pipeline.
        /// </summary>
        private VertexBufferBinding vertexBuffer;

        /// <summary>
        /// Material to use to render this mesh.
        /// </summary>
        private Material material;

        /// <summary>
        /// Constant buffer for model data.
        /// </summary>
        private Buffer modelBuffer;

        /// <summary>
        /// Constant buffer for the camera.
        /// </summary>
        private Buffer cameraBuffer;

        /// <summary>
        /// Constant buffer for the material.
        /// </summary>
        private Buffer materialBuffer;

        /// <summary>
        /// A standard texture sampler.
        /// </summary>
        private SamplerState sampler;

        /// <summary>
        /// Creates a new mesh.
        /// </summary>
        /// <param name="device">The device to use.</param>
        /// <param name="meshName">The mesh name (the material to use).</param>
        /// <param name="faces">The list of triangles in the mesh.</param>
        /// <param name="material">The material for this mesh.</param>
        public Mesh(Device device, String meshName, List<Triangle> faces, Material material)
        {
            using (DataStream triangleStream = new DataStream(Triangle.Size() * faces.Count, false, true))
            {
                foreach (Triangle triangle in faces) triangle.WriteTo(triangleStream);
                triangleStream.Position = 0;

                BufferDescription description = new BufferDescription()
                {
                    Usage = ResourceUsage.Immutable,
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    SizeInBytes = Triangle.Size() * faces.Count,
                };

                vertices = new Buffer(device, triangleStream, description);
                vertexBuffer = new VertexBufferBinding(vertices, Triangle.Size() / 3, 0);
            }

            this.material = material;

            using (DataStream materialStream = new DataStream(Material.Size(), false, true))
            {
                material.WriteTo(materialStream);
                materialStream.Position = 0;

                BufferDescription description = new BufferDescription()
                {
                    SizeInBytes = Material.Size(),
                    Usage = ResourceUsage.Immutable,
                    BindFlags = BindFlags.ConstantBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                };

                materialBuffer = new Buffer(device, materialStream, description);
            }

            cameraBuffer = new Buffer(device, new BufferDescription()
            {
                SizeInBytes = Camera.Size(),
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
            });

            modelBuffer = new Buffer(device, new BufferDescription()
            {
                SizeInBytes = 512,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
            });

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

        /// <summary>
        /// Renders the mesh using a camera.
        /// </summary>
        /// <param name="device">The device to use.</param>
        /// <param name="modelToWorld">Model to world matrix.</param>
        /// <param name="camera">The camera from with to render.</param>
        /// <param name="mapCache">A map cache, for texture access.</param>
        public void Render(Device device, Matrix modelToWorld, Camera camera, MapCache mapCache)
        {
            {
                DataStream cameraStream;
                device.ImmediateContext.MapSubresource(cameraBuffer, MapMode.WriteDiscard, MapFlags.None, out cameraStream);
                camera.WriteTo(cameraStream);
                device.ImmediateContext.UnmapSubresource(cameraBuffer, 0);
                cameraStream.Dispose();
            }

            {
                DataStream modelStream;
                device.ImmediateContext.MapSubresource(modelBuffer, MapMode.WriteDiscard, MapFlags.None, out modelStream);
                modelStream.Write<Matrix>(Matrix.Transpose(modelToWorld));
                device.ImmediateContext.UnmapSubresource(modelBuffer, 0);
                modelStream.Dispose();
            }

            ShaderResourceView color = mapCache.Request(device, material.ColorMap);
            ShaderResourceView bump = mapCache.Request(device, material.BumpMap);

            device.ImmediateContext.VertexShader.SetConstantBuffers(0, new[] { modelBuffer, cameraBuffer, materialBuffer });
            device.ImmediateContext.PixelShader.SetConstantBuffers(0, new[] { modelBuffer, cameraBuffer, materialBuffer });
            device.ImmediateContext.PixelShader.SetShaderResources(0, new[] { color, bump });
            device.ImmediateContext.PixelShader.SetSamplers(0, new[] { sampler });

            device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new[] { vertexBuffer });
            device.ImmediateContext.Draw(vertices.Description.SizeInBytes / vertexBuffer.Stride, 0);
        }
    }
}
