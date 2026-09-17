using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpShaders
{
    public static class Misc
    {
        internal static int GetBytesPerComponent(PixelType pixelType) => pixelType switch
        {
            PixelType.UnsignedByte or PixelType.Byte => 1,
            PixelType.UnsignedShort or PixelType.Short or PixelType.HalfFloat => 2,
            PixelType.UnsignedInt or PixelType.Int or PixelType.Float => 4,
            _ => throw new NotSupportedException($"Unsupported PixelType: {pixelType}")
        };

        internal static int GetComponentsCount(PixelFormat format) => format switch
        {
            PixelFormat.Red or PixelFormat.RedInteger or PixelFormat.DepthComponent => 1,
            PixelFormat.Rg or PixelFormat.RgInteger => 2,
            PixelFormat.Rgb or PixelFormat.Bgr or PixelFormat.RgbInteger or PixelFormat.BgrInteger => 3,
            PixelFormat.Rgba or PixelFormat.Bgra or PixelFormat.RgbaInteger or PixelFormat.BgraInteger => 4,
            _ => throw new NotSupportedException($"Unsupported PixelFormat: {format}")
        };

        internal static MagickFormat GetMagickFormat(PixelFormat format) => format switch
        {
            PixelFormat.Red or PixelFormat.RedInteger or PixelFormat.DepthComponent => MagickFormat.Gray,
            PixelFormat.Rg or PixelFormat.RgInteger => MagickFormat.Gray,
            PixelFormat.Rgb or PixelFormat.RgbInteger => MagickFormat.Rgb,
            PixelFormat.Bgr or PixelFormat.BgrInteger => MagickFormat.Bgr,
            PixelFormat.Rgba or PixelFormat.RgbaInteger => MagickFormat.Rgba,
            PixelFormat.Bgra or PixelFormat.BgraInteger => MagickFormat.Bgra,
            _ => throw new NotSupportedException($"Unsupported OpenGL PixelFormat: {format}")
        };

        internal static string GetPixelMapping(PixelFormat format) => format switch
        {
            PixelFormat.Red or PixelFormat.RedInteger or PixelFormat.DepthComponent => "R",
            PixelFormat.Rg or PixelFormat.RgInteger => "RG",
            PixelFormat.Rgb or PixelFormat.RgbInteger => "RGB",
            PixelFormat.Bgr or PixelFormat.BgrInteger => "BGR",
            PixelFormat.Rgba or PixelFormat.RgbaInteger => "RGBA",
            PixelFormat.Bgra or PixelFormat.BgraInteger => "BGRA",
            _ => throw new NotSupportedException($"Unsupported OpenGL PixelFormat string: {format}")
        };

        internal static StorageType GetStorageType(PixelType pixelType) => pixelType switch
        {
            PixelType.UnsignedByte or PixelType.Byte => StorageType.Char,
            PixelType.UnsignedShort or PixelType.Short => StorageType.Short,
            PixelType.UnsignedInt or PixelType.Int => StorageType.Int32,
            PixelType.Float => StorageType.Float,
            PixelType.HalfFloat => StorageType.Float,
            _ => throw new NotSupportedException($"Unsupported PixelType for Magick StorageType: {pixelType}")
        };
    }
}
