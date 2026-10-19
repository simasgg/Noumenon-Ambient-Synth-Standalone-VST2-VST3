using static Noumenon.Factory.Samples.Synth;

namespace Noumenon.Factory.Samples;

/// <summary>
/// The rhythmic loops and the digital debris. The loops are exactly two bars — 4.0 s, at 120 BPM
/// (the heartbeat at 60) — with every event on the grid; they are rendered with room for the
/// last tails and folded (<see cref="SampleBuffer.Fold"/>), so a tail that rings past the end
/// sounds over the start just as when the loop repeats, and the loop length stays sample-exact
/// for Host Sync. Data Burst is a one-shot that is meant to sound broken.
/// </summary>
internal static partial class SampleCatalogue
{
    private static SampleSpec[] Rhythms =>
    [
        new("Pulse Loop 120", "", -1.0, 51, true, PulseLoop),
        new("Clockwork 120", "", -1.0, 52, true, Clockwork),
        new("Heartbeat 60", "", -1.0, 53, true, Heartbeat),
        new("Data Burst", "", -1.0, 54, false, DataBurst),
    ];

    private const double TwoBars = 4.0;
    private const double Tail = 1.0;

    private static SampleBuffer PulseLoop(ulong seed)
    {
        const double beat = 0.5;
        double[] tickAccents = [1.0, 0.5, 0.7, 0.5, 0.9, 0.5, 0.7, 0.6];
        (double At, int Note, double Length)[] bass = [(0.0, 33, 0.4), (0.75, 33, 0.2), (1.0, 36, 0.4), (1.75, 33, 0.2), (2.0, 31, 0.4), (2.75, 31, 0.2), (3.0, 28, 0.6)];
        var noise = new Noise(seed);
        var buffer = new SampleBuffer(2, Frames(TwoBars + Tail));
        for (var b = 0; b < 8; b++)
            Thump(buffer, b * beat, 90, 48, 0.18, b % 4 == 0 ? 1.0 : 0.7, 0.0);

        for (var e = 0; e < 16; e++)
        {
            var filter = Biquad.BandPass(6000, 2.0);
            var start = Frames(e * beat / 2);
            var gain = 0.25 * tickAccents[e % tickAccents.Length];
            var pan = e % 2 == 0 ? -0.3 : 0.3;
            for (var i = 0; i < Frames(0.08); i++)
                buffer.Add(start + i, gain * Math.Exp(-(double)i / Rate / 0.015) * filter.Process(noise.White()), pan);
        }

        foreach (var (at, note, length) in bass)
        {
            var start = Frames(at);
            var hz = NoteHz(note);
            for (var i = 0; i < Frames(length + 0.1); i++)
            {
                var t = (double)i / Rate;
                var envelope = Math.Min(1.0, t / 0.01) * (t < length ? 1.0 : Math.Exp(-(t - length) / 0.02));
                buffer.Add(start + i, 0.45 * envelope * Math.Sin(2 * Math.PI * hz * t));
            }
        }

        return buffer.Fold(Frames(TwoBars));
    }

    private static SampleBuffer Clockwork(ulong seed)
    {
        const double sixteenth = 0.125;
        double[] accents = [1.0, 0.4, 0.6, 0.4, 0.8, 0.4, 0.6, 0.5];
        var noise = new Noise(seed);
        var buffer = new SampleBuffer(1, Frames(TwoBars + Tail));
        for (var s = 0; s < 32; s++)
        {
            var tock = s % 4 == 0;
            var filter = Biquad.BandPass(tock ? 1400 : 3200, tock ? 10 : 12);
            var decay = tock ? 0.015 : 0.008;
            var gain = (tock ? 1.6 : 1.0) * accents[s % accents.Length];
            var start = Frames(s * sixteenth);
            for (var i = 0; i < Frames(decay * 8); i++)
                buffer.Add(start + i, gain * filter.Process(i < 3 ? noise.White() * 4 : 0.0) * Math.Exp(-(double)i / Rate / decay));
        }

        // The gears: band-passed noise whose level ripples at 16 Hz, a whole number of times in the loop.
        var whirr = Biquad.BandPass(600, 3);
        for (var i = 0; i < Frames(TwoBars); i++)
        {
            var t = (double)i / Rate;
            buffer.Add(i, 0.12 * (0.6 + 0.4 * Math.Sin(2 * Math.PI * 16 * t)) * whirr.Process(noise.White()));
        }

        return buffer.Fold(Frames(TwoBars));
    }

