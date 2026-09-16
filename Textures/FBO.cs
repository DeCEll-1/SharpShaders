using OpenTK.Mathematics;

namespace SharpShaders.Textures
{
    public struct TextureInfo
    {
        public PixelInternalFormat InternalFormat;
        public PixelFormat Format;
        public PixelType Type;

        public TextureInfo(PixelInternalFormat internalFormat, PixelFormat format, PixelType type)
        {
            InternalFormat = internalFormat;
            Format = format;
            Type = type;
        }

        // Common presets
        public static TextureInfo Rgba8() => new(PixelInternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte);
        public static TextureInfo Rgba16f() => new(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat);
        public static TextureInfo Rgba32f() => new(PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float);
        public static TextureInfo Depth24Stencil8() => new(PixelInternalFormat.Depth24Stencil8, PixelFormat.DepthStencil, PixelType.UnsignedInt248);
    }

    public class FBO : IDisposable
    {
        private bool disposed = false;
        public string Name = "";
        public int Handle { get; set; }
        //public int StencilRenderBuffer { get; private set; }
        public Texture DepthStencilTexture { get; private set; }
        public Vector2i Size { get; private set; }
        public FBO() { }

        private readonly List<Texture> _colorAttachments = new();
        public IReadOnlyList<Texture> ColorAttachments => _colorAttachments;
        public int ColorAttachmentCount => _colorAttachments.Count;
        public Texture ColorAttachment0 => _colorAttachments.Count > 0 ? _colorAttachments[0] : null;

        public FBO Init(Vector2i size, string name = "", Texture? depthStencilTexture = null, TextureInfo[]? colorInfos = null)
        {
            Logger.BeginTimingBlock();
            // you just HAD to do shit with pointers
            unsafe
            {
                uint temp = 0; // create the fbo
                GL.CreateFramebuffers(1, &temp);
                Handle = (int)temp;
            }

            // bind the fbo so we are working on it
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);

            Size = size;

            _colorAttachments.Clear();
            int attachmentCount = colorInfos?.Length ?? 1;


            for (int i = 0; i < attachmentCount; i++)
            {
                var info = colorInfos?[i] ?? new TextureInfo(
                    PixelInternalFormat.Rgba8,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte
                );

                var tex = Texture.LoadFromSize(
                    Size.X, Size.Y,
                    target: TextureTarget.Texture2D,
                    pixelInternalFormat: info.InternalFormat,
                    pixelFormat: info.Format,
                    pixelType: info.Type,
                    textureMinFilter: TextureMinFilter.Linear,
                    textureMagFilter: TextureMagFilter.Linear,
                    name: $"ColorAttachment{i}",
                    logCreation: false
                );

                _colorAttachments.Add(tex);

                GL.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0 + i,
                    TextureTarget.Texture2D,
                    tex.Handle,
                    0
                );
            }

            if (attachmentCount > 1)
            {
                var drawBuffers = Enumerable.Range(0, attachmentCount)
                    .Select(i => DrawBuffersEnum.ColorAttachment0 + i)
                    .ToArray();

                GL.DrawBuffers(attachmentCount, drawBuffers);
            }
            else if (attachmentCount == 1)
            {
                GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            }
            else
            {
                GL.DrawBuffer(DrawBufferMode.None);
            }

            if (depthStencilTexture == null)
            {
                DepthStencilTexture = Texture.LoadFromSize(
                    Size.X, Size.Y,
                    target: TextureTarget.Texture2D,
                    pixelInternalFormat: PixelInternalFormat.Depth24Stencil8,
                    pixelFormat: PixelFormat.DepthStencil,
                    pixelType: PixelType.UnsignedInt248,
                    textureMinFilter: TextureMinFilter.Nearest,
                    textureMagFilter: TextureMagFilter.Nearest,
                    name: "DepthStencil",
                    logCreation: false
                );
            }
            else
            {
                DepthStencilTexture = depthStencilTexture;
            }


            GL.FramebufferTexture2D( // attatch the texture
                FramebufferTarget.Framebuffer, // the frame buffer will write to this texture
                FramebufferAttachment.DepthStencilAttachment, // attatchment type
                TextureTarget.Texture2D, // texture type
                DepthStencilTexture.Handle,
                0 // mipmap level
            );




            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                Logger.Log($"Framebuffer {Handle} incomplete: {status}");
            }
            else
            {

                var l = new LogBuilder()
                .WriteLine($"Loaded FBO {Handle} ")
                .Write($"sized {Size.X}x{Size.Y} ")
                .Write(string.IsNullOrEmpty(name) ? "" : $"named {name} ")
                .NewLine()
                .WriteLine($"Color attachments: {ColorAttachmentCount}");
                for (int i = 0; i < ColorAttachmentCount; i++)
                {
                    l.WriteLine("name: " + ColorAttachments[i].Name + " handle: " + ColorAttachments[i].Handle);
                }
                l.WriteLine($"Depth/Stencil handle: " +
                $"{DepthStencilTexture.Handle} sized {DepthStencilTexture.Width}x{DepthStencilTexture.Height}")
                .WriteLine($"in {Logger.EndTimingBlockFormatted()}")
                .Log();


                this.Name = name;
            }
            Unbind();
            return this;
        }


        public void Bind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
            GL.Viewport(0, 0, Size.X, Size.Y);
        }

        public void Unbind()
        // as this isnt like shaders (you cant forget a shader binded dude comeon)
        // we want to add an unbind here so its easier to unbind this as staying binded to it can and WİLL cause problems if 
        // forgotten
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, SharpS.windowSize.X, SharpS.windowSize.Y);
        }

        // also nice to have
        public static void BindToFBO(int handle) => GL.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
        public static void SetToDefaultFBO() => GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        public void ClearFBO()
        {
            Bind();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }

        #region disposal
        ~FBO()
        {
            if (disposed == false)
                Logger.Log(
                    $"GPU Resource leak for FBO! Did you forget to call Dispose()?"
                );
        }

        protected virtual void Dispose(bool disposing, bool log = true)
        {
            if (!disposed)
            {
                if (log)
                {
                    string logText = "";
                    logText += "Disposed FBO " + Handle;

                    if (Name != null)
                        logText += ", named " + Name;

                    Logger.Log(logText);


                    if (DepthStencilTexture != null)
                    {
                        DepthStencilTexture.logDisposal = log;
                        DepthStencilTexture.Dispose();
                    }

                    foreach (var tex in ColorAttachments)
                    {
                        tex.logDisposal = log;
                        tex.Dispose();
                    }

                    _colorAttachments.Clear();
                }

                GL.DeleteFramebuffer(Handle);
                Handle = 0;
                disposed = true;
            }
        }
        public bool logDisposal = true;

        public void Dispose()
        {
            Dispose(true, logDisposal);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
