using System;

using SharpDX;

namespace Sample
{
    /// <summary>
    /// Represents a mesh material.
    /// </summary>
    class Material
    {
        private String colorMap, bumpMap;
        private Vector3 kD, kS;
        private float illum;
        private float nS;

        /// <summary>
        /// Name of the color map.
        /// </summary>
        public String ColorMap
        {
            get { return colorMap; }
            set { colorMap = value; }
        }

        /// <summary>
        /// Name of the bump map.
        /// </summary>
        public String BumpMap
        {
            get { return bumpMap; }
            set { bumpMap = value; }
        }

        /// <summary>
        /// Diffuse reflectance coefficient.
        /// </summary>
        public Vector3 DiffuseReflectance
        {
            get { return kD; }
            set { kD = value; }
        }

        /// <summary>
        /// Specular reflectance coefficient.
        /// </summary>
        public Vector3 SpecularReflectance
        {
            get { return kS; }
            set { kS = value; }
        }

        /// <summary>
        /// Specular shininess coefficient.
        /// </summary>
        public float SpecularShininess
        {
            get { return nS; }
            set { nS = value; }
        }

        /// <summary>
        /// Brightness multiplier.
        /// </summary>
        public float Brightness
        {
            get { return illum; }
            set { illum = value; }
        }

        /// <summary>
        /// Creates a default material instance.
        /// </summary>
        public Material()
        {

        }

        /// <summary>
        /// Writes this Triangle instance to a stream, as v1/n1/t1 - v2/n2/t2 - v3/n3/t3 with 4-component vectors.
        /// </summary>
        /// <param name="stream">The stream (at the correct position) to which to write the triangle.</param>
        public void WriteTo(DataStream stream)
        {
            stream.Write<Vector4>(new Vector4(kD, 1.0f));
            stream.Write<Vector4>(new Vector4(kS, 1.0f));
            stream.Write<Vector4>(new Vector4(nS, nS, nS, nS));
            stream.Write<Vector4>(new Vector4(illum, illum, illum, illum));
        }

        /// <summary>
        /// Returns the size, in bytes, that this material will take in a constant buffer.
        /// </summary>
        /// <returns>Size of a Material instance, in bytes.</returns>
        public static int Size()
        {
            /* Note the maps are bound separately. */
            return sizeof(float) * 4 * 4;
        }
    }
}
