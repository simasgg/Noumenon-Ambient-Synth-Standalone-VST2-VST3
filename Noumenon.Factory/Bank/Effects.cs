using Noumenon.Dsp.Oscillators;
using Noumenon.Engine.Parameters;
using static Noumenon.Engine.Parameters.SectionId;

namespace Noumenon.Factory.Bank;

/// <summary>
/// The FX family, for Noumenon FX: Samp↔In turned to the input, so whatever a track feeds in runs
/// through the chain — the resonator, the drift delay, the distortion, the reverb — or, through the
/// envelope follower, shapes a drone. In the instrument, which has no audio input, these are
/// silent or nearly so.
/// </summary>
internal static partial class FactoryBank
{
    private static Recipe[] Effects =>
    [
        new("FX", "Input Cathedral", 601,
            "For Noumenon FX: whatever you feed in rings through the resonator's deep major chord and opens into a cathedral. On a busy source, start with the reverb mix lower.",
            p => Input(p)
                .Resonator("Deep Major", 0.88f, 0.4f).Reverb("Cathedral", 0.5f, size: 0.85f)),

        new("FX", "Input Resonator", 602,
            "For Noumenon FX: six tuned combs on a minor-ninth chord ring with the input, so drums and plucks turn into chords. P1 transposes the chord.",
            p => Input(p)
                .Resonator("Minor 9", 0.94f, 0.6f).Choose(ParamId.ResochordModSource, "P1").Pitch(1, 0f, midi: true).Reverb("Plate", 0.25f)),

        new("FX", "Input Drift", 603,
            "For Noumenon FX: the drift delay with long feedback and both its times on slow lanes — echoes that bend in pitch as they repeat.",
            p => Input(p)
                .Drift(380f, 470f, 0.7f, mix: 0.6f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Hall", 0.3f)
                .Lane(ParamId.SpinTime1, 29f, 0.04f).Lane(ParamId.SpinTime2, 37f, 0.04f)),

        new("FX", "Input Rust", 604,
            "For Noumenon FX: fold-back distortion behind a resonant low-pass that wanders — grit for anything that sounds too clean.",
            p => Input(p)
                .Distortion(0.55f, 0.45f, "Fold").MasterFilter(1500f, 0.5f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Room", 0.25f)
                .Lane(ParamId.MasterFilterCutoff, 23f, 0.25f)),

        new("FX", "Input Breath", 605,
            "For Noumenon FX: a soft drone that breathes with your audio — the envelope follower listens to the input and opens the drone through the AM route; a little of the input passes too.",
            p => Input(p)
                .Set(ParamId.SamplerAmp, 0.3f).Choose(ParamId.EnvFollowerSource, "Input").Set(ParamId.SamplerAmDepth, 0.9f)
                .Set(ParamId.EnvFollowerAttack, 20f).Set(ParamId.EnvFollowerRelease, 600f).Set(ParamId.EnvFollowerGain, 6f)
                .Level(A, 0.7f).Level(B, 0.7f)
                .A(1, Waveform.Saw, -12, 0f, 0.3f).A(2, Waveform.Saw, -12, 7f, 0.3f).A(3, Waveform.Sine, -24, 0f, 0.3f)
                .B(1, Waveform.Triangle, -5, -4f, 0.25f)
                .Filter(A, 1800f).Filter(B, 2000f).Combine(0.05f)
                .Resonator("Voices", 0.7f, 0f, on: false).Reverb("Hall", 0.35f)),

        new("FX", "Input Halo", 606,
            "For Noumenon FX: a shimmering wash — the resonator an octave up on open fifths, a cloud reverb and a touch of drift chorus.",
            p => Input(p)
                .Resonator("Fifths", 0.9f, 0.45f, pitch: 12).Drift(22f, 31f, 0.2f, mix: 0.5f).Reverb("Cloud", 0.55f, size: 0.95f)),
    ];

    /// <summary>The common start of an FX preset: the sampler hears the input instead of a sample and both sections are silent.</summary>
    private static PresetBuilder Input(PresetBuilder p) => p
        .Set(ParamId.SamplerInputMix, 1f).Set(ParamId.SamplerAmp, 0.8f).Set(ParamId.SamplerSelect, 127f)
        .Silence(A).Silence(B).Level(A, 0f).Level(B, 0f)
        .Drift(120f, 180f, 0.35f, mix: 0f);
}
