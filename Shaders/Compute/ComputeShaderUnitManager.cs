namespace SharpShaders.Shaders.Compute
{
    public class ComputeShaderUnitManager
    {
        public struct ImageBinding
        {
            public int Unit;
            public int Texture;
            public TextureAccess Access;
            public SizedInternalFormat Format;

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

        public ComputeShaderUnitManager(int handle)
        {
            Handle = handle;
        }

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

        // Set a float array as a SSBO at a binding point
        public void SetFloatArraySSBO(float[] data, int binding)
        {
            int buffer;
            if (ssboBindings.TryGetValue(binding, out buffer))
            {
                // Update existing buffer
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(float) * data.Length, data, BufferUsageHint.DynamicDraw);
            }
            else
            {
                buffer = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(float) * data.Length, data, BufferUsageHint.DynamicDraw);
                ssboBindings[binding] = buffer;
            }
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, binding, buffer);
        }

        // Set an int array as a SSBO at a binding point
        public void SetIntArraySSBO(int[] data, int binding)
        {
            int buffer;
            if (ssboBindings.TryGetValue(binding, out buffer))
            {
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(int) * data.Length, data, BufferUsageHint.DynamicDraw);
            }
            else
            {
                buffer = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(int) * data.Length, data, BufferUsageHint.DynamicDraw);
                ssboBindings[binding] = buffer;
            }
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, binding, buffer);
        }
    }
}
