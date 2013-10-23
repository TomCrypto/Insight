using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SharpDX;
using SharpDX.Windows;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;

namespace Sample
{
    /// <summary>
    /// A RenderForm which forwards mouse and keyboard events
    /// to the AntTweakBar library, to interact with bars.
    /// </summary>
    [System.ComponentModel.DesignerCategory("")]
    public class AntRenderForm : RenderForm
    {
        public AntRenderForm(String title)
            : base(title)
        {

        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (!AntTweakBar.TwEventWin(m.HWnd, m.Msg, m.WParam, m.LParam)) base.WndProc(ref m);
        }
    }

    /// <summary>
    /// Wrapper around a single tweak bar for DirectX 11 with SharpDX. Note
    /// it does not do full error checking, only upon bar creation.
    /// </summary>
    public class TweakBar : IDisposable
    {
        #region Variable

        /// <summary>
        /// A generic bar variable.
        /// </summary>
        private abstract class Variable : IDisposable
        {
            protected readonly IntPtr bar;
            protected readonly String name;

            /* Must maintain strong references to callbacks. */
            protected AntTweakBar.TwGetVarCallback getCallback;
            protected AntTweakBar.TwSetVarCallback setCallback;

            /// <summary>
            /// Creates a new variable.
            /// </summary>
            /// <param name="bar">The bar handle.</param>
            /// <param name="name">The variable name.</param>
            public Variable(IntPtr bar, String name)
            {
                this.bar  = bar;
                this.name = name;

                getCallback = GetCallback;
                setCallback = SetCallback;
            }

            protected abstract void SetCallback(IntPtr value, IntPtr clientData);
            protected abstract void GetCallback(IntPtr value, IntPtr clientData);

            /// <summary>
            /// Gets or sets the value of this variable.
            /// </summary>
            public abstract Object Value
            {
                get;
                set;
            }

            #region IDisposable

            /// <summary>
            /// Destroys this Variable instance.
            /// </summary>
            ~Variable()
            {
                Dispose(false);
            }

            /// <summary>
            /// Disposes of this variable.
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
                    AntTweakBar.TwRemoveVar(bar, name);
                }
            }

