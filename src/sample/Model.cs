using System;
using System.IO;
using System.Collections.Generic;

using SharpDX;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;

using Assimp;

namespace Sample
{
    /// <summary>
    /// A vertex class containing position, texture
    /// coordinates, and normal/tangent/bitangent.
    /// </summary>
    class Vertex
    {
        /// <summary>
        /// Position of the vertex.
        /// </summary>
        public Vector3 Pos { get; private set; }

        /// <summary>
        /// Surface normal at the vertex.
        /// </summary>
        public Vector3 Nml { get; private set; }

        /// <summary>
        /// Surface tangent of the vertex.
        /// </summary>
        public Vector3 Tan { get; private set; }

        /// <summary>
        /// Surface bitangent of the vertex.
        /// </summary>
        public Vector3 Btn { get; private set; }

        /// <summary>
        /// Texture coordinates at the vertex.
        /// </summary>
        public Vector3 Tex { get; private set; }

        /// <summary>
        /// Creates a Vertex from vertex data
        /// imported by Assimp from a mesh.
        /// </summary>
        /// <param name="pos">Vertex position.</param>
        /// <param name="nml">Vertex normal.</param>
        /// <param name="tan">Vertex tangent.</param>
        /// <param name="btn">Vertex bitangent.</param>
        /// <param name="tex">Vertex texture coordinates.</param>
        public Vertex(Vector3D pos,
                      Vector3D nml,
                      Vector3D tan,
                      Vector3D btn,
                      Vector3D tex)
        {
            Pos = ToVector3(pos);
            Nml = ToVector3(nml);
            Tan = ToVector3(tan);
            Btn = ToVector3(btn);
            Tex = ToVector3(tex);
        }

        private static Vector3 ToVector3(Assimp.Vector3D u)
        {
            return new Vector3(u.X, u.Y, u.Z);
        }

        /// <summary>
        /// Writes the texture to a DataStream.
        /// </summary>
        /// <param name="stream">The stream to write the vertex to.</param>
        public void WriteTo(DataStream stream)
        {
            stream.Write<Vector4>(new Vector4(Pos, 1));
            stream.Write<Vector4>(new Vector4(Nml, 1));
            stream.Write<Vector4>(new Vector4(Tan, 1));
            stream.Write<Vector4>(new Vector4(Btn, 1));
            stream.Write<Vector4>(new Vector4(Tex, 1));
        }

        /// <summary>
        /// Gets the size, in bytes, that a vertex
        /// will take in the vertex buffer.
        /// </summary>
        public static int Size
        {
            get
            {
                return Vector4.SizeInBytes * 5;
            }
        }
    }

    /// <summary>
    /// Represents a single model with possibly many meshes, each with
    /// their own material, required assets, and a model view matrix.
    /// </summary>
    class Model : IDisposable
    {
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
        /// Loads a model from an OBJ file.
        /// </summary>
        /// <param name="device">The device to use.</param>
        /// <param name="modelName">The model name.</param>
        public Model(Device device, String modelName)
        {
            LoadModel(device, modelName);

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

        private Vector3 ToVector3(Assimp.Vector3D v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        private Vector2 ToVector2(Assimp.Vector3D v)
        {
            return new Vector2(v.X, v.Y);
        }

        private void LoadModel(Device device, String fileName)
        {
            PostProcessSteps flags = PostProcessSteps.Triangulate
                                   | PostProcessSteps.OptimizeMeshes
                                   | PostProcessSteps.GenerateUVCoords
                                   | PostProcessSteps.FixInFacingNormals
                                   | PostProcessSteps.CalculateTangentSpace
                                   | PostProcessSteps.GenerateSmoothNormals;

            using (AssimpImporter importer = new AssimpImporter())
            {
                Console.WriteLine("Loading model " + fileName);

                var data = importer.ImportFile(fileName, flags);

                foreach (var mesh in data.Meshes)
                {
                    List<Vertex> geometry = new List<Vertex>(mesh.FaceCount * 3);
                    String meshName = data.Materials[mesh.MaterialIndex].Name;

                    Console.WriteLine("Loading mesh " + meshName);

                    if (!mesh.HasTextureCoords(0))
                    {
                        Console.WriteLine("INFO: Mesh has no UV's. This is perfectly fine if the material does not use textures.");
                    }

                    foreach (var face in mesh.Faces)
                    {
                        for (int t = 0; t < face.IndexCount; ++t)
                        {
                            if (!mesh.HasTextureCoords(0))
                            {
                                geometry.Add(new Vertex(mesh.Vertices[face.Indices[t]],
                                                        mesh.Normals[face.Indices[t]],
                                                        new Vector3D(0, 0, 0),
                                                        new Vector3D(0, 0, 0),
                                                        new Vector3D(0, 0, 0)));
                            }
                            else
                            {
                                geometry.Add(new Vertex(mesh.Vertices[face.Indices[t]],
                                                        mesh.Normals[face.Indices[t]],
                                                        mesh.Tangents[face.Indices[t]],
                                                        mesh.BiTangents[face.Indices[t]],
                                                        mesh.GetTextureCoords(0)[face.Indices[t]]));
                            }
                        }
                    }

                    meshes.Add(new Mesh(device, meshName, geometry));
                }
            }
        }

        private Buffer modelBuffer;

        public void Render(DeviceContext context, Camera camera, Dictionary<String, Material> materials, ResourceProxy proxy)
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

            context.VertexShader.SetConstantBuffer(1, modelBuffer);
            context.PixelShader.SetConstantBuffer(1, modelBuffer);

            foreach (Mesh mesh in meshes)
            {
                if (!materials.ContainsKey(mesh.MeshName))
                {
                    //Console.WriteLine("WARNING: " + mesh.MeshName + " has no material! Skipping...");
                    continue;
                }

                Material material = materials[mesh.MeshName];
                material.BindMaterial(context, proxy);
                mesh.Render(context);
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
