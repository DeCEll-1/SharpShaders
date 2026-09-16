namespace SharpShaders.Mesh
{
    public class ScreenQuad : IDisposable
    {
        private int _vao;
        private int _vbo;
        private bool disposed;
        public ScreenQuad()
        {
            float[] vertices =
            [
                // Position     // TexCoords
                -1.0f,  1.0f,   0.0f, 0.0f,
                 1.0f,  1.0f,   1.0f, 0.0f,
                -1.0f, -1.0f,   0.0f, 1.0f,
                 1.0f, -1.0f,   1.0f, 1.0f
            ];

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            GL.BindVertexArray(0);
        }

        public void Render()
        {
            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            GL.BindVertexArray(0);
        }

        void IDisposable.Dispose()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
        }

        ~ScreenQuad()
        {
            if (disposed == false)
            {
                Logger.Log(
                    $"GPU Resource leak for shader! Did you forget to call Dispose()?"
                );
            }
        }
    }
}
