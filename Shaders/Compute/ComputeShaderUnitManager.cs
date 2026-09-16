using System.Runtime.CompilerServices;

namespace SharpShaders.Shaders.Compute
{
    /// <summary>
    /// Handles binding and management of external GPU resources like image textures and 
    /// Shader Storage Buffer Objects (SSBOs) bound to a compute shader.
    /// </summary>
    public class ComputeShaderUnitManager : IDisposable
    {
        /// <summary>
        /// Represents an image unit binding configuration for compute shader reads/writes.
        /// </summary>
        public struct ImageBinding
        {
            /// <summary>The binding layout location/unit index in GLSL.</summary>
            public int Unit;

            /// <summary>The handle ID of the target OpenGL texture.</summary>
            public int Texture;

            /// <summary>Access permissions for the compute shader (ReadOnly, WriteOnly, ReadWrite).</summary>
            public TextureAccess Access;

            /// <summary>Internal sized format matching the GLSL texture declaration.</summary>
            public SizedInternalFormat Format;

            /// <summary>
            /// Creates an image binding configuration.
            /// </summary>
            /// <param name="unit">Image unit index.</param>
            /// <param name="texture">OpenGL texture handle ID.</param>
            /// <param name="access">Shader read/write access mode.</param>
            /// <param name="format">Internal pixel component format.</param>
            public ImageBinding(
                int unit,
                int texture,
                TextureAccess access,
                SizedInternalFormat format
            )
            {
                Unit = unit;
                Texture = texture;
                Access = access;
                Format = format;
            }
        }

        private int Handle;

        private Dictionary<int, int> ssboBindings = new Dictionary<int, int>(); // binding point -> buffer handle
        private List<ImageBinding> imageBindings = new List<ImageBinding>();
        private bool disposed = false;

        /// <summary>
        /// Initializes a new unit manager attached to a specific compute shader program handle.
        /// </summary>
        /// <param name="handle">OpenGL handle ID of the associated program.</param>
        public ComputeShaderUnitManager(int handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Registers or updates an image binding configuration for an OpenGL image unit.
        /// </summary>
        /// <param name="texture">OpenGL handle ID of the texture.</param>
        /// <param name="unit">The GLSL <c>binding = unit</c> index.</param>
        /// <param name="access">Memory access type (<c>ReadWrite</c>, <c>ReadOnly</c>, or <c>WriteOnly</c>).</param>
        /// <param name="format">Matching sized internal texture format.</param>
        public void SetImageTexture(
            int texture,
            int unit,
            TextureAccess access = TextureAccess.ReadWrite,
            SizedInternalFormat format = SizedInternalFormat.Rgba32f
        )
        {
            // Remove any existing binding on that unit to avoid duplicates
            imageBindings.RemoveAll(b => b.Unit == unit);

            imageBindings.Add(new ImageBinding(unit, texture, access, format));
        }

        /// <summary>
        /// Executes OpenGL bindings for all registered image textures to their respective units.
        /// </summary>
        public void ApplyTextures()
        {
            foreach (var binding in imageBindings)
            {
                GL.BindImageTexture(
                    binding.Unit, // Image unit index, corresponds to 'binding = unit' in GLSL
                    binding.Texture, // OpenGL texture handle
                    0, // Mipmap level
                    false, // Not layered
                    0, // Layer index
                    binding.Access, // WriteOnly, ReadOnly, or ReadWrite
                    binding.Format // Must match texture's internal format
                );
            }
        }

        // --- SSBO Management ---

        /// <summary>
        /// Uploads an array of value-type elements to an SSBO binding point.
        /// Reuses existing OpenGL buffer handles when available.
        /// </summary>
        public void SetSSBO<T>(T[] data, int binding, BufferUsageHint usage = BufferUsageHint.DynamicDraw) where T : unmanaged
        {
            int byteCount = Unsafe.SizeOf<T>() * data.Length;

            if (!ssboBindings.TryGetValue(binding, out int buffer))
            {
                buffer = GL.GenBuffer();
                ssboBindings[binding] = buffer;
            }

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, byteCount, data, usage);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, binding, buffer);
        }

        #region dispose
        ~ComputeShaderUnitManager()
        {
            if (disposed == false)
                Logger.Log($"GPU Resource leak! Did you forget to call Dispose()?");
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                foreach (KeyValuePair<int, int> item in ssboBindings)
                {
                    GL.DeleteBuffer(item.Value);
                }
                ssboBindings.Clear();
                imageBindings.Clear();

                Logger.Log(
                    $"Disposed compute shader unit manager for {Handle}"
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
