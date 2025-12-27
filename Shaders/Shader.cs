
namespace SharpShaders.Shaders
{
    public partial class Shader : IDisposable
    {
        #region Main Shader Functions
        public bool initalised = false;
        public string vertexSource { get; private set; }
        public string fragmentSource { get; private set; }
        public string name { get; set; }
        public int Handle { get; set; }
        private bool disposed = false;
        public ShaderUniformManager UniformManager;

        public Shader(string vertexSource, string fragmentSource, string name = "")
        {
            this.vertexSource = "#version 330 core\n" + vertexSource;
            this.fragmentSource = "#version 330 core\n" + fragmentSource;
            this.name = name ?? "";
        }

        public Shader Init()
        {
            Handle = GL.CreateProgram();

            this.UniformManager = new ShaderUniformManager(Handle);

            int vertShaderPointer = HandleShader(vertexSource, ShaderType.VertexShader);

            int fragShaderPointer = HandleShader(fragmentSource, ShaderType.FragmentShader);

            GL.AttachShader(Handle, vertShaderPointer);
            GL.AttachShader(Handle, fragShaderPointer);

            // init the geometry shader, if we have one
            InitGeometry();

            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int shaderLinkSuccess);

            if (shaderLinkSuccess == 0)
            {
                string errorLog = GL.GetProgramInfoLog(Handle);
                new LogBuilder()
                .WriteLine($"An error occured while loading shaders for {Handle}!")
                .WriteLine("Error log:")
                .WriteLine(errorLog)
                .Log();

            }

            GL.DetachShader(Handle, vertShaderPointer);
            GL.DetachShader(Handle, fragShaderPointer);
            GL.DeleteShader(vertShaderPointer);
            GL.DeleteShader(fragShaderPointer);

            initalised = true;
            return this;
        }

        private int HandleShader(string source, ShaderType type)
        {
            Logger.BeginTimingBlock();
            Logger.PushIndentLevel();

            int shaderPointer = GL.CreateShader(type);

            GL.ShaderSource(shaderPointer, source);
            GL.CompileShader(shaderPointer);

            GL.GetShader(shaderPointer, ShaderParameter.CompileStatus, out int shaderSuccess);

            if (shaderSuccess == 0)
            {
                string errorLog = GL.GetShaderInfoLog(shaderPointer);
                new LogBuilder()
                .WriteLine($"An error occured while loading shaders for {Handle}!")
                .WriteLine("Error log:")
                .WriteLine(errorLog)
                .WriteLine("Shader:")
                .WriteLine(source)
                .Log();

                //Logger.Log(
                //    $"An error occurred while compiling shader for {LogColors.BrightWhite(Handle)}!\nError log:\n{errorLog}",
                //    LogLevel.Error
                //);
            }

            Logger.Log(
                $"Compiled {type} for {Handle} :{(string.IsNullOrEmpty(name) ? "" : " " + name)} in {Logger.EndTimingBlockFormatted()}"
            );

            Logger.PopIndentLevel();

            return shaderPointer;
        }


        ~Shader()
        {
            if (disposed == false)
            {
                Logger.Log(
                    $"GPU Resource leak for shader! Did you forget to call Dispose()?"
                );
            }
        }

        public void Use()
        {
            if (initalised == false)
            {
                Logger.Log(
                    $"Shader with {Handle} used without initalisation, initalising.."
                );
                Init();
            }
            GL.UseProgram(Handle);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                GL.DeleteProgram(Handle);
                Logger.Log(
                    $"Disposed shader {Handle}"
                );
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
