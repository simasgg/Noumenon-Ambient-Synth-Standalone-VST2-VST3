using System.Globalization;
using Noumenon.Engine;
using Noumenon.Engine.Parameters;
using Noumenon.Engine.Presets;
using Noumenon.Engine.Samples;

namespace Noumenon.Ui.Widgets;

/// <summary>
/// The sample library as a list: the factory samples, their wavetables and the player's own
/// folder (Documents\Noumenon\Samples), each group under a heading — there whatever preset is
/// loaded, since a preset only carries the samples it plays. Click a sample to load it onto the
/// sampler's key; a wavetable goes onto a key of its own and becomes the oscillators' table; a
/// sample already on a key is selected instead of loaded twice. A row shows the key that holds
/// it, the sampler's lit. Right-click for the other target and the folder; drop files on it to
/// copy them into the player's folder. The folders are read again whenever the mouse comes back
/// over the list, so files added in Explorer appear without a restart; the wheel or the bar on
/// the right scrolls.
/// </summary>
public sealed class SampleLibraryWidget : Widget, IFileDropTarget
{
    private const float RowHeight = 13f;
    private const float BarWidth = 5f;
    private const int WheelRows = 3;

    private readonly NoumenonEngine engine;
    private readonly IEditorHost host;
    private readonly List<Row> rows = new();
    private readonly Dictionary<string, int> keysByPath = new(StringComparer.OrdinalIgnoreCase);
    private int first;
    private bool dragging;
    private bool wasHot;
    private bool rescan = true;
    private int shownHash = -1;

    private sealed record Row(string Text, LibrarySample? Sample, bool IsHeading);

    public SampleLibraryWidget(NoumenonEngine engine, IEditorHost host, float x, float y, float width, float height)
    {
        this.engine = engine;
        this.host = host;
        Bounds = new RectangleF(x, y, width, height);
    }

    public override string? Tooltip => "Sample library · click: load onto the sampler's key (a wavetable: onto the table's) · right-click: more · drop files: add them to My samples";

    private int Capacity => Math.Max(1, (int)((Bounds.Height - 4f) / RowHeight));

    private bool Scrolls => rows.Count > Capacity;

    public override void Draw(Graphics g)
    {
        EditorDraw.Display(g, Bounds, 3f);
        Refresh();
        var selected = engine.Parameters.GetInt(ParamId.SamplerSelect);
        var wavetable = engine.Parameters.GetInt(ParamId.WavetableKey);
        var width = Bounds.Width - 4f - (Scrolls ? BarWidth + 2f : 0f);
        var shown = Math.Min(Capacity, rows.Count - first);
        for (var i = 0; i < shown; i++)
        {
            var row = rows[first + i];
            var r = new RectangleF(Bounds.X + 2f, Bounds.Y + 2f + i * RowHeight, width, RowHeight);
            if (row.IsHeading)
            {
                EditorDraw.Text(g, row.Text, new RectangleF(r.X + 3f, r.Y, r.Width - 6f, r.Height), EditorTheme.SmallFont, EditorTheme.Accent, EditorTheme.Left);
                continue;
            }

            if (row.Sample is not { } sample)
            {
                EditorDraw.Text(g, row.Text, new RectangleF(r.X + 12f, r.Y, r.Width - 14f, r.Height), EditorTheme.SmallFont, EditorTheme.TextDim, EditorTheme.Left);
                continue;
            }

            var key = keysByPath.GetValueOrDefault(sample.Path, -1);
            var lit = key >= 0 && key == (sample.IsWavetable ? wavetable : selected);
            if (lit)
            {
                using var fill = new SolidBrush(EditorTheme.ButtonOn);
                g.FillRectangle(fill, r);
            }

            EditorDraw.Text(g, sample.Name, new RectangleF(r.X + 12f, r.Y, r.Width - 70f, r.Height), EditorTheme.SmallFont, lit ? EditorTheme.TextLight : EditorTheme.Readout, EditorTheme.Left);
            if (key >= 0)
            {
                var table = key == wavetable && (sample.IsWavetable || key != selected);
                var tag = (table ? "WT · " : "") + "key " + key.ToString(CultureInfo.InvariantCulture);
                EditorDraw.Text(g, tag, new RectangleF(r.Right - 60f, r.Y, 57f, r.Height), EditorTheme.SmallValueFont, table ? EditorTheme.Accent : EditorTheme.TextDim, EditorTheme.Right);
            }
        }

        if (!Scrolls)
            return;

        var track = Track;
        using (var back = new SolidBrush(EditorTheme.DisplayGrid))
        {
            g.FillRectangle(back, track);
        }

        var thumbHeight = Math.Max(12f, track.Height * Capacity / rows.Count);
        var thumbY = track.Y + (track.Height - thumbHeight) * first / Math.Max(1, rows.Count - Capacity);
        using var thumb = new SolidBrush(dragging ? EditorTheme.Accent : EditorTheme.TraceDim);
        g.FillRectangle(thumb, new RectangleF(track.X, thumbY, track.Width, thumbHeight));
    }

    public override bool Update()
    {
        if (Hot && !wasHot)
            rescan = true;

        wasHot = Hot;
        Refresh();
        var hash = first * 7 + rows.Count * 131 + engine.Parameters.GetInt(ParamId.SamplerSelect) * 8191 + engine.Parameters.GetInt(ParamId.WavetableKey) * 524287 + (dragging ? 1 : 0);
        foreach (var (path, key) in keysByPath)
            hash = hash * 31 + key + StringComparer.OrdinalIgnoreCase.GetHashCode(path);

        if (hash == shownHash)
            return false;

        shownHash = hash;

        return true;
    }

