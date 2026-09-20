using ImageMagick;
using System;
using System.Runtime.InteropServices;

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

        private IntPtr nativeBytes { get; set; }
        private int byteLength { get; set; }

        /// <summary>
        /// Gets an optional debug identifier name assigned to this texture instance.
        /// </summary>
        public string Name { get; private set; } = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class.
        /// </summary>
        public Texture() { }

        /// <summary>
        /// Loads an image file from the specified path into temporary unmanaged memory.
        /// Call <see cref="Init"/> to compile and transfer the pixel data onto the GPU context.
        /// </summary>
        /// <param name="path">The file system path to the image asset.</param>
        /// <param name="flipVertical">If set to <c>true</c>, flips the image vertically to match OpenGL UV coordinates.</param>
        /// <param name="target">The OpenGL texture binding target.</param>
        /// <param name="pixelInternalFormat">The pixel format used by the GPU internal storage.</param>
        /// <param name="pixelFormat">The pixel channel arrangement of the image data.</param>
        /// <param name="type">The component data type of each pixel payload channel.</param>
        /// <param name="textureSWrapMode">The horizontal texture coordinate wrap mode.</param>
        /// <param name="textureTWrapMode">The vertical texture coordinate wrap mode.</param>
        /// <param name="textureMinFilter">The minification sampling filter mode.</param>
        /// <param name="textureMagFilter">The magnification sampling filter mode.</param>
        /// <returns>An uninitialized <see cref="Texture"/> instance ready for <see cref="Init"/>.</returns>
        public static Texture LoadFromFile(
            string path,
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
            using var image = new MagickImage(path);

            if (flipVertical)
                image.Flip();

            return ProcessMagickImage(
                image, target, pixelInternalFormat, pixelFormat, type,
                textureSWrapMode, textureTWrapMode, textureMinFilter, textureMagFilter
            );
        }

        /// <summary>
        /// Decodes encoded image bytes (e.g., downloaded from network) using ImageMagick into uncompressed pixel data.
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
        /// <returns>An uninitialized <see cref="Texture"/> instance populated with raw decompressed pixel bytes.</returns>
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
            using var image = new MagickImage(bytes);

            if (flipVertical)
                image.Flip();

            return ProcessMagickImage(
                image, target, pixelInternalFormat, pixelFormat, type,
                textureSWrapMode, textureTWrapMode, textureMinFilter, textureMagFilter
            );
        }

        /// <summary>
        /// Processes a decoded <see cref="MagickImage"/>, configuring its color channels and extracting an unmanaged memory pointer to its pixel buffer.
        /// </summary>
        /// <param name="image">The decoded Magick.NET image instance.</param>
        /// <param name="target">The OpenGL texture target.</param>
        /// <param name="pixelInternalFormat">The internal GPU format to allocate.</param>
        /// <param name="pixelFormat">The channel mapping to extract.</param>
        /// <param name="type">The underlying component data type.</param>
        /// <param name="textureSWrapMode">Horizontal texture coordinate wrapping option.</param>
        /// <param name="textureTWrapMode">Vertical texture coordinate wrapping option.</param>
        /// <param name="textureMinFilter">Minification sampling filter.</param>
        /// <param name="textureMagFilter">Magnification sampling filter.</param>
        /// <returns>An uninitialized <see cref="Texture"/> instance populated with an unmanaged memory block containing the pixel data.</returns>
        private static unsafe Texture ProcessMagickImage(
            MagickImage image,
            TextureTarget target,
            PixelInternalFormat pixelInternalFormat,
            PixelFormat pixelFormat,
            PixelType type,
            TextureWrapMode textureSWrapMode,
            TextureWrapMode textureTWrapMode,
            TextureMinFilter textureMinFilter,
            TextureMagFilter textureMagFilter)
        {
            image.ColorSpace = ColorSpace.sRGB;
            int channels = pixelFormat == PixelFormat.Rgb ? 3 : 4;

            if (channels == 4)
                image.HasAlpha = true; // Forces RGBA layout

            using var pixelData = image.GetPixelsUnsafe();

            // Get a pointer directly to Magick.NET's internal memory
            IntPtr ptr = pixelData.GetAreaPointer(0, 0, image.Width, image.Height);

            // Calculate exact byte size (width * height * channels * (bytes per channel))
            int bytesPerChannel = type == PixelType.UnsignedByte ? 1 : 4; // Assuming 8-bit or 32-bit floats
            int lengthInBytes = (int)image.Width * (int)image.Height * channels * bytesPerChannel;

            return LoadFromBytes(
                sourcePtr: ptr,
                lengthInBytes: lengthInBytes,
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
        /// Constructs a <see cref="Texture"/> object directly from an unmanaged memory pointer containing raw, uncompressed pixel color data.
        /// </summary>
        /// <param name="sourcePtr">A pointer to the unmanaged memory block containing the raw pixel data.</param>
        /// <param name="lengthInBytes">The total size of the pixel payload in bytes.</param>
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
        public unsafe static Texture LoadFromBytes(
            IntPtr sourcePtr,
            int lengthInBytes,
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

            texture.byteLength = lengthInBytes;
            texture.nativeBytes = (IntPtr)NativeMemory.Alloc((nuint)lengthInBytes);

            System.Buffer.MemoryCopy(sourcePtr.ToPointer(), texture.nativeBytes.ToPointer(), lengthInBytes, lengthInBytes);

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
        public unsafe void Init(string? name = "", bool isCubemap = false, int cubemapHandle = -1)
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
                nativeBytes
            );
            this.Initialized = true;

            if (this.nativeBytes != IntPtr.Zero)
            {
                NativeMemory.Free(this.nativeBytes.ToPointer());
                this.nativeBytes = IntPtr.Zero;
                this.byteLength = 0;
            }

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
            int depth = 1, // Added depth parameter
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
            texture.Depth = depth;

            if (texture.Handle == 0)
            {
                string errorMsg = $"Failed to generate texture handle for texture '{name}'.";
                Logger.Log(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            GL.BindTexture(target, texture.Handle);

            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)textureSWrapMode);

            // Texture1DArray and Texture2DArray both use WrapT
            if (target == TextureTarget.Texture2D || target == TextureTarget.Texture2DArray || target == TextureTarget.Texture3D)
                GL.TexParameter(target, TextureParameterName.TextureWrapT, (int)textureTWrapMode);

            GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)textureMinFilter);
            GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)textureMagFilter);
            GL.TexParameter(target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(target, TextureParameterName.TextureMaxLevel, 0);

            // Texture creation
            if (target == TextureTarget.Texture1D)
            {
                GL.TexImage1D(target, 0, pixelInternalFormat, width, 0, pixelFormat, pixelType, nint.Zero);
            }
            else if (target == TextureTarget.Texture2D)
            {
                GL.TexImage2D(target, 0, pixelInternalFormat, width, height, 0, pixelFormat, pixelType, nint.Zero);
            }
            else if (target == TextureTarget.Texture2DArray || target == TextureTarget.Texture3D)
            {
                // Texture arrays require TexImage3D, where depth represents the array slice count
                GL.TexImage3D(target, 0, pixelInternalFormat, width, height, depth, 0, pixelFormat, pixelType, nint.Zero);
            }

            texture.Initialized = true;

            if (logCreation)
                new LogBuilder()
                .Write("Loaded empty Texture ")
                .Write(texture.Handle)
                .Write((name != "" ? $", named {name}" : "") + " ")
                .Write($"sized {width}x{height}x{depth} in {Logger.EndTimingBlockFormatted()}")
                .Log();

            return texture;
        }

        /// <summary>
        /// Loads a collection of image files from disk and packs them into a single GPU <see cref="Texture"/>.
        /// <br/> Does not need <see cref="Init(string?, bool, int)"/>.
        /// </summary>
        /// <param name="paths">An array of file paths pointing to the source images to be loaded.</param>
        /// <param name="flipVertical">If <c>true</c>, flips each image vertically upon loading to match OpenGL coordinate conventions. Defaults to <c>true</c>.</param>
        /// <param name="pixelInternalFormat">The internal sizing and format storage used by the GPU for the texture array. Defaults to <see cref="PixelInternalFormat.Rgba"/>.</param>
        /// <param name="pixelFormat">The format of the pixel data contained in the source images. Defaults to <see cref="PixelFormat.Rgba"/>.</param>
        /// <param name="type">The datatype of the pixel components (e.g., unsigned byte, float). Defaults to <see cref="PixelType.UnsignedByte"/>.</param>
        /// <param name="textureSWrapMode">The texture wrapping mode for the S (horizontal) coordinate. Defaults to <see cref="TextureWrapMode.Repeat"/>.</param>
        /// <param name="textureTWrapMode">The texture wrapping mode for the T (vertical) coordinate. Defaults to <see cref="TextureWrapMode.Repeat"/>.</param>
        /// <param name="textureMinFilter">The minification filter used when scaling the texture down. Defaults to <see cref="TextureMinFilter.Linear"/>.</param>
        /// <param name="textureMagFilter">The magnification filter used when scaling the texture up. Defaults to <see cref="TextureMagFilter.Linear"/>.</param>
        /// <returns>A fully initialized <see cref="Texture"/> instance representing the 2D texture array containing all successfully uploaded layers.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="paths"/> is <c>null</c> or contains zero elements.</exception>
        /// <exception cref="InvalidOperationException">Thrown if any subsequent image's dimensions do not match the width and height of the first image in the batch.</exception>
        public static Texture LoadTextureArray(
            string[] paths,
            bool parallel = false,
            bool expandToFit = false,
            bool flipVertical = true,
            PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgba,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType type = PixelType.UnsignedByte,
            TextureWrapMode textureSWrapMode = TextureWrapMode.Repeat,
            TextureWrapMode textureTWrapMode = TextureWrapMode.Repeat,
            TextureMinFilter textureMinFilter = TextureMinFilter.Linear,
            TextureMagFilter textureMagFilter = TextureMagFilter.Linear)
        {
            if (paths == null || paths.Length == 0)
                throw new ArgumentException("At least one path must be provided to create a Texture Array.");

            Logger.BeginTimingBlock();

            PixelMapping mapping = pixelFormat == PixelFormat.Rgb ? PixelMapping.RGB : PixelMapping.RGBA;
            string textureName = $"TextureArray_{paths.Length}_Layers";

            int targetWidth = 0;
            int targetHeight = 0;

            if (expandToFit)
            {
                int maxW = 0;
                int maxH = 0;

                foreach (string path in paths)
                {
                    using MagickImage pinger = new();
                    pinger.Ping(path);

                    if (pinger.Width > maxW) maxW = (int)pinger.Width;
                    if (pinger.Height > maxH) maxH = (int)pinger.Height;
                }

                targetWidth = maxW;
                targetHeight = maxH;
            }
            else
            {
                using MagickImage firstPinger = new(paths[0]);
                targetWidth = (int)firstPinger.Width;
                targetHeight = (int)firstPinger.Height;
            }

            // Allocate the texture array on the GPU once
            Texture arrayTexture = LoadFromSize(
                width: targetWidth,
                height: targetHeight,
                depth: paths.Length,
                target: TextureTarget.Texture2DArray,
                pixelInternalFormat: pixelInternalFormat,
                pixelFormat: pixelFormat,
                pixelType: type,
                textureSWrapMode: textureSWrapMode,
                textureTWrapMode: textureTWrapMode,
                textureMinFilter: textureMinFilter,
                textureMagFilter: textureMagFilter,
                name: textureName
            );

            int bytesPerComponent = GetBytesPerComponent(type);
            int components = GetComponentsCount(pixelFormat);
            int layerSizeBytes = targetWidth * targetHeight * components * bytesPerComponent;
            int totalBufferSize = layerSizeBytes * paths.Length;

            IntPtr nativeBuffer = Marshal.AllocHGlobal(totalBufferSize);

            try
            {
                Action<int> processImageAction = i =>
                {
                    using var image = new MagickImage(File.ReadAllBytes(paths[i]));

                    if ((image.Width != targetWidth || image.Height != targetHeight) && !expandToFit)
                    {
                        throw new InvalidOperationException($"Image at index {i} ({paths[i]}) does not match target dimensions of {targetWidth}x{targetHeight}.");
                    }

                    if (flipVertical)
                    {
                        image.Flip();
                    }

                    if (expandToFit)
                    {
                        image.Extent((uint)targetWidth, (uint)targetHeight, Gravity.Northwest, MagickColors.Transparent);
                    }

                    IntPtr layerDestination = IntPtr.Add(nativeBuffer, i * layerSizeBytes);

                    using IUnsafePixelCollection<byte> pixels = image.GetPixelsUnsafe();

                    unsafe
                    {
                        byte* nativePointer = (byte*)pixels.GetAreaPointer(0, 0, image.Width, image.Height);

                        // Use MemoryCopy instead of BlockCopy for raw unmanaged pointers (void*)
                        System.Buffer.MemoryCopy(
                            source: nativePointer,
                            destination: layerDestination.ToPointer(),
                            destinationSizeInBytes: layerSizeBytes,
                            sourceBytesToCopy: layerSizeBytes
                        );
                    }
                };

                if (parallel)
                {
                    Parallel.For(0, paths.Length, processImageAction);
                }
                else
                {
                    for (int i = 0; i < paths.Length; i++)
                    {
                        processImageAction(i);
                    }
                }

                // Phase 2: Create a ReadOnlySpan over each native layer slice and upload to GPU sequentially
                unsafe
                {
                    for (int i = 0; i < paths.Length; i++)
                    {
                        IntPtr layerPtr = IntPtr.Add(nativeBuffer, i * layerSizeBytes);
                        ReadOnlySpan<byte> layerSpan = new ReadOnlySpan<byte>(layerPtr.ToPointer(), layerSizeBytes);
                        arrayTexture.SetLayerData(i, layerSpan);
                    }
                }
            }
            finally
            {
                // Clean up unmanaged memory block
                Marshal.FreeHGlobal(nativeBuffer);
            }

            new LogBuilder()
                .Write("Loaded Texture Array ")
                .Write(arrayTexture.Handle)
                .Write($", named {textureName} ")
                .Write($"with {paths.Length} layers sized {arrayTexture.Width}x{arrayTexture.Height}x{paths.Length} in {Logger.EndTimingBlockFormatted()}")
                .Log();

            return arrayTexture;
        }
    }
}