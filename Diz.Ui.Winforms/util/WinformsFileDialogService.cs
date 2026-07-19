using Diz.Controllers.interfaces;

namespace Diz.Ui.Winforms.util;

// WinForms implementation of the async file-dialog seam (new-ui plan, step 4).
//
// Pattern note: WinForms dialogs are synchronous, so each method blocks in ShowDialog and
// returns an ALREADY-COMPLETED task via Task.FromResult. That is the intended shape -- the
// interface is async only because Avalonia's IStorageProvider has no synchronous API, so
// the contract has to be async for the hard backend; completing a finished Task here costs
// nothing. Callers may await these from the UI thread safely (no deadlock: the task is
// already completed by the time it is returned).
public class WinformsFileDialogService : IFileDialogService
{
    public Task<string?> PromptOpenFileAsync(string title, string filter)
    {
        using var dialog = new OpenFileDialog { Filter = filter };
        ApplyTitle(dialog, title);
        return Task.FromResult(ShowAndGetFilename(dialog));
    }

    public Task<string?> PromptSaveFileAsync(string title, string filter, string? suggestedName = null)
    {
        using var dialog = new SaveFileDialog { Filter = filter };
        ApplyTitle(dialog, title);
        if (!string.IsNullOrEmpty(suggestedName))
            dialog.FileName = suggestedName;
        return Task.FromResult(ShowAndGetFilename(dialog));
    }

    public Task<string?> PromptSelectFolderAsync(string title, string? initialPath = null)
    {
        using var dialog = new FolderBrowserDialog();
        if (!string.IsNullOrEmpty(title))
            dialog.Description = title;
        if (!string.IsNullOrEmpty(initialPath))
            dialog.SelectedPath = initialPath;

        var selected = dialog.ShowDialog() == DialogResult.OK && dialog.SelectedPath != ""
            ? dialog.SelectedPath
            : null;
        return Task.FromResult(selected);
    }

    // interface contract: empty title = keep the OS default ("Open" / "Save As"), so call
    // sites migrated from bare OpenFileDialog/SaveFileDialog look identical to the user.
    private static void ApplyTitle(FileDialog dialog, string title)
    {
        if (!string.IsNullOrEmpty(title))
            dialog.Title = title;
    }

    private static string? ShowAndGetFilename(FileDialog dialog) =>
        dialog.ShowDialog() == DialogResult.OK && dialog.FileName != ""
            ? dialog.FileName
            : null;
}