    public override void MouseDown(PointF p, MouseButtons button, int clicks)
    {
        Refresh();
        if (Scrolls && p.X >= Track.X - 2f)
        {
            dragging = button == MouseButtons.Left;
            ScrollTo(p.Y);
            return;
        }

        var index = first + (int)((p.Y - Bounds.Y - 2f) / RowHeight);
        if (index < first || index >= rows.Count || rows[index].Sample is not { } sample)
            return;

        if (button == MouseButtons.Left)
        {
            Pick(sample);
            return;
        }

        if (button != MouseButtons.Right)
            return;

        var menu = new ContextMenuStrip();
        menu.Items.Add($"Load onto the sampler's key {engine.Parameters.GetInt(ParamId.SamplerSelect)}", null, (_, _) => host.LoadSampleFile(engine.Parameters.GetInt(ParamId.SamplerSelect), sample.Path));
        menu.Items.Add($"Load as the wavetable (key {WaveformWidget.WavetableTarget(engine)})", null, (_, _) => host.LoadSampleFile(WaveformWidget.WavetableTarget(engine), sample.Path, asWavetable: true));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Show the file in its folder", null, (_, _) => SampleLibraryMenu.ShowInFolder(FactoryContent.Resolve(sample.Path, engine.Samples.ContentFolder)));
        menu.Items.Add("Open My samples folder", null, (_, _) => SampleLibraryMenu.OpenUserFolder());
        ShowMenu(menu, p);
    }

    public override void MouseMove(PointF p, MouseButtons buttons)
    {
        if (dragging)
            ScrollTo(p.Y);
    }

    public override void MouseUp(PointF p, MouseButtons button)
    {
        if (!dragging)
            return;

        dragging = false;
        Invalidate();
    }

    public override void MouseWheel(PointF p, int delta)
    {
        Refresh();
        Scroll(first - Math.Sign(delta) * WheelRows);
    }

    public void DropFiles(string[] paths)
    {
        var files = paths.Where(path => File.Exists(path) && SampleLibrary.IsAudioFile(path)).ToArray();
        if (files.Length == 0)
        {
            host.Notice("Drop audio files to add them to My samples");
            return;
        }

        host.Notice(files.Length == 1 ? $"Adding {Path.GetFileName(files[0])} to My samples…" : $"Adding {files.Length} files to My samples…");
        Task.Run(() =>
        {
            foreach (var file in files)
                SampleLibrary.AddToUserFolder(file);
        }).ContinueWith(task =>
        {
            rescan = true;
            Invalidate();
            host.Notice(task.IsFaulted
                ? "Not added: " + (task.Exception?.GetBaseException().Message ?? "unknown error")
                : $"Added to My samples ({SampleLibrary.UserFolder})");
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private RectangleF Track => new(Bounds.Right - 2f - BarWidth, Bounds.Y + 2f, BarWidth, Bounds.Height - 4f);

    private void Pick(LibrarySample sample)
    {
        if (keysByPath.TryGetValue(sample.Path, out var key))
        {
            Commit(() => engine.Parameters.Set(sample.IsWavetable ? ParamId.WavetableKey : ParamId.SamplerSelect, key));
            Invalidate();
            return;
        }

        if (sample.IsWavetable)
            host.LoadSampleFile(WaveformWidget.WavetableTarget(engine), sample.Path, asWavetable: true);
        else
            host.LoadSampleFile(engine.Parameters.GetInt(ParamId.SamplerSelect), sample.Path);
    }

    private void ScrollTo(float y)
    {
        var track = Track;
        var fraction = Math.Clamp((y - track.Y) / Math.Max(1f, track.Height), 0f, 1f);
        Scroll((int)MathF.Round(fraction * (rows.Count - Capacity)));
    }

    private void Scroll(int row)
    {
        var next = Math.Clamp(row, 0, Math.Max(0, rows.Count - Capacity));
        if (next == first)
            return;

        first = next;
        Invalidate();
    }

    /// <summary>Reads the folders again when asked to, and which key holds each loaded file — the sampler's key first when two hold the same one.</summary>
    private void Refresh()
    {
        if (rescan)
        {
            rescan = false;
            Rebuild();
        }

        keysByPath.Clear();
        var selected = engine.Parameters.GetInt(ParamId.SamplerSelect);
        foreach (var slot in engine.Samples.Loaded)
        {
            if (slot.Path is { } path && (slot.Key == selected || !keysByPath.ContainsKey(path)))
                keysByPath[path] = slot.Key;
        }
    }

    private void Rebuild()
    {
        rows.Clear();
        var heading = "";
        var anyUser = false;
        foreach (var sample in SampleLibrary.Scan(engine.Samples.ContentFolder))
        {
            var next = SampleLibraryMenu.Heading(sample);
            if (next != heading)
            {
                heading = next;
                rows.Add(new Row(heading, null, true));
            }

            rows.Add(new Row(sample.Name, sample, false));
            anyUser |= !sample.IsFactory;
        }

        if (!anyUser)
        {
            rows.Add(new Row("MY SAMPLES", null, true));
            rows.Add(new Row("empty — drop files here, or put them in Documents\\Noumenon\\Samples", null, false));
        }

        first = Math.Clamp(first, 0, Math.Max(0, rows.Count - Capacity));
    }
}
