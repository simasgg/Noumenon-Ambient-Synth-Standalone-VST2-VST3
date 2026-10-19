namespace Noumenon.Factory.Samples;

/// <summary>One factory sample: its file name, the subfolder of content/samples it goes in ("" for the top), the peak it is normalized to, the seed of its noise, whether it is made to loop, and how it is made.</summary>
internal sealed record SampleSpec(string Name, string Folder, double PeakDb, ulong Seed, bool Loops, Func<ulong, SampleBuffer> Render)
{
    public string RelativePath => Folder.Length == 0 ? Name + ".wav" : Folder + "/" + Name + ".wav";
}

/// <summary>
/// Every factory sample, in families: five wavetables for the oscillators' Wavetable wave,
/// sustained tones, plucked and struck events, field-like textures, 120 BPM loops and digital
/// debris — all synthesized here, so the bank holds nothing recorded or borrowed. Each sample is a
/// method with its own seed; the wavetables are 32 cycles of 2048 points built from whole
/// harmonics, and the loops are closed with <see cref="SampleBuffer.CloseSeam"/> or
/// <see cref="SampleBuffer.Fold"/> so they repeat without a seam.
/// </summary>
internal static partial class SampleCatalogue
{
    private const string WavetableFolder = "wavetables";

    public static IReadOnlyList<SampleSpec> All => [..Wavetables, ..Tones, ..Events, ..Textures, ..Rhythms];

    private static SampleSpec[] Wavetables =>
    [
        new("Harmonic Ladder", WavetableFolder, -1.0, 11, false, _ => HarmonicLadder()),
        new("Vowel Morph", WavetableFolder, -1.0, 12, false, _ => VowelMorph()),
        new("Pulse Width", WavetableFolder, -1.0, 13, false, _ => PulseWidth()),
        new("Odd Glass", WavetableFolder, -1.0, 14, false, _ => OddGlass()),
        new("Organ Stops", WavetableFolder, -1.0, 15, false, _ => OrganStops()),
    ];
}
