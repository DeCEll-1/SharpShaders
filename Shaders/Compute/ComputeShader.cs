using SharpShaders.Exceptions;
using System.Numerics;

namespace SharpShaders.Shaders.Compute
{
    /// <summary>
    /// Manages the lifecycle, compilation, linking, execution, and resource cleanup of an OpenGL Compute Shader.
    /// </summary>
    public class ComputeShader : IDisposable
    { // https://learnopengl.com/Guest-Articles/2022/Compute-Shaders/Introduction

        /// <summary>Indicates whether the shader program has been compiled and linked successfully.</summary>
        public bool initalised = false;

        /// <summary>The raw GLSL source code for the compute shader.</summary>
        public string computeShaderSource;

        /// <summary>The OpenGL handle (ID) for the linked shader program.</summary>
        public int Handle;

        /// <summary>Manages SSBOs and image texture bindings associated with this compute shader.</summary>
        public ComputeShaderUnitManager UnitManager;

        /// <summary>Manages uniform variables for this compute shader program.</summary>
        public ShaderUniformManager UniformManager;

        private bool disposed = false;

        /// <summary>
        /// Gets the workgroup dimensions (<c>local_size_x</c>, <c>local_size_y</c>, <c>local_size_z</c>) 
        /// defined inside the GLSL source code.
        /// </summary>
        public Vector3 groupSize { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeShader"/> class using the provided GLSL source code,
        /// automatically compiling and linking the program on the current OpenGL context.
        /// </summary>
        /// <param name="computeShaderSource">The raw GLSL compute shader source code string to compile.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="computeShaderSource"/> is <c>null</c> or empty.
        /// </exception>
        /// <exception cref="ShaderCompilationException">
        /// Thrown when GLSL compilation or program linking fails. Details can be found in the exception's <c>Log</c> property.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no active OpenGL context is bound to the current calling thread.
        /// </exception>
        public ComputeShader(string computeShaderSource)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(computeShaderSource);

            this.computeShaderSource = computeShaderSource;
            Init();
        }

        /// <summary>
        /// Compiles the GLSL compute shader source, links the OpenGL program object, queries the target local 
        /// workgroup dimensions, and initializes unit and uniform manager dependencies.
        /// </summary>
        /// <remarks>
        /// Executed automatically during class instantiation or on-demand via fallback initialization logic. 
        /// If compilation or linking fails, any created OpenGL handles are safely detached and deleted prior to throwing.
        /// </remarks>
        /// <exception cref="ShaderCompilationException">
        /// Thrown when GLSL source compilation fails or when program linking fails. Contains the raw OpenGL info log.
        /// </exception>
        /// <exception cref="OpenGLException">
        /// Thrown if underlying OpenGL context state calls fail during object creation or parameter retrieval.
        /// </exception>
        private void Init()
        {
            Logger.BeginTimingBlock();
            Handle = GL.CreateProgram();

            int computeShaderPointer = 0;

            try
            {
                computeShaderPointer = HandleComputeShader(computeShaderSource);

                GL.AttachShader(Handle, computeShaderPointer);

                GL.LinkProgram(Handle);

                GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int shaderLinkSuccess);

                if (shaderLinkSuccess == 0)
                {
                    string errorLog = GL.GetProgramInfoLog(Handle);

                    Logger.Log(
                        $"An error occurred while linking compute shader program {Handle}!\nError log:\n{errorLog}"
                    );

                    // Clean up shader object and program before throwing
                    GL.DetachShader(Handle, computeShaderPointer);
                    GL.DeleteShader(computeShaderPointer);
                    GL.DeleteProgram(Handle);
                    Handle = 0;

                    throw new ShaderCompilationException(
                        $"Failed to link compute shader program (Handle: {Handle}). Check Log for details.",
                        Handle,
                        errorLog
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
            catch
            {
                // Guard against handle leaks if compilation in HandleComputeShader threw an exception
                if (computeShaderPointer != 0)
                {
                    GL.DeleteShader(computeShaderPointer);
                }
                if (Handle != 0)
                {
                    GL.DeleteProgram(Handle);
                    Handle = 0;
                }
                throw;
            }
        }

        /// <summary>
        /// Creates an OpenGL compute shader object, loads GLSL source code, triggers driver compilation, 
        /// and verifies execution status.
        /// </summary>
        /// <param name="computeSource">The raw GLSL source code string to compile.</param>
        /// <returns>
        /// The valid OpenGL handle pointing to the compiled compute shader stage object.
        /// </returns>
        /// <exception cref="ShaderCompilationException">
        /// Thrown when GLSL syntax errors, unsupported layout modifiers, or missing extension directives cause compilation failure.
        /// </exception>
        private int HandleComputeShader(string computeSource)
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
                    $"An error occurred while compiling compute shader stage!\nError log:\n{errorLog}"
                );

                // Delete shader handle before throwing
                GL.DeleteShader(computeShaderPointer);

                throw new ShaderCompilationException(
                    "Failed to compile compute shader source code.",
                    computeShaderPointer,
                    errorLog
                );
            }

            return computeShaderPointer;
        }

        /// <summary>
        /// Calculates the required workgroup count based on total target dimensions (e.g. total pixels or array length)
        /// and local workgroup size, rounding up to cover edge elements, then dispatches the compute shader.
        /// </summary>
        /// <param name="x">Target global element count in X dimension (e.g., image width).</param>
        /// <param name="y">Target global element count in Y dimension (e.g., image height).</param>
        /// <param name="z">Target global element count in Z dimension (e.g., image depth or array slices).</param>
        public void DispatchForTotalElements(int x, int y, int z)
        {
            int groupX = Math.Max(1, (int)groupSize.X);
            int groupY = Math.Max(1, (int)groupSize.Y);
            int groupZ = Math.Max(1, (int)groupSize.Z);

            // Integer ceiling division: (total + groupSize - 1) / groupSize
            int dispatchX = (x + groupX - 1) / groupX;
            int dispatchY = (y + groupY - 1) / groupY;
            int dispatchZ = (z + groupZ - 1) / groupZ;

            Dispatch(dispatchX, dispatchY, dispatchZ);
        }

        /// <summary>
        /// Binds the shader program and dispatches explicit workgroup counts to the GPU.
        /// </summary>
        /// <param name="x">Number of workgroups to dispatch in the X dimension.</param>
        /// <param name="y">Number of workgroups to dispatch in the Y dimension.</param>
        /// <param name="z">Number of workgroups to dispatch in the Z dimension.</param>
        public void Dispatch(int x, int y, int z)
        {
            Use();
            GL.DispatchCompute(x, y, z);
        }

        /// <summary>
        /// Binds this compute shader program for execution using <c>GL.UseProgram</c>. Auto-initializes if necessary.
        /// </summary>
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
        /// <summary>
        /// Finalizer that alerts if unmanaged GPU resources were not properly released using <see cref="Dispose()"/>.
        /// </summary>
        ~ComputeShader()
        {
            if (disposed == false)
                Logger.Log($"GPU Resource leak! Did you forget to call Dispose()?");
        }

        /// <summary>
        /// Releases unmanaged OpenGL program resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> if invoked from application code; <c>false</c> if invoked by the garbage collector.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                UnitManager.Dispose();
                GL.DeleteProgram(Handle);

                Logger.Log(
                    $"Disposed compute shader {Handle}"
                );
                disposed = true;
            }
        }

        /// <summary>
        /// Disposes unmanaged OpenGL program memory and suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
