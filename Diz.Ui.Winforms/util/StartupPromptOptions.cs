#nullable enable

namespace Diz.Ui.Winforms.util;

/// <summary>
/// Startup-only switches for prompts that would otherwise park an unattended launch on a modal box.
/// Set once from the command line in Program.cs, before the main window is built.
/// </summary>
public static class StartupPromptOptions
{
    /// <summary>
    /// --acceptProjectOpenWarnings: auto-accept the informational warnings raised after a project
    /// opens (e.g. the "save format on disk was older" upgrade notice). These are OK-only notices
    /// with nothing to decide, so accepting them unattended loses no choice -- they're written to
    /// the startup trace instead of shown.
    ///
    /// This does NOT change anything on disk: the save-format upgrade happens on the next SAVE
    /// either way, and dismissing the notice neither performs nor approves that upgrade.
    /// </summary>
    public static bool AcceptProjectOpenWarnings { get; set; }
}
