using Noumenon.Engine.Presets;

namespace Noumenon.Tests;

public class FactoryContentTests
{
    private static readonly string Content = Path.Combine(Path.GetTempPath(), "noumenon-content", "content");

    [Fact]
    public void A_File_Inside_The_Content_Folder_Becomes_A_Portable_Reference_And_Back()
    {
        var file = Path.Combine(Content, "samples", "wavetables", "Vowel Morph.wav");

        var reference = FactoryContent.ToReference(file, Content);

        Assert.Equal("factory:samples/wavetables/Vowel Morph.wav", reference);
        Assert.Equal(FactoryContent.Sample("wavetables/Vowel Morph.wav"), reference);
        Assert.Equal(file, FactoryContent.Resolve(reference, Content));
        Assert.Equal(reference, FactoryContent.ToReference(reference, Content));
    }

    [Fact]
    public void Paths_Outside_The_Content_Folder_Are_Left_As_They_Are()
    {
        var outside = Path.Combine(Path.GetTempPath(), "elsewhere", "field.wav");
        var sibling = Content + "-old" + Path.DirectorySeparatorChar + "field.wav";

        Assert.Equal(outside, FactoryContent.ToReference(outside, Content));
        Assert.Equal(sibling, FactoryContent.ToReference(sibling, Content));
        Assert.Equal("field.wav", FactoryContent.ToReference("field.wav", Content));
        Assert.Equal(outside, FactoryContent.Resolve(outside, Content));
    }

    [Fact]
    public void The_Scheme_Is_Recognised_In_Any_Case_And_Plain_Paths_Are_Not_References()
    {
        Assert.False(FactoryContent.IsReference(Path.Combine(Content, "samples", "Wind.wav")));
        Assert.True(FactoryContent.IsReference("Factory:samples/Wind.wav"));
        Assert.Equal(Path.Combine(Content, "samples", "Wind.wav"), FactoryContent.Resolve("Factory:samples/Wind.wav", Content));
    }

    [Fact]
    public void The_Repository_Content_Is_Found_From_The_Test_Output()
    {
        var located = FactoryContent.Root;

        Assert.NotNull(located);
        Assert.True(Directory.Exists(Path.Combine(located, "presets")));
        Assert.Equal(Path.Combine(located, "samples"), FactoryContent.SamplesFolder);
    }
}
