using ImageMagick;

namespace SharpShaders.Textures
{
    /// <summary>
    /// Represents a managed wrapper for an OpenGL texture resource, providing methods
    /// to load image data, set texture parameters, and allocate GPU memory.
    /// </summary>
    public partial class Texture
    {
        /// <summary>
        /// Gets or sets the target texture type for OpenGL binding (e.g., <see cref="TextureTarget.Texture2D"/>).
        /// </summary>
        public TextureTarget Target { get; set; }

        /// <summary>
        /// Gets or sets the internal color storage format on the GPU (e.g., <see cref="PixelInternalFormat.Rgba"/>).
        /// </summary>
        public PixelInternalFormat PixelInternalFormat { get; set; }

        /// <summary>
        /// Gets or sets the format of the incoming pixel data channels (e.g., <see cref="PixelFormat.Rgba"/>).
        /// </summary>
        public PixelFormat PixelFormat { get; set; }

        /// <summary>
        /// Gets or sets the data type of the pixel components (e.g., <see cref="PixelType.UnsignedByte"/>).
        /// </summary>
        public PixelType PixelType { get; set; }

        /// <summary>
        /// Gets or sets the wrap mode along the S (horizontal) texture axis.
        /// </summary>
        public TextureWrapMode TextureSWrapMode { get; set; }

        /// <summary>
        /// Gets or sets the wrap mode along the T (vertical) texture axis.
        /// </summary>
        public TextureWrapMode TextureTWrapMode { get; set; }

        /// <summary>
        /// Gets or sets the minification filter applied when sampling the texture at a smaller scale.
        /// </summary>
        public TextureMinFilter TextureMinFilter { get; set; }

        /// <summary>
        /// Gets or sets the magnification filter applied when sampling the texture at a larger scale.
        /// </summary>
        public TextureMagFilter TextureMagFilter { get; set; }

        /// <summary>
        /// Controls whether initial creation logs are emitted to <see cref="Logger"/> upon successful setup.
        /// </summary>
        public bool logCreation = true;

        /// <summary>
        /// Holds raw pixel payload bytes prior to GPU allocation during <see cref="Init"/>.
        /// </summary>
        private byte[]? bytes { get; set; }

        /// <summary>
        /// Gets an optional debug identifier name assigned to this texture instance.
        /// </summary>
        public string Name { get; private set; } = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class.
        /// </summary>
        public Texture() { }

        /// <summary>
        /// Loads an image file from the specified path into temporary memory.
        /// Call <see cref="Init"/> to compile and transfer the pixel data onto the GPU context.
        /// </summary>
        /// <param name="path">The file system path to the image asset.</param>
        /// <param name="target">The OpenGL texture binding target.</param>
        /// <param name="pixelInternalFormat">The pixel format used by the GPU internal storage.</param>
        /// <param name="pixelFormat">The pixel channel arrangement of the image data.</param>
        /// <param name="type">The component data type of each pixel payload channel.</param>
        /// <param name="textureSWrapMode">The horizontal texture coordinate wrap mode.</param>
        /// <param name="textureTWrapMode">The vertical texture coordinate wrap mode.</param>
        /// <param name="textureMinFilter">The minification sampling filter mode.</param>
        /// <param name="textureMagFilter">The magnification sampling filter mode.</param>
        /// <returns>A uninitialized <see cref="Texture"/> instance ready for <see cref="Init"/>.</returns>
        public static Texture LoadFromFile(
            string path,
            TextureTarget target = TextureTarget.Texture2D,
            PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgba,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType type = PixelType.UnsignedByte,
            TextureWrapMode textureSWrapMode = TextureWrapMode.Repeat,
            TextureWrapMode textureTWrapMode = TextureWrapMode.Repeat,
            TextureMinFilter textureMinFilter = TextureMinFilter.Linear,
            TextureMagFilter textureMagFilter = TextureMagFilter.Linear
        )
        => LoadFromTextureBytes(
                   File.ReadAllBytes(path),
                   target: target,
                   pixelInternalFormat: pixelInternalFormat,
                   pixelFormat: pixelFormat,
                   type: type,
                   textureSWrapMode: textureSWrapMode,
                   textureTWrapMode: textureTWrapMode,
                   textureMinFilter: textureMinFilter,
                   textureMagFilter: textureMagFilter
              );

