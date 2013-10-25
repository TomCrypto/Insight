﻿using System;
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
    class Mesh : IDisposable
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
        /// Gets the name of this mesh.
        /// </summary>
        public String MeshName { get; private set; }

        /// <summary>
        /// Creates a new mesh.
        /// </summary>
        /// <param name="device">The device to use.</param>
        /// <param name="name">The name of the mesh.</param>
        /// <param name="faces">The list of triangles in the mesh.</param>
        public Mesh(Device device, String name, List<Triangle> faces)
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

            MeshName = name;
        }

        /// <summary>
        /// Renders the mesh using a camera.
        /// </summary>
        /// <param name="device">The device to use.</param>
        public void Render(Device device, DeviceContext context)
        {
            context.InputAssembler.SetVertexBuffers(0, new[] { vertexBuffer });
            context.Draw(vertices.Description.SizeInBytes / vertexBuffer.Stride, 0);
        }

        #region IDisposable

        /// <summary>
        /// Destroys this Mesh instance.
        /// </summary>
        ~Mesh()
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
                vertices.Dispose();
            }
        }

        #endregion
    }
}
