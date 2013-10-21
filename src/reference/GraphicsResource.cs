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

namespace Insight
{
    /// <summary>
    /// A texture resource which can be bound for input and output (SRV/RTV).
    /// </summary>
    public class GraphicsResource : IDisposable
    {
        /// <summary>
        /// Creates a new graphics resource.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="dimensions">The resource dimensions.</param>
        /// <param name="format">The resource's DXGI format.</param>
        /// <param name="renderTargetView">Whether to bind as RTV.</param>
        /// <param name="shaderResourceView">Whether to bind as SRV.</param>
        /// <param name="hasMipMaps">Whether to enable mip-maps for this texture.</param>
        public GraphicsResource(Device device, Size dimensions, Format format, Boolean renderTargetView = true, Boolean shaderResourceView = true, Boolean hasMipMaps = false)
        {
            if ((!renderTargetView) && (!shaderResourceView))
                throw new ArgumentException("The requested resource cannot be bound at all to the pipeline.");

            if ((hasMipMaps) && ((!renderTargetView) || (!shaderResourceView)))
                throw new ArgumentException("A resource with mipmaps must be bound as both input and output.");

            BindFlags bindFlags = (renderTargetView ? BindFlags.RenderTarget : 0) | (shaderResourceView ? BindFlags.ShaderResource : 0);
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
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                SampleDescription = new SampleDescription(1, 0),
            });

            RTV = (  renderTargetView ?   new RenderTargetView(device, Resource) : null);
            SRV = (shaderResourceView ? new ShaderResourceView(device, Resource) : null);
        }

        /// <summary>
        /// Wraps a GraphicsResource around an existing Texture2D. Once this method returns this
        /// class instance should be considered to own the resource (and so the parameter should
        /// be discarded by the caller in favor of the newly created GraphicsResource instance).
        /// </summary>
        /// <param name="resource">The Texture2D resource.</param>
        public GraphicsResource(Texture2D resource)
        {
            if ((resource.Description.BindFlags & BindFlags.RenderTarget) != 0) RTV = new RenderTargetView(resource.Device, resource);
            if ((resource.Description.BindFlags & BindFlags.ShaderResource) != 0) SRV = new ShaderResourceView(resource.Device, resource);
            Resource = resource;
        }

        /// <summary>
        /// The device associated with this graphics resource.
        /// </summary>
        public Device Device { get { return Resource.Device; } }

        /// <summary>
        /// The underlying resource (as a 2D texture).
        /// </summary>
        public Texture2D Resource { get; private set; }

        /// <summary>
        /// A render target view of this resource. Will be null if the resource is not bound as RTV.
        /// </summary>
        public RenderTargetView RTV { get; private set; }

        /// <summary>
        /// A shader resource view of this resource. Will be null if the resource is not bound as SRV.
        /// </summary>
        public ShaderResourceView SRV { get; private set; }

        /// <summary>
        /// Gets the texture's dimensions.
        /// </summary>
        public Size Dimensions
        {
            get
            {
                return GraphicsUtils.TextureSize(Resource);
            }
        }

        /// <summary>
        /// Gets the texture's format.
        /// </summary>
        public Format Format
        {
            get
            {
                return GraphicsUtils.TextureFormat(Resource);
            }
        }

        #region IDisposable

        /// <summary>
        /// Destroys this GraphicsResource instance.
        /// </summary>
        ~GraphicsResource()
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
                if (SRV != null) SRV.Dispose();
                if (RTV != null) RTV.Dispose();
                Resource.Dispose();
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