        /// <summary>
        /// Decodes encoded image bytes (e.g., PNG/JPEG file streams) using ImageMagick into uncompressed pixel data.
        /// </summary>
        /// <param name="bytes">Encoded image payload byte array.</param>
        /// <param name="flipVertical">If set to <c>true</c>, flips the image vertically to match OpenGL UV coordinates.</param>
        /// <param name="target">The OpenGL texture target.</param>
        /// <param name="pixelInternalFormat">The internal GPU format to allocate.</param>
        /// <param name="pixelFormat">The channel mapping to extract from ImageMagick.</param>
        /// <param name="type">The underlying component data type.</param>
        /// <param name="textureSWrapMode">Horizontal texture coordinate wrapping option.</param>
        /// <param name="textureTWrapMode">Vertical texture coordinate wrapping option.</param>
        /// <param name="textureMinFilter">Minification sampling filter.</param>
        /// <param name="textureMagFilter">Magnification sampling filter.</param>
        /// <returns>A uninitialized <see cref="Texture"/> instance populated with raw decompressed pixel bytes.</returns>
        public static Texture LoadFromTextureBytes(
            byte[] bytes,
            bool flipVertical = true,
            TextureTarget target = TextureTarget.Texture2D,
            PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgba,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType type = PixelType.UnsignedByte,
            TextureWrapMode textureSWrapMode = TextureWrapMode.Repeat,
            TextureWrapMode textureTWrapMode = TextureWrapMode.Repeat,
            TextureMinFilter textureMinFilter = TextureMinFilter.Linear,
            TextureMagFilter textureMagFilter = TextureMagFilter.Linear
        )
        {
            using var ms = new MemoryStream(bytes);
            using var image = new MagickImage(ms);
            if (flipVertical)
            {
                image.Flip();
            }

            using var pixelDataArray = image.GetPixels();

            PixelMapping mapping = pixelFormat == PixelFormat.Rgb ? PixelMapping.RGB : PixelMapping.RGBA;
            byte[] pixelArray = pixelDataArray.ToByteArray(0, 0, image.Width, image.Height, mapping)!;

            return LoadFromBytes(
                bytes: pixelArray,
                width: (int)image.Width,
                height: (int)image.Height,
                target: target,
                pixelInternalFormat: pixelInternalFormat,
                pixelFormat: pixelFormat,
                type: type,
                textureSWrapMode: textureSWrapMode,
                textureTWrapMode: textureTWrapMode,
                textureMinFilter: textureMinFilter,
                textureMagFilter: textureMagFilter
            );
        }

        /// <summary>
        /// Constructs a <see cref="Texture"/> object around raw, uncompressed pixel color bytes.
        /// </summary>
        /// <param name="bytes">Raw pixel color buffer array matching the target layout.</param>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <param name="target">The target texture dimension type.</param>
        /// <param name="pixelInternalFormat">Internal GPU pixel storage layout.</param>
        /// <param name="pixelFormat">Source color channel layout.</param>
        /// <param name="type">Data type of each component channel.</param>
        /// <param name="textureSWrapMode">Horizontal UV wrapping rule.</param>
        /// <param name="textureTWrapMode">Vertical UV wrapping rule.</param>
        /// <param name="textureMinFilter">Minification filter behavior.</param>
        /// <param name="textureMagFilter">Magnification filter behavior.</param>
        /// <returns>An uninitialized <see cref="Texture"/> object ready for GPU upload via <see cref="Init"/>.</returns>
        public static Texture LoadFromBytes(
            byte[] bytes,
            int width,
            int height,
            TextureTarget target = TextureTarget.Texture2D,
            PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgba,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType type = PixelType.UnsignedByte,
            TextureWrapMode textureSWrapMode = TextureWrapMode.Repeat,
            TextureWrapMode textureTWrapMode = TextureWrapMode.Repeat,
            TextureMinFilter textureMinFilter = TextureMinFilter.Linear,
            TextureMagFilter textureMagFilter = TextureMagFilter.Linear
        )
        {
            Texture texture = new();
            texture.bytes = bytes;
            texture.Width = width;
            texture.Height = height;
            texture.Target = target;
            texture.PixelInternalFormat = pixelInternalFormat;
            texture.PixelFormat = pixelFormat;
            texture.PixelType = type;
            texture.TextureSWrapMode = textureSWrapMode;
            texture.TextureTWrapMode = textureTWrapMode;
            texture.TextureMinFilter = textureMinFilter;
            texture.TextureMagFilter = textureMagFilter;

            return texture;
        }