            #endregion
        }

        #endregion

        #region IntegerVariable

        /// <summary>
        /// An integer scalar variable.
        /// </summary>
        private class IntegerVariable : Variable
        {
            /// <summary>
            /// Backing store for the integer scalar value.
            /// </summary>
            private int val;

            public IntegerVariable(IntPtr bar, String name, String group, int minValue, int maxValue, int defaultValue, int step, String help)
                : base(bar, name)
            {
                val = defaultValue;

                String definition = String.Format("min={0} max={1} step={2} group='{3}' help='{4}'", minValue, maxValue, step, group, help);
                AntTweakBar.TwAddVarCB(bar, name, AntTweakBar.Type.TW_TYPE_INT32, setCallback, getCallback, IntPtr.Zero, definition);
            }

            protected override void SetCallback(IntPtr value, IntPtr clientData)
            {
                val = Marshal.ReadInt32(value);
            }

            protected override void GetCallback(IntPtr value, IntPtr clientData)
            {
                Marshal.WriteInt32(value, val);
            }

            /// <summary>
            /// Gets or sets the value of this variable. The
            /// Object reference must be of type Int32.
            /// </summary>
            public override Object Value
            {
                get
                {
                    return val;
                }

                set
                {
                    if (value is Int32) { val = (Int32)value; return; }
                    throw new ArgumentException("Expected Int32 value.");
                }
            }
        }

        #endregion

        #region FloatVariable

        /// <summary>
        /// A floating-point (double precision) scalar variable.
        /// </summary>
        private class FloatVariable : Variable
        {
            /// <summary>
            /// Backing store for the floating-point scalar value.
            /// </summary>
            private double val;

            public FloatVariable(IntPtr bar, String name, String group, double minValue, double maxValue, double defaultValue, double step, double precision, String help)
                : base(bar, name)
            {
                val = defaultValue;

                String definition = String.Format("min={0} max={1} step={2} precision={3} group='{4}' help='{5}'", minValue, maxValue, step, precision, group, help);
                AntTweakBar.TwAddVarCB(bar, name, AntTweakBar.Type.TW_TYPE_DOUBLE, setCallback, getCallback, IntPtr.Zero, definition);
            }

            protected override void SetCallback(IntPtr value, IntPtr clientData)
            {
                double[] dest = new double[1];
                Marshal.Copy(value, dest, 0, 1);
                val = dest[0];
            }

            protected override void GetCallback(IntPtr value, IntPtr clientData)
            {
                Marshal.Copy(new[] { val }, 0, value, 1);
            }

            /// <summary>
            /// Gets or sets the value of this variable. The
            /// Object reference must be of type Double.
            /// </summary>
            public override Object Value
            {
                get
                {
                    return val;
                }

                set
                {
                    if (value is Double) { val = (Double)value; return; }
                    throw new ArgumentException("Expected Double value.");
                }
            }
        }

        #endregion

        #region BooleanVariable

        /// <summary>
        /// A boolean variable.
        /// </summary>
        private class BooleanVariable : Variable
        {
            /// <summary>
            /// Backing store for the boolean value.
            /// </summary>
            private bool val;

            public BooleanVariable(IntPtr bar, String name, String group, String trueLabel, String falseLabel, bool defaultValue, String help)
                : base(bar, name)
            {
                val = defaultValue;

                String definition = String.Format("true={0} false={1} group='{2}' help='{3}'", trueLabel, falseLabel, group, help);
                AntTweakBar.TwAddVarCB(bar, name, AntTweakBar.Type.TW_TYPE_BOOL32, setCallback, getCallback, IntPtr.Zero, definition);
            }

            protected override void SetCallback(IntPtr value, IntPtr clientData)
            {
                val = (Marshal.ReadInt32(value) != 0);
            }

            protected override void GetCallback(IntPtr value, IntPtr clientData)
            {
                Int32 v = (val ? 1 : 0);
                Marshal.WriteInt32(value, v);
            }

            /// <summary>
            /// Gets or sets the value of this variable. The
            /// Object reference must be of type Boolean.
            /// </summary>
            public override Object Value
            {
                get
                {
                    return val;
                }

                set
                {
                    if (value is Boolean) { val = (Boolean)value; return; }
                    throw new ArgumentException("Expected Boolean value.");
                }
            }
        }

        #endregion

        #region DirectionVariable

        /// <summary>
        /// A 3D direction variable.
        /// </summary>
        private class DirectionVariable : Variable
        {
            /// <summary>
            /// Backing store for the direction value.
            /// </summary>
            private Vector3 val;

            public DirectionVariable(IntPtr bar, String name, String group, Vector3 defaultValue, String help)
                : base(bar, name)
            {
                val = defaultValue;

                String definition = String.Format("group='{0}' help='{1}'", group, help);
                AntTweakBar.TwAddVarCB(bar, name, AntTweakBar.Type.TW_TYPE_DIR3D, setCallback, getCallback, IntPtr.Zero, definition);
            }

            protected override void SetCallback(IntPtr value, IntPtr clientData)
            {
                double[] dest = new double[3];
                Marshal.Copy(value, dest, 0, 3);
                val = new Vector3((float)dest[0],
                                  (float)dest[1],
                                  (float)dest[2]);
            }

            protected override void GetCallback(IntPtr value, IntPtr clientData)
            {
                Marshal.Copy(new[] { val.X, val.Y, val.Z }, 0, value, 1);
            }

            /// <summary>
            /// Gets or sets the value of this variable. The
            /// Object reference must be of type Vector3.
            /// </summary>
            public override Object Value
            {
                get
                {
                    return val;
                }

                set
                {
                    if (value is Vector3) { val = (Vector3)value; return; }
                    throw new ArgumentException("Expected Vector3 value.");
                }
            }
        }

        #endregion

        #region ColorVariable

        /// <summary>
        /// An RGB color variable.
        /// </summary>
        private class ColorVariable : Variable
        {
            /// <summary>
            /// Backing store for the direction value.
            /// </summary>
            private Color3 val;

            public ColorVariable(IntPtr bar, String name, String group, Color3 defaultValue, String help)
                : base(bar, name)
            {
                val = defaultValue;

                String definition = String.Format("group='{0}' help='{1}'", group, help);
                AntTweakBar.TwAddVarCB(bar, name, AntTweakBar.Type.TW_TYPE_COLOR3F, setCallback, getCallback, IntPtr.Zero, definition);
            }

            protected override void SetCallback(IntPtr value, IntPtr clientData)
            {
                float[] dest = new float[3];
                Marshal.Copy(value, dest, 0, 3);
                val = new Color3(dest[0], dest[1], dest[2]);
            }

            protected override void GetCallback(IntPtr value, IntPtr clientData)
            {
                Marshal.Copy(new[] { val.Red, val.Green, val.Blue }, 0, value, 1);
            }

            /// <summary>
            /// Gets or sets the value of this variable. The
            /// Object reference must be of type Color3.
            /// </summary>
            public override Object Value
            {
                get
                {
                    return val;
                }

                set
                {
                    if (value is Vector3) { val = (Color3)value; return; }
                    throw new ArgumentException("Expected Color3 value.");
                }
            }
        }

        #endregion

        private readonly RenderForm window;

        /// <summary>
        /// Gets the window this bar is to be rendered to.
        /// </summary>
        public RenderForm Window { get { return window; } }

        private readonly IntPtr bar;

        /// <summary>
        /// Gets the handle of this tweak bar.
        /// </summary>
        public IntPtr BarHandle { get { return bar; } }

        private Dictionary<String, Variable> variables = new Dictionary<String, Variable>();
        
        /// <summary>
        /// Creates a new tweak bar.
        /// </summary>
        /// <param name="window">The window to which to render the bar.</param>
        /// <param name="barName">The bar's name.</param>
        public TweakBar(RenderForm window, String barName)
        {
            this.window = window;
            bar = AntTweakBar.TwNewBar(barName);
            if (bar == null) throw new ExternalException("Failed to create new tweak bar.");
        }

        /// <summary>
        /// Adds an integer scalar variable.
        /// </summary>
        /// <param name="identifier">The variable identifier.</param>
        /// <param name="name">The variable's display name.</param>
        /// <param name="group">The variable's group.</param>
        /// <param name="minValue">The minimum value.</param>
        /// <param name="maxValue">The maximum value.</param>
        /// <param name="defaultValue">The default (initial) value.</param>
        /// <param name="step">The interval at which the variable changes.</param>
        /// <param name="help">An optional help string.</param>
        public void AddInteger(String identifier, String name, String group, int minValue, int maxValue, int defaultValue, int step = 1, String help = "")
        {
            if (variables.ContainsKey(identifier)) throw new ArgumentException("Variable identifier already defined.");
            variables.Add(identifier, new IntegerVariable(bar, name, group, minValue, maxValue, defaultValue, step, help));
        }

        /// <summary>
        /// Adds a floating-point scalar variable.
        /// </summary>
        /// <param name="identifier">The variable identifier.</param>
        /// <param name="name">The variable's display name.</param>
        /// <param name="group">The variable's group.</param>
        /// <param name="minValue">The minimum value.</param>
        /// <param name="maxValue">The maximum value.</param>
        /// <param name="defaultValue">The default (initial) value.</param>
        /// <param name="step">The interval at which the variable changes.</param>
        /// <param name="precision">The number of significant digits printed after the period.</param>
        /// <param name="help">An optional help string.</param>
        public void AddFloat(String identifier, String name, String group, double minValue, double maxValue, double defaultValue, double step = 0.1, double precision = 2, String help = "")
        {
            if (variables.ContainsKey(identifier)) throw new ArgumentException("Variable identifier already defined.");
            variables.Add(identifier, new FloatVariable(bar, name, group, minValue, maxValue, defaultValue, step, precision, help));
        }

        /// <summary>
        /// Adds a floating-point scalar variable.
        /// </summary>
        /// <param name="identifier">The variable identifier.</param>
        /// <param name="name">The variable's display name.</param>
        /// <param name="group">The variable's group.</param>
        /// <param name="trueLabel">The label to display when the variable is set to true.</param>
        /// <param name="falseLabel">The label to display when the variable is set to false.</param>
        /// <param name="defaultValue">The default (initial) value.</param>
        /// <param name="help">An optional help string.</param>
        public void AddBoolean(String identifier, String name, String group, String trueLabel, String falseLabel, bool defaultValue, String help = "")
        {
            if (variables.ContainsKey(identifier)) throw new ArgumentException("Variable identifier already defined.");
            variables.Add(identifier, new BooleanVariable(bar, name, group, trueLabel, falseLabel, defaultValue, help));
        }

        /// <summary>
        /// Adds a 3D direction variable.
        /// </summary>
        /// <param name="identifier">The variable identifier.</param>
        /// <param name="name">The variable's display name.</param>
        /// <param name="group">The variable's group.</param>
        /// <param name="defaultValue">The default (initial) value.</param>
        /// <param name="help">An optional help string.</param>
        public void AddDirection(String identifier, String name, String group, Vector3 defaultValue, String help = "")
        {
            if (variables.ContainsKey(identifier)) throw new ArgumentException("Variable identifier already defined.");
            variables.Add(identifier, new DirectionVariable(bar, name, group, defaultValue, help));
        }

        /// <summary>
        /// Adds an RGB color variable.
        /// </summary>
        /// <param name="identifier">The variable identifier.</param>
        /// <param name="name">The variable's display name.</param>
        /// <param name="group">The variable's group.</param>
        /// <param name="defaultValue">The default (initial) value.</param>
        /// <param name="help">An optional help string.</param>
        public void AddColor(String identifier, String name, String group, Color3 defaultValue, String help = "")
        {
            if (variables.ContainsKey(identifier)) throw new ArgumentException("Variable identifier already defined.");
            variables.Add(identifier, new ColorVariable(bar, name, group, defaultValue, help));
        }

        /// <summary>
        /// Checks if a variable identifier is defined.
        /// </summary>
        /// <param name="identifier">The variable identifier.</param>
        /// <returns>Returns True if the bar contains this variable.</returns>
        public bool HasVariable(String identifier)
        {
            return variables.ContainsKey(identifier);
        }

        /// <summary>
        /// Removes a variable from the bar.
        /// </summary>
        /// <param name="identifier">The variable identifier.</param>
        public void RemoveVariable(String identifier)
        {
            if (!variables.ContainsKey(identifier)) throw new ArgumentException("No such variable.");
            variables[identifier].Dispose();
            variables.Remove(identifier);
        }

        /// <summary>
        /// Gets or sets the value of a bar variable. You are expected to
        /// know the type of the variable you are querying. This property
        /// will throw an exception if you set an invalid variable type.
        /// </summary>
        /// <param name="name">Identifier of the variable.</param>
        /// <returns></returns>
        public Object this[String identifier]
        {
            get
            {
                if (!variables.ContainsKey(identifier))
                    throw new ArgumentException("No such variable.");

                return variables[identifier].Value;
            }

            set
            {
                if (!variables.ContainsKey(identifier))
                    throw new ArgumentException("No such variable.");

                variables[identifier].Value = value;
            }
        }
        
        #region IDisposable

        /// <summary>
        /// Destroys this Renderer instance.
        /// </summary>
        ~TweakBar()
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
                AntTweakBar.TwDeleteBar(bar);
            }
        }

        #endregion

        #region Static Library Management

        /// <summary>
        /// Initializes the AntTweakBar library.
        /// </summary>
        /// <param name="device">SharpDX DirectX 11 device to use.</param>
        /// <returns>True if the call was successful, false otherwise.</returns>
        public static bool InitializeLibrary(Device device)
        {
            return AntTweakBar.Init(AntTweakBar.GraphAPI.D3D11, device.NativePointer);
        }

        /// <summary>
        /// Notify the AntTweakBar library of any window changes.
        /// </summary>
        /// <param name="window">The render window.</param>
        /// <returns>True if the call was successful, false otherwise.</returns>
        public static bool UpdateWindow(RenderForm window)
        {
            return AntTweakBar.TwWindowSize(window.ClientSize.Width, window.ClientSize.Height);
        }

        /// <summary>
        /// Renders all TweakBars.
        /// </summary>
        public static void Render()
        {
            AntTweakBar.TwDraw();
        }

        /// <summary>
        /// Finalizes the AntTweakBar library.
        /// </summary>
        public static void FinalizeLibrary()
        {
            AntTweakBar.TwTerminate();
        }

        #endregion
    }

    #region PInvoke

    /// <summary>
    /// Provides raw access to AntTweakBar functions.
    /// </summary>
    public static class AntTweakBar
    {
        /// <summary>
        /// These constants are used by TwInit to specify which graphic API is used.
        /// </summary>
        public enum GraphAPI
        {
            /// <summary>
            /// Tell AntTweakBar to use OpenGL (compatibility profile).
            /// </summary>
            OpenGL      = 1,

            /// <summary>
            /// Tell AntTweakBar to use Direct3D 9. In this case, the D3D9 device pointer must also be supplied to TwInit.
            /// </summary>
            D3D9        = 2,

            /// <summary>
            /// Tell AntTweakBar to use Direct3D 10. In this case, the D3D10 device pointer must also be supplied to TwInit.
            /// </summary>
            D3D10       = 3,

            /// <summary>
            /// Tell AntTweakBar to use Direct3D 11. In this case, the D3D11 device pointer must also be supplied to TwInit.
            /// </summary>
            D3D11       = 4,

            /// <summary>
            /// Tell AntTweakBar to use OpenGL core profile (OpenGL 3.2 and higher).
            /// </summary>
            OpenGLCore  = 5
        }

        /// <summary>
        /// This function initializes the AntTweakBar library. It must be called once at the beginning of the program, just after graphic mode is initialized.
        /// </summary>
        /// <param name="graphAPI">This parameter specifies which graphic API is used: OpenGL, OpenGL core profile (3.2 and higher), Direct3D 9, Direct3D 10 or Direct3D 11. It is one of the TwGraphAPI enum element. If graphAPI is Direct3D, the D3D device pointer must be supplied.</param>
        /// <param name="device">Pointer to the Direct3D device, or NULL for OpenGL. If graphAPI is OpenGL, this parameter must be NULL, otherwise it is the IDirect3DDevice9, ID3D10Device or ID3D11Device pointer returned by the appropriate D3D CreateDevice function when D3D has been initialized.</param>
        [DllImport("AntTweakBar.dll", EntryPoint = "TwInit")]
        public static extern bool Init(GraphAPI graphAPI, IntPtr device);

        /// <summary>
        /// Uninitialize the AntTweakBar API. Must be called at the end of the program, before terminating the graphics API.
        /// </summary>
        [DllImport("AntTweakBar.dll")]
        public static extern int TwTerminate();

        /// <summary>
        /// Draws all the created tweak bars.
        /// </summary>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwDraw();

        /// <summary>
        /// Call this function to inform AntTweakBar of the size of the application graphics window, or to restore AntTweakBar graphics resources (after a fullscreen switch for instance).
        /// </summary>
        /// <param name="width">Width of the graphics window.</param>
        /// <param name="height">Height of the graphics window.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwWindowSize(int width, int height);

        /// <summary>
        /// This function is intended to be used by applications with multiple graphical windows. It tells AntTweakBar to switch its current context to the context associated to the identifier windowID. All AntTweakBar functions (except TwTerminate) called after the switch would be executed in this context. If the context does not exist (ie., if this is the first time that TwSetCurrentWindow is called for this windowID), it is created.
        /// </summary>
        /// <param name="windowID">Window context identifier. This identifier could be any integer. The window context identifier 0 always exist, this is the default context created when AntTweakBar is initialized through TwInit.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwSetCurrentWindow(int windowID);

        /// <summary>
        /// Returns the current window context identifier previously set by TwSetCurrentWindow.
        /// </summary>
        /// <returns>Current window context identifier. 0 denotes the default context created by TwInit at initialization.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern int TwGetCurrentWindow();

        /// <summary>
        /// Check if a window context associated to the identifier windowID exists. A window context exists if it has previously been created by TwSetCurrentWindow.
        /// </summary>
        /// <param name="windowID">Window context identifier. The window context identifier 0 always exist, this is the default context created when AntTweakBar is initialized through TwInit.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwWindowExists(int windowID);

        /// <summary>
        /// Creates a new tweak bar. The AntTweakBar library must have been initialized (by calling TwInit) before creating a bar.
        /// </summary>
        /// <param name="barName">Name of the new tweak bar.</param>
        /// <returns>Tweak bar identifier. It is a pointer to an internal TwBar structure.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern IntPtr TwNewBar(string barName);

        /// <summary>
        /// This function deletes a tweak bar previously created by TwNewBar.
        /// </summary>
        /// <param name="bar">Identifier to the tweak bar to delete.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwDeleteBar(IntPtr bar);

        /// <summary>
        /// Delete all bars previously created by TwNewBar.
        /// </summary>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwDeleteAllBars();

        /// <summary>
        /// Set the specified bar as the foreground bar. It will be displayed on top of the other bars.
        /// </summary>
        /// <param name="bar">Bar identifier.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwSetTopBar(IntPtr bar);

        /// <summary>
        /// Returns the identifier of the current foreground bar (the bar displayed on top of the others).
        /// </summary>
        /// <returns>Null if no bar is displayed. Otherwise, pointer to the top bar.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern IntPtr TwGetTopBar();

        /// <summary>
        /// Set the specified bar as the background bar. It will be displayed behind the other bars.
        /// </summary>
        /// <param name="bar">Bar identifier.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwSetBottomBar(IntPtr bar);

        /// <summary>
        /// Returns the identifier of the current background bar (the bar displayed behind the others).
        /// </summary>
        /// <returns>Null if no bar is displayed. Otherwise, pointer to the bottom bar.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern IntPtr TwGetBottomBar();

        /// <summary>
        /// Returns the name of a given tweak bar.
        /// </summary>
        /// <param name="bar">Identifier to the tweak bar.</param>
        /// <returns>Name of the bar, or null if an error occurs.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern string TwGetBarName(IntPtr bar);

        /// <summary>
        /// Returns the number of created bars.
        /// </summary>
        /// <returns>Number of bars.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern int TwGetBarCount();

        /// <summary>
        /// Returns the bar of index barIndex.
        /// </summary>
        /// <param name="barIndex">Index of the requested bar. barIndex must be between 0 and TwGetBarCount().</param>
        /// <returns>Bar identifier (a pointer to an internal TwBar structure), or NULL if the index is out of range.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern IntPtr TwGetBarByIndex(int barIndex);

        /// <summary>
        /// Returns the bar named barName.
        /// </summary>
        /// <param name="barName">Name of the requested bar (a zero-terminated c string).</param>
        /// <returns>Bar identifier (a pointer to an internal TwBar structure), or NULL if the bar is not found.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern IntPtr TwGetBarByName(string barName);

        /// <summary>
        /// Forces bar content to be updated. By default bar content is periodically refreshed when TwDraw is called (the update frequency is defined by the bar parameter refresh). This function may be called to force a bar to be immediately refreshed at next TwDraw call.
        /// </summary>
        /// <param name="bar">Bar identifier.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwRefreshBar(IntPtr bar);

        /// <summary>
        /// Constants used by TwKeyPressed to denote modifier key codes.
        /// </summary>
        public enum KeyModifier
        {
            /// <summary>
            /// No modifier pressed.
            /// </summary>
            TW_KMOD_NONE        = 0x0000,

            /// <summary>
            /// Left or right SHIFT key pressed.
            /// </summary>
            TW_KMOD_SHIFT       = 0x0003,

            /// <summary>
            /// Left or right CTRL key pressed.
            /// </summary>
            TW_KMOD_CTRL        = 0x00c0,

            /// <summary>
            /// Left or right ALT key pressed.
            /// </summary>
            TW_KMOD_ALT         = 0x0100,

            TW_KMOD_META        = 0x0c00
        }

        /// <summary>
        /// Constants used by TwKeyPressed to denote special key codes.
        /// </summary>
        public enum KeySpecial
        {
            TW_KEY_BACKSPACE    = '\b',
            TW_KEY_TAB          = '\t',
            TW_KEY_CLEAR        = 0x0c,
            TW_KEY_RETURN       = '\r',
            TW_KEY_PAUSE        = 0x13,
            TW_KEY_ESCAPE       = 0x1b,
            TW_KEY_SPACE        = ' ',
            TW_KEY_DELETE       = 0x7f,
            TW_KEY_UP           = 273,
            TW_KEY_DOWN,
            TW_KEY_RIGHT,
            TW_KEY_LEFT,
            TW_KEY_INSERT,
            TW_KEY_HOME,
            TW_KEY_END,
            TW_KEY_PAGE_UP,
            TW_KEY_PAGE_DOWN,
            TW_KEY_F1,
            TW_KEY_F2,
            TW_KEY_F3,
            TW_KEY_F4,
            TW_KEY_F5,
            TW_KEY_F6,
            TW_KEY_F7,
            TW_KEY_F8,
            TW_KEY_F9,
            TW_KEY_F10,
            TW_KEY_F11,
            TW_KEY_F12,
            TW_KEY_F13,
            TW_KEY_F14,
            TW_KEY_F15,
            TW_KEY_LAST
        }

        /// <summary>
        /// Call this function to inform AntTweakBar when a keyboard event occurs.
        /// </summary>
        /// <param name="key">The ASCII code of the pressed key, or one of the TwKeySpecial codes.</param>
        /// <param name="modifiers">One or a OR-ed combination of the TwKeyModifier constants.</param>
        /// <returns>True if the event has been handled by AntTweakBar, false otherwise.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwKeyPressed(int key, AntTweakBar.KeyModifier modifiers);

        /// <summary>
        /// This function checks if a key event would be processed by TwKeyPressed but without processing it. TwKeyTest could be helpful to prevent bad handling report, for instance when processing WM_KEYUP and WM_KEYDOWN in windows event loop (see file src/TwEventWin.c).
        /// </summary>
        /// <param name="key">The ASCII code of the key, or one of the TwKeySpecial codes.</param>
        /// <param name="modifiers">One or a OR-ed combination of the TwKeyModifier constants.</param>
        /// <returns>True if the event would have been handled by AntTweakBar, false otherwise.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwKeyTest(int key, AntTweakBar.KeyModifier modifiers);

        /// <summary>
        /// Constants used by TwMouseButton to denote a mouse button action.
        /// </summary>
        public enum MouseAction
        {
            /// <summary>
            /// Mouse button is released.
            /// </summary>
            TW_MOUSE_RELEASED,

            /// <summary>
            /// Mouse button is pressed.
            /// </summary>
            TW_MOUSE_PRESSED,
        }

        /// <summary>
        /// Constants used by TwMouseButton to identify a mouse button.
        /// </summary>
        public enum MouseButtonID
        {
            /// <summary>
            /// The left mouse button.
            /// </summary>
            TW_MOUSE_LEFT       = 1,

            /// <summary>
            /// The middle mouse button.
            /// </summary>
            TW_MOUSE_MIDDLE     = 2,

            /// <summary>
            /// The right mouse button.
            /// </summary>
            TW_MOUSE_RIGHT      = 3,
        }

        /// <summary>
        /// Call this function to inform AntTweakBar that a mouse button is pressed.
        /// </summary>
        /// <param name="action">Tells if the button is pressed or released. It is one of the TwMouseAction constants.</param>
        /// <param name="button">Tells which button is pressed. It is one of the TwMouseButtonID constants.</param>
        /// <returns>True if the event has been handled by AntTweakBar, false otherwise.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwMouseButton(MouseAction action, MouseButtonID button);

        /// <summary>
        /// Call this function to inform AntTweakBar that the mouse has moved.
        /// </summary>
        /// <param name="mouseX">The new X position of the mouse, relative to the left border of the graphics window.</param>
        /// <param name="mouseY">The new Y position of the mouse, relative to the top border of the graphics window.</param>
        /// <returns>True if the event has been handled by AntTweakBar, false otherwise.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwMouseMotion(int mouseX, int mouseY);

        /// <summary>
        /// Call this function to inform AntTweakBar that the mouse wheel has been used.
        /// </summary>
        /// <param name="pos">The new position of the wheel.</param>
        /// <returns>True if the event has been handled by AntTweakBar, false otherwise.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwMouseWheel(int pos);

        /// <summary>
        /// Constants used by TwSetParam and TwGetParam to specify the type of parameter(s) value(s).
        /// </summary>
        public enum ParamValueType
        {
            /// <summary>
            /// Parameter(s) of type int (32 bits).
            /// </summary>
            TW_PARAM_INT32,

            /// <summary>
            /// Parameter(s) of type float.
            /// </summary>
            TW_PARAM_FLOAT,

            /// <summary>
            /// Parameter(s) of type double.
            /// </summary>
            TW_PARAM_DOUBLE,

            /// <summary>
            /// Parameter of type C-string, i.e, a zero-terminated array of char.
            /// </summary>
            TW_PARAM_CSTRING
        }

        /// <summary>
        /// This function returns the current value of a bar or variable parameter. Parameters define the behavior of bars and vars and may have been set or modified by functions TwDefine, TwAddVar* or TwSetParam.
        /// </summary>
        /// <param name="bar">Bar identifier. If the requested parameter is global (not linked to a particular bar), NULL may be used as identifier.</param>
        /// <param name="varName">varName is the name of the parameter’s variable (ie., the unique name used to create the variable). If the parameter is directly related to a bar (and not specific to a var), varName should be NULL.</param>
        /// <param name="paramName">Name of the parameter. This is one of the key words listed in the bar parameters page if the parameter is directly related to a bar, or listed in the var parameters page if the parameter is related to a variable.</param>
        /// <param name="paramValueType">Type of the data to be stored in outValues. Should be one of the constants defined by TwParamValueType.</param>
        /// <param name="outValueMaxCount">Each parameter may have one or more values (eg., a position parameter has two values x and y). outValueMaxCount is the maximum number of output values that the function can write in the outValues buffer.</param>
        /// <param name="outValues">Pointer to the buffer that will be filled with the requested parameter values. The buffer must be large enough to contain at least outValueMaxCount values of type specified by paramValueType.</param>
        /// <returns>Number of parameter values returned in outValues. This number is less or equal to outValueMaxCount. Otherwise, 0 on error.</returns>
        [DllImport("AntTweakBar.dll")]
        public static extern int TwGetParam(IntPtr bar, string varName, string paramName, ParamValueType paramValueType, uint outValueMaxCount, IntPtr outValues);

        /// <summary>
        /// This function modifies the value(s) of a bar or variable parameter. Parameters define the behavior of bars and vars and may be set by functions TwDefine or TwAddVar* using a definition string. TwSetParam is an alternative to these functions avoiding the conversion of the new parameter value into a definition string.
        /// </summary>
        /// <param name="bar">Bar identifier. If the parameter to modify is global, NULL may be used as identifier.</param>
        /// <param name="varName">If the parameter is directly related to a bar, varName should be NULL. Otherwise, varName is the name of the parameter’s variable (ie., the unique name used to create the variable).</param>
        /// <param name="paramName">Name of the parameter. This is one of the key words listed in the bar parameters page if the parameter is directly related to a bar, or listed in the var parameters page if the parameter is related to a variable.</param>
        /// <param name="paramValueType">Type of the data pointed by inValues. Should be one of the constants defined by TwParamValueType: TW_PARAM_INT32, TW_PARAM_FLOAT, TW_PARAM_DOUBLE or TW_PARAM_CSTRING.</param>
        /// <param name="outValueMaxCount">Depending on the parameter, one or more values may be required to modify it. For instance, a state parameter requires one value while a rgb-color parameter requires 3 values. If the parameter value is a string, inValueCount must be 1 (not the length of the string), and the string must be an array of chars terminated by a zero (ie., a C-style string).</param>
        /// <param name="outValues">Pointer to the new parameter value(s). If there is more than one value, the values must be stored consecutively as an array starting at the address pointed by inValues.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwSetParam(IntPtr bar, string varName, string paramName, ParamValueType paramValueType, uint outValueMaxCount, IntPtr outValues);

        /// <summary>
        /// Constants used to declare the type of variables added to a tweak bar. Used by functions TwAddVarRW, TwAddVarRO and TwAddVarCB.
        /// </summary>
        public enum Type
        {
            TW_TYPE_UNDEF   = 0,

            /// <summary>
            /// A byte (8 bits) which represents a boolean value.
            /// </summary>
            TW_TYPE_BOOL8   = 2,

            /// <summary>
            /// A 16 bits variable which represents a boolean value.
            /// </summary>
            TW_TYPE_BOOL16,

            /// <summary>
            /// A 32 bits variable which represents a boolean value.
            /// </summary>
            TW_TYPE_BOOL32,

            /// <summary>
            /// A character (8 bits).
            /// </summary>
            TW_TYPE_CHAR,

            /// <summary>
            /// A 8 bits signed integer.
            /// </summary>
            TW_TYPE_INT8,

            /// <summary>
            /// A 8 bits unsigned integer.
            /// </summary>
            TW_TYPE_UINT8,

            /// <summary>
            /// A 16 bits signed integer.
            /// </summary>
            TW_TYPE_INT16,

            /// <summary>
            /// A 16 bits unsigned integer.
            /// </summary>
            TW_TYPE_UINT16,

            /// <summary>
            /// A 32 bits signed integer.
            /// </summary>
            TW_TYPE_INT32,

            /// <summary>
            /// A 32 bits unsigned integer.
            /// </summary>
            TW_TYPE_UINT32,

            /// <summary>
            /// A 32 bits floating point value (float).
            /// </summary>
            TW_TYPE_FLOAT,

            /// <summary>
            /// A 64 bits floating point value (double).
            /// </summary>
            TW_TYPE_DOUBLE,

            /// <summary>
            /// A color represented by a 32 bits integer. Order is RGBA if the graphic API is OpenGL or Direct3D10, and inversed if the graphic API is Direct3D9. R, G, B and A are respectively the Red, Green, Blue and Alpha channels. The alpha channel can be made editable by specifying alpha in the definition string of the variable. The order can be modified by specifying colorOrder=... in the definition string of the variable.
            /// </summary>
            TW_TYPE_COLOR32,

            /// <summary>
            /// A color represented by 3 floats in the range [0,1] and corresponding to the Red, Green and Blue color channels (in that order).
            /// </summary>
            TW_TYPE_COLOR3F,

            /// <summary>
            /// A color with transparency represented by 4 floats in the range [0,1] and corresponding to the Red, Green, Blue and Alpha channels (in that order).
            /// </summary>
            TW_TYPE_COLOR4F,

            /// <summary>
            /// A C-Dynamic null-terminated String. This null-terminated string can be allocated and reallocated dynamically (eg. by calling malloc/realloc/strdup...). So its pointer can change.
            /// </summary>
            TW_TYPE_CDSTRING,

            /// <summary>
            /// A rotation represented by a unit quaternion coded with 4 floats.
            /// </summary>
            TW_TYPE_QUAT4F = TW_TYPE_CDSTRING+2,

            /// <summary>
            /// A rotation represented by a unit quaternion coded with 4 doubles.
            /// </summary>
            TW_TYPE_QUAT4D,

            /// <summary>
            /// A direction represented by a 3D vector coded with 3 floats.
            /// </summary>
            TW_TYPE_DIR3F,

            /// <summary>
            /// A direction represented by a 3D vector coded with 3 doubles.
            /// </summary>
            TW_TYPE_DIR3D,
        };

        /// <summary>
        /// Callback function for when the user changes the variable value.
        /// </summary>
        /// <param name="value">The new variable value.</param>
        /// <param name="clientData">Associated client data.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TwSetVarCallback(IntPtr value, IntPtr clientData);

        /// <summary>
        /// Callback function for when AntTweakBar needs the variable value.
        /// </summary>
        /// <param name="value">Where to write the variable value.</param>
        /// <param name="clientData">Associated client data.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TwGetVarCallback(IntPtr value, IntPtr clientData);

        /// <summary>
        /// Callback function for when the user presses a button.
        /// </summary>
        /// <param name="clientData">Associated client data.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TwButtonCallback(IntPtr clientData);

        /// <summary>
        /// This function adds a new variable to a tweak bar by specifying the variable’s pointer. The variable is declared Read-Write (RW), so it could be modified interactively by the user.
        /// </summary>
        /// <param name="bar">The tweak bar to which adding a new variable.</param>
        /// <param name="name">The name of the variable. It will be displayed in the tweak bar if no label is specified for this variable. It will also be used to refer to this variable in other functions, so choose a unique, simple and short name and avoid special characters like spaces or punctuation marks.</param>
        /// <param name="type">Type of the variable. It must be one of the TwType constants or a user defined type created with TwDefineStruct or TwDefineEnum*.</param>
        /// <param name="var">Pointer to the variable linked to this entry.</param>
        /// <param name="def">An optional definition string used to modify the behavior of this new entry. This string must follow the variable parameters syntax, or set to NULL to get the default behavior. It could be set or modified later by calling the TwDefine or TwSetParam functions.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwAddVarRW(IntPtr bar, string name, Type type, IntPtr var, string def);

        /// <summary>
        /// This function adds a new variable to a tweak bar by specifying the variable’s pointer. The variable is declared Read-Only (RO), so it could <b>not</b> be modified interactively by the user.
        /// </summary>
        /// <param name="bar">The tweak bar to which adding a new variable.</param>
        /// <param name="name">The name of the variable. It will be displayed in the tweak bar if no label is specified for this variable. It will also be used to refer to this variable in other functions, so choose a unique, simple and short name and avoid special characters like spaces or punctuation marks.</param>
        /// <param name="type">Type of the variable. It must be one of the TwType constants or a user defined type created with TwDefineStruct or TwDefineEnum*.</param>
        /// <param name="var">Pointer to the variable linked to this entry.</param>
        /// <param name="def">An optional definition string used to modify the behavior of this new entry. This string must follow the variable parameters syntax, or set to NULL to get the default behavior. It could be set or modified later by calling the TwDefine or TwSetParam functions.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwAddVarRO(IntPtr bar, string name, Type type, IntPtr var, string def);

        /// <summary>
        /// This function adds a new variable to a tweak bar by providing CallBack (CB) functions to access it. If the setCallback parameter is set to NULL, the variable is declared Read-Only, so it could not be modified interactively by the user. Otherwise, it is a Read-Write variable, and could be modified interactively by the user.
        /// </summary>
        /// <param name="bar">The tweak bar to which adding a new variable.</param>
        /// <param name="name">The name of the variable. It will be displayed in the tweak bar if no label is specified for this variable. It will also be used to refer to this variable in other functions, so choose a unique, simple and short name and avoid special characters like spaces or punctuation marks.</param>
        /// <param name="type">Type of the variable. It must be one of the TwType constants or a user defined type created with TwDefineStruct or TwDefineEnum*.</param>
        /// <param name="setCallback">The callback function that will be called by AntTweakBar to change the variable’s value.</param>
        /// <param name="getCallback">The callback function that will be called by AntTweakBar to get the variable’s value.</param>
        /// <param name="clientData">For your convenience, this is a supplementary pointer that will be passed to the callback functions when they are called. For instance, if you set it to an object pointer, you can use it to access to the object’s members inside the callback functions.</param>
        /// <param name="def">An optional definition string used to modify the behavior of this new entry. This string must follow the variable parameters syntax, or set to NULL to get the default behavior. It could be set or modified later by calling the TwDefine or TwSetParam functions.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwAddVarCB(IntPtr bar, string name, Type type, TwSetVarCallback setCallback, TwGetVarCallback getCallback, IntPtr clientData, string def);

        /// <summary>
        /// This function adds a button entry to a tweak bar. When the button is clicked by a user, the callback function provided to TwAddButton is called.
        /// </summary>
        /// <param name="bar">The tweak bar to which adding a new variable.</param>
        /// <param name="name">The name of the button. It will be displayed in the tweak bar if no label is specified for this button. It will also be used to refer to this button in other functions, so choose a unique, simple and short name and avoid special characters like spaces or punctuation marks.</param>
        /// <param name="callback">The callback function that will be called by AntTweakBar when the button is clicked.</param>
        /// <param name="clientData">For your convenience, this is a supplementary pointer that will be passed to the callback function when it is called. For instance, if you set it to an object pointer, you can use it to access to the object’s members inside the callback function.</param>
        /// <param name="def">An optional definition string used to modify the behavior of this new entry. This string must follow the variable parameters syntax, or set to NULL to get the default behavior. It could be set or modified later by calling the TwDefine or TwSetParam functions.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwAddButton(IntPtr bar, string name, TwButtonCallback callback, IntPtr clientData, string def);

        /// <summary>
        /// This function adds a horizontal separator line to a tweak bar. It may be useful if one wants to separate several sets of variables inside a same group.
        /// </summary>
        /// <param name="bar">The tweak bar to which adding the separator.</param>
        /// <param name="name">The name of the separator. It is optional, this parameter can be set to NULL. But if you need to refer to this separator later in other commands, name it (like for other var names, choose a unique, simple and short name, and avoid special characters like spaces or punctuation marks).</param>
        /// <param name="def">An optional definition string used to modify the behavior of this new entry. This string must follow the variable parameters syntax, or set to NULL to get the default behavior. It could be set or modified later by calling the TwDefine or TwSetParam functions.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwAddSeparator(IntPtr bar, string name, string def);

        /// <summary>
        /// This function removes a variable, button or separator from a tweak bar.
        /// </summary>
        /// <param name="bar">The tweak bar from which to remove a variable.</param>
        /// <param name="name">The name of the variable. It is the same name as the one provided to the TwAdd* functions when the variable was added.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwRemoveVar(IntPtr bar, string name);

        /// <summary>
        /// This function removes all the variables, buttons and separators previously added to a tweak bar.
        /// </summary>
        /// <param name="bar">The tweak bar from which to remove all variables.</param>
        [DllImport("AntTweakBar.dll")]
        public static extern bool TwRemoveAllVars(IntPtr bar);

        [DllImport("AntTweakBar.dll")]
        public static extern bool TwEventWin(IntPtr wnd, int msg, IntPtr wParam, IntPtr lParam);
    }

    #endregion
}
