using static Noumenon.Factory.Samples.Synth;

namespace Noumenon.Factory.Samples;

/// <summary>
/// The plucked and struck events — material for Granular and retriggered Classic playback. They
/// are modal: each note is a handful of exponentially decaying partials at the instrument's
/// ratios (kalimba tines, music-box combs, church bells, glass), except the harp, which is a
/// Karplus-Strong string. They are one-shots, not loops, and end with a short fade.
/// </summary>
internal static partial class SampleCatalogue
{
    private static SampleSpec[] Events =>
    [
        new("Kalimba Run", "", -1.0, 31, false, KalimbaRun),
        new("Music Box", "", -1.0, 32, false, MusicBox),
        new("Bell Strikes", "", -1.0, 33, false, BellStrikes),
        new("Glass Taps", "", -1.0, 34, false, GlassTaps),
        new("Harp Glissando", "", -1.0, 35, false, HarpGlissando),
    ];

    private static SampleBuffer KalimbaRun(ulong seed)
    {
        int[] notes = [60, 64, 67, 69, 72, 74, 76, 79];
        var noise = new Noise(seed);
        var buffer = SampleBuffer.Mono(6.0);
        for (var n = 0; n < notes.Length; n++)
        {
            var at = 0.05 + 0.6 * n + 0.04 * noise.Random.NextBipolar();
            var velocity = 0.7 + 0.3 * noise.Random.NextFloat();
            Strike(buffer, at, NoteHz(notes[n]), velocity, 0.0, [1.0, 5.4, 11.2], [1.0, 0.25, 0.08], [0.9, 0.12, 0.05]);
            Click(buffer, at, 4000, 0.04 * velocity, 0.003, 0.0, noise);
        }

        return buffer.Fade(0.002, 0.3);
    }

    private static SampleBuffer MusicBox(ulong seed)
    {
        int[] tune = [81, 76, 73, 76, 78, 76, 73, 71, 69, 71, 73, 76, 73, 69, 71, 69];
        var noise = new Noise(seed);
        var buffer = SampleBuffer.Mono(6.0);
        for (var n = 0; n < tune.Length; n++)
        {
            var at = 0.05 + 0.33 * n;
            var velocity = 0.6 + 0.4 * noise.Random.NextFloat();
            Strike(buffer, at, NoteHz(tune[n]), velocity, 0.0, [1.0, 2.76, 5.40, 8.93], [1.0, 0.4, 0.15, 0.06], [0.7, 0.25, 0.1, 0.05]);
            Click(buffer, at - 0.004, 3000, 0.05, 0.002, 0.0, noise);
        }

        return buffer.Fade(0.002, 0.4);
    }

    private static SampleBuffer BellStrikes(ulong seed)
    {
        double[] ratios = [0.5, 1.0, 1.183, 1.506, 2.0, 2.514, 2.662, 3.011, 4.166];
        double[] gains = [0.35, 0.6, 0.5, 0.3, 1.0, 0.25, 0.2, 0.25, 0.12];
        double[] decays = [3.2, 2.0, 1.5, 1.1, 1.0, 0.55, 0.5, 0.35, 0.25];
        (double At, double Nominal, double Pan)[] bells = [(0.2, NoteHz(67), -0.6), (2.6, NoteHz(72), 0.0), (5.0, NoteHz(70), 0.6)];
        var noise = new Noise(seed);
        var buffer = SampleBuffer.Stereo(8.0);
        foreach (var (at, nominal, pan) in bells)
        {
            // Each partial is a pair a fraction of a hertz apart: real bells are never quite round, and they warble.
            Strike(buffer, at, nominal / 2.0, 0.5, pan, ratios.Select(r => r * 1.0003).ToArray(), gains, decays);
            Strike(buffer, at, nominal / 2.0, 0.5, pan, ratios.Select(r => r * 0.9997).ToArray(), gains, decays);
            Click(buffer, at, 2200, 0.06, 0.005, pan, noise);
        }

        return buffer.Fade(0.002, 0.6);
    }

    private static SampleBuffer GlassTaps(ulong seed)
    {
        int[] scale = [72, 74, 76, 79, 81, 84, 86, 88, 91, 93, 96];
        var noise = new Noise(seed);
        var buffer = SampleBuffer.Mono(4.0);
        var at = 0.05;
        while (at < 3.4)
        {
            var hz = NoteHz(scale[noise.Random.NextInt(scale.Length)]);
            var velocity = 0.4 + 0.6 * noise.Random.NextFloat();
            Strike(buffer, at, hz, velocity, 0.0, [1.0, 2.32, 4.25], [1.0, 0.35, 0.12], [0.35, 0.12, 0.05]);
            Click(buffer, at, 6000, 0.03 * velocity, 0.0015, 0.0, noise);
            at += 0.15 + 0.4 * noise.Random.NextFloat();
        }

        return buffer.Fade(0.002, 0.25);
    }

    private static SampleBuffer HarpGlissando(ulong seed)
    {
        int[] dMajor = [50, 52, 54, 55, 57, 59, 61, 62, 64, 66, 67, 69, 71, 73, 74];
        var noise = new Noise(seed);
        var buffer = SampleBuffer.Mono(5.0);
        for (var n = 0; n < dMajor.Length; n++)
        {
            var start = Frames(0.1 + 0.12 * n);
            var hz = NoteHz(dMajor[n]);
            var harpString = new PluckedString(hz, 3.5 - n * 0.12, 0.35, noise);
            var level = 0.8 - 0.02 * n;
            for (var i = start; i < buffer.Frames; i++)
                buffer.Left[i] += level * harpString.Next();
        }

        return buffer.Fade(0.002, 0.5);
    }

    /// <summary>Adds a struck note: partials at <paramref name="ratios"/> of <paramref name="hz"/> with their gains and decay times (seconds to fall by e), a soft 2 ms attack.</summary>
    private static void Strike(SampleBuffer buffer, double at, double hz, double velocity, double pan, double[] ratios, double[] gains, double[] decays)
    {
        var start = Frames(at);
        for (var p = 0; p < ratios.Length; p++)
        {
            var partialHz = hz * ratios[p];
            if (partialHz >= 0.45 * Rate)
                continue;

            var end = Math.Min(buffer.Frames, start + Frames(decays[p] * 9.0));
            for (var i = Math.Max(0, start); i < end; i++)
            {
                var t = (double)(i - start) / Rate;
                var attack = Math.Min(1.0, t / 0.002);
                buffer.Add(i, velocity * gains[p] * attack * Math.Exp(-t / decays[p]) * Math.Sin(2 * Math.PI * partialHz * t), pan);
            }
        }
    }

    /// <summary>Adds a short burst of band-passed noise: the strike of a mallet, the tick of a mechanism.</summary>
    private static void Click(SampleBuffer buffer, double at, double hz, double gain, double length, double pan, Noise noise)
    {
        var filter = Biquad.BandPass(hz, 1.5);
        var start = Frames(at);
        var frames = Frames(length * 4);
        for (var i = 0; i < frames; i++)
        {
            var t = (double)i / Rate;
            buffer.Add(start + i, gain * Math.Exp(-t / length) * filter.Process(noise.White()), pan);
        }
    }
}
