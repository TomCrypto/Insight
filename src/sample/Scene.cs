using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Windows;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.DirectInput;
using Point = System.Drawing.Point;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using MapFlags = SharpDX.Direct3D11.MapFlags;

using Insight;

namespace Sample
{
    /// <summary>
    /// The sample scene renderer, with a camera and
    /// basic HDR environment to navigate around of.
    /// </summary>
    class Scene : IDisposable
    {
        /* Resources concerning the depth buffer. */
        private DepthStencilState depthStencilState;
        private DepthStencilView depthStencilView;
        private RasterizerState rasterizerState;
        private Texture2D depthBuffer;
        
        /* Variables concerning the user interacting. */
        private Point mousePosition = new Point(-1, -1);
        private DirectInput directInput;
        private Buffer cameraBuffer;
        private Keyboard keyboard;

        private GraphicsResource skyEnvMap;
        private VertexShader vertexShader;
        private InputLayout inputLayout;
        private TweakBar materialBar;
        private ResourceProxy proxy;
        private Camera camera;

        /* Materials and models, the actual content. */
        private Dictionary<String, Material> materials;
        private Dictionary<String,    Model> models;

        /// <summary>
        /// Gets or sets the rotation sensitivity.
        /// </summary>
        public double RotationSensitivity { get; set; }

        /// <summary>
        /// Gets the device this Scene is associated to.
        /// </summary>
        public Device Device { get; private set; }

        /// <summary>
        /// Gets or sets the resolution at which
        /// the scene is to be rendered. If this
        /// property is set, the backbuffer sent
        /// to the Render() method should be the
        /// same dimensions.
        /// </summary>
        public Size RenderDimensions
        {
            get
            {
                return new Size(depthBuffer.Description.Width,
                                depthBuffer.Description.Height);
            }

            set
            {
                InitializeResources(value);
            }
        }

        /// <summary>
        /// Gets or sets the camera's field of
        /// view. This property is in degrees.
        /// </summary>
        public float FieldOfView
        {
            get
            {
                return camera.FieldOfView;
            }

            set
            {
                camera.FieldOfView = value;
            }
        }

        /// <summary>
        /// Creates any required tweak bars.
        /// </summary>
        /// <param name="window">The window to draw into.</param>
        private void SetupTweakBars(RenderForm window)
        {
            materialBar = new TweakBar(null, "Material Settings");
        }

        /// <summary>
        /// Loads all models, shaders, materials, etc.. from disk,
        /// and initializes them into Model objects for rendering.
        /// </summary>
        private void LoadSceneAssets()
        {
            materials = Material.Parse(Device, materialBar, File.ReadLines("definitions/materials.def"));
            models = Model.Parse(Device, File.ReadLines("definitions/models.def"));
        }

        #region Vertex Shader Loading

        private void CompileVertexShader()
        {
            InputElement[] VertexLayout = new[] { new InputElement("POSITION", 0, Format.R32G32B32A32_Float,  0, 0),
                                                  new InputElement("NORMAL",   0, Format.R32G32B32A32_Float, 16, 0),
                                                  new InputElement("TEXCOORD", 0, Format.R32G32B32A32_Float, 32, 0) };

            String shader = File.ReadAllText("shaders/render.hlsl");
            using (ShaderBytecode bytecode = ShaderBytecode.Compile(shader, "main", "vs_5_0"))
            {
                inputLayout = new InputLayout(Device, bytecode, VertexLayout);
                vertexShader = new VertexShader(Device, bytecode);
            }
        }

        #endregion

        public Scene(Device device, DeviceContext context, RenderForm window, Size resolution)
        {
            Device = device;

            SetupTweakBars(window);
            CompileVertexShader();
            LoadSceneAssets();

            RenderDimensions = resolution;
            CreateCamera(resolution);
            CreateInput(window);
            
            window.MouseDown += MouseDown;
            window.MouseMove += MouseMove;
            window.MouseUp   += MouseUp;

            proxy = new ResourceProxy(device);

            // TODO: create SkyGenerator instance here

            skyEnvMap = new GraphicsResource(device, new Size(1024, 1024), Format.R32G32B32A32_Float, true, true);
        }

        #region Mouse Movement

        private void MouseDown(object sender, MouseEventArgs e)
        {
            mousePosition = new Point(e.X, e.Y);
        }

        private void MouseUp(object sender, MouseEventArgs e)
        {
            mousePosition = new Point(-1, -1);
        }

        private void MouseMove(object sender, MouseEventArgs e)
        {
            if (mousePosition != new Point(-1, -1))
            {
                float dy = (float)(mousePosition.Y - e.Y) / RenderDimensions.Height;
                float dx = (float)(e.X - mousePosition.X) / RenderDimensions.Width;
                camera.RotateCamera(new Vector2(dx, dy) * (float)RotationSensitivity);
                mousePosition = new Point(e.X, e.Y);
            }
        }

