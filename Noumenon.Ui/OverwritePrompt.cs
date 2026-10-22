namespace Noumenon.Ui;

/// <summary>
/// The question SAVE asks before it writes over one of the player's own presets. On a loaded user
/// preset SAVE goes straight to its file — the one press that can lose a sound — so every host asks
/// first, with No as the default answer. Saving under a new name needs nothing more: the Save
/// dialog asks by itself before it replaces a file.
/// </summary>
public static class OverwritePrompt
{
    public static bool Confirm(IWin32Window owner, string path) =>
        MessageBox.Show(owner, $"Save over “{Path.GetFileNameWithoutExtension(path)}”?\n\nThe saved preset is replaced with the current sound.", "Save preset",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
}
