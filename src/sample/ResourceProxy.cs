using System;
using System.Collections.Generic;

using SharpDX.Direct3D11;

namespace Sample
{
    /// <summary>
    /// Proxy which lazily loads shader resources
    /// such as textures and cube maps, and makes
    /// them available to the graphics pipeline.
    /// </summary>
    public class ResourceProxy : IDisposable
    {
        /// <summary>
        /// Reference to a resource and its resource
        /// view. This is a convenience class only.
        /// </summary>
        private class ResourceReference : IDisposable
        {
            /// <summary>
            /// Gets the shader resource view for this resource.
            /// </summary>
            public ShaderResourceView ResourceView { get; private set; }

            /// <summary>
            /// Gets this resource.
            /// </summary>
            public Resource Resource { get; private set; }

            /// <summary>
            /// Creates a new ResourceReference.
            /// </summary>
            /// <param name="device">The graphics device.</param>
            /// <param name="resource">The resource.</param>
            public ResourceReference(Device device, Resource resource)
            {
                ResourceView = new ShaderResourceView(device, resource);
                Resource = resource;
            }

            #region IDisposable

            /// <summary>
            /// Destroys this ResourceReference instance.
            /// </summary>
            ~ResourceReference()
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
                    ResourceView.Dispose();
                    Resource.Dispose();
                }
            }

            #endregion
        }

        /// <summary>
        /// Gets the device associated to this ResourceProxy.
        /// </summary>
        public Device Device { get; private set; }

        private Dictionary<String, ResourceReference> references = new Dictionary<String, ResourceReference>();

        /// <summary>
        /// Creates a new ResourceProxy instance associated with
        /// a given graphics device (for resource management).
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        public ResourceProxy(Device device)
        {
            Device = device;
        }

        /// <summary>
        /// Gets a resource view from a resource name. Note that this property will return
        /// null if and only if the resource name is null (for consistency with DirectX).
        /// </summary>
        /// <param name="name">The resource name, e.g. "texture.png".</param>
        /// <returns>The resource view. Throws an exception if the resource does not exist.</returns>
        public ShaderResourceView this[String name]
        {
            get
            {
                if (name == null) return null;

                if (!references.ContainsKey(name)) AddResource(name);

                return references[name].ResourceView;
            }
        }

        /// <summary>
        /// Adds a resource to the proxy, with specific loading information.
        /// </summary>
        /// <param name="name">The resource name.</param>
        /// <param name="loadInformation">Image loading information.</param>
        public void AddResource(String name, ImageLoadInformation loadInformation)
        {
            references.Add(name, new ResourceReference(Device, Resource.FromFile(Device, name, loadInformation)));
        }

        /// <summary>
        /// Adds a resource to the proxy. You do not have to call this, you
        /// can just ask for the resource, and the proxy will automatically
        /// load it for you if it hasn't been loaded yet.
        /// </summary>
        /// <param name="name">The resource name.</param>
        public void AddResource(String name)
        {
            references.Add(name, new ResourceReference(Device, Resource.FromFile(Device, name)));
        }

        #region IDisposable

        /// <summary>
        /// Destroys this ResourceProxy instance.
        /// </summary>
        ~ResourceProxy()
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
                foreach (ResourceReference reference in references.Values) reference.Dispose();
            }
        }

        #endregion
    }
}
