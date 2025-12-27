using OpenTK.Mathematics;
using SharpShaders.Extensions;
using SharpShaders.Textures;
namespace SharpShaders.Shaders
{
    public class ShaderUniformManager
    {
        private int Handle;

        public ShaderUniformManager(int handle)
        {
            Handle = handle;
        }

        #region Uniform Functions
        // cache uniforms so we dont run *expensive* get uniform location a shit ton
        private Dictionary<string, int> uniformCache = new Dictionary<string, int>();

        public void SetMatrix4(string name, Matrix4 matrix)
        {
            if (uniformCache.ContainsKey(name))
            {
                uniformCache.TryGetValue(name, out int loc);
                GL.UniformMatrix4(loc, true, ref matrix);
            }
            else
            {
                uniformCache.Add(name, GL.GetUniformLocation(Handle, name));
                SetMatrix4(name, matrix);
            }
        }

        public void SetVector4(string name, Vector4 vector)
        {
            if (uniformCache.ContainsKey(name))
            {
                uniformCache.TryGetValue(name, out int loc);
                GL.Uniform4(loc, vector);
            }
            else
            {
                uniformCache.Add(name, GL.GetUniformLocation(Handle, name));
                SetVector4(name, vector);
            }
        }

        public void SetVector3(string name, Vector3 vector)
        {
            if (uniformCache.ContainsKey(name))
            {
                uniformCache.TryGetValue(name, out int loc);
                GL.Uniform3(loc, vector);
            }
            else
            {
                uniformCache.Add(name, GL.GetUniformLocation(Handle, name));
                SetVector3(name, vector);
            }
        }

        public void SetVector2(string name, Vector2 vector)
        {
            if (uniformCache.ContainsKey(name))
            {
                uniformCache.TryGetValue(name, out int loc);
                GL.Uniform2(loc, vector);
            }
            else
            {
                uniformCache.Add(name, GL.GetUniformLocation(Handle, name));
                SetVector2(name, vector);
            }
        }

        public void SetFloat(string name, float value)
        {
            if (uniformCache.ContainsKey(name))
            {
                uniformCache.TryGetValue(name, out int loc);
                GL.Uniform1(loc, value);
            }
            else
            {
                uniformCache.Add(name, GL.GetUniformLocation(Handle, name));
                SetFloat(name, value);
            }
        }

        public void SetInt(string name, int value)
        {
            if (uniformCache.ContainsKey(name))
            {
                uniformCache.TryGetValue(name, out int loc);
                GL.Uniform1(loc, value);
            }
            else
            {
                uniformCache.Add(name, GL.GetUniformLocation(Handle, name));
                SetInt(name, value);
            }
        }

        public void SetFloatArray(string name, float[] values)
        {
            if (uniformCache.ContainsKey(name))
            {
                uniformCache.TryGetValue(name, out int loc);
                GL.Uniform1(loc, values.Length, values);
            }
            else
            {
                uniformCache.Add(name, GL.GetUniformLocation(Handle, name));
                SetFloatArray(name, values);
            }
        }

        public void SetIntArray(string name, int[] values)
        {
            if (uniformCache.ContainsKey(name))
            {
                uniformCache.TryGetValue(name, out int loc);
                GL.Uniform1(loc, values.Length, values);
            }
            else
            {
                uniformCache.Add(name, GL.GetUniformLocation(Handle, name));
                SetIntArray(name, values);
            }
        }

        public void SetColor4(string name, Color4 color)
        {
            if (uniformCache.ContainsKey(name))
            {
                uniformCache.TryGetValue(name, out int loc);
                GL.Uniform4(loc, color);
            }
            else
            {
                uniformCache.Add(name, GL.GetUniformLocation(Handle, name));
                SetColor4(name, color);
            }
        }

        public void SetColor3(string name, Color4 color)
        {
            if (uniformCache.ContainsKey(name))
            {
                uniformCache.TryGetValue(name, out int loc);
                GL.Uniform3(loc, new Vector3(color.ToVector4()));
            }
            else
            {
                uniformCache.Add(name, GL.GetUniformLocation(Handle, name));
                SetColor3(name, color);
            }
        }

        public void SetTexture(string name, Texture tex, TextureUnit unit)
        {
            tex.Activate(unit);
            tex.Bind();

            if (uniformCache.ContainsKey(name))
            {
                uniformCache.TryGetValue(name, out int loc);
                int unitint = (int)unit - (int)TextureUnit.Texture0;
                GL.Uniform1(loc, unitint);
            }
            else
            {
                uniformCache.Add(name, GL.GetUniformLocation(Handle, name));
                SetTexture(name, tex, unit);
            }
        }

        public void SetCubemap(string name, Cubemap cubemap, TextureUnit unit)
        {

            cubemap.Activate(unit);
            cubemap.Bind();

            if (uniformCache.ContainsKey(name))
            {
                uniformCache.TryGetValue(name, out int loc);
                int unitint = (int)unit - (int)TextureUnit.Texture0;
                GL.Uniform1(loc, unitint);
            }
            else
            {
                uniformCache.Add(name, GL.GetUniformLocation(Handle, name));
                SetCubemap(name, cubemap, unit);
            }
        }

        #endregion
    }
}
