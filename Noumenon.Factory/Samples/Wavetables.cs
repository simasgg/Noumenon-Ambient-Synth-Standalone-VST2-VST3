namespace Noumenon.Factory.Samples;

/// <summary>
/// The five factory wavetables. Each is 32 single cycles of 2048 points — the layout the engine
/// recognises as a wavetable file — added up from whole harmonics read off one sine table, so a
/// frame is exactly one period and band-limited by construction (512 harmonics at most). Frame 0
/// to frame 31 is the morph the oscillator's Shape scans.
/// </summary>
internal static partial class SampleCatalogue
{
    private const int FrameSize = 2048;
    private const int FrameCount = 32;

    private static readonly double[] SineTable = Enumerable.Range(0, FrameSize).Select(n => Math.Sin(2.0 * Math.PI * n / FrameSize)).ToArray();

    /// <summary>From a pure sine to a bright 1/n spectrum: the harmonics join one after another, each faded in over a frame.</summary>
    private static SampleBuffer HarmonicLadder() => Table(512, (frame, h) =>
    {
        var reach = Math.Pow(2.0, frame * 8.0 / (FrameCount - 1));
        var weight = Math.Clamp(reach - h + 1.0, 0.0, 1.0);

        return weight / h;
    });

    /// <summary>A 1/n source shaped by three formants moving through a, e, i, o, u, for a voice near 130 Hz.</summary>
    private static SampleBuffer VowelMorph()
    {
        double[][] vowels = [[730, 1090, 2440], [530, 1840, 2480], [270, 2290, 3010], [570, 840, 2410], [300, 870, 2240]];
        double[] gains = [1.0, 0.55, 0.3];
        const double voice = 130.0;

        return Table(128, (frame, h) =>
        {
            var position = frame / (double)(FrameCount - 1) * (vowels.Length - 1);
            var from = Math.Min((int)position, vowels.Length - 2);
            var t = position - from;
            var hz = voice * h;
            var amplitude = 0.015;
            for (var f = 0; f < 3; f++)
            {
                var centre = Math.Exp(Math.Log(vowels[from][f]) * (1 - t) + Math.Log(vowels[from + 1][f]) * t);
                var distance = Math.Log2(hz / centre) / 0.18;
                amplitude += gains[f] * Math.Exp(-0.5 * distance * distance);
            }

            return amplitude / Math.Sqrt(h);
        });
    }

    /// <summary>A band-limited pulse narrowing from 50 % to 3 %; a Lanczos taper keeps the edges from ringing.</summary>
    private static SampleBuffer PulseWidth() => Table(256, (frame, h) =>
    {
        var width = 0.5 - 0.47 * frame / (FrameCount - 1);
        var taper = Sinc(h / 257.0);

        return 2.0 / (Math.PI * h) * Math.Sin(Math.PI * h * width) * taper;
    }, cosine: true);

    /// <summary>Odd harmonics, their tilt going from dark to bright while a gentle resonance climbs from the third to the fortieth.</summary>
    private static SampleBuffer OddGlass() => Table(255, (frame, h) =>
    {
        if (h % 2 == 0)
            return 0.0;

        var t = frame / (double)(FrameCount - 1);
        var tilt = 2.0 - 1.4 * t;
        var peak = 3.0 * Math.Pow(40.0 / 3.0, t);
        var distance = Math.Log2(h / peak) / 0.35;

        return Math.Pow(h, -tilt) * (1.0 + 2.0 * Math.Exp(-0.5 * distance * distance));
    });

    /// <summary>Eight drawbar registrations on harmonics 1, 2, 3, 4, 6, 8, 10, 12 and 16, crossfading frame by frame; a step of a bar is 3 dB.</summary>
    private static SampleBuffer OrganStops()
    {
        int[] harmonics = [1, 3, 2, 4, 6, 8, 10, 12, 16];
        string[] registrations = ["808000000", "888000000", "008808000", "006876400", "888888888", "800000008", "008000880", "068600000"];

        return Table(16, (frame, h) =>
        {
            var bar = Array.IndexOf(harmonics, h);
            if (bar < 0)
                return 0.0;

            var position = frame / (double)(FrameCount - 1) * (registrations.Length - 1);
            var from = Math.Min((int)position, registrations.Length - 2);
            var t = position - from;

            return Bar(registrations[from][bar]) * (1 - t) + Bar(registrations[from + 1][bar]) * t;
        });

        static double Bar(char level) => level == '0' ? 0.0 : Synth.Db(-3.0 * ('8' - level));
    }

    private static SampleBuffer Table(int harmonics, Func<int, int, double> amplitude, bool cosine = false)
    {
        var buffer = new SampleBuffer(1, FrameSize * FrameCount);
        var data = buffer.Left;
        var quarter = cosine ? FrameSize / 4 : 0;
        for (var frame = 0; frame < FrameCount; frame++)
        {
            for (var h = 1; h <= harmonics; h++)
            {
                var a = amplitude(frame, h);
                if (a == 0.0)
                    continue;

                for (var n = 0; n < FrameSize; n++)
                    data[frame * FrameSize + n] += a * SineTable[(h * n + quarter) % FrameSize];
            }
        }

        return buffer;
    }

    private static double Sinc(double x) => x == 0.0 ? 1.0 : Math.Sin(Math.PI * x) / (Math.PI * x);
}
