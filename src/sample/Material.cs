using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Sample
{
    /// <summary>
    /// Abstract base class for materials. Note material
    /// classes should be <b>specific</b>, for instance,
    /// "CarMaterial". Generic materials are not useful,
    /// as they are not easy for the user to configure.
    /// </summary>
    public abstract class Material : IDisposable
    {
        /// <summary>
        /// Gets the device associated with this material.
        /// </summary>
        public Device Device { get; private set; }

        /// <summary>
        /// Gets the bar associated with this material.
        /// </summary>
        public TweakBar Bar { get; private set; }

        /// <summary>
        /// Gets the name of this material.
        /// </summary>
        public String Name { get; private set; }

        /// <summary>
        /// Gets a unique tweak bar prefix.
        /// </summary>
        protected String Prefix { get { return "'" + Name + "'-"; } }

        /// <summary>
        /// Allocates a new material instance, under a given tweak bar and group. The material should
        /// create a sensible group inside the tweak bar it is provided, and place its options there.
        /// </summary>
        /// <param name="device">The graphics device to associate this new material instance to.</param>
        /// <param name="bar">The tweak bar to use. Can be null if this instance should not be configurable.</param>
        /// <param name="name">The material instance's name, which is guaranteed to be unique over all material instances.</param>
        public Material(Device device, TweakBar bar, String name)
        {
            Device = device;
            Name = name;
            Bar = bar;
        }

        /// <summary>
        /// Gets or sets the value of a material attribute.
        /// </summary>
        /// <param name="name">Name of the material attribute.</param>
        /// <returns>The value of the material attribute.</returns>
        public Object this[String name]
        {
            get { return GetProperty(name).GetValue(this); }
            set { GetProperty(name).SetValue(this, value); }
        }

        private PropertyInfo GetProperty(String name)
        {
            PropertyInfo property = GetType().GetProperty(name);
            if (property != null) return property;
            else
            {
                throw new ArgumentException("No such material attribute: " + name + ".");
            }
        }

        /// <summary>
        /// Binds this material instance to a device context for rendering. A material must
        /// initialize its own pixel shader as well as its constant buffer, sampler(s), and
        /// shader resources (served by a ResourceProxy).
        /// </summary>
        /// <param name="context">The device context to bind the material to.</param>
        /// <param name="proxy">The resource proxy which provides resources.</param>
        public abstract void BindMaterial(DeviceContext context, ResourceProxy proxy);

        #region IDisposable

        /// <summary>
        /// Destroys this Material instance.
        /// </summary>
        ~Material()
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

        protected abstract void Dispose(bool disposing);

        #endregion

        #region Material Definition Parser

        private static Random random = new Random();

        /// <summary>
        /// Attempts to parse a definition file, which maps a set of
        /// mesh names with their corresponding material and initial
        /// attribute values. Will throw an exception on failure.
        /// </summary>
        /// <param name="definition">The definition file, line per line.</param>
        /// <returns>A mapping between mesh names and materials.</returns>
        public static Dictionary<String, Material> Parse(Device device, TweakBar bar, IEnumerable<String> definition)
        {
            try
            {
                Dictionary<String, Material> materials = new Dictionary<String, Material>();

                using (IEnumerator<String> data = definition.GetEnumerator())
                {
                    String currentMesh = null;

                    while (data.MoveNext())
                    {
                        String[] tokens = data.Current.Split('#');
                        if (tokens.Length == 0) continue;
                        String line = tokens[0].Trim();
                        if (line.Length == 0) continue;

                        if (line.StartsWith("mtl "))
                        {
                            if (TrimSplit(line).Length == 5)
                            {
                                String meshName = TrimSplit(line)[1];
                                String materialName = TrimSplit(line)[2];
                                String materialClass = TrimSplit(line)[3];
                                String materialVisibility = TrimSplit(line)[4];
                                Type materialType = Type.GetType(materialClass); /* Needs to be namespace-qualified. */
                                if (materialType == null) throw new ArgumentException("no such material (" + materialClass + ")");

                                if ((materialVisibility != "show") && (materialVisibility != "hide"))
                                    throw new ArgumentException("expected material visibility");
                                bool visible = (materialVisibility == "show");

                                if (meshName == "-") meshName = random.NextLong().ToString();

                                materials.Add(meshName, (Material)Activator.CreateInstance(materialType, device, bar, materialName));
                                if (!visible) AntTweakBar.TwDefine("'" + bar.Name + "'" + "/" + materialName + " visible=false");
                                currentMesh = meshName;
                            }
                            else throw new ArgumentException("invalid mesh material declaration");
                        }
                        else
                        {
                            if (currentMesh == null) throw new ArgumentException("no mesh material declared");

                            if (TrimSplit(line, '=').Length == 2)
                            {
                                String header        = TrimSplit(line, '=')[0].Trim();    /* [ATTRTYPE ATTRNAME] = ATTRVALUE */
                                String attribute     = TrimSplit(line, '=')[1].Trim();    /* ATTRTYPE ATTRNAME = [ATTRVALUE] */

                                if (TrimSplit(header, ' ').Length == 2)
                                {
                                    String attributeType = TrimSplit(header, ' ')[0].Trim();  /* [ATTRTYPE] ATTRNAME = ATTRVALUE */
                                    String attributeName = TrimSplit(header, ' ')[1].Trim();  /* ATTRTYPE [ATTRNAME] = ATTRVALUE */

                                    materials[currentMesh][attributeName] = ParseAttribute(attributeType, attribute);
                                }
                                else throw new ArgumentException("invalid material attribute declaration");
                            }
                            else throw new ArgumentException("invalid material attribute declaration");
                        }
                    }
                }

                return materials;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Failed to parse definition file: " + ex.Message + ".", ex);
            }
        }

        private static Object ParseAttribute(String attributeType, String attribute)
        {
            try
            {
                Console.WriteLine(String.Format("Parsing {0} ::= {1}", attributeType, attribute));

                switch (attributeType)
                {
                    case "string":
                        {
                            Console.WriteLine("Found string");
                            return attribute;
                        }

                    case "bool":
                        Console.WriteLine("Found bool");
                        if (attribute == "false") return false;
                        if (attribute == "true") return true;
                        Console.WriteLine("Not a valid bool");
                        break;

                    case "int":
                        {
                            Console.WriteLine("Found int = " + Int32.Parse(attribute));
                            return Int32.Parse(attribute);
                        }

                    case "float":
                        {
                            Console.WriteLine("Found float = " + Double.Parse(attribute));
                            return Double.Parse(attribute);
                        }

                    case "float3":
                        {
                            String[] tokens = TrimSplit(attribute, ' ');
                            Console.WriteLine(String.Format("Found float3 = ({0}, {1}, {2})", Single.Parse(tokens[0]), Single.Parse(tokens[1]), Single.Parse(tokens[2])));
                            return new Vector3(Single.Parse(tokens[0]),
                                               Single.Parse(tokens[1]),
                                               Single.Parse(tokens[2]));
                        }

                    case "color3":
                        {
                            String[] tokens = TrimSplit(attribute, ' ');
                            Console.WriteLine(String.Format("Found color3 = ({0}, {1}, {2})", Single.Parse(tokens[0]), Single.Parse(tokens[1]), Single.Parse(tokens[2])));
                            return new Color3(Single.Parse(tokens[0]),
                                              Single.Parse(tokens[1]),
                                              Single.Parse(tokens[2]));
                        }
                }

                Console.WriteLine("Failed to find attribute type");
                throw new ArgumentException("Attribute was not assigned.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Parsing error: " + e.Message);
                throw new ArgumentException("error parsing attribute", e);
            }

            throw new ArgumentException("unknown attribute type " + attributeType);
        }

        private static String[] TrimSplit(String value, char separator = ' ')
        {
            return value.Trim().Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        }

        #endregion

        #region Utility Material Methods

        /// <summary>
        /// Convenience method to compile material pixel shaders.
        /// </summary>
        /// <param name="device">The graphics device.</param>
        /// <param name="shaderName">The shader name.</param>
        /// <returns>The compiled pixel shader.</returns>
        protected static PixelShader CompileShader(Device device, String shaderName)
        {
            String shader = File.ReadAllText("shaders/materials/" + shaderName + ".hlsl");
            using (ShaderBytecode bytecode = ShaderBytecode.Compile(shader, "main", "ps_5_0",
                                                                    ShaderFlags.OptimizationLevel3,
                                                                    EffectFlags.None, null,
                                                                    includeHandler))
                return new PixelShader(device, bytecode);
        }

        private class ShaderInclude : Include
        {
            public Stream Open(IncludeType type, string fileName, Stream stream)
            {
                String data = File.ReadAllText(fileName, Encoding.ASCII);
                return new MemoryStream(Encoding.ASCII.GetBytes(data));
            }

            public void Close(Stream stream)
            {
                stream.Close();
            }

            public IDisposable Shadow { get; set; }
            public void Dispose() { }
        }

        private static ShaderInclude includeHandler = new ShaderInclude();

        /// <summary>
        /// Convenience method to allocate constant buffers for materials.
        /// </summary>
        /// <param name="device">The graphics device.</param>
        /// <param name="sizeInBytes">Size, in bytes, of the buffer.</param>
        /// <returns>An allocated constant buffer ready for use.</returns>
        protected static Buffer AllocateMaterialBuffer(Device device, int sizeInBytes)
        {
            return new Buffer(device, new BufferDescription()
            {
                SizeInBytes = sizeInBytes,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
            });
        }

        /// <summary>
        /// Convenience method to copy the contents of a DataStream into a
        /// constant buffer. The buffer must have CPU read access, and its
        /// position will be reset to zero before copying (by convention).
        /// </summary>
        /// <param name="context">Device context to use.</param>
        /// <param name="buffer">Constant buffer.</param>
        /// <param name="stream">The data stream.</param>
        protected static void CopyStream(DeviceContext context, Buffer buffer, DataStream stream)
        {
            DataStream staging = null;
            try
            {
                context.MapSubresource(buffer, MapMode.WriteDiscard, MapFlags.None, out staging);
                stream.Position = 0; stream.CopyTo(staging);
                context.UnmapSubresource(buffer, 0);
            }
            finally
            {
                if (staging != null) staging.Dispose();
            }
        }

        #endregion
    }
}
