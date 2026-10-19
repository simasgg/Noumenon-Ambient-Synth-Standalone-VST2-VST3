using Noumenon.Dsp.Oscillators;
using Noumenon.Engine;
using Noumenon.Engine.Parameters;
using Noumenon.Engine.Presets;
using Noumenon.Engine.Samples;

namespace Noumenon.Tests;

public class FactoryBankTests
{
    private static readonly string[] Families = ["", "Broken", "Drone", "FX", "Pad", "Rhythmic", "Texture"];

    private static string Content => FactoryContent.Root ?? throw new InvalidOperationException("The repository's content folder was not found.");

    public static TheoryData<string> Presets()
    {
        var data = new TheoryData<string>();
        foreach (var preset in PresetIo.Scan(Path.Combine(Content, "presets")))
            data.Add(Path.GetRelativePath(Content, preset.Path));

        return data;
    }

    [Fact]
    public void The_Bank_Has_Its_Families_And_Unique_Names()
    {
        var bank = PresetIo.Scan(Path.Combine(Content, "presets"));

        Assert.InRange(bank.Count, 40, 60);
        Assert.All(bank, preset => Assert.Contains(preset.Family, Families));
        Assert.Equal(bank.Count, bank.Select(preset => preset.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(Families.Length, bank.Select(preset => preset.Family).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Presets))]
    public void A_Factory_Preset_Loads_With_Its_Samples_And_Plays(string relativePath)
    {
        var state = PresetIo.Load(Path.Combine(Content, relativePath));
        var engine = new NoumenonEngine();
        var warnings = new List<string>();

        engine.ApplyPreset(state, loadSamples: true, warnings);
        engine.Prepare(48000, 256);
        engine.Reset(state.Seed);
        var left = new float[256];
        var right = new float[256];
        var finite = true;
        for (var block = 0; block < 48000 / 256; block++)
        {
            engine.Process(left, right, 0, left.Length);
            for (var i = 0; i < left.Length; i++)
                finite &= float.IsFinite(left[i]) && float.IsFinite(right[i]);
        }

        Assert.Empty(warnings);
        Assert.True(finite);
        Assert.False(string.IsNullOrWhiteSpace(state.Description));
        Assert.Equal(ParameterTable.Count, state.Parameters.Count);
        Assert.All(state.Samples, sample => Assert.StartsWith(FactoryContent.Scheme, sample.Path));
        Assert.All(engine.Samples.Loaded, slot => Assert.True(FactoryContent.IsReference(slot.Path!)));
    }

    [Fact]
    public void The_Factory_Samples_Decode_And_The_Wavetables_Have_Thirty_Two_Frames()
    {
        var samples = Directory.GetFiles(Path.Combine(Content, "samples"), "*.wav");
        var wavetables = Directory.GetFiles(Path.Combine(Content, "samples", "wavetables"), "*.wav");

        Assert.InRange(samples.Length + wavetables.Length, 20, 30);
        Assert.All(samples, path => Assert.Equal(48000, SampleDecoder.Decode(path).SampleRate));
        Assert.All(wavetables, path => Assert.Equal(Wavetable.MaxFrames, SampleDecoder.Decode(path).BuildWavetable().FrameCount));
    }

    [Fact]
    public void A_Factory_Sample_Loaded_By_Its_Path_Is_Kept_As_A_Portable_Reference()
    {
        var map = new SampleMap();
        var file = Path.Combine(Content, "samples", "Glass Taps.wav");

        var byPath = map.Load(1, file);
        var byReference = map.Load(2, FactoryContent.Sample("Glass Taps.wav"));

        Assert.Equal("factory:samples/Glass Taps.wav", byPath.Path);
        Assert.Equal(byPath.Path, byReference.Path);
        Assert.Equal(byPath.Data.Length, byReference.Data.Length);
    }
}
