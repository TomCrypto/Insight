using System;
using System.Drawing;

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
    /// <summary> Implements a camera. </summary>
    class Camera
    {
        /// <summary> The camera's current position.</summary>
        public Vector3 cameraPosition;
        /// <summary> The camera's current rotation, around the Y and X axis respectively.</summary>
        public Vector2 cameraRotation;
        /// <summary> The camera's eye vector (normalized). </summary>
        public Vector3 eyeVector;
        /// <summary> The camera's view matrix.</summary>
        public Matrix viewMatrix;
        /// <summary> The camera's projection matrix.</summary>
        public Matrix projMatrix;

        /// <param name="initialPosition"> The default position of the camera.</param>
        /// <param name="initialRotation"> The default rotation of the camera.</param>
        public Camera(Vector3 initialPosition, Vector2 initialRotation)
        {
            cameraPosition = initialPosition;
            cameraRotation = initialRotation;
        }

        /// <summary> Creates the projection matrix according to the field of view. </summary>
        /// <param name="fieldOfView"> The camera's field of view (in degrees). </param>
        public void SetProjectionMatrix(float fieldOfView, float aspectRatio)
        {
            projMatrix = Matrix.PerspectiveFovLH(fieldOfView * (float)(Math.PI / 180), aspectRatio, Settings.nearPlane, Settings.farPlane);
        }

        /// <summary> Updates the view matrix according to the current position and rotation vectors. </summary>
        public void UpdateViewMatrix()
        {
            Matrix rotationMatrix = Matrix.RotationX(cameraRotation.Y) * Matrix.RotationY(cameraRotation.X);
            Vector3 originalTarget = new Vector3(0, 0, -1);
            Vector4 rotatedTarget = Vector3.Transform(originalTarget, rotationMatrix);
            Vector3 finalTarget = cameraPosition + new Vector3(rotatedTarget.X, rotatedTarget.Y, rotatedTarget.Z);
            Vector3 cameraOriginalUpVector = new Vector3(0, 1, 0);
            Vector4 upVector = Vector3.Transform(cameraOriginalUpVector, rotationMatrix);
            viewMatrix = Matrix.LookAtLH(cameraPosition, finalTarget, new Vector3(upVector.X, upVector.Y, upVector.Z));
            eyeVector = Vector3.Normalize(finalTarget - cameraPosition);
        }

        /// <summary> Transforms a vector to the direction of the eye vector. </summary>
        public Vector3 EyeTransform(Vector2 vector)
        {
            Vector3 movement = new Vector3(vector.X, 0, vector.Y);
            Matrix rotationMatrix = Matrix.RotationX(cameraRotation.Y) * Matrix.RotationY(cameraRotation.X);
            Vector4 rotatedTarget = Vector3.Transform(movement, rotationMatrix);
            Vector3 finalMovement = new Vector3(rotatedTarget.X, rotatedTarget.Y, rotatedTarget.Z);
            return finalMovement;
        }

        /// <summary> Moves the camera. </summary>
        /// <param name="movement"> The movement vector to move by (rotated via the camera eye vector.)</param>
        public void MoveCamera(Vector3 movement)
        {
            Matrix rotationMatrix = Matrix.RotationX(cameraRotation.Y) * Matrix.RotationY(cameraRotation.X);
            Vector4 rotatedTarget = Vector3.Transform(movement, rotationMatrix);
            Vector3 finalMovement = new Vector3(rotatedTarget.X, rotatedTarget.Y, rotatedTarget.Z);
            cameraPosition += Vector3.Normalize(finalMovement) * Settings.movementSensitivity;
        }

        /// <summary> Rotates the camera. </summary>
        /// <param name="rotation"> The rotation vector to offset the camera rotation by.</param>
        public void RotateCamera(Vector2 rotation)
        {
            cameraRotation += rotation * Settings.rotationSensitivity;
            if (cameraRotation.Y > Math.PI / 2) { cameraRotation.Y = (float)Math.PI / 2; }
            if (cameraRotation.Y < -Math.PI / 2) { cameraRotation.Y = -(float)Math.PI / 2; }
        }
    }

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
                        ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(Settings.shaderDirectory + shader + ".fx", "main", "vs_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, Settings.includeFX);
                        vertexShader = new VertexShader(device, bytecode);
                        inputLayout = new InputLayout(device, bytecode, elements);
                        bytecode.Dispose();
                        break;
                    }

                case ShaderType.Geometry:
                    {
                        ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(Settings.shaderDirectory + shader + ".fx", "main", "gs_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, Settings.includeFX);
                        geometryShader = new GeometryShader(device, bytecode, SOElements, SOStrides, GeometryShader.StreamOutputNoRasterizedStream);
                        bytecode.Dispose();
                        break;
                    }

                case ShaderType.Pixel:
                    {
                        ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(Settings.shaderDirectory + shader + ".fx", "main", "ps_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, Settings.includeFX);
                        pixelShader = new PixelShader(device, bytecode);
                        bytecode.Dispose();
                        break;
                    }

                case ShaderType.Compute:
                    {
                        ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(Settings.shaderDirectory + shader + ".fx", "main", "cs_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, Settings.includeFX);
                        computeShader = new ComputeShader(device, bytecode);
                        bytecode.Dispose();
                        break;
                    }

                case ShaderType.Hull:
                    {
                        ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(Settings.shaderDirectory + shader + ".fx", "main", "hs_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, Settings.includeFX);
                        hullShader = new HullShader(device, bytecode);
                        bytecode.Dispose();
                        break;
                    }

                case ShaderType.Domain:
                    {
                        ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(Settings.shaderDirectory + shader + ".fx", "main", "ds_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, Settings.includeFX);
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
    class Scene
    {
        private Device device;

        private Camera camera;

        private DepthStencilState depthStencilState;

        private Texture2D depthBuffer;
        private DepthStencilView depthStencilView;

        private DirectInput directInput;

        private Keyboard keyboard;

        private Point mousePoint;

        private Buffer vertexBuffer;

        private Size resolution;

        public Scene(Device device, RenderForm window, Size resolution)
        {
            this.device = device;
            this.resolution = resolution;

            CreateDepthbuffer(resolution);
            CreateInput(window);
            CreateCamera(resolution);

            vertexBuffer = new Buffer(device, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 32 * 36,
                StructureByteStride = 32,
                Usage = ResourceUsage.Dynamic
            });

            DataStream stream;
            device.ImmediateContext.MapSubresource(vertexBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

            stream.Position = 0;
            stream.Write<Vector4>(new Vector4(-0.3f, 60.5f, -0.3f, 1.0f)); stream.Write<Vector4>(new Vector4(1000, 1000, 1000, 1));
            stream.Write<Vector4>(new Vector4(0.3f, 60.5f, -0.3f, 1.0f)); stream.Write<Vector4>(new Vector4(1000, 1000, 1000, 1));
            stream.Write<Vector4>(new Vector4(0.3f, 60.5f, 0.3f, 1.0f)); stream.Write<Vector4>(new Vector4(1000, 1000, 1000, 1));

            stream.Write<Vector4>(new Vector4(-1, 0.5f, -1, 1.0f)); stream.Write<Vector4>(new Vector4(0.0f, 0.1f, 0.0f, 1));
            stream.Write<Vector4>(new Vector4(1, 0.5f, -1, 1.0f)); stream.Write<Vector4>(new Vector4(0.0f, 0.1f, 0.1f, 1));
            stream.Write<Vector4>(new Vector4(1, 0.5f, 1, 1.0f)); stream.Write<Vector4>(new Vector4(0.0f, 0.1f, 0.0f, 1));
            stream.Position = 0;

            device.ImmediateContext.UnmapSubresource(vertexBuffer, 0);

            stream.Dispose();

            constantBuffer = new Buffer(device, new BufferDescription
            {
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 64 + 16 + 16,
                StructureByteStride = 16,
                Usage = ResourceUsage.Dynamic,
            });

            renderVS = new Shader(device, "renderVS", ShaderType.Vertex, new[] { new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0), new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0) }, null, null);
            renderPS = new Shader(device, "renderPS", ShaderType.Pixel, null, null, null);
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
            mousePoint = new Point(window.Left + window.Width / 2, window.Top + window.Height / 2);
        }

        /// <summary> Creates the camera. </summary>
        private void CreateCamera(Size resolution)
        {
            camera = new Camera(Settings.initialCameraPosition, Settings.initialCameraRotation);
            camera.SetProjectionMatrix(75, (float)resolution.Width / resolution.Height);
            camera.UpdateViewMatrix();
        }

        /// <summary> Returns the mouse rotation vector. Also resets the mouse to the center of the screen. </summary>
        public Vector2 AcquireMouseInput()
        {
            Point mousePosition = System.Windows.Forms.Cursor.Position;
            System.Windows.Forms.Cursor.Position = mousePoint;
            return new Vector2((mousePosition.X - mousePoint.X) / (float)mousePoint.X, (mousePoint.Y - mousePosition.Y) / (float)mousePoint.Y);
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

        Buffer constantBuffer;
        Shader renderVS, renderPS;

        public void Render(RenderTargetView renderTargetView)
        {
            /* Acquire user input, also clamp the up/down rotation term. */
            camera.RotateCamera(AcquireMouseInput());
            camera.MoveCamera(AcquireKeyboardInput());
            camera.UpdateViewMatrix();

            if (keyboard.GetCurrentState().PressedKeys.Contains(Key.Escape)) Environment.Exit(1);

            device.ImmediateContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            device.ImmediateContext.ClearRenderTargetView(renderTargetView, new Color4(0, 0, 0, 1));

            device.ImmediateContext.Rasterizer.State = new RasterizerState(device, new RasterizerStateDescription() { CullMode = CullMode.None, FillMode = FillMode.Solid });
            device.ImmediateContext.Rasterizer.SetViewports(new[] { new ViewportF(0, 0, resolution.Width, resolution.Height) });
            device.ImmediateContext.Rasterizer.SetScissorRectangles(new[] { new SharpDX.Rectangle(0, 0, resolution.Width, resolution.Height) });
            device.ImmediateContext.OutputMerger.DepthStencilState = depthStencilState;
            device.ImmediateContext.OutputMerger.SetTargets(depthStencilView, renderTargetView);

            /* Fill in the constant buffer. */
            DataStream input;
            device.ImmediateContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out input);
            input.Write<Matrix>(Matrix.Transpose(camera.viewMatrix * camera.projMatrix));
            input.Write<Vector4>(new Vector4(camera.cameraPosition, 1));
            input.Write<Vector4>(new Vector4(camera.eyeVector, 1));
            device.ImmediateContext.UnmapSubresource(constantBuffer, 0);

            /* Set up the device state. */
            device.ImmediateContext.PixelShader.Set(renderPS.pixelShader);
            device.ImmediateContext.VertexShader.Set(renderVS.vertexShader);
            device.ImmediateContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            device.ImmediateContext.InputAssembler.InputLayout = renderVS.inputLayout;
            device.ImmediateContext.VertexShader.SetConstantBuffer(0, constantBuffer);
            device.ImmediateContext.PixelShader.SetConstantBuffer(0, constantBuffer);
            device.ImmediateContext.PixelShader.SetShaderResource(0, null);
            device.ImmediateContext.PixelShader.SetShaderResource(1, null);
            device.ImmediateContext.PixelShader.SetShaderResource(2, null);

            /* Render the chunk. */
            device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding[] { new VertexBufferBinding(vertexBuffer, 32, 0) });

            device.ImmediateContext.Draw(6, 0);

            device.ImmediateContext.OutputMerger.SetTargets((DepthStencilView)null, (RenderTargetView)null);
        }
    }
}
