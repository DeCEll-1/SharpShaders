using ImageMagick;

namespace SharpShaders.Textures
{
    /// <summary>
    /// Represents a managed wrapper for an OpenGL texture resource, providing methods
    /// to bind, activate, read back raw pixel data, save images, and manage resource disposal.
    /// </summary>
    public partial class Texture : IDisposable
    {
        /// <summary>
        /// Gets or sets the native OpenGL handle for this texture instance.
        /// </summary>
        public int Handle { get; set; }

        /// <summary>
        /// Indicates whether the texture has been initialized and allocated on the GPU context.
        /// </summary>
        public bool Initialized = false;

        /// <summary>
        /// The width of the texture in pixels.
        /// </summary>
        public int Width;

        /// <summary>
        /// The height of the texture in pixels.
        /// </summary>
        public int Height;

        /// <summary>
        /// Gets a value indicating whether this instance has been disposed.
        /// </summary>
        public bool disposed { get; private set; } = false;

        #region opengl functions

        /// <summary>
        /// Sets a texture parameter for the currently bound texture target.
        /// </summary>
        /// <param name="name">The texture parameter to adjust.</param>
        /// <param name="param">The value to set for the specified parameter.</param>
        public void Parameter(TextureParameterName name, int param)
        {
            GL.TexParameter(
                this.Target,
                name,
                param
            );
        }

        /// <summary>
        /// Verifies whether the texture has been initialized and logs a warning if it has not.
        /// </summary>
        private void Check()
        {
            if (Initialized)
                return;
            Logger.Log($"Texture {Handle} used without initalisation");
        }

        /// <summary>
        /// Binds this texture to its associated <see cref="Target"/> in the active OpenGL context.
        /// </summary>
        public void Bind()
        {
            Check();
            GL.BindTexture(this.Target, Handle);
        }

        /// <summary>
        /// Selects and activates the specified OpenGL texture unit.
        /// </summary>
        /// <param name="unit">The texture unit to activate (e.g., <see cref="TextureUnit.Texture0"/>).</param>
        public void Activate(TextureUnit unit)
        {
            Check();
            GL.ActiveTexture(unit);
        }

        #endregion

        /// <summary>
        /// Downloads and returns the raw pixel byte array directly from GPU memory.
        /// Supports byte, float, single-channel, and multi-channel textures.
        /// </summary>
        public byte[] GetBytes()
        {
            int bytesPerComponent = GetBytesPerComponent(this.PixelType);
            int components = GetComponentsCount(this.PixelFormat);
            int bytesPerPixel = components * bytesPerComponent;

            byte[] output = new byte[Width * Height * bytesPerPixel];

            Bind();

            // Prevent row alignment issues for odd width textures or single-channel data
            GL.GetInteger(GetPName.PackAlignment, out int previousAlignment);
            GL.PixelStore(PixelStoreParameter.PackAlignment, 1);

            GL.GetTexImage(this.Target, 0, this.PixelFormat, this.PixelType, output);

            // Restore previous alignment state
            GL.PixelStore(PixelStoreParameter.PackAlignment, previousAlignment);

            return output;
        }

        /// <summary>
        /// Exports the GPU texture data to a local image file on disk.
        /// </summary>
        public void SaveToFile(string filePath)
        {
            byte[] rawBytes = GetBytes();

            // Use PixelReadSettings to specify dimensions, storage type, and channel mapping
            var settings = new PixelReadSettings(
                (uint)Width,
                (uint)Height,
                GetStorageType(this.PixelType),
                GetPixelMapping(this.PixelFormat)
            );

            // Read raw pixel bytes using PixelReadSettings
            using var image = new MagickImage(rawBytes, settings);

            // Normalize high dynamic range float data (like R32F) down to visible display bounds
            if (this.PixelType == PixelType.Float || this.PixelType == PixelType.HalfFloat)
            {
                image.Normalize();
            }

            image.Flip();

            image.Format = (filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            filePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                ? MagickFormat.Jpeg
                : MagickFormat.Png;

            image.Write(filePath);
            Logger.Log($"Saved Texture {Handle} as {filePath}");
        }

        #region disposal

        /// <summary>
        /// Finalizes an instance of the <see cref="Texture"/> class and warns if the GPU resource was leaked.
        /// </summary>
        ~Texture()
        {
            if (disposed == false)
                Logger.Log(
                    $"GPU Resource leak for texture! Did you forget to call Dispose()?"
                );
        }

        /// <summary>
        /// Releases unmanaged OpenGL texture resources and optionally logs disposal.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        /// <param name="log">Controls whether a disposal confirmation message is written to <see cref="Logger"/>.</param>
        protected virtual void Dispose(bool disposing, bool log = true)
        {
            if (!disposed)
            {
                GL.DeleteTexture(Handle);
                if (log)
                    Logger.Log(
                        $"Disposed Texture {Handle} {(Name != "" ? $", named {Name}" : "")}"
                    );
                Handle = 0;
                disposed = true;
            }
        }

        /// <summary>
        /// Controls whether disposal messages are emitted to <see cref="Logger"/> when <see cref="Dispose()"/> is invoked.
        /// </summary>
        public bool logDisposal = true;

        /// <summary>
        /// Releases all unmanaged GPU resources used by the <see cref="Texture"/> and suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true, logDisposal);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}