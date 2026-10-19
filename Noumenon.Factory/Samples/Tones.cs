using static Noumenon.Factory.Samples.Synth;

namespace Noumenon.Factory.Samples;

/// <summary>
/// The sustained tones: loops meant to be held, stretched and granulated. Every oscillator and
/// every LFO runs at a frequency with a whole number of cycles in the loop, so the tonal part
/// repeats exactly; what noise they carry is closed with a seam crossfade. Each renders a second
/// of pre-roll first, so filters have settled when the loop begins.
/// </summary>
internal static partial class SampleCatalogue
{
    private static SampleSpec[] Tones =>
    [
        new("Bowed Glass", "", -1.0, 21, true, BowedGlass),
        new("Choir Breath", "", -1.0, 22, true, ChoirBreath),
        new("Tape Strings", "", -1.0, 23, true, TapeStrings),
        new("Reed Organ", "", -1.0, 24, true, ReedOrgan),
        new("Deep Drone", "", -1.0, 25, true, DeepDrone),
        new("Breath Flute", "", -1.0, 26, true, BreathFlute),
    ];

    private const double PreRoll = 1.0;
    private const double Seam = 0.5;

    /// <summary>A glass bowl near C4: inharmonic partials, each a slowly beating pair, and a breath of band-passed noise as the bow.</summary>
    private static SampleBuffer BowedGlass(ulong seed)
    {
        const double seconds = 6.0;
        double[] ratios = [1.0, 2.32, 4.25, 6.63, 9.38];
        double[] gains = [1.0, 0.5, 0.28, 0.16, 0.09];
        var noise = new Noise(seed);
        var buffer = Loop(2, seconds);
        var bows = ratios.Select(r => (Left: Biquad.BandPass(NoteHz(60) * r, 40), Right: Biquad.BandPass(NoteHz(60) * r * 1.002, 40))).ToArray();
        var partials = new List<(double Hz, double Gain, double Phase, int Channel)>();
        for (var p = 0; p < ratios.Length; p++)
        {
            for (var c = 0; c < 2; c++)
            {
                var beat = 0.25 + 0.4 * noise.Random.NextFloat();
                var hz = NoteHz(60) * ratios[p];
                partials.Add((Periodic(hz - beat / 2, seconds), gains[p], noise.Random.NextFloat() * 6.283, c));
                partials.Add((Periodic(hz + beat / 2, seconds), gains[p], noise.Random.NextFloat() * 6.283, c));
            }
        }

        var swell = SmoothLoop(noise.Random, seconds);
        Render(buffer, (i, t) =>
        {
            var bow = 0.5 + 0.5 * swell(t);
            double left = 0, right = 0;
            foreach (var (hz, gain, phase, channel) in partials)
            {
                var value = gain * Math.Sin(2 * Math.PI * hz * t + phase);
                if (channel == 0)
                    left += value;
                else
                    right += value;
            }

            for (var p = 0; p < bows.Length; p++)
            {
                left += 0.6 * gains[p] * bow * bows[p].Left.Process(noise.White());
                right += 0.6 * gains[p] * bow * bows[p].Right.Process(noise.White());
            }

            return (left, right);
        });

        return Close(buffer, seconds);
    }

    /// <summary>A small choir on A3 singing 'ah': detuned saws with their own vibratos through four formant band-passes, a little breath, different voices left and right.</summary>
    private static SampleBuffer ChoirBreath(ulong seed)
    {
        const double seconds = 6.0;
        double[] formants = [730, 1090, 2440, 3400];
        double[] widths = [90, 110, 160, 250];
        double[] gains = [1.0, 0.5, 0.25, 0.1];
        double[][] detunes = [[-8, -4, 0, 4, 8], [-6, -2, 1, 5, 9]];
        var noise = new Noise(seed);
        var buffer = Loop(2, seconds);
        var voices = detunes.Select(side => side.Select(cents => (Saw: new BlepSaw(noise.Random.NextFloat()), Hz: Periodic(NoteHz(57) * Cents(cents), seconds), Rate: Periodic(4.2 + noise.Random.NextFloat() * 0.8, seconds), Phase: noise.Random.NextFloat() * 6.283)).ToArray()).ToArray();
        var filters = Enumerable.Range(0, 2).Select(_ => formants.Select((f, k) => Biquad.BandPass(f, f / widths[k])).ToArray()).ToArray();
        var breath = Enumerable.Range(0, 2).Select(_ => Biquad.BandPass(2500, 1.0)).ToArray();
        var air = Enumerable.Range(0, 2).Select(_ => Biquad.LowPass(6000, 0.7)).ToArray();
        Render(buffer, (i, t) =>
        {
            var output = new double[2];
            for (var c = 0; c < 2; c++)
            {
                var source = 0.0;
                foreach (var voice in voices[c])
                    source += voice.Saw.Next(voice.Hz * Cents(6 * Math.Sin(2 * Math.PI * voice.Rate * t + voice.Phase)));

                var sung = 0.0;
                for (var k = 0; k < formants.Length; k++)
                    sung += gains[k] * filters[c][k].Process(source);

                output[c] = air[c].Process(sung + 0.04 * breath[c].Process(noise.White()));
            }

            return (output[0], output[1]);
        });

        return Close(buffer, seconds);
    }

