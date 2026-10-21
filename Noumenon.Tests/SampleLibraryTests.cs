using Noumenon.Engine.Presets;
using Noumenon.Engine.Samples;

namespace Noumenon.Tests;

public class SampleLibraryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "noumenon-samples-" + Guid.NewGuid().ToString("N"));

    public SampleLibraryTests() => Directory.CreateDirectory(root);

    public void Dispose()
    {
        try
        {
            Directory.Delete(root, true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void The_Factory_Samples_Are_Listed_As_Portable_References_With_The_Wavetables_Flagged()
    {
        var content = FactoryContent.Root ?? throw new InvalidOperationException("The repository's content folder was not found.");

        var library = SampleLibrary.Scan(content, Path.Combine(root, "nobody"));

        Assert.NotEmpty(library);
        Assert.All(library, sample => Assert.True(sample.IsFactory));
        Assert.All(library, sample => Assert.True(File.Exists(FactoryContent.Resolve(sample.Path, content))));
        Assert.Equal(Directory.GetFiles(Path.Combine(content, "samples"), "*.wav").Length, library.Count(sample => sample.Folder == "" && !sample.IsWavetable));
        Assert.Equal(Directory.GetFiles(Path.Combine(content, "samples", "wavetables"), "*.wav").Length, library.Count(sample => sample.IsWavetable));
        Assert.Contains(library, sample => sample.Path == "factory:samples/Glass Taps.wav" && sample.Name == "Glass Taps");
        Assert.Contains(library, sample => sample.Path.StartsWith("factory:samples/wavetables/", StringComparison.Ordinal) && sample.Folder == "wavetables");
    }

    [Fact]
    public void The_Players_Folder_Lists_Its_Own_Files_Then_Its_Subfolders_And_Skips_What_Is_Not_Audio()
    {
        var user = Path.Combine(root, "Samples");
        Touch(user, "b.wav");
        Touch(user, "A.flac");
        Touch(user, "notes.txt");
        Touch(Path.Combine(user, "Drums"), "kick.aiff");
        Touch(Path.Combine(user, "Wavetables"), "saw.wav");
        Touch(Path.Combine(user, "Drums", "Old"), "snare.mp3");

        var library = SampleLibrary.Scan(Path.Combine(root, "no content"), user);

        Assert.Equal(["A", "b", "kick", "snare", "saw"], library.Select(sample => sample.Name));
        Assert.Equal(["", "", "Drums", "Drums/Old", "Wavetables"], library.Select(sample => sample.Folder));
        Assert.Equal([false, false, false, false, true], library.Select(sample => sample.IsWavetable));
        Assert.All(library, sample => Assert.False(sample.IsFactory));
        Assert.Equal(Path.Combine(user, "Drums", "kick.aiff"), library[2].Path);
    }

    [Fact]
    public void A_File_Added_To_The_Players_Folder_Gets_A_Free_Name_And_One_Already_There_Stays()
    {
        var user = Path.Combine(root, "Samples");
        var outside = Path.Combine(root, "Elsewhere");
        Touch(outside, "rain.wav");

        var first = SampleLibrary.AddToUserFolder(Path.Combine(outside, "rain.wav"), user);
        var second = SampleLibrary.AddToUserFolder(Path.Combine(outside, "rain.wav"), user);
        var again = SampleLibrary.AddToUserFolder(first, user);

        Assert.Equal(Path.Combine(user, "rain.wav"), first);
        Assert.Equal(Path.Combine(user, "rain (2).wav"), second);
        Assert.Equal(first, again);
        Assert.Equal(2, Directory.GetFiles(user).Length);
    }

    private static void Touch(string folder, string name)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, name), [0]);
    }
}
