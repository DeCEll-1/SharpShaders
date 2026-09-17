using OpenTK.Mathematics;
using SharpShaders.Textures;

namespace SharpShaders.Shaders
{
    /// <summary>
    /// Caches and manages uniform variable locations and updates for an OpenGL shader program.
    /// </summary>
    public class ShaderUniformManager
    {
        /// <summary>
        /// The OpenGL program handle (ID) associated with these uniforms.
        /// </summary>
        private readonly int Handle;

        /// <summary>
        /// Caches resolved uniform names to their corresponding OpenGL uniform locations
        /// to eliminate redundant driver queries via <c>GL.GetUniformLocation</c>.
        /// </summary>
        private readonly Dictionary<string, int> uniformCache = new Dictionary<string, int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderUniformManager"/> class for a specific OpenGL shader program.
        /// </summary>
        /// <param name="handle">The valid OpenGL handle (ID) of the linked shader program.</param>
        public ShaderUniformManager(int handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Gets the location of a uniform variable, caching it if not already queried.
        /// </summary>
        private int GetLocation(string name)
        {
            if (!uniformCache.TryGetValue(name, out int location))
            {
                location = GL.GetUniformLocation(Handle, name);
                uniformCache[name] = location;
            }
            return location;
        }

        #region Uniform Functions

        /// <summary>
        /// Sets a <see cref="Matrix4"/> uniform variable in the shader program.
        /// </summary>
        public ShaderUniformManager SetMatrix4(string name, Matrix4 matrix)
        {
            int loc = GetLocation(name);
            if (loc != -1)
            {
                GL.UniformMatrix4(loc, true, ref matrix);
            }
            return this;
        }

        /// <summary>
        /// Sets a 4-component floating-point vector (<see cref="Vector4"/>) uniform variable in the shader program.
        /// </summary>
        public ShaderUniformManager SetVector4(string name, Vector4 vector)
        {
            int loc = GetLocation(name);
            if (loc != -1) GL.Uniform4(loc, vector);
            return this;
        }

        /// <summary>
        /// Sets a 3-component floating-point vector (<see cref="Vector3"/>) uniform variable in the shader program.
        /// </summary>
        public ShaderUniformManager SetVector3(string name, Vector3 vector)
        {
            int loc = GetLocation(name);
            if (loc != -1) GL.Uniform3(loc, vector);
            return this;
        }

        /// <summary>
        /// Sets a 2-component floating-point vector (<see cref="Vector2"/>) uniform variable in the shader program.
        /// </summary>
        public ShaderUniformManager SetVector2(string name, Vector2 vector)
        {
            int loc = GetLocation(name);
            if (loc != -1) GL.Uniform2(loc, vector);
            return this;
        }

        /// <summary>
        /// Sets a single floating-point (<c>float</c>) uniform variable in the shader program.
        /// </summary>
        public ShaderUniformManager SetFloat(string name, float value)
        {
            int loc = GetLocation(name);
            if (loc != -1) GL.Uniform1(loc, value);
            return this;
        }

        /// <summary>
        /// Sets a single integer (<c>int</c>) uniform variable in the shader program.
        /// </summary>
        public ShaderUniformManager SetInt(string name, int value)
        {
            int loc = GetLocation(name);
            if (loc != -1) GL.Uniform1(loc, value);
            return this;
        }

        /// <summary>
        /// Sets an array of floating-point values (<c>float[]</c>) uniform variable in the shader program.
        /// </summary>
        public ShaderUniformManager SetFloatArray(string name, float[] values)
        {
            int loc = GetLocation(name);
            if (loc != -1) GL.Uniform1(loc, values.Length, values);
            return this;
        }

        /// <summary>
        /// Sets an array of integer values (<c>int[]</c>) uniform variable in the shader program.
        /// </summary>
        public ShaderUniformManager SetIntArray(string name, int[] values)
        {
            int loc = GetLocation(name);
            if (loc != -1) GL.Uniform1(loc, values.Length, values);
            return this;
        }

        /// <summary>
        /// Sets a 4-component RGBA color (<see cref="Color4"/>) uniform variable in the shader program.
        /// </summary>
        public ShaderUniformManager SetColor4(string name, Color4 color)
        {
            int loc = GetLocation(name);
            if (loc != -1) GL.Uniform4(loc, color);
            return this;
        }

        /// <summary>
        /// Sets a 3-component RGB color uniform variable in the shader program from a <see cref="Color4"/> object.
        /// </summary>
        public ShaderUniformManager SetColor3(string name, Color4 color)
        {
            int loc = GetLocation(name);
            if (loc != -1) GL.Uniform3(loc, color.R, color.G, color.B);
            return this;
        }

        /// <summary>
        /// Binds a 2D texture to a given texture unit and updates the sampler uniform in GLSL.
        /// </summary>
        public ShaderUniformManager SetTexture(string name, Texture tex, TextureUnit unit)
        {
            tex.Activate(unit);
            tex.Bind();

            int loc = GetLocation(name);
            if (loc != -1)
            {
                int unitint = (int)unit - (int)TextureUnit.Texture0;
                GL.Uniform1(loc, unitint);
            }
            return this;
        }

        /// <summary>
        /// Binds a Cubemap texture to a given texture unit and updates the samplerCube uniform in GLSL.
        /// </summary>
        public ShaderUniformManager SetCubemap(string name, Cubemap cubemap, TextureUnit unit)
        {
            cubemap.Activate(unit);
            cubemap.Bind();

            int loc = GetLocation(name);
            if (loc != -1)
            {
                int unitint = (int)unit - (int)TextureUnit.Texture0;
                GL.Uniform1(loc, unitint);
            }
            return this;
        }

        #endregion
    }
}