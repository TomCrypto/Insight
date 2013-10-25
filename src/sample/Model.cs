using System;
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
    /// Represents a single model with possibly many meshes, each with
    /// their own material, required assets, and a model view matrix.
    /// </summary>
    class Model : IDisposable
    {
        /// <summary>
        /// Vertex layout to use for the vertex shader.
        /// </summary>
        private static InputElement[] VertexLayout = new[] { new InputElement("POSITION", 0, Format.R32G32B32A32_Float,  0, 0),
                                                             new InputElement("NORMAL",   0, Format.R32G32B32A32_Float, 16, 0),
                                                             new InputElement("TEXCOORD", 0, Format.R32G32B32A32_Float, 32, 0) };

        /// <summary>
        /// List of meshes in the model.
        /// </summary>
        private List<Mesh> meshes = new List<Mesh>();

        /// <summary>
        /// Scale of the model.
        /// </summary>
        public Vector3 Scale { get; set; }

        /// <summary>
        /// Translation of the model.
        /// </summary>
        public Vector3 Translation { get; set; }

        /// <summary>
        /// Rotation of the model.
        /// </summary>
        public Vector3 Rotation { get; set; }

        /// <summary>
        /// Vertex shader for this model.
        /// </summary>
        //private Shader vertexShader;

        /// <summary>
        /// Pixel shader for this model.
        /// </summary>
        //private Shader pixelShader;

        /// <summary>
        /// Loads a model from an OBJ file.
        /// </summary>
        /// <param name="device">The device to use.</param>
        /// <param name="modelName">The model name.</param>
        public Model(Device device, String modelName)
        {
            LoadModel(device, File.ReadLines(modelName));

            Scale = Vector3.One;
            Rotation = Vector3.Zero;
            Translation = Vector3.Zero;

            modelBuffer = new Buffer(device, new BufferDescription()
            {
                SizeInBytes = 512,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
            });
        }

        private void LoadModel(Device device, IEnumerable<String> geometry)
        {
            /* Step 1 -- Parse every vertex (+ UV's) of the model into a list. */

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> texCoord = new List<Vector3>();

            foreach (String line in geometry)
            {
                string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); if (tokens.Length < 1) continue;
                if (tokens[0].Equals("v"))
                {
                    if (tokens.Length < 4) continue;
                    vertices.Add(new Vector3(Single.Parse(tokens[1]), Single.Parse(tokens[2]), Single.Parse(tokens[3])));
                }

                if (tokens[0].Equals("vt"))
                {
                    if (tokens.Length < 3) continue;
                    texCoord.Add(new Vector3(Single.Parse(tokens[1]), Single.Parse(tokens[2]), (tokens.Length == 3 ? 0 : Single.Parse(tokens[3]))));
                }
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

            String currentMesh = "";

            foreach (String line in geometry)
            {
                string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 1) continue;

                if (tokens[0].Equals("usemtl"))
                {
                    if (tokens.Length < 2) continue;
                    currentMesh = tokens[1];
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

                        meshFaces[currentMesh].Add(new Triangle(vertices[v1], vertices[v2], vertices[v3],
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

                        meshFaces[currentMesh].Add(new Triangle(vertices[v1], vertices[v2], vertices[v3],
                                                                     normals[v1],  normals[v2],  normals[v3],
                                                                    texCoord[t1], texCoord[t2], texCoord[t3]));

                        meshFaces[currentMesh].Add(new Triangle(vertices[v1], vertices[v3], vertices[v4],
                                                                     normals[v1],  normals[v3],  normals[v4],
                                                                    texCoord[t1], texCoord[t3], texCoord[t4]));
                    }
                }
            }

            /* Step 5 -- Create each mesh instance from mesh name and faces. */

            foreach (String meshName in meshFaces.Keys)
            {
                if (meshName.Equals("sprljci")) continue; // temporary
                if (meshName.Equals("staklo")) continue; // temporary

                meshes.Add(new Mesh(device, meshName, meshFaces[meshName]));
            }
        }

        private Buffer modelBuffer;

        public void Render(Device device, DeviceContext context, Camera camera, Dictionary<String, Material> materials, ResourceProxy proxy)
        {
            Matrix modelToWorld = Matrix.Scaling(Scale)
                                * Matrix.RotationYawPitchRoll(Rotation.X, Rotation.Y, Rotation.Z)
                                * Matrix.Translation(Translation);

            {
                DataStream modelStream;
                context.MapSubresource(modelBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out modelStream);
                modelStream.Write<Matrix>(Matrix.Transpose(modelToWorld));
                context.UnmapSubresource(modelBuffer, 0);
                modelStream.Dispose();
            }

            context.VertexShader.SetConstantBuffer(0, modelBuffer);
            context.PixelShader.SetConstantBuffer(0, modelBuffer);

            foreach (Mesh mesh in meshes)
            {
                Material material = materials[mesh.MeshName];
                material.BindMaterial(context, proxy);
                mesh.Render(device, context);
            }
        }

        #region IDisposable

        /// <summary>
        /// Destroys this Model instance.
        /// </summary>
        ~Model()
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
                modelBuffer.Dispose();

                foreach (Mesh mesh in meshes) mesh.Dispose();
            }
        }

        #endregion

        public static Dictionary<String, Model> Parse(Device device, IEnumerable<String> definition)
        {
            try
            {
                Dictionary<String, Model> models = new Dictionary<String, Model>();

                using (IEnumerator<String> data = definition.GetEnumerator())
                {
                    String currentModel = null;

                    while (data.MoveNext())
                    {
                        String[] tokens = data.Current.Split('#');
                        if (tokens.Length == 0) continue;
                        String line = tokens[0].Trim();
                        if (line.Length == 0) continue;

                        if (line.StartsWith("model "))
                        {
                            if (TrimSplit(line).Length == 3)
                            {
                                String modelPath = TrimSplit(line)[1];
                                String modelName = TrimSplit(line)[2];

                                models.Add(modelName, new Model(device, modelPath));
                                currentModel = modelName;
                            }
                            else throw new ArgumentException("invalid model declaration");
                        }
                        else
                        {
                            if (currentModel == null) throw new ArgumentException("no model declared");

                            if (TrimSplit(line, ' ').Length == 4)
                            {
                                String header = TrimSplit(line, ' ')[0];
                                Vector3 vector = Vector3.Zero;

                                try
                                {
                                    float x = Single.Parse(TrimSplit(line, ' ')[1]);
                                    float y = Single.Parse(TrimSplit(line, ' ')[2]);
                                    float z = Single.Parse(TrimSplit(line, ' ')[3]);
                                    vector = new Vector3(x, y, z);
                                }
                                catch
                                {
                                    throw new ArgumentException("invalid model declaration");
                                }

                                switch (header)
                                {
                                    case "s":
                                        models[currentModel].Scale = vector;
                                        break;

                                    case "r":
                                        models[currentModel].Rotation = vector;
                                        break;

                                    case "t":
                                        models[currentModel].Translation = vector;
                                        break;

                                    default:
                                        throw new ArgumentException("error parsing model declaration");
                                }
                            }
                            else throw new ArgumentException("invalid model declaration");
                        }
                    }
                }

                return models;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Failed to parse definition file: " + ex.Message + ".", ex);
            }
        }

        private static String[] TrimSplit(String value, char separator = ' ')
        {
            return value.Trim().Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