        /// <summary>
        /// Generates the native OpenGL texture handle, binds parameters, and uploads raw pixel data to video memory.
        /// </summary>
        /// <param name="name">An optional friendly debug name for logging.</param>
        /// <param name="isCubemap">Indicates whether this texture initialization targets a CubeMap face context.</param>
        /// <param name="cubemapHandle">Existing OpenGL handle if <paramref name="isCubemap"/> is set to <c>true</c>.</param>
        /// <exception cref="InvalidOperationException">Thrown when OpenGL fails to generate a valid texture handle (returns 0).</exception>
        public void Init(string? name = "", bool isCubemap = false, int cubemapHandle = -1)
        {
            Logger.BeginTimingBlock();
            var paramTarget = this.Target;
            if (isCubemap)
            {
                this.Handle = cubemapHandle;
                paramTarget = TextureTarget.TextureCubeMap;
            }
            else
                this.Handle = GL.GenTexture();

            if (Handle == 0)
            {
                string errorMsg = $"Failed to generate texture handle for texture '{name}'.";
                Logger.Log(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            GL.BindTexture(paramTarget, this.Handle);

            GL.TexParameter(
                paramTarget,
                TextureParameterName.TextureWrapS,
                (int)TextureSWrapMode
            );

            GL.TexParameter(
                paramTarget,
                TextureParameterName.TextureWrapT,
                (int)TextureTWrapMode
            );

            GL.TexParameter(paramTarget, TextureParameterName.TextureMinFilter, (int)TextureMinFilter);
            GL.TexParameter(paramTarget, TextureParameterName.TextureMagFilter, (int)TextureMagFilter);

            GL.TexParameter(paramTarget, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(paramTarget, TextureParameterName.TextureMaxLevel, 0);

            GL.TexImage2D(
                this.Target,
                0,
                this.PixelInternalFormat,
                Width,
                Height,
                0,
                this.PixelFormat,
                this.PixelType,
                bytes
            );
            this.Initialized = true;

            this.bytes = null;

            this.Name = name ?? string.Empty;
            if (logCreation)
                Logger.Log(
                    $"Loaded Texture {this.Handle}{(name != "" ? $", named {name}" : "")} in {Logger.EndTimingBlockFormatted()}"
                );
        }

        /// <summary>
        /// Directly generates, allocates, and initializes an empty OpenGL texture buffer of specified dimensions without needing an explicit call to <see cref="Init"/>.
        /// </summary>
        /// <param name="width">Width of the texture allocation in pixels.</param>
        /// <param name="height">Height of the texture allocation in pixels.</param>
        /// <param name="target">The OpenGL texture target target type (defaults to <see cref="TextureTarget.Texture2D"/>).</param>
        /// <param name="pixelInternalFormat">Format specification for VRAM storage.</param>
        /// <param name="pixelFormat">Pixel component format layout.</param>
        /// <param name="pixelType">Data component representation type.</param>
        /// <param name="textureSWrapMode">Horizontal UV wrapping rule.</param>
        /// <param name="textureTWrapMode">Vertical UV wrapping rule.</param>
        /// <param name="textureMinFilter">Minification filter parameter.</param>
        /// <param name="textureMagFilter">Magnification filter parameter.</param>
        /// <param name="name">Optional identifier for logging outputs.</param>
        /// <param name="logCreation">Toggles performance and creation logging.</param>
        /// <returns>A fully initialized <see cref="Texture"/> ready for rendering or Framebuffer attachment.</returns>
        /// <exception cref="InvalidOperationException">Thrown when OpenGL fails to generate a valid handle (returns 0).</exception>
        public static Texture LoadFromSize(
            int width,
            int height,
            TextureTarget target = TextureTarget.Texture2D,
            PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgba,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType pixelType = PixelType.UnsignedByte,
            TextureWrapMode textureSWrapMode = TextureWrapMode.Repeat,
            TextureWrapMode textureTWrapMode = TextureWrapMode.Repeat,
            TextureMinFilter textureMinFilter = TextureMinFilter.Linear,
            TextureMagFilter textureMagFilter = TextureMagFilter.Linear,
            string? name = "",
            bool logCreation = true
        )
        {
            Logger.BeginTimingBlock();
            Texture texture = new Texture();
            #region texture info
            texture.Target = target;
            texture.PixelInternalFormat = pixelInternalFormat;
            texture.PixelFormat = pixelFormat;
            texture.PixelType = pixelType;
            texture.TextureSWrapMode = textureSWrapMode;
            texture.TextureTWrapMode = textureTWrapMode;
            texture.TextureMinFilter = textureMinFilter;
            texture.TextureMagFilter = textureMagFilter;
            #endregion
            texture.Handle = GL.GenTexture();
            texture.Name = name ?? string.Empty;
            texture.Width = width;
            texture.Height = height;

            if (texture.Handle == 0)
            {
                string errorMsg = $"Failed to generate texture handle for texture '{name}'.";
                Logger.Log(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            GL.BindTexture(target, texture.Handle);

            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)textureSWrapMode);

            if (target == TextureTarget.Texture2D)
                GL.TexParameter(target, TextureParameterName.TextureWrapT, (int)textureTWrapMode);

            GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)textureMinFilter);
            GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)textureMagFilter);

            GL.TexParameter(target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(target, TextureParameterName.TextureMaxLevel, 0);

            // Texture creation
            if (target == TextureTarget.Texture1D)
            {
                GL.TexImage1D(
                    target,
                    0, // mipmap level
                    pixelInternalFormat,
                    width,
                    0, // border
                    pixelFormat,
                    pixelType,
                    nint.Zero // initialization pixels
                );
            }
            else if (target == TextureTarget.Texture2D)
            {
                GL.TexImage2D(
                    target,
                    0, // mipmap level
                    pixelInternalFormat,
                    width,
                    height,
                    0, // border
                    pixelFormat,
                    pixelType,
                    nint.Zero // initialization pixels
                );
            }

            texture.Initialized = true;

            if (logCreation)
                new LogBuilder()
                .WriteLine("Loaded empty Texture ")
                .Write(texture.Handle)
                .Write((name != "" ? $", named {name}" : "") + " ")
                .Write($"sized {width}x{height} in {Logger.EndTimingBlockFormatted()}")
                .Log();
            return texture;
        }
    }
}