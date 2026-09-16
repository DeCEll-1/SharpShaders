namespace SharpShaders.Exceptions
{
    /// <summary>
    /// Exception thrown when a GLSL shader fails to compile or link into an OpenGL program.
    /// </summary>
    public class ShaderCompilationException : Exception
    {
        /// <summary>The OpenGL handle (ID) of the shader or program that failed.</summary>
        public int ShaderHandle { get; }

        /// <summary>The compilation or linking log output from OpenGL.</summary>
        public string Log { get; }

        public ShaderCompilationException(string message, int shaderHandle, string log)
            : base(message)
        {
            ShaderHandle = shaderHandle;
            Log = log;
        }

        public ShaderCompilationException(string message, int shaderHandle, string log, Exception innerException)
            : base(message, innerException)
        {
            ShaderHandle = shaderHandle;
            Log = log;
        }
    }
}
