﻿using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;

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

    /// <summary>
    /// Represents a 3D triangle.
    /// </summary>
    class Triangle
    {
        private Vector3 v1, v2, v3, n1, n2, n3, t1, t2, t3;

        /// <summary>
        /// Creates a new triangle instance.
        /// </summary>
        /// <param name="v1">1st vertex position.</param>
        /// <param name="v2">2nd vertex position.</param>
        /// <param name="v3">3rd vertex position.</param>
        /// <param name="n1">1st vertex normal.</param>
        /// <param name="n2">2nd vertex normal.</param>
        /// <param name="n3">3rd vertex normal.</param>
        /// <param name="t1">1st texture coordinates.</param>
        /// <param name="t2">2nd texture coordinates.</param>
        /// <param name="t3">3rd texture coordinates.</param>
        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3,
                        Vector3 n1, Vector3 n2, Vector3 n3,
                        Vector3 t1, Vector3 t2, Vector3 t3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;

            this.n1 = n1;
            this.n2 = n2;
            this.n3 = n3;

            this.t1 = t1;
            this.t2 = t2;
            this.t3 = t3;
        }

        /// <summary>
        /// Computes a vertex normal from a list of face normals
        /// from all triangles adjacent to said vertex.
        /// </summary>
        /// <param name="faceNormals">Adjacent face normals.</param>
        /// <returns>The estimated vertex normal.</returns>
        public static Vector3 ComputeNormal(List<Vector3> faceNormals)
        {
            Vector3 avg = Vector3.Zero;

            foreach (Vector3 faceNormal in faceNormals)
            {
                avg = Vector3.Add(avg, faceNormal);
            }
            
            return Vector3.Normalize(avg);
        }

        /// <summary>
        /// Writes this Triangle instance to a stream, as v1/n1/t1 - v2/n2/t2 - v3/n3/t3 with 4-component vectors.
        /// </summary>
        /// <param name="stream">The stream (at the correct position) to which to write the triangle.</param>
        public void WriteTo(DataStream stream)
        {
            stream.Write<Vector4>(new Vector4(v1, 1.0f));
            stream.Write<Vector4>(new Vector4(n1, 1.0f));
            stream.Write<Vector4>(new Vector4(t1, 1.0f));
            stream.Write<Vector4>(new Vector4(v2, 1.0f));
            stream.Write<Vector4>(new Vector4(n2, 1.0f));
            stream.Write<Vector4>(new Vector4(t2, 1.0f));
            stream.Write<Vector4>(new Vector4(v3, 1.0f));
            stream.Write<Vector4>(new Vector4(n3, 1.0f));
            stream.Write<Vector4>(new Vector4(t3, 1.0f));
        }

        /// <summary>
        /// Returns the size, in bytes, that a triangle will take in the vertex buffer.
        /// </summary>
        /// <returns>Size of a Triangle instance, in bytes.</returns>
        public static int Size()
        {
            return (sizeof(float) * 4) * 3 * 3;
        }
    }

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
        /// Constant buffer for the material.
        /// </summary>
        private Buffer materialBuffer;

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

            sampler = new SamplerState(device, new SamplerStateDescription()
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                BorderColor = Color4.Black,
                ComparisonFunction = Comparison.Always,
                Filter = Filter.Anisotropic,
                MaximumAnisotropy = 16,
                MaximumLod = 15,
                MinimumLod = 0,
                MipLodBias = 0
            });
        }

        public void Render(Device device, Camera camera, MapCache mapCache)
        {
            ShaderResourceView color = (material.ColorMap == null ? null : mapCache.Request(device, material.ColorMap));
            ShaderResourceView bump  = ( material.BumpMap == null ? null : mapCache.Request(device, material.BumpMap));

            device.ImmediateContext.PixelShader.SetConstantBuffer(1, materialBuffer);
            device.ImmediateContext.PixelShader.SetShaderResource(0, color);
            device.ImmediateContext.PixelShader.SetShaderResource(1, bump);

            device.ImmediateContext.PixelShader.SetSampler(0, sampler);
            
            device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new[] { vertexBuffer });

            device.ImmediateContext.Draw(vertices.Description.SizeInBytes / vertexBuffer.Stride, 0);
        }
    }

    /// <summary>
    /// Utility class to load and cache color/bump maps.
    /// </summary>
    class MapCache
    {
        private Dictionary<String, ShaderResourceView> cache = new Dictionary<String, ShaderResourceView>();

        /// <summary>
        /// Requests a map by a given name.
        /// </summary>
        /// <param name="device">The device to use.</param>
        /// <param name="mapName">The map's name.</param>
        /// <returns>The map view (as a texture SRV).</returns>
        public ShaderResourceView Request(Device device, String mapName)
        {
            if (!cache.ContainsKey(mapName))
                cache.Add(mapName, LoadMap(device, mapName));

            return cache[mapName];
        }

        private ShaderResourceView LoadMap(Device device, String mapName)
        {
            String path = Settings.TextureDir + mapName;
            Texture2D texture = (Texture2D)Texture2D.FromFile(device, path);
            ShaderResourceView view = new ShaderResourceView(device, texture);

            return view;
        }
    }

    /// <summary>
    /// Represents a single model with possibly many meshes, each with
    /// their own material, required assets, and a model view matrix.
    /// </summary>
    class Model
    {
        /// <summary>
        /// List of meshes in the model.
        /// </summary>
        private List<Mesh> meshes = new List<Mesh>();

        /// <summary>
        /// Cache to store all the textures.
        /// </summary>
        private MapCache mapCache = new MapCache();

        /// <summary>
        /// Loads a model from an OBJ file.
        /// </summary>
        /// <param name="device">The device to use.</param>
        /// <param name="modelName">The model name.</param>
        public Model(Device device, String modelName)
        {
            LoadModel(device, File.ReadLines(Settings.ModelDir + modelName + Settings.ModelExt),
                              File.ReadLines(Settings.MaterialDir + modelName + Settings.MaterialExt));
        }

        private void LoadModel(Device device, IEnumerable<String> geometry, IEnumerable<String> materials)
        {
            /* Step 1 -- Parse every vertex (+ UV's) of the model into a list. */

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> texCoord = new List<Vector3>();

            foreach (String line in geometry)
            {
                string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); if (tokens.Length < 4) continue;
                if (tokens[0].Equals("v")) vertices.Add(new Vector3(Single.Parse(tokens[1]), Single.Parse(tokens[2]), Single.Parse(tokens[3])));
                if (tokens[0].Equals("vt")) texCoord.Add(new Vector3(Single.Parse(tokens[1]), Single.Parse(tokens[2]), Single.Parse(tokens[3])));
            }

            /* Step 2 -- Find every triangle adjacent to any vertex. */

            Dictionary<Int32, List<Vector3>> adjacency = new Dictionary<Int32, List<Vector3>>();
            for (int t = 0; t < vertices.Count; ++t) adjacency.Add(t, new List<Vector3>());

            foreach (String line in geometry)
            {
                string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 4) continue;
                if (tokens[0].Equals("f"))
                {
                    int i1 = Int32.Parse(tokens[1].Split('/')[0]) - 1;
                    int i2 = Int32.Parse(tokens[2].Split('/')[0]) - 1;
                    int i3 = Int32.Parse(tokens[3].Split('/')[0]) - 1;

                    Vector3 v1 = vertices[i1];
                    Vector3 v2 = vertices[i2];
                    Vector3 v3 = vertices[i3];

                    Vector3 normal = Vector3.Cross(Vector3.Subtract(v2, v1), Vector3.Subtract(v3, v1));

                    adjacency[i1].Add(normal);
                    adjacency[i2].Add(normal);
                    adjacency[i3].Add(normal);
                }
            }

            /* Step 3 -- Derive vertex normals using adjacency information. */

            List<Vector3> normals = new List<Vector3>();

            for (int t = 0; t < vertices.Count; ++t)
                normals.Add(Triangle.ComputeNormal(adjacency[t]));

            /* Step 4 -- Locate each mesh and extract its triangle list. */

            Dictionary<String, List<Triangle>> meshFaces = new Dictionary<String, List<Triangle>>();

            foreach (String line in geometry)
            {
                string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 2) continue;
                if (tokens[0].Equals("usemtl"))
                    if (!meshFaces.ContainsKey(tokens[1]))
                        meshFaces.Add(tokens[1], new List<Triangle>());
            }

            String currentMaterial = "";

            foreach (String line in geometry)
            {
                string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 1) continue;

                if (tokens[0].Equals("usemtl"))
                {
                    if (tokens.Length < 2) continue;
                    currentMaterial = tokens[1];
                }
                else if (tokens[0].Equals("f"))
                {
                    if (tokens.Length == 4) // Triangle face
                    {
                        int v1 = Int32.Parse(tokens[1].Split('/')[0]) - 1;
                        int v2 = Int32.Parse(tokens[2].Split('/')[0]) - 1;
                        int v3 = Int32.Parse(tokens[3].Split('/')[0]) - 1;

                        int t1 = Int32.Parse(tokens[1].Split('/')[1]) - 1;
                        int t2 = Int32.Parse(tokens[2].Split('/')[1]) - 1;
                        int t3 = Int32.Parse(tokens[3].Split('/')[1]) - 1;

                        meshFaces[currentMaterial].Add(new Triangle(vertices[v1], vertices[v2], vertices[v3],
                                                                     normals[v1],  normals[v2],  normals[v3],
                                                                    texCoord[t1], texCoord[t2], texCoord[t3]));
                    }
                    else if (tokens.Length == 5) // Quad face
                    {
                        int v1 = Int32.Parse(tokens[1].Split('/')[0]) - 1;
                        int v2 = Int32.Parse(tokens[2].Split('/')[0]) - 1;
                        int v3 = Int32.Parse(tokens[3].Split('/')[0]) - 1;
                        int v4 = Int32.Parse(tokens[4].Split('/')[0]) - 1;

                        int t1 = Int32.Parse(tokens[1].Split('/')[1]) - 1;
                        int t2 = Int32.Parse(tokens[2].Split('/')[1]) - 1;
                        int t3 = Int32.Parse(tokens[3].Split('/')[1]) - 1;
                        int t4 = Int32.Parse(tokens[4].Split('/')[1]) - 1;

                        meshFaces[currentMaterial].Add(new Triangle(vertices[v1], vertices[v2], vertices[v3],
                                                                     normals[v1],  normals[v2],  normals[v3],
                                                                    texCoord[t1], texCoord[t2], texCoord[t3]));

                        meshFaces[currentMaterial].Add(new Triangle(vertices[v1], vertices[v3], vertices[v4],
                                                                     normals[v1],  normals[v3],  normals[v4],
                                                                    texCoord[t1], texCoord[t3], texCoord[t4]));
                    }
                }
            }

            /* Step 5 -- Create each mesh instance from mesh name and faces. */

            foreach (String meshName in meshFaces.Keys)
            {
                if (meshName.Equals("sprljci")) continue; // temporary

                Material material = ParseMaterial(meshName, materials);
                meshes.Add(new Mesh(device, meshName, meshFaces[meshName], material));
            }
        }

        private Material ParseMaterial(String materialName, IEnumerable<String> materials)
        {
            Material material = new Material();
            bool located = false;

            foreach (String line in materials)
            {
                string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 1) continue;
                if (tokens[0].Equals("newmtl"))
                {
                    if (tokens.Length < 2) continue;
                    located = tokens[1].Equals(materialName);
                }

                if (located)
                {
                    switch (tokens[0])
                    {
                        case "\tNs": 
                            material.SpecularShininess = Single.Parse(tokens[1]);
                            break;

                        case "\tillum":
                            material.Brightness = Single.Parse(tokens[1]);
                            break;

                        case "\tKd":
                            material.DiffuseReflectance = new Vector3(Single.Parse(tokens[1]),
                                                                      Single.Parse(tokens[2]),
                                                                      Single.Parse(tokens[3]));
                            break;

                        case "\tKs":
                            material.SpecularReflectance = new Vector3(Single.Parse(tokens[1]),
                                                                       Single.Parse(tokens[2]),
                                                                       Single.Parse(tokens[3]));
                            break;

                        case "\tmap_Kd":
                            material.ColorMap = tokens[1];
                            break;

                        case "\tmap_bump":
                            material.BumpMap = tokens[1];
                            break;
                    }
                }
            }

            return material;
        }

        public void Render(Device device, Camera camera)
        {
            foreach (Mesh mesh in meshes) mesh.Render(device, camera, mapCache);
        }
    }
}
