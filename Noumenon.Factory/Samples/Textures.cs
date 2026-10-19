using static Noumenon.Factory.Samples.Synth;

namespace Noumenon.Factory.Samples;

/// <summary>
/// The field-like textures, made from noise: wind through moving band-passes, rain as a bed and a
/// rain of tiny filtered drops, surf whose low-pass opens on each swell, embers, tape hiss,
/// shortwave static and whistles, insects, vinyl. Their slow movements (gusts, swells, fades)
/// are smooth loops at whole multiples of the loop rate, so the shape repeats; the noise itself
/// is closed with the seam crossfade like the sustained tones.
/// </summary>
internal static partial class SampleCatalogue
{
    private static SampleSpec[] Textures =>
    [
        new("Wind", "", -1.0, 41, true, Wind),
        new("Rain", "", -1.0, 42, true, Rain),
        new("Surf", "", -1.0, 43, true, Surf),
        new("Embers", "", -1.0, 44, true, Embers),
        new("Tape Hiss", "", -12.0, 45, true, TapeHiss),
        new("Shortwave", "", -1.0, 46, true, Shortwave),
        new("Night Insects", "", -1.0, 47, true, NightInsects),
        new("Vinyl Crackle", "", -6.0, 48, true, VinylCrackle),
    ];

    private static SampleBuffer Wind(ulong seed)
    {
        const double seconds = 8.0;
        var noise = new Noise(seed);
        var buffer = Loop(2, seconds);
        var bands = Enumerable.Range(0, 2).Select(_ => (Low: new Svf(), High: new Svf(), Whistle: new Svf())).ToArray();
        var gust = SmoothLoop(noise.Random, seconds, 4);
        var sweep = Enumerable.Range(0, 2).Select(_ => SmoothLoop(noise.Random, seconds)).ToArray();
        var whistle = SmoothLoop(noise.Random, seconds, 2);
        var rumble = Biquad.LowPass(120, 0.7);
        var rumbleHigh = Biquad.HighPass(30, 0.7);
        Render(buffer, (i, t) =>
        {
            var strength = 0.35 + 0.65 * Math.Pow(0.5 + 0.5 * gust(t), 1.5);
            var low = rumbleHigh.Process(rumble.Process(noise.Brown()));
            var output = new double[2];
            for (var c = 0; c < 2; c++)
            {
                var source = noise.Pink();
                var first = bands[c].Low.Process(source, 350 * Math.Pow(2.0, 1.2 * sweep[c](t)), 1.2).Band;
                var second = bands[c].High.Process(source, 1100 * Math.Pow(2.0, 0.8 * sweep[1 - c](t + 3)), 2.0).Band;
                var whistleLevel = Math.Max(0.0, whistle(t + c)) * Math.Max(0.0, gust(t)) * 0.6;
                var tone = bands[c].Whistle.Process(source, 1650 * (1 + 0.08 * sweep[c](t)), 60).Band;
                output[c] = strength * (first + 0.5 * second + whistleLevel * tone) + 0.3 * low;
            }

            return (output[0], output[1]);
        });

        return Close(buffer, seconds);
    }

    private static SampleBuffer Rain(ulong seed)
    {
        const double seconds = 6.0;
        var noise = new Noise(seed);
        var buffer = Loop(2, seconds);
        var beds = Enumerable.Range(0, 2).Select(_ => (High: Biquad.HighPass(400, 0.7), Low: Biquad.LowPass(6000, 0.7))).ToArray();
        Render(buffer, (i, t) =>
        {
            var left = 0.15 * beds[0].Low.Process(beds[0].High.Process(noise.Pink()));
            var right = 0.15 * beds[1].Low.Process(beds[1].High.Process(noise.Pink()));

            return (left, right);
        });

        // Drops: Poisson-timed impulses, each ringing a short band-pass at its own pitch, at its own place in the stereo field.
        var at = 0.0;
        while (at < buffer.Frames / (double)Rate)
        {
            at += -Math.Log(1.0 - noise.Random.NextFloat() * 0.999) / 45.0;
            var filter = Biquad.BandPass(1500 + 5500 * noise.Random.NextFloat(), 5 + 10 * noise.Random.NextFloat());
            var gain = Math.Pow(noise.Random.NextFloat(), 2.0) * 1.5;
            var pan = noise.Random.NextBipolar();
            var start = Frames(at);
            for (var i = 0; i < Frames(0.03); i++)
                buffer.Add(start + i, gain * filter.Process(i == 0 ? 1.0 : 0.0), pan);
        }

        return Close(buffer, seconds);
    }

