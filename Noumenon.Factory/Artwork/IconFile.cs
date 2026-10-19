using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Noumenon.Factory.Artwork;

/// <summary>
/// Writes a Windows <c>.ico</c> holding several sizes of one image. Entries below 64 pixels are
/// 32-bit DIBs (straight alpha plus the 1-bit AND mask older APIs still read), the larger ones PNG,
/// the form Windows expects for the 256-pixel entry — so the file works for the executable's
/// resource, a window icon, a shortcut and the installer alike.
/// </summary>
internal static class IconFile
{
    private const int PngFrom = 64;

    public static void Write(string path, IReadOnlyList<Bitmap> images)
    {
        var payloads = images.Select(image => image.Width >= PngFrom ? Png(image) : Dib(image)).ToArray();
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)images.Count);

        var offset = 6 + 16 * images.Count;
        for (var i = 0; i < images.Count; i++)
        {
            writer.Write((byte)(images[i].Width >= 256 ? 0 : images[i].Width));
            writer.Write((byte)(images[i].Height >= 256 ? 0 : images[i].Height));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(payloads[i].Length);
            writer.Write(offset);
            offset += payloads[i].Length;
        }

        foreach (var payload in payloads)
            writer.Write(payload);
    }

    private static byte[] Png(Bitmap image)
    {
        using var stream = new MemoryStream();
        image.Save(stream, ImageFormat.Png);

        return stream.ToArray();
    }

    // A BITMAPINFOHEADER whose height counts the colour rows and the mask rows, the BGRA rows
    // bottom-up, then the AND mask (1 = transparent), each row padded to 32 bits.
    private static byte[] Dib(Bitmap image)
    {
        var width = image.Width;
        var height = image.Height;
        var maskStride = (width + 31) / 32 * 4;
        var pixels = new byte[width * height * 4];
        var data = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (var y = 0; y < height; y++)
                Marshal.Copy(data.Scan0 + y * data.Stride, pixels, y * width * 4, width * 4);
        }
        finally
        {
            image.UnlockBits(data);
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(40);
        writer.Write(width);
        writer.Write(height * 2);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0);
        writer.Write(pixels.Length + maskStride * height);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        for (var y = height - 1; y >= 0; y--)
            writer.Write(pixels, y * width * 4, width * 4);

        var mask = new byte[maskStride];
        for (var y = height - 1; y >= 0; y--)
        {
            Array.Clear(mask);
            for (var x = 0; x < width; x++)
            {
                if (pixels[(y * width + x) * 4 + 3] == 0)
                    mask[x / 8] |= (byte)(0x80 >> (x % 8));
            }

            writer.Write(mask);
        }

        return stream.ToArray();
    }
}
