using System;
using System.Drawing;
using System.Windows.Forms;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.D3DCompiler;
using EffectFlags = SharpDX.D3DCompiler.EffectFlags;
using Point = System.Drawing.Point;

using SharpDX.DirectInput;
using SharpDX.Windows;

namespace Sample
{
    enum ShaderType { Vertex, Geometry, Pixel, Compute, Hull, Domain };

    /// <summary> Wraps a single shader. </summary>
    class Shader
    {
        public ShaderType shaderType;
        public ComputeShader computeShader;
        public VertexShader vertexShader;
        public GeometryShader geometryShader;
        public PixelShader pixelShader;
        public HullShader hullShader;
        public DomainShader domainShader;
        public InputLayout inputLayout;

        /// <summary> Loads and compiles a shader. </summary>
        /// <param name="device"> The device context in use. </param>
        /// <param name="shader"> The shader name. </param>
        /// <param name="type"> The shader type (vertex, geometry, pixel, or compute). </param>
        /// <param name="elements"> The input layout elements. </param>
        public Shader(Device device, string shader, ShaderType type, InputElement[] elements = null, StreamOutputElement[] SOElements = null, int[] SOStrides = null)
        {
            shaderType = type;
            string error;

            switch (shaderType)
            {
                case ShaderType.Vertex:
                    {
                        ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(Settings.ShaderDir + shader + ".vs.fx", "main", "vs_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, Settings.includeFX);
                        vertexShader = new VertexShader(device, bytecode);
                        inputLayout = new InputLayout(device, bytecode, elements);
                        bytecode.Dispose();
                        break;
                    }

                case ShaderType.Geometry:
                    {
                        ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(Settings.ShaderDir + shader + ".fx", "main", "gs_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, Settings.includeFX);
                        geometryShader = new GeometryShader(device, bytecode, SOElements, SOStrides, GeometryShader.StreamOutputNoRasterizedStream);
                        bytecode.Dispose();
                        break;
                    }

                case ShaderType.Pixel:
                    {
                        ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(Settings.ShaderDir + shader + ".ps.fx", "main", "ps_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, Settings.includeFX);
                        pixelShader = new PixelShader(device, bytecode);
                        bytecode.Dispose();
                        break;
                    }

                case ShaderType.Compute:
                    {
                        ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(Settings.ShaderDir + shader + ".fx", "main", "cs_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, Settings.includeFX);
                        computeShader = new ComputeShader(device, bytecode);
                        bytecode.Dispose();
                        break;
                    }

                case ShaderType.Hull:
                    {
                        ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(Settings.ShaderDir + shader + ".fx", "main", "hs_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, Settings.includeFX);
                        hullShader = new HullShader(device, bytecode);
                        bytecode.Dispose();
                        break;
                    }

                case ShaderType.Domain:
                    {
                        ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(Settings.ShaderDir + shader + ".fx", "main", "ds_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, Settings.includeFX);
                        domainShader = new DomainShader(device, bytecode);
                        bytecode.Dispose();
                        break;
                    }
            }
        }

        /// <summary> Frees the shader from memory. </summary>
        public void Dispose()
        {
            if (shaderType == ShaderType.Domain) { domainShader.Dispose(); }
            if (shaderType == ShaderType.Hull) { hullShader.Dispose(); }
            if (shaderType == ShaderType.Compute) { computeShader.Dispose(); }
            if (shaderType == ShaderType.Pixel) { pixelShader.Dispose(); }
            if (shaderType == ShaderType.Geometry) { geometryShader.Dispose(); }
            if (shaderType == ShaderType.Vertex) { vertexShader.Dispose(); inputLayout.Dispose(); }
        }
    }

    /// <summary>
    /// The sample scene renderer, with a camera and
    /// basic HDR environment.
    /// </summary>
    class Scene : IDisposable
    {
        private Device device;

        private Camera camera;

        private DepthStencilState depthStencilState;

        private Texture2D depthBuffer;
        private DepthStencilView depthStencilView;

        private RasterizerState rasterizerState;

        private DirectInput directInput;

        private Keyboard keyboard;

        private RenderForm window;
        private Point mousePoint;
        private bool pressed;

        private Size resolution;

        private Model model, skydome, ground, house;

        public void Resize(Size size)
        {
            resolution = size;

            camera.AspectRatio = (float)resolution.Width / resolution.Height;
            camera.FieldOfView = 75;

            depthStencilView.Dispose();
            depthBuffer.Dispose();
            CreateDepthbuffer(resolution);
        }

        public Scene(Device device, DeviceContext context, RenderForm window, Size resolution)
        {
            model = new Model(device, "sibenik");

            skydome = new Model(device, "skydome");
            skydome.Scale *= 100;
            skydome.Translation = new Vector3(0, -15.35f, 0);

            ground = new Model(device, "ground");
            ground.Translation = new Vector3(0, -15.35f, 0);

            house = new Model(device, "house");
            house.Translation = new Vector3(-35, -15.35f, 10);
            house.Rotation = new Vector3(-1.5f, 0, 0);
            house.Scale *= 0.01f;

            this.device = device;
            this.window = window;
            this.resolution = resolution;

            CreateDepthbuffer(resolution);
            CreateInput(window);
            CreateCamera(resolution);

            rasterizerState = new RasterizerState(device, new RasterizerStateDescription() { CullMode = CullMode.None, FillMode = FillMode.Solid });
            rasterizerState.DebugName = "Scene RasterizerState";

            window.MouseUp += MouseUp;
            window.MouseDown += MouseDown;
            window.MouseMove += MouseMove;
        }

        private void MouseDown(object sender, MouseEventArgs e)
        {
            mousePoint = new Point(e.X, e.Y);
            pressed = true;
        }

        private void MouseUp(object sender, MouseEventArgs e)
        {
            pressed = false;
        }

        private void MouseMove(object sender, MouseEventArgs e)
        {
            if (pressed)
            {
                float dx = (float)(e.X - mousePoint.X) / window.ClientSize.Width;
                float dy = (float)(mousePoint.Y - e.Y) / window.ClientSize.Height;

                camera.RotateCamera(new Vector2(dx, dy));
                mousePoint = new Point(e.X, e.Y);
            }
        }

        /// <summary> Creates the depth buffer with the current settings. </summary>
        private void CreateDepthbuffer(Size resolution)
        {
            depthStencilState = new DepthStencilState(device, new DepthStencilStateDescription
            {
                IsDepthEnabled = true,
                IsStencilEnabled = false,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Less
            });

            depthBuffer = new Texture2D(device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.D32_Float,
                Height = resolution.Height,
                Width = resolution.Width,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            });

            depthStencilView = new DepthStencilView(device, depthBuffer);
        }

        /// <summary> Creates the input manager. </summary>
        private void CreateInput(RenderForm window)
        {
            directInput = new DirectInput();
            keyboard = new Keyboard(directInput);
            keyboard.SetCooperativeLevel(window.Handle, CooperativeLevel.NonExclusive | CooperativeLevel.Background);
            keyboard.Acquire();
        }

        /// <summary> Creates the camera. </summary>
        private void CreateCamera(Size resolution)
        {
            camera = new Camera(Settings.initialCameraPosition, Settings.initialCameraRotation, 75, (float)resolution.Width / resolution.Height);
        }

        /// <summary> Returns the keyboard movement vector. </summary>
        public Vector3 AcquireKeyboardInput()
        {
            Vector3 movement = Vector3.Zero;
            KeyboardState state = keyboard.GetCurrentState();
            if (state.IsPressed(Key.W)) { movement += new Vector3(0, 0, -1); }
            if (state.IsPressed(Key.S)) { movement += new Vector3(0, 0, 1); }
            if (state.IsPressed(Key.A)) { movement += new Vector3(1, 0, 0); }
            if (state.IsPressed(Key.D)) { movement += new Vector3(-1, 0, 0); }
            return movement;
        }

        public bool isKeyPressed(Key key)
        {
            return keyboard.GetCurrentState().PressedKeys.Contains(key);
        }

        public void Render(RenderTargetView renderTargetView, DeviceContext context)
        {
            /* Acquire user input, also clamp the up/down rotation term. */
            camera.MoveCamera(AcquireKeyboardInput());

            context.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderTargetView, new Color4(0.5f, 0, 1, 1));

            context.Rasterizer.State = rasterizerState;
            context.Rasterizer.SetViewports(new[] { new ViewportF(0, 0, resolution.Width, resolution.Height) });
            context.Rasterizer.SetScissorRectangles(new[] { new SharpDX.Rectangle(0, 0, resolution.Width, resolution.Height) });
            context.OutputMerger.DepthStencilState = depthStencilState;
            context.OutputMerger.SetTargets(depthStencilView, renderTargetView);

            model.Render(device, context, camera);
            skydome.Render(device, context, camera);
            ground.Render(device, context, camera);
            house.Render(device, context, camera);
        }

        #region IDisposable

        /// <summary>
        /// Destroys this Scene instance.
        /// </summary>
        ~Scene()
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
                model.Dispose();
                skydome.Dispose();
                ground.Dispose();
                house.Dispose();

                depthStencilState.Dispose();
                depthStencilView.Dispose();
                rasterizerState.Dispose();

                depthBuffer.Dispose();

                directInput.Dispose();
                keyboard.Dispose();
            }
        }

        #endregion
    }
}
