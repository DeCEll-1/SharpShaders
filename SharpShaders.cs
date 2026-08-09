global using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpShaders
{
    public class SharpS
    {
        private static double CurrTimeGLFW => GLFW.GetTime();
        public static TimeSpan CurrTime => TimeSpan.FromSeconds(CurrTimeGLFW);
        public static TextWriter LibLogWriter { get => Logger.Writer; set => Logger.Writer = value; }
        private static GameWindow? window;
        internal static readonly Vector2i windowSize = new(1, 1);
        public static void OpenOGL()
        {
            var settings = GameWindowSettings.Default;
            var nativeSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(1, 1),
                Title = "Hidden OpenGL Compute Context",
                WindowBorder = WindowBorder.Hidden,
                StartVisible = false,
                StartFocused = false,
            };

            window = new GameWindow(settings, nativeSettings);
            window.MakeCurrent();
            window.Context.SwapInterval = 0;
            Logger.LogOpenglAttributes();
        }

        public static void CloseOGLContext()
        {
            if (window != null)
                window.Dispose();
        }

    }
}
