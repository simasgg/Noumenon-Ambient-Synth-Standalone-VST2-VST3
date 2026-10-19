using Noumenon.Dsp.Shared;

namespace Noumenon.Factory.Samples;

/// <summary>
/// The small offline toolkit the factory samples are made with: tuning helpers, equal-power
/// panning, and the trick that makes tonal loops seamless — every oscillator and LFO frequency is
/// rounded to a whole number of cycles in the loop (<see cref="Periodic"/>), so the deterministic
/// part of a loop is exactly periodic and only its noise needs the seam crossfade. Everything runs
/// in double precision at 48 kHz; allocation and speed do not matter here, repeatability does.
/// </summary>
internal static class Synth
{
    public const int Rate = 48000;

    public static int Frames(double seconds) => (int)Math.Round(seconds * Rate);

    public static double NoteHz(double midiNote) => 440.0 * Math.Pow(2.0, (midiNote - 69.0) / 12.0);

    public static double Cents(double cents) => Math.Pow(2.0, cents / 1200.0);

    /// <summary>The frequency nearest <paramref name="hz"/> with a whole number of cycles (at least one) in <paramref name="seconds"/>.</summary>
    public static double Periodic(double hz, double seconds) => Math.Max(1.0, Math.Round(hz * seconds)) / seconds;

    /// <summary>Equal-power gains for a pan position from −1 (left) to +1 (right).</summary>
    public static (double Left, double Right) Pan(double pan)
    {
        var angle = (Math.Clamp(pan, -1.0, 1.0) + 1.0) * Math.PI / 4.0;

        return (Math.Cos(angle), Math.Sin(angle));
    }

    public static double Db(double db) => Math.Pow(10.0, db / 20.0);

    /// <summary>A raised-cosine window value at <paramref name="t"/> in 0–1.</summary>
    public static double Hann(double t) => t <= 0.0 || t >= 1.0 ? 0.0 : 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * t);

    /// <summary>A sum of slow sines at whole multiples of the loop rate: a smooth, seamless random curve in −1…1.</summary>
    public static Func<double, double> SmoothLoop(Xoshiro128 rng, double seconds, int partials = 3)
    {
        var phases = new double[partials];
        var amplitudes = new double[partials];
        var total = 0.0;
        for (var k = 0; k < partials; k++)
        {
            phases[k] = rng.NextFloat() * 2.0 * Math.PI;
            amplitudes[k] = (0.5 + 0.5 * rng.NextFloat()) / (k + 1);
            total += amplitudes[k];
        }

        return t =>
        {
            var sum = 0.0;
            for (var k = 0; k < partials; k++)
                sum += amplitudes[k] * Math.Sin(2.0 * Math.PI * (k + 1) * t / seconds + phases[k]);

            return sum / total;
        };
    }
}

/// <summary>White, pink (Paul Kellet's refined filter) and brown (leaky integrated) noise from one seeded generator.</summary>
internal sealed class Noise
{
    private double b0, b1, b2, b3, b4, b5, b6;
    private double brown;

    public Noise(ulong seed) => Random = new Xoshiro128(seed);

    public Xoshiro128 Random { get; }

    public double White() => Random.NextBipolar();

    public double Pink()
    {
        var white = White();
        b0 = 0.99886 * b0 + white * 0.0555179;
        b1 = 0.99332 * b1 + white * 0.0750759;
        b2 = 0.96900 * b2 + white * 0.1538520;
        b3 = 0.86650 * b3 + white * 0.3104856;
        b4 = 0.55000 * b4 + white * 0.5329522;
        b5 = -0.7616 * b5 - white * 0.0168980;
        var pink = b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362;
        b6 = white * 0.115926;

        return pink * 0.11;
    }

    public double Brown()
    {
        brown = (brown + 0.02 * White()) / 1.02;

        return brown * 3.5;
    }
}

/// <summary>An RBJ-cookbook biquad in transposed direct form II; the band-pass has 0 dB at its centre.</summary>
internal sealed class Biquad
{
    private double b0, b1, b2, a1, a2, z1, z2;

    public static Biquad LowPass(double hz, double q) => new Biquad().SetLowPass(hz, q);

    public static Biquad HighPass(double hz, double q) => new Biquad().SetHighPass(hz, q);

    public static Biquad BandPass(double hz, double q) => new Biquad().SetBandPass(hz, q);

    public static Biquad Peak(double hz, double q, double db)
    {
        var (w, alpha) = Angles(hz, q);
        var a = Math.Pow(10.0, db / 40.0);

        return new Biquad().Set(1 + alpha * a, -2 * Math.Cos(w), 1 - alpha * a, 1 + alpha / a, -2 * Math.Cos(w), 1 - alpha / a);
    }

