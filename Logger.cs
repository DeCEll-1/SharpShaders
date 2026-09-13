using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpShaders
{
    internal class Logger
    {
        // doing it this way instead of using field works on any SDK (and it's used for a project built for 8.0 anyway) --snark5885
        private static TextWriter writer = TextWriter.Synchronized(TextWriter.Null);
        internal static TextWriter Writer
        {
            get => writer;
            set => writer = TextWriter.Synchronized(value ?? TextWriter.Null);
        }
        public static unsafe void Log(string info)
        {
            OpenTK.Windowing.GraphicsLibraryFramework.Window* window = OpenTK.Windowing.GraphicsLibraryFramework.GLFW.GetCurrentContext();
            if (window != null)
            {
                ErrorCode error = GL.GetError();
                if (error != ErrorCode.NoError) LogWithoutGLErrorCheck(error.ToString());
            }

            LogWithoutGLErrorCheck(info);
        }

        public static void LogWithoutGLErrorCheck(string info)
        {
            LogReal(info);
        }
        private static void LogReal(string info)
        {
            string text = Indent + info;
            Writer.WriteLine(text);
        }

        private static Stack<double> startTimes = new Stack<double>();
        public static void BeginTimingBlock()
        {
            startTimes.Push(SharpS.CurrTime.TotalSeconds);
        }
        public static double EndTimingBlock()
            => SharpS.CurrTime.TotalSeconds - startTimes.Pop();
        public static string EndTimingBlockFormatted()
            => $"{EndTimingBlock():F3}ms";
        private const int IndentLenght = 2;
        private static int IndentLevel = 0;
        private static string Indent = "";
        public static void PushIndentLevel()
        {
            IndentLevel++;
            Indent = new(' ', IndentLevel * IndentLenght);
        }
        public static void PopIndentLevel()
        {
            IndentLevel--;
            Indent = new(' ', IndentLevel * IndentLenght);
        }

        private static Stack<long> startMemoryUsages = new Stack<long>();
        private static long CurrMemory => GC.GetTotalMemory(forceFullCollection: false);
        public static void BeginMemoryBlock()
        {
            startMemoryUsages.Push(CurrMemory);
        }
        public static long EndMemoryBlock()
        {
            return CurrMemory - startMemoryUsages.Pop();
        }
        public static string EndMemoryBlockFormatted()
        {
            long delta = EndMemoryBlock();
            return FormatBytes(delta) + " RAM";
        }
        public static string FormatBytes(long bytes)
        {
            bool isNegative = (bytes < 0);
            bytes = Math.Abs(bytes);
            const long KB = 1024;
            const long MB = 1024 * KB;
            const long GB = 1024 * MB;

            string negative = "";
            if (isNegative)
                negative = "-";

            if (bytes >= GB) return $"{negative}{bytes / (double)GB:F2} GB";
            if (bytes >= MB) return $"{negative}{bytes / (double)MB:F2} MB";
            if (bytes >= KB) return $"{negative}{bytes / (double)KB:F2} KB";

            return $"{negative}{bytes} B";
        }

        public static void PrintEmptyLine() => Writer.WriteLine();
        public static void LogOpenglAttributes()
        {
            // Single-value parameters
            LogInt(GetPName.MaxVertexAttribs, "Max Vertex Attribs");
            LogInt(GetPName.MaxTextureImageUnits, "Max Texture Image Units");
            LogInt(GetPName.MaxVertexUniformComponents, "Max Vertex Uniform Components");
            LogInt(GetPName.MaxFragmentUniformComponents, "Max Fragment Uniform Components");
            LogInt(GetPName.MaxCombinedTextureImageUnits, "Max Combined Texture Image Units");
            LogInt(GetPName.MaxTextureSize, "Max Texture Size");
            LogInt(GetPName.MaxCubeMapTextureSize, "Max Cube Map Texture Size");
            LogInt(GetPName.MaxRenderbufferSize, "Max Renderbuffer Size");
            LogInt(GetPName.MaxDrawBuffers, "Max Draw Buffers");
            LogInt(GetPName.MaxElementsIndices, "Max Elements Indices");
            LogInt(GetPName.MaxElementsVertices, "Max Elements Vertices");
            LogInt(GetPName.MaxUniformBufferBindings, "Max Uniform Buffer Bindings");
            LogInt(GetPName.MaxUniformBlockSize, "Max Uniform Block Size");
            LogInt(GetPName.MaxVertexUniformBlocks, "Max Vertex Uniform Blocks");
            LogInt(GetPName.MaxFragmentUniformBlocks, "Max Fragment Uniform Blocks");
            LogInt(GetPName.MaxCombinedUniformBlocks, "Max Combined Uniform Blocks");
            LogInt(GetPName.MaxSamples, "Max Samples");
            LogInt(GetPName.MaxVertexOutputComponents, "Max Vertex Output Components");
            LogInt(GetPName.MaxFragmentInputComponents, "Max Fragment Input Components");
            LogInt(GetPName.MaxColorAttachments, "Max Color Attachments");

            LogInt2(GetPName.MaxViewportDims, "Max Viewport Dims");
        }

        private static void LogInt(GetPName pname, string label)
        {
            int value = GL.GetInteger(pname);
            Logger.Log($"{label}: {value}");
        }
        private static void LogInt2(GetPName pname, string label)
        {
            int[] values = new int[2];
            GL.GetInteger(pname, values);
            Logger.Log($"{label}: {values[0]} x {values[1]}");
        }
    }
}
