using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using Noumenon.Ui;

namespace Noumenon.Factory.Artwork;

/// <summary>
/// Draws Noumenon's artwork from code, so it is reproducible and stays in the editor's palette
/// (<see cref="EditorTheme"/>): the application icon — a dark tile holding a teal Lissajous
/// figure, the shape the XY display draws when two tones a fifth apart meet — at every size
/// Windows asks for, and the installer's wizard images (the tall welcome panel with the wordmark,
/// the small corner tile) at 100, 150 and 200 % DPI. The figure is drawn as the scope draws it:
/// a soft wide glow under a thin bright trace, over a faint display grid on the larger sizes.
/// </summary>
internal static class ArtworkRenderer
{
    private const int FigureX = 3;
    private const int FigureY = 2;
    private const float FigurePhase = MathF.PI / 2f;

    public static Bitmap Icon(int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        Prepare(g);
        g.Clear(Color.Transparent);

        var inset = size >= 32 ? size / 32f : 0f;
        var tile = new RectangleF(inset, inset, size - 2f * inset, size - 2f * inset);
        using (var path = RoundedRectangle(tile, size * 0.2f))
        {
            using var fill = new LinearGradientBrush(tile, EditorTheme.PanelLight, EditorTheme.PanelDark, LinearGradientMode.Vertical);
            g.FillPath(fill, path);
            if (size >= 48)
                DrawGrid(g, tile, size >= 128 ? 8 : 4, path);

            using var edge = new Pen(EditorTheme.FrameLight, Math.Max(1f, size / 64f));
            g.DrawPath(edge, path);
        }

        // Below 32 pixels the 3:2 figure is a tangle; the octave's figure eight still reads.
        var small = size < 32;
        var margin = tile.Width * (small ? 0.18f : 0.2f);
        var figure = RectangleF.Inflate(tile, -margin, -margin * (small ? 1.6f : 1f));
        var stroke = size >= 64 ? size / 40f : Math.Max(1.4f, size / 20f);
        DrawFigure(g, figure, stroke, glow: size >= 64, small ? 1 : FigureX, small ? 2 : FigureY, small ? 0f : FigurePhase);

        return bitmap;
    }

    public static Bitmap WizardImage(int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bitmap);
        Prepare(g);
        var bounds = new RectangleF(0f, 0f, width, height);
        using (var fill = new LinearGradientBrush(bounds, EditorTheme.Panel, EditorTheme.PanelDark, LinearGradientMode.Vertical))
        {
            g.FillRectangle(fill, bounds);
        }

        var unit = width / 164f;
        var display = new RectangleF(14f * unit, 22f * unit, width - 28f * unit, width - 28f * unit);
        using (var path = RoundedRectangle(display, 6f * unit))
        {
            using var screen = new SolidBrush(EditorTheme.Display);
            g.FillPath(screen, path);
            DrawGrid(g, display, 6, path);
            using var edge = new Pen(EditorTheme.DisplayEdge, Math.Max(1f, unit));
            g.DrawPath(edge, path);
        }

        DrawFigure(g, RectangleF.Inflate(display, -16f * unit, -16f * unit), 2.2f * unit, glow: true);

        using var title = new Font("Bahnschrift", 19f * unit, FontStyle.Regular, GraphicsUnit.Pixel);
        using var subtitle = new Font("Segoe UI Semibold", 9f * unit, FontStyle.Regular, GraphicsUnit.Pixel);
        using var light = new SolidBrush(EditorTheme.TextLight);
        using var dim = new SolidBrush(EditorTheme.TextDim);
        using var accent = new SolidBrush(EditorTheme.Accent);
        var textTop = display.Bottom + 34f * unit;
        DrawSpaced(g, "NOUMENON", title, light, width / 2f, textTop, 2.2f * unit);
        g.DrawString("generative ambient loops", subtitle, dim, new RectangleF(0f, textTop + 30f * unit, width, 14f * unit), EditorTheme.Center);
        g.FillRectangle(accent, width / 2f - 14f * unit, textTop + 50f * unit, 28f * unit, Math.Max(1f, 1.5f * unit));

        return bitmap;
    }

    public static Bitmap WizardSmallImage(int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bitmap);
        Prepare(g);
        g.Clear(Color.White);
        using var icon = Icon(Math.Min(width, height));
        g.DrawImage(icon, (width - icon.Width) / 2f, (height - icon.Height) / 2f);

        return bitmap;
    }

    private static void Prepare(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
    }

    private static void DrawFigure(Graphics g, RectangleF area, float stroke, bool glow, int ratioX = FigureX, int ratioY = FigureY, float phase = FigurePhase)
    {
        const int steps = 720;
        var points = new PointF[steps + 1];
        for (var i = 0; i <= steps; i++)
        {
            var t = 2f * MathF.PI * i / steps;
            var x = MathF.Sin(ratioX * t + phase);
            var y = MathF.Sin(ratioY * t);
            points[i] = new PointF(area.X + (x + 1f) * 0.5f * area.Width, area.Y + (1f - y) * 0.5f * area.Height);
        }

        if (glow)
        {
            using var halo = new Pen(Color.FromArgb(30, EditorTheme.Accent), stroke * 3f) { LineJoin = LineJoin.Round };
            g.DrawLines(halo, points);
            using var inner = new Pen(Color.FromArgb(70, EditorTheme.Accent), stroke * 1.7f) { LineJoin = LineJoin.Round };
            g.DrawLines(inner, points);
        }

        using var trace = new Pen(EditorTheme.Accent, stroke) { LineJoin = LineJoin.Round };
        g.DrawLines(trace, points);
    }

    private static void DrawGrid(Graphics g, RectangleF area, int divisions, GraphicsPath clip)
    {
        var state = g.Save();
        g.SetClip(clip);
        using var pen = new Pen(EditorTheme.DisplayGrid, 1f);
        for (var i = 1; i < divisions; i++)
        {
            var x = area.X + area.Width * i / divisions;
            var y = area.Y + area.Height * i / divisions;
            g.DrawLine(pen, x, area.Top, x, area.Bottom);
            g.DrawLine(pen, area.Left, y, area.Right, y);
        }

        g.Restore(state);
    }

    /// <summary>Draws <paramref name="text"/> centred on <paramref name="centreX"/> with <paramref name="spacing"/> pixels between the letters.</summary>
    private static void DrawSpaced(Graphics g, string text, Font font, Brush brush, float centreX, float top, float spacing)
    {
        var widths = text.Select(c => g.MeasureString(c.ToString(), font, PointF.Empty, StringFormat.GenericTypographic).Width).ToArray();
        var total = widths.Sum() + spacing * (text.Length - 1);
        var x = centreX - total / 2f;
        for (var i = 0; i < text.Length; i++)
        {
            g.DrawString(text[i].ToString(), font, brush, x, top, StringFormat.GenericTypographic);
            x += widths[i] + spacing;
        }
    }

    private static GraphicsPath RoundedRectangle(RectangleF r, float radius)
    {
        var d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180f, 90f);
        path.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
        path.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
        path.CloseFigure();

        return path;
    }
}
