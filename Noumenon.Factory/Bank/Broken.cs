using Noumenon.Dsp.Oscillators;
using Noumenon.Engine.Parameters;
using static Noumenon.Engine.Parameters.SectionId;

namespace Noumenon.Factory.Bank;

/// <summary>The Broken family: distortion, bit reduction, ring modulation, stepped pitch lanes and feedback near its edge — the instrument misbehaving on purpose.</summary>
internal static partial class FactoryBank
{
    private static Recipe[] Broken =>
    [
        new("Broken", "Rust Signal", 501,
            "Sample-and-hold noise and thin pulses through fold-back distortion whose drive and mix both ride lanes, behind a resonant band-pass — a corroded transmission.",
            p => p
                .A(1, Waveform.NoiseSh, 0, 0f, 0.3f, noiseCutoff: 800f).A(2, Waveform.Pulse, -12, 0f, 0.25f, shape: 0.15f).A(3, Waveform.Square, -24, 9f, 0.2f).Off(A, 4, 5, 6)
                .B(1, Waveform.Saw, -17, 0f, 0.25f).B(2, Waveform.NoiseSh, 0, 0f, 0.15f, noiseCutoff: 3000f).Off(B, 3, 4, 5, 6)
                .Filter(A, 1200f, 0.5f, "BP").Filter(B, 2500f).Combine(0.2f)
                .MasterFilter(3000f, 0.4f).Distortion(0.6f, 0.5f, "Fold").Resonator("Voices", 0.7f, 0f, on: false).Reverb("Room", 0.3f)
                .Lane(ParamId.DistortionDrive, 21f, 0.3f).Lane(ParamId.DistortionMix, 29f, 0.2f).Lane(ParamId.MasterFilterCutoff, 17f, 0.2f)),

        new("Broken", "Data Rot", 502,
            "The Data Burst sample in short, scattered grains up to an octave off, bit-reduced and smeared through a resonator tuned to a cluster — digital debris.",
            p => p
                .Sample(1, "Data Burst.wav").Sampler(1, "Granular", amp: 0.6f).Grains(40f, 20f, 0.5f, 0.9f, pitchSpread: 12f, window: 0f, width: 1f)
                .A(1, Waveform.Sine, -24, 0f, 0.15f).Off(A, 2, 3, 4, 5, 6).Silence(B)
                .Filter(A, 1500f).Combine(0f)
                .Distortion(0.5f, 0.5f, "Bit").Resonator("Cluster", 0.85f, 0.35f).Reverb("Plate", 0.3f)
                .Lane(ParamId.GrainDensity, 19f, 0.2f)),

        new("Broken", "Fractured Choir", 503,
            "The Choir Breath sample in tiny grains sprayed a fifth either way, and through the AM route the same grains chop the sine drone — a choir on a broken radio.",
            p => p
                .Sample(1, "Choir Breath.wav").Sampler(1, "Granular", amp: 0.6f).Grains(30f, 35f, 0.4f, 0.8f, pitchSpread: 7f, window: 0f, width: 1f)
                .Set(ParamId.SamplerAmDepth, 0.5f).Set(ParamId.EnvFollowerAttack, 2f).Set(ParamId.EnvFollowerRelease, 40f)
                .A(1, Waveform.Sine, -3, 0f, 0.4f).A(2, Waveform.Sine, 4, 13f, 0.25f).Off(A, 3, 4, 5, 6)
                .B(1, Waveform.Triangle, -15, 0f, 0.3f).Off(B, 2, 3, 4, 5, 6)
                .Filter(A, 4000f).Filter(B, 2000f).Combine(0.3f)
                .Distortion(0.3f, 0.6f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Hall", 0.3f)),

        new("Broken", "Static Bloom", 504,
            "Vinyl crackle feeding a resonator kept at the edge of feedback; a lane on the feedback fader lets the chord bloom and collapse.",
            p => p
                .Sample(1, "Vinyl Crackle.wav", gain: 3f).Sampler(1, amp: 1f, master: 1f)
                .A(1, Waveform.Sine, -24, 0f, 0.15f).Off(A, 2, 3, 4, 5, 6).Silence(B)
                .Filter(A, 1500f).Combine(0f)
                .Resonator("Wide", 0.9f, 0.55f).Set(ParamId.ResochordFeedbackFader, 0.04f).Reverb("Hall", 0.35f)
                .Lane(ParamId.ResochordFeedbackFader, 33f, 0.04f)),

        new("Broken", "Bent Wires", 505,
            "Squares and pulses ring-modulated, with stepped semitone lanes jumping between intervals — a melody from a machine that has lost its tuning — and long drift feedback.",
            p => p
                .A(1, Waveform.Square, -12, 0f, 0.45f).A(2, Waveform.Pulse, -5, 0f, 0.4f, shape: 0.2f).Off(A, 3, 4, 5, 6)
                .B(1, Waveform.Square, -10, 30f, 0.45f).B(2, Waveform.Pulse, 1, 0f, 0.35f, shape: 0.7f).Off(B, 3, 4, 5, 6)
                .Filter(A, 3000f, 0.3f).Filter(B, 2500f, 0.3f).Combine(0.7f, ring: 2f)
                .Drift(210f, 330f, 0.75f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Room", 0.25f)
                .Steps(ParamId.OscA2Semitone, 12f, -5, 2, -5, 7, 0, -7, 3, -5)
                .Steps(ParamId.OscB1Semitone, 15f, -10, -3, -15, -10, 2, -8)),

        new("Broken", "Warped Tape", 506,
            "The Tape Strings sample on a worn-out machine: the direction lane wobbles its speed, the drift times slide against each other, the top end is gone.",
            p => p
                .Sample(1, "Tape Strings.wav").Sampler(1, amp: 0.6f)
                .A(1, Waveform.Sine, -15, 0f, 0.1f).Off(A, 2, 3, 4, 5, 6).Silence(B)
                .Filter(A, 1500f).Combine(0f).Eq(2f, -4f)
                .MasterFilter(5000f).Distortion(0.2f, 0.35f).Drift(40f, 55f, 0.3f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Room", 0.25f)
                .Lane(ParamId.SamplerDir, 7f, 0.03f).Lane(ParamId.SpinTime1, 11f, 0.04f).Lane(ParamId.SpinTime2, 13f, 0.04f)),
    ];
}