    private static SampleBuffer Surf(ulong seed)
    {
        const double seconds = 10.0;
        const double swell = 5.0;
        var noise = new Noise(seed);
        var buffer = Loop(2, seconds);
        var bodies = Enumerable.Range(0, 2).Select(_ => new Svf()).ToArray();
        var crashes = Enumerable.Range(0, 2).Select(_ => Biquad.HighPass(2000, 0.7)).ToArray();
        Render(buffer, (i, t) =>
        {
            var output = new double[2];
            for (var c = 0; c < 2; c++)
            {
                // A wave rises slowly and falls away faster: the swell's phase is bent before the sine.
                var phase = ((t + 0.3 * c) / swell % 1.0 + 1.0) % 1.0;
                var shaped = Math.Pow(Math.Sin(Math.PI * Math.Pow(phase, 0.7)), 2.0);
                var body = bodies[c].Process(noise.Pink(), 250 + 3800 * shaped, 0.7).Low;
                var crash = crashes[c].Process(noise.White()) * Math.Pow(shaped, 4.0);
                output[c] = (0.25 + 0.75 * shaped) * body + 0.25 * crash;
            }

            return (output[0], output[1]);
        });

        return Close(buffer, seconds);
    }

    private static SampleBuffer Embers(ulong seed)
    {
        const double seconds = 4.0;
        var noise = new Noise(seed);
        var buffer = Loop(2, seconds);
        var rumble = Enumerable.Range(0, 2).Select(_ => Biquad.LowPass(150, 0.7)).ToArray();
        var glow = SmoothLoop(noise.Random, seconds);
        Render(buffer, (i, t) =>
        {
            var level = 0.8 * (0.75 + 0.25 * glow(t));

            return (level * rumble[0].Process(noise.Brown()), level * rumble[1].Process(noise.Brown()));
        });

        var at = 0.0;
        while (at < buffer.Frames / (double)Rate)
        {
            at += -Math.Log(1.0 - noise.Random.NextFloat() * 0.999) / 12.0;
            var filter = Biquad.BandPass(800 + 7200 * Math.Pow(noise.Random.NextFloat(), 1.5), 2 + 6 * noise.Random.NextFloat());
            var gain = Math.Pow(noise.Random.NextFloat(), 3.0) * 4.0;
            var decay = 0.002 + 0.008 * noise.Random.NextFloat();
            var pan = 0.8 * noise.Random.NextBipolar();
            var start = Frames(at);
            var burst = 1 + noise.Random.NextInt(4);
            for (var i = 0; i < Frames(decay * 6); i++)
            {
                var excitation = i < burst ? noise.White() : 0.0;
                buffer.Add(start + i, gain * Math.Exp(-(double)i / Rate / decay) * filter.Process(excitation), pan);
            }
        }

        return Close(buffer, seconds);
    }

    private static SampleBuffer TapeHiss(ulong seed)
    {
        const double seconds = 4.0;
        var noise = new Noise(seed);
        var buffer = Loop(2, seconds);
        var shaping = Enumerable.Range(0, 2).Select(_ => (High: Biquad.HighPass(200, 0.7), Presence: Biquad.Peak(5000, 0.8, 4), Low: Biquad.LowPass(14000, 0.7))).ToArray();
        Render(buffer, (i, t) =>
        {
            var hum = 0.004 * (Math.Sin(2 * Math.PI * 50 * t) + 0.5 * Math.Sin(2 * Math.PI * 100 * t + 1) + 0.25 * Math.Sin(2 * Math.PI * 150 * t + 2));
            var output = new double[2];
            for (var c = 0; c < 2; c++)
                output[c] = 0.2 * shaping[c].Low.Process(shaping[c].Presence.Process(shaping[c].High.Process(noise.White()))) + hum;

            return (output[0], output[1]);
        });

        return Close(buffer, seconds);
    }

