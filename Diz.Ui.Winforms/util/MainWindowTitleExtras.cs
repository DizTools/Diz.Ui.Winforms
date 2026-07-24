#nullable enable

namespace Diz.Ui.Winforms.util;

/// <summary>
/// Optional free-form text appended to the main window's title bar, set from the --extraTitleBar
/// "&lt;text&gt;" command-line switch. Purely cosmetic developer QoL: label a particular running
/// instance (e.g. which worktree/build it came out of) so several open at once can be told apart.
/// </summary>
public static class MainWindowTitleExtras
{
    /// <summary>
    /// Free-form text from the --extraTitleBar command-line switch. Set once at startup, before the
    /// main window is built.
    /// </summary>
    public static string? CommandLineText { get; set; }

    /// <summary>
    /// The suffix to append to the window title (leading separator included), or "" when there's
    /// nothing to add.
    /// </summary>
    public static string Suffix =>
        string.IsNullOrWhiteSpace(CommandLineText) ? "" : "  —  " + CommandLineText.Trim();
}
