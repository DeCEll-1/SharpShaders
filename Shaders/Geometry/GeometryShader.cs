namespace SharpShaders.Shaders
{
    public partial class Shader
    { // geometry part of the shader
        public string geometrySource { get; set; } = "NULL";
        public Shader(string vertexSource, string fragmentSource, string geometrySource, string name = "")
        {
            this.vertexSource = "#version 330 core\n" + vertexSource;
            this.fragmentSource = "#version 330 core\n" + fragmentSource;
            this.geometrySource = "#version 330 core\n" + geometrySource;
            this.Name = name ?? "";
        }
        private void InitGeometry()
        {
            if (this.geometrySource == "NULL")
                return;

            int geomShaderPointer = HandleShader(geometrySource, ShaderType.GeometryShader);

            GL.AttachShader(Handle, geomShaderPointer);
        }
    }
}