    private static SampleBuffer Shortwave(ulong seed)
    {
        const double seconds = 8.0;
        var noise = new Noise(seed);
        var buffer = Loop(1, seconds);
        var hiss = Biquad.BandPass(1500, 0.5);
        var fading = SmoothLoop(noise.Random, seconds);
        double phaseOne = 0, phaseTwo = 0;
        var carrier = Periodic(440, seconds);
        var syllables = Periodic(3, seconds);
        var cycle = Periodic(1.0 / seconds, seconds);
        Render(buffer, (i, t) =>
        {
            phaseOne += (800 + 600 * Math.Sin(2 * Math.PI * cycle * t)) / Rate;
            phaseTwo += (1900 + 300 * Math.Sin(2 * Math.PI * 2 * cycle * t + 1)) / Rate;
            var whistles = 0.08 * Math.Sin(2 * Math.PI * phaseOne) * (0.6 + 0.4 * Math.Sin(2 * Math.PI * 5 * cycle * t)) + 0.05 * Math.Sin(2 * Math.PI * phaseTwo);
            var voice = 0.12 * Math.Pow(Math.Sin(Math.PI * cycle * t), 2.0) * Math.Sin(2 * Math.PI * carrier * t) * (1 + 0.8 * Math.Sin(2 * Math.PI * syllables * t));
            var crackle = noise.Random.Chance(0.0004f) ? noise.White() * 0.8 : 0.0;
            var level = 0.7 + 0.3 * fading(t);

            return (level * (0.25 * hiss.Process(noise.White()) + whistles + voice) + crackle, 0);
        });

        return Close(buffer, seconds);
    }

    private static SampleBuffer NightInsects(ulong seed)
    {
        const double seconds = 4.0;
        var noise = new Noise(seed);
        var buffer = Loop(2, seconds);
        var bed = Enumerable.Range(0, 2).Select(_ => Biquad.LowPass(2000, 0.7)).ToArray();
        // Carrier, pulses per chirp, pulse rate, chirp period (dividing the loop), pan, level.
        (double Hz, int Pulses, double PulseRate, double Period, double Pan, double Level)[] insects =
        [
            (4500, 3, 40, 0.5, -0.5, 0.5),
            (6200, 40, 60, 2.0, 0.4, 0.25),
            (5100, 4, 25, 1.0, 0.1, 0.4),
            (7400, 5, 50, 2.0, 0.7, 0.2),
        ];
        Render(buffer, (i, t) =>
        {
            var left = 0.02 * bed[0].Process(noise.Pink());
            var right = 0.02 * bed[1].Process(noise.Pink());
            foreach (var (hz, pulses, pulseRate, period, pan, level) in insects)
            {
                var inChirp = ((t % period) + period) % period;
                var pulse = inChirp * pulseRate;
                if (pulse >= pulses)
                    continue;

                var envelope = Hann(pulse % 1.0) * level;
                var value = envelope * Math.Sin(2 * Math.PI * Periodic(hz, seconds) * t + 0.02 * hz / 30 * Math.Sin(2 * Math.PI * 30 * t));
                var (l, r) = Pan(pan);
                left += value * l;
                right += value * r;
            }

            return (left, right);
        });

        return Close(buffer, seconds);
    }

    private static SampleBuffer VinylCrackle(ulong seed)
    {
        const double seconds = 4.0;
        var noise = new Noise(seed);
        var buffer = Loop(2, seconds);
        var rumble = Biquad.LowPass(40, 0.7);
        var hiss = Enumerable.Range(0, 2).Select(_ => Biquad.HighPass(4000, 0.7)).ToArray();
        var turntable = Periodic(0.5556, seconds);
        Render(buffer, (i, t) =>
        {
            var low = 0.12 * rumble.Process(noise.Brown()) * (1 + 0.3 * Math.Sin(2 * Math.PI * turntable * t));

            return (low + 0.03 * hiss[0].Process(noise.White()), low + 0.03 * hiss[1].Process(noise.White()));
        });

        var at = 0.0;
        while (at < buffer.Frames / (double)Rate)
        {
            var pop = noise.Random.Chance(0.03f);
            at += -Math.Log(1.0 - noise.Random.NextFloat() * 0.999) / 40.0;
            var gain = (pop ? 0.3 : 0.15) * Math.Pow(noise.Random.NextFloat(), 2.0) * (noise.Random.Chance(0.5f) ? 1 : -1);
            var smoothing = new OnePole(pop ? 1500 : 6000 + 6000 * noise.Random.NextFloat());
            var pan = 0.3 * noise.Random.NextBipolar();
            var start = Frames(at);
            for (var i = 0; i < Frames(0.004); i++)
                buffer.Add(start + i, smoothing.Process(i < 2 ? gain * 12 : 0.0), pan);
        }

        return Close(buffer, seconds);
    }
}