    /// <summary>An A minor chord from a string machine: three saws a note, a lazy low-pass, slow wow and light flutter, a whisper of tape hiss.</summary>
    private static SampleBuffer TapeStrings(ulong seed)
    {
        const double seconds = 6.0;
        int[] chord = [45, 52, 57, 60, 64];
        var noise = new Noise(seed);
        var buffer = Loop(2, seconds);
        var saws = Enumerable.Range(0, 2).Select(c => chord.SelectMany(note => new[] { -7.0, 0.0, 7.0 }.Select(cents => (Saw: new BlepSaw(noise.Random.NextFloat()), Hz: Periodic(NoteHz(note) * Cents(cents + (c == 0 ? -1.5 : 1.5)), seconds)))).ToArray()).ToArray();
        var tone = Enumerable.Range(0, 2).Select(_ => (A: Biquad.LowPass(2500, 0.6), B: Biquad.LowPass(4000, 0.5))).ToArray();
        var hiss = Enumerable.Range(0, 2).Select(_ => Biquad.HighPass(3000, 0.7)).ToArray();
        var wow = Periodic(0.25, seconds);
        var flutter = Periodic(6.0, seconds);
        Render(buffer, (i, t) =>
        {
            var output = new double[2];
            for (var c = 0; c < 2; c++)
            {
                var drift = Cents(8 * Math.Sin(2 * Math.PI * wow * t + c) + 2 * Math.Sin(2 * Math.PI * flutter * t + 2 * c));
                var sum = 0.0;
                foreach (var (saw, hz) in saws[c])
                    sum += saw.Next(hz * drift);

                output[c] = tone[c].B.Process(tone[c].A.Process(sum / 6)) + 0.004 * hiss[c].Process(noise.White());
            }

            return (output[0], output[1]);
        });

        return Close(buffer, seconds);
    }

    /// <summary>A harmonium reed on C3: a reed of odd-heavy harmonics and a quieter one slightly sharp, which beats against it, under a gentle tremolo.</summary>
    private static SampleBuffer ReedOrgan(ulong seed)
    {
        const double seconds = 6.0;
        var noise = new Noise(seed);
        var buffer = Loop(1, seconds);
        var partials = new List<(double Hz, double Gain)>();
        foreach (var (detune, reed) in new[] { (1.0, 1.0), (1.0035, 0.55) })
        {
            for (var h = 1; h <= 40; h++)
            {
                var gain = h % 2 == 1 ? 1.0 / h : 0.6 / Math.Pow(h, 1.2);
                partials.Add((Periodic(NoteHz(48) * detune * h, seconds), reed * gain * Math.Exp(-h / 28.0)));
            }
        }

        var tremolo = Periodic(5.5, seconds);
        var reedNoise = Biquad.BandPass(1800, 0.8);
        Render(buffer, (i, t) =>
        {
            var sum = 0.0;
            foreach (var (hz, gain) in partials)
                sum += gain * Math.Sin(2 * Math.PI * hz * t);

            var level = 1.0 + 0.08 * Math.Sin(2 * Math.PI * tremolo * t);

            return (level * sum + 0.01 * reedNoise.Process(noise.White()), 0);
        });

        return Close(buffer, seconds);
    }