        #endregion

        /// <summary>
        /// Initializes the depth buffer, depth stencil
        /// state, rasterizer state, for rendering.
        /// </summary>
        private void InitializeResources(Size resolution)
        {
            if (depthStencilState != null) depthStencilState.Dispose();
            if ( depthStencilView != null) depthStencilView.Dispose();
            if (  rasterizerState != null) rasterizerState.Dispose();
            if (     cameraBuffer != null) cameraBuffer.Dispose();
            if (      depthBuffer != null) depthBuffer.Dispose();

            depthStencilState = new DepthStencilState(Device, new DepthStencilStateDescription
            {
                IsDepthEnabled = true,
                IsStencilEnabled = false,
                DepthComparison = Comparison.Less,
                DepthWriteMask = DepthWriteMask.All,
            });

            depthBuffer = new Texture2D(Device, new Texture2DDescription
            {
                MipLevels = 1,
                ArraySize = 1,
                Width = resolution.Width,
                Height = resolution.Height,
                
                Format = Format.D32_Float,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = Settings.MultisamplingOptions,
            });

            depthStencilView = new DepthStencilView(Device, depthBuffer);

            rasterizerState = new RasterizerState(Device, new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
            });

            cameraBuffer = new Buffer(Device, new BufferDescription()
            {
                SizeInBytes = Camera.Size(),
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
            });
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

        public void Update()
        {
            camera.MoveCamera(AcquireKeyboardInput());

            camera.AspectRatio = (float)RenderDimensions.Width / RenderDimensions.Height;
        }

        public void Render(RenderTargetView renderTargetView, DeviceContext context, SurfacePass pass)
        {
            // TODO: generate sky envmap here, with custom params

            pass.Pass(context, @"
            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            float3 main(PS_IN input) : SV_Target
            {
                input.tex = (input.tex - 0.5f) * 2;

                float y2 = 1 - pow(input.tex.x, 2) - pow(input.tex.y, 2);
                if (y2 < 0) return float3(0, 0, 0); /* Outside circle. */

                float3 p = float3(input.tex.x, sqrt(y2), input.tex.y);

                /* TEMPORARY */

                float brightness = 50;

                float sunBrightness = (dot(p, normalize(float3(-0.5f, 0.8f, 0.9f))) > 0.9995f) ? 1 : 0;

	            return lerp(float3(1, 1, 1), float3(0.7f, 0.7f, 1), p.y) * brightness + sunBrightness * 175000;
            }
            ", skyEnvMap.Dimensions, skyEnvMap.RTV, null, null);

            context.OutputMerger.SetRenderTargets((RenderTargetView)null);

            context.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderTargetView, new Color4(0.5f, 0, 1, 1));

            context.Rasterizer.State = rasterizerState;
            context.OutputMerger.DepthStencilState = depthStencilState;
            context.OutputMerger.SetTargets(depthStencilView, renderTargetView);
            context.Rasterizer.SetViewports(new[] { new ViewportF(0, 0, RenderDimensions.Width, RenderDimensions.Height) });
            context.Rasterizer.SetScissorRectangles(new[] { new SharpDX.Rectangle(0, 0, RenderDimensions.Width, RenderDimensions.Height) });

            context.VertexShader.Set(vertexShader);
            context.InputAssembler.InputLayout = inputLayout;
            context.PixelShader.SetShaderResource(0, skyEnvMap.SRV);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            {
                DataStream cameraStream;
                context.MapSubresource(cameraBuffer, MapMode.WriteDiscard, MapFlags.None, out cameraStream);
                camera.WriteTo(cameraStream);
                context.UnmapSubresource(cameraBuffer, 0);
                cameraStream.Dispose();
            }

            context.VertexShader.SetConstantBuffer(0, cameraBuffer);
            context.PixelShader.SetConstantBuffer(0, cameraBuffer);

            foreach (Model model in models.Values)
                model.Render(context, camera, materials, proxy);
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
                vertexShader.Dispose();
                inputLayout.Dispose();
                skyEnvMap.Dispose();
                proxy.Dispose();

                foreach (Material material in materials.Values) material.Dispose();
                foreach (   Model    model in    models.Values)    model.Dispose();

                depthStencilState.Dispose();
                depthStencilView.Dispose();
                rasterizerState.Dispose();
                cameraBuffer.Dispose();
                depthBuffer.Dispose();

                materialBar.Dispose();
                directInput.Dispose();
                keyboard.Dispose();
            }
        }

        #endregion
    }
}