    public Biquad SetLowPass(double hz, double q)
    {
        var (w, alpha) = Angles(hz, q);
        var c = Math.Cos(w);

        return Set((1 - c) / 2, 1 - c, (1 - c) / 2, 1 + alpha, -2 * c, 1 - alpha);
    }

    public Biquad SetHighPass(double hz, double q)
    {
        var (w, alpha) = Angles(hz, q);
        var c = Math.Cos(w);

        return Set((1 + c) / 2, -(1 + c), (1 + c) / 2, 1 + alpha, -2 * c, 1 - alpha);
    }

    public Biquad SetBandPass(double hz, double q)
    {
        var (w, alpha) = Angles(hz, q);

        return Set(alpha, 0, -alpha, 1 + alpha, -2 * Math.Cos(w), 1 - alpha);
    }

    public double Process(double x)
    {
        var y = b0 * x + z1;
        z1 = b1 * x - a1 * y + z2;
        z2 = b2 * x - a2 * y;

        return y;
    }

    private static (double W, double Alpha) Angles(double hz, double q)
    {
        var w = 2.0 * Math.PI * Math.Clamp(hz, 1.0, 0.49 * Synth.Rate) / Synth.Rate;

        return (w, Math.Sin(w) / (2.0 * q));
    }

    private Biquad Set(double nb0, double nb1, double nb2, double na0, double na1, double na2)
    {
        b0 = nb0 / na0;
        b1 = nb1 / na0;
        b2 = nb2 / na0;
        a1 = na1 / na0;
        a2 = na2 / na0;

        return this;
    }
}

/// <summary>A trapezoidal state-variable filter (Simper) whose cutoff and Q may change every sample.</summary>
internal sealed class Svf
{
    private double ic1, ic2;

    public (double Low, double Band, double High) Process(double x, double hz, double q)
    {
        var g = Math.Tan(Math.PI * Math.Clamp(hz, 5.0, 0.45 * Synth.Rate) / Synth.Rate);
        var k = 1.0 / q;
        var a1 = 1.0 / (1.0 + g * (g + k));
        var a2 = g * a1;
        var a3 = g * a2;
        var v3 = x - ic2;
        var v1 = a1 * ic1 + a2 * v3;
        var v2 = ic2 + a2 * ic1 + a3 * v3;
        ic1 = 2.0 * v1 - ic1;
        ic2 = 2.0 * v2 - ic2;

        return (v2, v1, x - k * v1 - v2);
    }
}

/// <summary>A one-pole low-pass (smoothing, gentle tone control).</summary>
internal sealed class OnePole(double hz)
{
    private readonly double a = Math.Exp(-2.0 * Math.PI * hz / Synth.Rate);
    private double y;

    public double Process(double x) => y = x + a * (y - x);
}

/// <summary>A sawtooth with PolyBLEP corrections at the wrap: alias products sit low enough for the pads and strings it feeds, which are filtered afterwards anyway.</summary>
internal sealed class BlepSaw(double phase = 0.0)
{
    private double phase = phase;

    public double Next(double hz)
    {
        var dt = hz / Synth.Rate;
        phase += dt;
        if (phase >= 1.0)
            phase -= 1.0;

        return 2.0 * phase - 1.0 - PolyBlep(phase, dt);
    }

    private static double PolyBlep(double t, double dt)
    {
        if (t < dt)
        {
            t /= dt;

            return t + t - t * t - 1.0;
        }

        if (t > 1.0 - dt)
        {
            t = (t - 1.0) / dt;

            return t * t + t + t + 1.0;
        }

        return 0.0;
    }
}

/// <summary>
/// A plucked string (Karplus-Strong): a delay line of one period fed back through a two-point
/// average and a loss factor set from the wanted decay time; a first-order all-pass makes up the
/// fraction of a sample the period does not divide into, so the string is in tune.
/// </summary>
internal sealed class PluckedString
{
    private readonly double[] line;
    private readonly int length;
    private readonly double allpass;
    private readonly double loss;
    private int index;
    private double previous;
    private double allpassState;

    public PluckedString(double hz, double decaySeconds, double brightness, Noise noise)
    {
        var period = Synth.Rate / hz - 0.5;
        length = Math.Max(2, (int)Math.Floor(period));
        var fraction = period - length;
        allpass = (1.0 - fraction) / (1.0 + fraction);
        loss = Math.Pow(0.001, 1.0 / (decaySeconds * hz));
        line = new double[length];
        var smooth = new OnePole(Math.Clamp(brightness, 0.05, 1.0) * 8000.0);
        for (var i = 0; i < length; i++)
            line[i] = smooth.Process(noise.White());
    }

    public double Next()
    {
        var current = line[index];
        var averaged = loss * 0.5 * (current + previous);
        previous = current;
        var tuned = allpass * averaged + allpassState;
        allpassState = averaged - allpass * tuned;
        line[index] = tuned;
        index = (index + 1) % length;

        return current;
    }
}
