using Noumenon.Dsp.Spectral;
using Noumenon.Engine;
using Noumenon.Engine.Presets;
using Noumenon.Engine.Samples;

namespace Noumenon.Factory.Bank;

/// <summary>How a preset is played to be measured: on its own, with notes (a gated or MIDI-played preset is silent without them) or with audio fed in (an FX preset processes its input).</summary>
internal enum AuditionKind
{
    Free,
    Notes,
    Input,
}

/// <summary>
/// Plays a preset offline the way someone first hears it — loaded fresh, from its own seed, for
/// half a minute — and measures what the bank has to keep consistent: integrated loudness per
/// ITU-R BS.1770 (K-weighting, 400 ms blocks, the absolute and the relative gate), the sample
/// peak, the quietest second (a preset that falls silent shows here), how far the level wanders
/// (a living drone from a static one) and the spectral centroid (dark from bright). The first two
/// seconds are left out, so the start-up glide and a slow attack do not count. Notes are a held
/// C4, then G3, then E4; the input is two factory samples, looped.
/// </summary>
internal static class Audition
{
    public const int Rate = 48000;
    private const int Block = 256;
    private const double SkipSeconds = 2.0;

    public sealed record Result(double Lufs, double PeakDb, double QuietestDb, double SwingLu, double CentroidHz, bool Finite);

    public static Result Measure(PresetState state, AuditionKind kind, double seconds = 30.0)
    {
        var engine = new NoumenonEngine();
        var warnings = new List<string>();
        engine.ApplyPreset(state, loadSamples: true, warnings);
        if (warnings.Count > 0)
            throw new InvalidOperationException($"{state.Name}: {string.Join("; ", warnings)}");

        engine.Prepare(Rate, Block);
        engine.Reset(state.Seed);

        var total = (int)(seconds * Rate);
        var left = new float[total];
        var right = new float[total];
        var input = kind == AuditionKind.Input ? TestInput(total) : null;
        var notes = kind == AuditionKind.Notes ? Notes() : [];
        var next = 0;
        for (var pos = 0; pos < total; pos += Block)
        {
            var count = Math.Min(Block, total - pos);
            while (next < notes.Length && notes[next].Seconds * Rate < pos + count)
            {
                if (notes[next].On)
                    engine.Midi.NoteOn(notes[next].Note, 100);
                else
                    engine.Midi.NoteOff(notes[next].Note);

                next++;
            }

            if (input is not null)
                engine.Process(input, input, left, right, pos, count);
            else
                engine.Process(left, right, pos, count);
        }

        return Analyze(left, right);
    }

    private static (double Seconds, int Note, bool On)[] Notes() =>
    [
        (0.5, 60, true), (11.0, 60, false),
        (12.0, 55, true), (21.0, 55, false),
        (22.0, 64, true),
    ];

    private static float[] TestInput(int total)
    {
        var buffer = new float[total];
        var sources = new[] { "Kalimba Run.wav", "Choir Breath.wav", "Rain.wav" }
            .Select(name => FactoryContent.Resolve(FactoryContent.Sample(name)))
            .Where(File.Exists)
            .Select(SampleDecoder.Decode)
            .ToArray();
        if (sources.Length == 0)
            throw new InvalidOperationException("The FX audition needs the factory samples; run the samples command first.");

        var pos = 0;
        while (pos < total)
        {
            foreach (var source in sources)
            {
                for (var i = 0; i < source.Length && pos < total; i++, pos++)
                    buffer[pos] = 0.5f * (source.Left[i] + source.Right[i]);
            }
        }

        return buffer;
    }

    private static Result Analyze(float[] left, float[] right)
    {
        var start = (int)(SkipSeconds * Rate);
        var peak = 0f;
        var finite = true;
        for (var i = start; i < left.Length; i++)
        {
            peak = Math.Max(peak, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
            finite &= float.IsFinite(left[i]) && float.IsFinite(right[i]);
        }

        var weightedLeft = KWeight(left, start);
        var weightedRight = KWeight(right, start);
        var blocks = MeanSquares(weightedLeft, weightedRight, (int)(0.4 * Rate), (int)(0.1 * Rate));
        var seconds = MeanSquares(weightedLeft, weightedRight, Rate, Rate);
        var quietest = double.MaxValue;
        for (var s = 0; s < seconds.Length; s++)
            quietest = Math.Min(quietest, Loudness(seconds[s]));

        var perSecond = seconds.Select(Loudness).Where(l => l > -70.0).ToArray();
        var swing = perSecond.Length > 1 ? Math.Sqrt(perSecond.Select(l => (l - perSecond.Average()) * (l - perSecond.Average())).Average()) : 0.0;

        return new Result(Integrated(blocks), Db(peak), quietest, swing, Centroid(left, right, start), finite);
    }

    // BS.1770 K-weighting at 48 kHz: the head-related high shelf, then the RLB high-pass.
    private static double[] KWeight(float[] x, int start)
    {
        var y = new double[x.Length - start];
        double s1 = 0, s2 = 0, h1 = 0, h2 = 0;
        for (var i = 0; i < y.Length; i++)
        {
            var input = (double)x[start + i];
            var shelf = 1.53512485958697 * input + s1;
            s1 = -2.69169618940638 * input + 1.69065929318241 * shelf + s2;
            s2 = 1.19839281085285 * input - 0.73248077421585 * shelf;
            var high = shelf + h1;
            h1 = -2.0 * shelf + 1.99004745483398 * high + h2;
            h2 = shelf - 0.99007225036621 * high;
            y[i] = high;
        }

        return y;
    }

    private static double[] MeanSquares(double[] l, double[] r, int length, int step)
    {
        var result = new List<double>();
        for (var start = 0; start + length <= l.Length; start += step)
        {
            var sum = 0.0;
            for (var i = start; i < start + length; i++)
                sum += l[i] * l[i] + r[i] * r[i];

            result.Add(sum / length);
        }

        return result.ToArray();
    }

    private static double Loudness(double meanSquare) => meanSquare <= 0.0 ? -120.0 : -0.691 + 10.0 * Math.Log10(meanSquare);

    private static double Integrated(double[] blocks)
    {
        var absolute = blocks.Where(z => Loudness(z) > -70.0).ToArray();
        if (absolute.Length == 0)
            return -120.0;

        var relativeGate = Loudness(absolute.Average()) - 10.0;
        var gated = absolute.Where(z => Loudness(z) > relativeGate).ToArray();

        return gated.Length == 0 ? -120.0 : Loudness(gated.Average());
    }

    private static double Centroid(float[] left, float[] right, int start)
    {
        const int size = 4096;
        var fft = new Fft(size);
        var re = new float[size];
        var im = new float[size];
        double weighted = 0, total = 0;
        for (var frame = start; frame + size <= left.Length; frame += Rate / 2)
        {
            for (var i = 0; i < size; i++)
            {
                var window = 0.5f - 0.5f * MathF.Cos(2f * MathF.PI * i / size);
                re[i] = window * 0.5f * (left[frame + i] + right[frame + i]);
                im[i] = 0f;
            }

            fft.Forward(re, im, size);
            for (var bin = 1; bin < size / 2; bin++)
            {
                var magnitude = Math.Sqrt(re[bin] * re[bin] + im[bin] * im[bin]);
                weighted += magnitude * bin * Rate / (double)size;
                total += magnitude;
            }
        }

        return total > 0 ? weighted / total : 0;
    }

    private static double Db(float linear) => linear <= 0f ? -120.0 : 20.0 * Math.Log10(linear);
}
