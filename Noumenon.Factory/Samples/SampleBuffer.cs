using Noumenon.Dsp.Shared;

namespace Noumenon.Factory.Samples;

/// <summary>
/// One factory sample while it is made: one or two channels of doubles, and the finishing steps
/// every file goes through. A loop is rendered longer than it will be and closed one of two ways:
/// <see cref="CloseSeam"/> crossfades the overhang into the head with equal power (for material
/// with noise in it, whose two ends are unrelated), <see cref="Fold"/> adds the overhang onto the
/// head (for rhythmic loops whose events sit on the grid, so a tail rings into the next bar
/// exactly as it would when the loop repeats). A one-shot gets short fades instead. Then the DC
/// goes and the peak is set.
/// </summary>
internal sealed class SampleBuffer
{
    public SampleBuffer(int channels, int frames)
    {
        Channels = channels;
        Data = new double[channels][];
        for (var c = 0; c < channels; c++)
            Data[c] = new double[frames];
    }

    public int Channels { get; }

    public double[][] Data { get; private set; }

    public int Frames => Data[0].Length;

    public double[] Left => Data[0];

    public double[] Right => Data[Channels - 1];

    public static SampleBuffer Mono(double seconds) => new(1, Synth.Frames(seconds));

    public static SampleBuffer Stereo(double seconds) => new(2, Synth.Frames(seconds));

    /// <summary>Adds a mono value at a frame, panned when the buffer is stereo.</summary>
    public void Add(int frame, double value, double pan = 0.0)
    {
        if ((uint)frame >= (uint)Frames)
            return;

        if (Channels == 1)
        {
            Data[0][frame] += value;
            return;
        }

        var (l, r) = Synth.Pan(pan);
        Data[0][frame] += value * l;
        Data[1][frame] += value * r;
    }

    /// <summary>Keeps <paramref name="frames"/> frames and crossfades the rest (the overhang) into the start, so the end flows into the beginning.</summary>
    public SampleBuffer CloseSeam(int frames)
    {
        var overhang = Frames - frames;
        for (var c = 0; c < Channels; c++)
        {
            var source = Data[c];
            var looped = new double[frames];
            Array.Copy(source, looped, frames);
            for (var i = 0; i < overhang; i++)
            {
                var t = (i + 0.5) / overhang;
                looped[i] = source[i] * Math.Sin(0.5 * Math.PI * t) + source[frames + i] * Math.Cos(0.5 * Math.PI * t);
            }

            Data[c] = looped;
        }

        return this;
    }

    /// <summary>Keeps <paramref name="frames"/> frames and adds everything after them back onto the start, as the loop would play it.</summary>
    public SampleBuffer Fold(int frames)
    {
        for (var c = 0; c < Channels; c++)
        {
            var source = Data[c];
            var looped = new double[frames];
            for (var i = 0; i < source.Length; i++)
                looped[i % frames] += source[i];

            Data[c] = looped;
        }

        return this;
    }

    public SampleBuffer Fade(double inSeconds, double outSeconds)
    {
        var fadeIn = Synth.Frames(inSeconds);
        var fadeOut = Synth.Frames(outSeconds);
        for (var c = 0; c < Channels; c++)
        {
            var data = Data[c];
            for (var i = 0; i < fadeIn && i < data.Length; i++)
                data[i] *= Math.Sin(0.5 * Math.PI * i / fadeIn);

            for (var i = 0; i < fadeOut && i < data.Length; i++)
                data[data.Length - 1 - i] *= Math.Sin(0.5 * Math.PI * i / fadeOut);
        }

        return this;
    }

    public void RemoveDc()
    {
        foreach (var data in Data)
        {
            var mean = data.Average();
            for (var i = 0; i < data.Length; i++)
                data[i] -= mean;
        }
    }

    public double Peak() => Data.Max(data => data.Max(Math.Abs));

    public void Normalize(double peakDb)
    {
        var peak = Peak();
        if (peak <= 0.0)
            return;

        var gain = Synth.Db(peakDb) / peak;
        foreach (var data in Data)
        {
            for (var i = 0; i < data.Length; i++)
                data[i] *= gain;
        }
    }
}

/// <summary>Writes 16-bit PCM WAV files at 48 kHz with triangular (TPDF) dither from a seeded generator, so the same samples give the same bytes.</summary>
internal static class WavWriter
{
    public static void Write(string path, SampleBuffer buffer, ulong ditherSeed)
    {
        var rng = new Xoshiro128(ditherSeed);
        var channels = buffer.Channels;
        var frames = buffer.Frames;
        var dataBytes = frames * channels * 2;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataBytes);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(Synth.Rate);
        writer.Write(Synth.Rate * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataBytes);
        for (var i = 0; i < frames; i++)
        {
            for (var c = 0; c < channels; c++)
            {
                var dither = rng.NextFloat() - rng.NextFloat();
                var value = Math.Round(buffer.Data[c][i] * 32767.0 + dither);
                writer.Write((short)Math.Clamp(value, -32768.0, 32767.0));
            }
        }
    }
}
