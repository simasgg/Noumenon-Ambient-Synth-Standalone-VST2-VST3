using System.Diagnostics;

namespace Noumenon.Ui;

/// <summary>
/// Who made Noumenon: the author's name and page, for the wordmark on the panel (every host) and
/// the standalone's About box, so the credit reads the same everywhere.
/// </summary>
public static class Credit
{
    public const string Author = "Garda";
    public const string Url = "https://gardaofficial.bandcamp.com/";
    public const string UrlText = "gardaofficial.bandcamp.com";

    /// <summary>Opens <see cref="Url"/> in the default browser; false when Windows could not.</summary>
    public static bool OpenPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
