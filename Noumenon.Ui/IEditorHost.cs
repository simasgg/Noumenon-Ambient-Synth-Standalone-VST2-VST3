namespace Noumenon.Ui;

/// <summary>
/// What the editor needs from whoever hosts it (the standalone, the plugin wrappers): the preset
/// bank, the file dialogs and the notice line, which live outside the shared control because a
/// plugin host owns its own files and windows. Everything the editor does with the engine itself
/// goes through <c>NoumenonEngine</c> directly.
/// </summary>
public interface IEditorHost
{
    IReadOnlyList<string> PresetNames { get; }

    /// <summary>Beside <see cref="PresetNames"/>: the menu group each preset is shown in (see <see cref="PresetMenu"/>), empty for the top level.</summary>
    IReadOnlyList<string> PresetGroups { get; }

    int CurrentPresetIndex { get; }

    string CurrentPresetName { get; }

    /// <summary>A short line for the top bar: the device, the load, a notice.</summary>
    string StatusText { get; }

    /// <summary>True while letter keys play notes, so the editor leaves R and M alone.</summary>
    bool KeyboardNotesActive { get; }

    void LoadPreset(int index);

    void SavePreset();

    /// <summary>Glides every parameter to the preset at <paramref name="index"/> over <paramref name="seconds"/>; lanes and samples stay.</summary>
    void MorphToPreset(int index, float seconds);

    void LoadSample(int key);

    /// <summary>Decodes a file onto a key off the UI thread; once it has loaded, the key becomes the sampler's (or, with <paramref name="asWavetable"/>, the oscillators' wavetable), so a failed load changes nothing.</summary>
    void LoadSampleFile(int key, string path, bool asWavetable = false);

    /// <summary>Shows a line in the status area for a few seconds.</summary>
    void Notice(string text);
}
