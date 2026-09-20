using ImageMagick;
using System.Runtime.InteropServices;

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
        /// The depth of the texture (used for Texture3D or the number of layers in a Texture2DArray).
        /// </summary>
        public int Depth { get; set; } = 1;

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
        /// Downloads raw pixel data directly into native unmanaged memory.
        /// Caller is responsible for freeing the returned IntPtr using Marshal.FreeHGlobal.
        /// </summary>
        public IntPtr GetNativeBytes(out int bufferSize)
        {
            int bytesPerComponent = GetBytesPerComponent(this.PixelType);
            int components = GetComponentsCount(this.PixelFormat);
            int bytesPerPixel = components * bytesPerComponent;
            bufferSize = Width * Height * Depth * bytesPerPixel;

            Bind();

            IntPtr nativeBuffer = Marshal.AllocHGlobal(bufferSize);

            // Prevent row alignment issues for odd width textures or single-channel data
            GL.GetInteger(GetPName.PackAlignment, out int previousAlignment);
            GL.PixelStore(PixelStoreParameter.PackAlignment, 1);

            GL.GetTexImage(this.Target, 0, this.PixelFormat, this.PixelType, nativeBuffer);

            // Restore previous alignment state
            GL.PixelStore(PixelStoreParameter.PackAlignment, previousAlignment);

            return nativeBuffer;
        }

        /// <summary>
        /// Exports the GPU texture data to a local image file on disk.
        /// </summary>
        public void SaveToFile(string filePath)
        {
            IntPtr nativeBuffer = GetNativeBytes(out int bufferSize);

            try
            {
                unsafe
                {
                    ReadOnlySpan<byte> pixelSpan = new ReadOnlySpan<byte>(nativeBuffer.ToPointer(), bufferSize);
                    SaveBufferToFile(pixelSpan, filePath);
                }
                Logger.Log($"Saved Texture {Handle} as {filePath}");
            }
            finally
            {
                Marshal.FreeHGlobal(nativeBuffer);
            }
        }


        /// <summary>
        /// Core helper that converts a raw pixel memory buffer span into an ImageMagick image and saves it to disk.
        /// </summary>
        private void SaveBufferToFile(ReadOnlySpan<byte> pixelSpan, string filePath)
        {
            var settings = new PixelReadSettings(
                (uint)Width,
                (uint)Height,
                GetStorageType(this.PixelType),
                GetPixelMapping(this.PixelFormat)
            );

            using var image = new MagickImage(pixelSpan, settings);

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
        }

        /// <summary>
        /// Uploads raw pixel data to a specific layer index of a Texture2DArray.
        /// </summary>
        public unsafe void SetLayerData(int layerIndex, ReadOnlySpan<byte> pixels)
        {
            if (this.Target != TextureTarget.Texture2DArray)
                throw new InvalidOperationException("SetLayerData is only valid for TextureTarget.Texture2DArray.");

            if (layerIndex < 0 || layerIndex >= this.Depth)
                throw new ArgumentOutOfRangeException(nameof(layerIndex), $"Layer {layerIndex} is out of bounds for depth {this.Depth}.");

            Bind();

            // Safely pin the span's memory reference without allocations or array copying
            fixed (byte* ptr = &MemoryMarshal.GetReference(pixels))
            {
                GL.TexSubImage3D(
                    this.Target,
                    0,                  // level
                    0, 0, layerIndex,   // xoffset, yoffset, zoffset
                    this.Width, this.Height, 1, // width, height, depth (1 layer being updated)
                    this.PixelFormat,
                    this.PixelType,
                    (IntPtr)ptr
                );
            }
        }

        /// <summary>
        /// Exports a specific layer of a Texture2DArray to an image file on disk.
        /// </summary>
        public void SaveLayerToFile(int layerIndex, string filePath)
        {
            if (this.Target != TextureTarget.Texture2DArray)
                throw new InvalidOperationException("SaveLayerToFile is only valid for TextureTarget.Texture2DArray.");

            if (layerIndex < 0 || layerIndex >= this.Depth)
                throw new ArgumentOutOfRangeException(nameof(layerIndex), $"Layer {layerIndex} is out of bounds for depth {this.Depth}.");

            IntPtr fullBuffer = GetNativeBytes(out int totalBufferSize);

            int bytesPerComponent = GetBytesPerComponent(this.PixelType);
            int components = GetComponentsCount(this.PixelFormat);
            int layerSizeBytes = Width * Height * components * bytesPerComponent;

            // Calculate memory offset for the specific layer slice
            IntPtr layerBuffer = IntPtr.Add(fullBuffer, layerIndex * layerSizeBytes);

            try
            {
                unsafe
                {
                    // Wrap the specific layer's unmanaged memory into a span
                    ReadOnlySpan<byte> layerSpan = new ReadOnlySpan<byte>(layerBuffer.ToPointer(), layerSizeBytes);
                    SaveBufferToFile(layerSpan, filePath);
                }
                Logger.Log($"Saved Texture {Handle} Layer {layerIndex} as {filePath}");
            }
            finally
            {
                Marshal.FreeHGlobal(fullBuffer);
            }
        }

        /// <summary>
        /// Exports all layers of a Texture2DArray to corresponding image files on disk.
        /// </summary>
        public void SaveLayersToFiles(string[] filePaths, bool parallel = true)
        {
            if (this.Target != TextureTarget.Texture2DArray)
                throw new InvalidOperationException("SaveLayersToFiles is only valid for TextureTarget.Texture2DArray.");

            if (filePaths == null)
                throw new ArgumentNullException(nameof(filePaths));

            int layersToSave = Math.Min(this.Depth, filePaths.Length);

            // Fetch the entire texture array buffer from the GPU once
            IntPtr fullBuffer = GetNativeBytes(out int totalBufferSize);

            int bytesPerComponent = GetBytesPerComponent(this.PixelType);
            int components = GetComponentsCount(this.PixelFormat);
            int layerSizeBytes = Width * Height * components * bytesPerComponent;

            try
            {
                unsafe
                {
                    if (parallel)
                    {
                        Parallel.For(0, layersToSave, i =>
                        {
                            if (string.IsNullOrWhiteSpace(filePaths[i]))
                                return;

                            IntPtr layerBuffer = IntPtr.Add(fullBuffer, i * layerSizeBytes);
                            ReadOnlySpan<byte> layerSpan = new ReadOnlySpan<byte>(layerBuffer.ToPointer(), layerSizeBytes);

                            SaveBufferToFile(layerSpan, filePaths[i]);
                            Logger.LogThreadSafe($"Saved Texture {Handle} Layer {i} as {filePaths[i]}");
                        });
                    }
                    else
                    {
                        for (int i = 0; i < layersToSave; i++)
                        {
                            if (string.IsNullOrWhiteSpace(filePaths[i]))
                                return;

                            IntPtr layerBuffer = IntPtr.Add(fullBuffer, i * layerSizeBytes);
                            ReadOnlySpan<byte> layerSpan = new ReadOnlySpan<byte>(layerBuffer.ToPointer(), layerSizeBytes);

                            SaveBufferToFile(layerSpan, filePaths[i]);
                            Logger.LogThreadSafe($"Saved Texture {Handle} Layer {i} as {filePaths[i]}");
                        }
                    }
                }
            }
            finally
            {
                // Ensure the full buffer allocated by GetNativeBytes is always cleaned up
                Marshal.FreeHGlobal(fullBuffer);
            }
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