    private static SampleBuffer Heartbeat(ulong seed)
    {
        var buffer = new SampleBuffer(1, Frames(TwoBars + Tail));
        for (var beat = 0; beat < 4; beat++)
        {
            Thump(buffer, beat * 1.0, 70, 52, 0.08, 1.0, 0.0);
            Thump(buffer, beat * 1.0 + 0.28, 85, 62, 0.06, 0.7, 0.0);
        }

        var buffered = buffer.Fold(Frames(TwoBars));
        var low = Biquad.LowPass(220, 0.7);
        for (var pass = 0; pass < 2; pass++)
        {
            // Twice round the loop, so the filter state at the start is the one the end leaves behind.
            for (var i = 0; i < buffered.Frames; i++)
            {
                var filtered = low.Process(buffered.Left[i]);
                if (pass == 1)
                    buffered.Left[i] = filtered;
            }
        }

        return buffered;
    }

    private static SampleBuffer DataBurst(ulong seed)
    {
        var noise = new Noise(seed);
        var rng = noise.Random;
        var buffer = SampleBuffer.Mono(4.0);
        var at = 0.02;
        while (at < 3.8)
        {
            var kind = rng.NextInt(4);
            var length = 0.03 + 0.09 * rng.NextFloat();
            var start = Frames(at);
            var frames = Frames(length);
            switch (kind)
            {
                case 0:
                {
                    // A burst of square wave at a semitone between G3 and B6.
                    var hz = NoteHz(55 + rng.NextInt(40));
                    for (var i = 0; i < frames; i++)
                        buffer.Add(start + i, 0.35 * (Math.Sin(2 * Math.PI * hz * i / Rate) >= 0 ? 1 : -1));

                    break;
                }
                case 1:
                {
                    // Noise crushed to a few bits and held for several samples at a time.
                    var bits = 3 + rng.NextInt(2);
                    var hold = 4 << rng.NextInt(3);
                    var levels = 1 << bits;
                    var held = 0.0;
                    for (var i = 0; i < frames; i++)
                    {
                        if (i % hold == 0)
                            held = Math.Round(noise.White() * levels / 2) / (levels / 2.0);

                        buffer.Add(start + i, 0.4 * held);
                    }

                    break;
                }
                case 2:
                {
                    // A run of stepped sine tones, a new random pitch every 30 ms.
                    length = 0.03 * (4 + rng.NextInt(12));
                    frames = Frames(length);
                    var phase = 0.0;
                    var hz = 0.0;
                    for (var i = 0; i < frames; i++)
                    {
                        if (i % Frames(0.03) == 0)
                            hz = NoteHz(60 + rng.NextInt(36));

                        phase += hz / Rate;
                        buffer.Add(start + i, 0.4 * Math.Sin(2 * Math.PI * phase));
                    }

                    break;
                }
            }

            // Every event, and the silence after it, is a whole number of 10 ms ticks: it stays digital.
            at += Math.Ceiling(length / 0.01) * 0.01 + 0.05 + 0.25 * rng.NextFloat();
        }

        return buffer.Fade(0.001, 0.05);
    }

    /// <summary>Adds a soft thump: a sine whose pitch falls from <paramref name="fromHz"/> to <paramref name="toHz"/> in 60 ms, decaying with <paramref name="decay"/> seconds.</summary>
    private static void Thump(SampleBuffer buffer, double at, double fromHz, double toHz, double decay, double gain, double pan)
    {
        var start = Frames(at);
        var phase = 0.0;
        for (var i = 0; i < Frames(decay * 8); i++)
        {
            var t = (double)i / Rate;
            var hz = toHz + (fromHz - toHz) * Math.Exp(-t / 0.02);
            phase += hz / Rate;
            var envelope = Math.Min(1.0, t / 0.002) * Math.Exp(-t / decay);
            buffer.Add(start + i, gain * envelope * Math.Sin(2 * Math.PI * phase), pan);
        }
    }
}