    /// <summary>A low drone on C2 with its second and third harmonics and a fifth above that beats slowly, differently in each ear, under one slow swell.</summary>
    private static SampleBuffer DeepDrone(ulong seed)
    {
        const double seconds = 8.0;
        var noise = new Noise(seed);
        var buffer = Loop(2, seconds);
        var root = NoteHz(36);
        var fifth = NoteHz(43);
        (double Hz, double Gain)[] Side(double beat) =>
        [
            (Periodic(root, seconds), 1.0), (Periodic(root * 2, seconds), 0.45), (Periodic(root * 3, seconds), 0.2),
            (Periodic(fifth, seconds), 0.4), (Periodic(fifth + beat, seconds), 0.35), (Periodic(fifth * 2, seconds), 0.12),
        ];

        var sides = new[] { Side(0.2), Side(0.3) };
        var phases = sides.Select(side => side.Select(_ => noise.Random.NextFloat() * 6.283).ToArray()).ToArray();
        var swell = Periodic(0.1, seconds);
        Render(buffer, (i, t) =>
        {
            var output = new double[2];
            for (var c = 0; c < 2; c++)
            {
                var sum = 0.0;
                for (var k = 0; k < sides[c].Length; k++)
                    sum += sides[c][k].Gain * Math.Sin(2 * Math.PI * sides[c][k].Hz * t + phases[c][k]);

                output[c] = sum * (0.8 + 0.2 * Math.Sin(2 * Math.PI * swell * t + c));
            }

            return (output[0], output[1]);
        });

        return Close(buffer, seconds);
    }

    /// <summary>A breathy flute on D4: a sine core with a little of its overtones, narrow noise resonances on the harmonics, a 5 Hz vibrato.</summary>
    private static SampleBuffer BreathFlute(ulong seed)
    {
        const double seconds = 6.0;
        var noise = new Noise(seed);
        var buffer = Loop(1, seconds);
        var f0 = Periodic(NoteHz(62), seconds);
        var vibrato = Periodic(5.0, seconds);
        var resonances = new[] { (Filter: Biquad.BandPass(f0, 40), Gain: 0.5), (Filter: Biquad.BandPass(f0 * 2, 40), Gain: 0.18), (Filter: Biquad.BandPass(f0 * 3, 40), Gain: 0.08) };
        var breath = Biquad.LowPass(3000, 0.7);
        var phase = 0.0;
        Render(buffer, (i, t) =>
        {
            // The vibrato is integrated into the phase; a whole number of its cycles fits the loop, so the phase closes too.
            phase += f0 * Cents(10 * Math.Sin(2 * Math.PI * vibrato * t)) / Rate;
            var core = Math.Sin(2 * Math.PI * phase) + 0.12 * Math.Sin(4 * Math.PI * phase) + 0.05 * Math.Sin(6 * Math.PI * phase);
            var white = noise.White();
            var air = 0.0;
            foreach (var (filter, gain) in resonances)
                air += gain * filter.Process(white);

            return (0.6 * core + 3.0 * air + 0.05 * breath.Process(white), 0);
        });

        return Close(buffer, seconds);
    }

    /// <summary>A buffer for a loop of <paramref name="seconds"/> with room for the pre-roll and the seam's overhang.</summary>
    private static SampleBuffer Loop(int channels, double seconds) => new(channels, Frames(PreRoll + seconds + Seam));

    /// <summary>Renders frame by frame; the time handed to <paramref name="frame"/> starts at minus the pre-roll, so the loop proper begins at 0.</summary>
    private static void Render(SampleBuffer buffer, Func<int, double, (double Left, double Right)> frame)
    {
        for (var i = 0; i < buffer.Frames; i++)
        {
            var (left, right) = frame(i, (double)i / Rate - PreRoll);
            buffer.Left[i] = left;
            if (buffer.Channels == 2)
                buffer.Right[i] = right;
        }
    }

    /// <summary>Drops the pre-roll and closes the loop with the seam crossfade.</summary>
    private static SampleBuffer Close(SampleBuffer buffer, double seconds)
    {
        var skip = Frames(PreRoll);
        var trimmed = new SampleBuffer(buffer.Channels, buffer.Frames - skip);
        for (var c = 0; c < buffer.Channels; c++)
            Array.Copy(buffer.Data[c], skip, trimmed.Data[c], 0, trimmed.Frames);

        return trimmed.CloseSeam(Frames(seconds));
    }
}
