using Noumenon.Engine.Presets;

namespace Noumenon.Tests;

public class PresetLibraryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "noumenon-library-" + Guid.NewGuid().ToString("N"));

    public PresetLibraryTests() => Directory.CreateDirectory(root);

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
    public void Factory_Content_Is_Found_Up_The_Tree_From_A_Plugin_Deploy_Folder()
    {
        var factory = Path.Combine(root, "content", "presets");
        Directory.CreateDirectory(factory);
        PresetIo.Save(new PresetState { Name = "Alpha" }, Path.Combine(factory, "Alpha" + PresetIo.Extension));
        var deploy = Path.Combine(root, "Noumenon.Vst2", "bin", "x64", "Debug", "net10.0-windows", "deploy");
        Directory.CreateDirectory(deploy);

        var library = new PresetLibrary(deploy);

        Assert.Equal(factory, library.FactoryFolder);
        Assert.Contains(library.Factory, preset => preset.Name == "Alpha");
        Assert.True(library.IsFactory(library.Factory[0].Path));
        Assert.False(library.IsFactory(Path.Combine(library.UserFolder, "Mine" + PresetIo.Extension)));
        Assert.Equal(Path.Combine(factory, "Alpha" + PresetIo.Extension), PresetLibrary.Resolve("Alpha", deploy));
        Assert.Null(PresetLibrary.Resolve("Nope", deploy));
    }

    [Fact]
    public void Subfolders_Are_Families_Listed_After_The_Root_And_Resolved_By_Name()
    {
        var factory = Path.Combine(root, "content", "presets");
        Save(factory, "Init");
        Save(Path.Combine(factory, "Pad"), "Velvet Field");
        Save(Path.Combine(factory, "Drone"), "Basalt");
        Save(Path.Combine(factory, "Drone"), "Ashfall");

        var library = new PresetLibrary(root);

        Assert.Equal(["Init", "Ashfall", "Basalt", "Velvet Field"], library.Factory.Select(preset => preset.Name));
        Assert.Equal(["", "Drone", "Drone", "Pad"], library.Factory.Select(preset => preset.Family));
        Assert.Equal(Path.Combine(factory, "Pad", "Velvet Field" + PresetIo.Extension), PresetLibrary.Resolve("Velvet Field", root));
        Assert.Equal(Path.Combine(factory, "Drone", "Basalt" + PresetIo.Extension), PresetLibrary.Resolve("Drone/Basalt", root));
        Assert.Equal(Path.Combine(factory, "Drone", "Basalt" + PresetIo.Extension), PresetLibrary.Resolve(@"Drone\Basalt.noumenon", root));
        Assert.Null(PresetLibrary.Resolve("Pad/Basalt", root));
    }

    [Fact]
    public void Save_Starts_In_The_Folder_Used_Last_Unless_It_Is_The_Factory_Bank()
    {
        var factory = Path.Combine(root, "content", "presets");
        Save(Path.Combine(factory, "Drone"), "Basalt");
        var mine = Path.Combine(root, "Mine");

        var library = new PresetLibrary(root);

        Assert.Equal(library.UserFolder, library.SaveFolder(null));
        Assert.Equal(library.UserFolder, library.SaveFolder(factory));
        Assert.Equal(library.UserFolder, library.SaveFolder(Path.Combine(factory, "Drone")));
        Assert.Equal(mine, library.SaveFolder(mine));
    }

    private static void Save(string folder, string name)
    {
        Directory.CreateDirectory(folder);
        PresetIo.Save(new PresetState { Name = name }, Path.Combine(folder, name + PresetIo.Extension));
    }
}
