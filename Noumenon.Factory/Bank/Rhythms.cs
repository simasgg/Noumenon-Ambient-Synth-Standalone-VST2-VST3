using Noumenon.Dsp.Oscillators;
using Noumenon.Engine.Parameters;
using static Noumenon.Engine.Parameters.SectionId;

namespace Noumenon.Factory.Bank;

/// <summary>The Rhythmic family: pulse from the 120 BPM loops, the AM route, square LFOs and the chord sequencer; with Host Sync the lanes and steps follow the project's tempo.</summary>
internal static partial class FactoryBank
{
    private static Recipe[] Rhythms =>
    [
        new("Rhythmic", "Pulse Engine", 401,
            "The Pulse Loop 120 sample keeps time under a drone whose filters move in whole bars; with Host Sync on, the lanes and the restart follow your project.",
            p => p
                .Sample(1, "Pulse Loop 120.wav").Sampler(1, amp: 0.6f).Set(ParamId.HostSync, true).Set(ParamId.RestartOnHostPlay, true)
                .A(1, Waveform.Saw, -24, 0f, 0.3f).A(2, Waveform.Pulse, -12, 5f, 0.25f, shape: 0.3f).Off(A, 3, 4, 5, 6)
                .B(1, Waveform.Sine, -17, 0f, 0.25f).B(2, Waveform.Triangle, -5, 0f, 0.15f).Off(B, 3, 4, 5, 6)
                .Filter(A, 900f, 0.3f).Filter(B, 1500f).Combine(0.1f)
                .Drift(250f, 375f, 0.35f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Room", 0.25f)
                .Lane(ParamId.SectionACutoff, 8f, 0.15f).Lane(ParamId.SectionBCutoff, 16f, 0.15f)),

        new("Rhythmic", "Clockwork Garden", 402,
            "Ticks and tocks from the Clockwork 120 sample strike a pentatonic resonator, so the clock plays a tune; ring modulation adds a little metal underneath.",
            p => p
                .Sample(1, "Clockwork 120.wav", gain: 3.5f).Sampler(1, amp: 0.8f, master: 1f).Set(ParamId.HostSync, true)
                .A(1, Waveform.Sine, -24, 0f, 0.2f).Off(A, 2, 3, 4, 5, 6)
                .B(1, Waveform.Sine, -12, 7f, 0.2f).Off(B, 2, 3, 4, 5, 6)
                .Filter(A, 2000f).Filter(B, 2000f).Combine(0.6f)
                .Resonator("Pentatonic", 0.93f, 0.6f).Reverb("Chamber", 0.3f)),

        new("Rhythmic", "Heartwood", 403,
            "A warm drone that beats like a heart: the Heartbeat 60 sample is barely heard — through the AM route it opens the drone on every beat.",
            p => p
                .Sample(1, "Heartbeat 60.wav", gain: 2f).Sampler(1, amp: 0.15f).Set(ParamId.SamplerAmDepth, 0.85f).Set(ParamId.EnvFollowerRelease, 250f).Set(ParamId.EnvFollowerGain, 9f)
                .A(1, Waveform.Saw, -24, 0f, 0.5f).A(2, Waveform.Triangle, -12, 4f, 0.45f).A(3, Waveform.Sine, -5, -3f, 0.3f).Off(A, 4, 5, 6)
                .B(1, Waveform.Saw, -17, 6f, 0.4f).B(2, Waveform.Sine, -12, 0f, 0.45f).Off(B, 3, 4, 5, 6)
                .Filter(A, 1400f, 0.2f).Filter(B, 1800f).Combine(0.1f)
                .Resonator("Voices", 0.7f, 0f, on: false).Reverb("Room", 0.3f)),

        new("Rhythmic", "Stepping Stones", 404,
            "Chords that change every two seconds — I, IV, VI, V in A minor — struck on the resonator by a noise pulse that a square LFO chops; Host Sync turns the steps into beats.",
            p => p
                .Scale("Minor", "A").Sequencer(2f, "Seventh", "Close", 3, "None", "I", "IV", "VI", "V").Set(ParamId.HostSync, true)
                .A(1, Waveform.NoiseLp, 0, 0f, 0.6f, noiseCutoff: 4000f).A(2, Waveform.Pulse, -12, 0f, 0.4f, shape: 0.2f).Off(A, 3, 4, 5, 6)
                .B(1, Waveform.Sine, -15, 0f, 0.5f).Off(B, 2, 3, 4, 5, 6)
                .Filter(A, 5000f).Filter(B, 1200f).Combine(0f)
                .Lfo(1, 4f, "Square").Mod("LFO 1", ParamId.SectionALevel, 0.5f)
                .Resonator("Voices", 0.9f, 0.75f).Reverb("Hall", 0.3f)),

        new("Rhythmic", "Tremor", 405,
            "A drone chopped by square LFOs at slightly different rates on each section, so the pulse drifts against itself — rhythm out of detuning.",
            p => p
                .A(1, Waveform.Saw, -12, 0f, 0.35f).A(2, Waveform.Saw, -12, 7f, 0.3f).A(3, Waveform.Sine, -24, 0f, 0.3f).Off(A, 4, 5, 6)
                .B(1, Waveform.Saw, -5, -6f, 0.3f).B(2, Waveform.Sine, 0, 0f, 0.2f).Off(B, 3, 4, 5, 6)
                .Filter(A, 2500f, 0.2f).Filter(B, 2000f).Combine(0.05f)
                .Lfo(1, 3f, "Square").Lfo(2, 2.9f, "Square")
                .Mod("LFO 1", ParamId.SectionALevel, -0.5f).Mod("LFO 2", ParamId.SectionBLevel, -0.5f)
                .Drift(166f, 333f, 0.3f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Room", 0.25f)),

        new("Rhythmic", "Kalimba Loop", 406,
            "The first half of the Kalimba Run sample in a loop that every MIDI gate restarts; P1 transposes it and the drift echo answers each phrase.",
            p => p
                .Sample(1, "Kalimba Run.wav").Sampler(1, amp: 0.7f, mod: "P1").Loop(0f, 0.5f, 30f).Set(ParamId.SamplerRetrigger, true).Pitch(1, 0f, midi: true)
                .A(1, Waveform.Sine, -24, 0f, 0.15f).Off(A, 2, 3, 4, 5, 6).Silence(B)
                .Filter(A, 1500f).Combine(0f)
                .Drift(333f, 500f, 0.45f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Hall", 0.3f)),
    ];
}
