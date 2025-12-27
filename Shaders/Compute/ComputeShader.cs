using System.Numerics;

namespace SharpShaders.Shaders.Compute
{
    public class ComputeShader : IDisposable
    { // https://learnopengl.com/Guest-Articles/2022/Compute-Shaders/Introduction
        public bool initalised = false;
        public string computeShaderSource;
        public int Handle;
        public ComputeShaderUnitManager UnitManager;
        public ShaderUniformManager UniformManager;
        private bool disposed = false;
        public Vector3 groupSize { get; private set; }

        public ComputeShader(string computeShaderSource)
        {
            this.computeShaderSource = computeShaderSource;
        }

        public void Init()
        {
            Logger.BeginTimingBlock();
            Handle = GL.CreateProgram();

            int computeShaderPointer = HandleComputeShader(computeShaderSource);

            GL.AttachShader(Handle, computeShaderPointer);

            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int shaderLinkSuccess);

            if (shaderLinkSuccess == 0)
            {
                string errorLog = GL.GetProgramInfoLog(Handle);
                Logger.Log(
                    $"An error occured while loading compute shader for {Handle}!\nError log:\n{errorLog}"
                );
            }


            // get the group size
            int[] size = new int[3];
            GL.GetProgram(Handle, (GetProgramParameterName)All.ComputeWorkGroupSize, size);
            groupSize = new Vector3(size[0], size[1], size[2]);

            GL.DetachShader(Handle, computeShaderPointer);
            GL.DeleteShader(computeShaderPointer);

            UnitManager = new ComputeShaderUnitManager(Handle);
            UniformManager = new ShaderUniformManager(Handle);
            initalised = true;

            Logger.Log(
                $"Loaded {ShaderType.ComputeShader} for {Handle} in {Logger.EndTimingBlockFormatted()}"
            );
        }

        public int HandleComputeShader(string computeSource)
        {
            int computeShaderPointer = GL.CreateShader(ShaderType.ComputeShader);

            GL.ShaderSource(computeShaderPointer, computeSource);

            GL.CompileShader(computeShaderPointer);

            GL.GetShader(
                computeShaderPointer,
                ShaderParameter.CompileStatus,
                out int shaderCompileSuccess
            );

            if (shaderCompileSuccess == 0)
            {
                string errorLog = GL.GetShaderInfoLog(computeShaderPointer);
                Logger.Log(
                    $"An error occured while loading compute shader {computeSource}!\nError log:\n{errorLog}"
                );
            }

            return computeShaderPointer;
        }

        public void DispatchForSize(int x, int y, int z)
        {
            Dispatch((int)(x / groupSize.X), (int)(y / groupSize.Y), (int)(z / groupSize.Z));
        }

        public void Dispatch(int x, int y, int z)
        {
            Use();
            if (initalised == false)
            {
                Logger.Log(
                    $"Shader with {Handle} used without initalisation, initalising.."
                );
                Init();
            }
            GL.DispatchCompute(x, y, z);
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

        #region dispose
        ~ComputeShader()
        {
            if (disposed == false)
                Logger.Log($"GPU Resource leak! Did you forget to call Dispose()?");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                GL.DeleteProgram(Handle);
                Logger.Log(
                    $"Disposed compute shader {Handle}"
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
