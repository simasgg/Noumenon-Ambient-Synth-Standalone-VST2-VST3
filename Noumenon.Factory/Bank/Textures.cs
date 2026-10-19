using Noumenon.Dsp.Oscillators;
using Noumenon.Engine.Parameters;
using static Noumenon.Engine.Parameters.SectionId;

namespace Noumenon.Factory.Bank;

/// <summary>The Texture family: the factory samples in the lead — granulated, stretched, ringing through the resonator — with the oscillators as a quiet floor.</summary>
internal static partial class FactoryBank
{
    private static Recipe[] Textures =>
    [
        new("Texture", "Rain Window", 301,
            "Rain on glass: the Rain sample in loose grains, ringing a tuned resonator chord so the drops land on pitch, with a faint drone underneath.",
            p => p
                .Sample(1, "Rain.wav").Sampler(1, "Granular", amp: 0.7f).Grains(90f, 24f, 0.5f, 0.6f, window: 0.6f, width: 1f)
                .A(1, Waveform.Sine, -24, 0f, 0.25f).A(2, Waveform.Sine, -17, 3f, 0.15f).Off(A, 3, 4, 5, 6).Silence(B)
                .Filter(A, 2000f).Combine(0f)
                .Resonator("Add 9", 0.9f, 0.45f).Reverb("Hall", 0.35f)
                .Lane(ParamId.GrainDensity, 29f, 0.15f).Lane(ParamId.ResochordMix, 37f, 0.15f)),

        new("Texture", "Night Garden", 302,
            "Night Insects in a slow loop, their chirps catching a pentatonic resonator and the drift echoes, over a low drone that stays out of the way.",
            p => p
                .Sample(1, "Night Insects.wav").Sampler(1, amp: 0.5f).Loop(0f, 1f, 150f)
                .A(1, Waveform.Sine, -24, 0f, 0.3f).A(2, Waveform.Sine, -12, 4f, 0.15f).Off(A, 3, 4, 5, 6)
                .B(1, Waveform.Sine, -17, -3f, 0.2f).Off(B, 2, 3, 4, 5, 6)
                .Filter(A, 1500f).Filter(B, 1500f).Combine(0.05f)
                .Drift(310f, 470f, 0.4f).Resonator("Pentatonic", 0.85f, 0.3f).Reverb("Hall", 0.35f)
                .Lane(ParamId.SamplerAmp, 31f, 0.15f)),

        new("Texture", "Shoreline", 303,
            "The Surf sample slowed by Stretch into one long wave, over a low drone; the playhead's speed wanders, so the sea never quite repeats.",
            p => p
                .Sample(1, "Surf.wav").Sampler(1, "Stretch", amp: 0.7f).Stretch(6f)
                .A(1, Waveform.Sine, -24, 0f, 0.35f).A(2, Waveform.Sine, -19, 3f, 0.25f).Off(A, 3, 4, 5, 6)
                .B(1, Waveform.Sine, -12, -4f, 0.2f).Off(B, 2, 3, 4, 5, 6)
                .Filter(A, 1500f).Filter(B, 1500f).Combine(0f)
                .MasterFilter(8000f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Hall", 0.3f)
                .Lane(ParamId.SamplerDir, 49f, 0.1f).Lane(ParamId.StretchAmount, 33f, 0.1f)),

        new("Texture", "Ember Hours", 304,
            "Fire crackle from the Embers sample, slowed a little and folded by the distortion, in a dark chamber; the drone underneath glows and fades.",
            p => p
                .Sample(1, "Embers.wav", gain: 2.5f).Sampler(1, amp: 0.8f, dir: 0.75f, pitch: -5, master: 1f)
                .A(1, Waveform.Saw, -24, 0f, 0.45f).A(2, Waveform.Triangle, -12, 4f, 0.4f).Off(A, 3, 4, 5, 6)
                .B(1, Waveform.Sine, -17, 0f, 0.4f).Off(B, 2, 3, 4, 5, 6)
                .Filter(A, 600f, 0.3f).Filter(B, 1200f).Combine(0.05f)
                .Distortion(0.25f, 0.35f, "Fold").Resonator("Voices", 0.7f, 0f, on: false).Reverb("Chamber", 0.35f)
                .Lane(ParamId.SectionACutoff, 25f, 0.12f).Lane(ParamId.SamplerAmp, 17f, 0.15f)),

        new("Texture", "Bell Field", 305,
            "The Bell Strikes sample spread over a field of long grains, each a little off pitch — a carillon heard from very far away.",
            p => p
                .Sample(1, "Bell Strikes.wav").Sampler(1, "Granular", amp: 0.6f).Grains(350f, 6f, 0.2f, 0.4f, pitchSpread: 0.5f, window: 0.2f, width: 1f)
                .A(1, Waveform.Sine, -24, 0f, 0.2f).Off(A, 2, 3, 4, 5, 6).Silence(B)
                .Filter(A, 1500f).Combine(0f)
                .Resonator("Voices", 0.7f, 0f, on: false).Reverb("Cloud", 0.55f, size: 0.9f)
                .Lane(ParamId.GrainPosition, 41f, 0.25f)),

        new("Texture", "Slow Carousel", 306,
            "The Music Box sample stretched until its tune smears into slow chords, with long drift echoes and a warm room.",
            p => p
                .Sample(1, "Music Box.wav").Sampler(1, "Stretch", amp: 0.7f).Stretch(6f, "Medium")
                .A(1, Waveform.Sine, -15, 0f, 0.2f).Off(A, 2, 3, 4, 5, 6).Silence(B)
                .Filter(A, 1500f).Combine(0f)
                .MasterFilter(6000f).Drift(260f, 390f, 0.45f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Chamber", 0.3f)),

        new("Texture", "Shortwave Ghosts", 307,
            "Shortwave static ringing through the resonator's open fifths while the drift times bend it — voices that are not there.",
            p => p
                .Sample(1, "Shortwave.wav").Sampler(1, amp: 0.55f)
                .A(1, Waveform.Sine, -24, 0f, 0.2f).Off(A, 2, 3, 4, 5, 6).Silence(B)
                .Filter(A, 1500f).Combine(0f)
                .Drift(140f, 210f, 0.5f).Resonator("Fifths", 0.92f, 0.5f, pitch: -12).Reverb("Hall", 0.35f)
                .Lane(ParamId.SpinTime1, 37f, 0.05f).Lane(ParamId.SpinTime2, 43f, 0.05f)),

        new("Texture", "Glass Curtain", 308,
            "The Glass Taps sample in dense, short grains scattered up to a fifth either way, into a bright plate — a glittering curtain.",
            p => p
                .Sample(1, "Glass Taps.wav").Sampler(1, "Granular", amp: 0.55f).Grains(60f, 40f, 0.5f, 0.8f, pitchSpread: 7f, window: 0.1f, width: 1f)
                .A(1, Waveform.Sine, 0, 0f, 0.12f).Off(A, 2, 3, 4, 5, 6).Silence(B)
                .Filter(A, 4000f).Combine(0f).Eq(0f, 1f)
                .Resonator("Voices", 0.7f, 0f, on: false).Reverb("Plate", 0.5f, size: 0.85f)
                .Lane(ParamId.GrainSpray, 27f, 0.15f)),
    ];
}
