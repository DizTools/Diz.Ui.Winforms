using System.Threading.Tasks;
using Diz.Controllers.interfaces;
using Diz.Core.commands;
using Diz.Cpu._65816;
using Diz.LogWriter;
using Diz.Ui.ViewModels.Goto;
using Diz.Ui.ViewModels.HarshAutoStep;
using Diz.Ui.ViewModels.MarkMany;
using Diz.Ui.ViewModels.MisalignmentChecker;
using Diz.Ui.Winforms.dialogs;
using Diz.Ui.Winforms.util;

namespace Diz.Ui.Winforms.window;

public partial class MainWindow
{
    private async Task<bool> PromptContinueEvenIfUnsavedChanges()
    {
        if (Project == null || !(Project.Session?.UnsavedChanges ?? true))
            return true;

        var result = PromptDialog.Show(
            "You have unsaved changes; they will be lost if you continue.\nDo you want to save changes?",
            "Unsaved Changes", MessageBoxButtons.YesNoCancel);

        if (result == DialogResult.Yes)
            await SaveProject(askFilenameIfNotSet: true, alwaysAsk: false);

        return result != DialogResult.Cancel;
    }

    private string PromptForOpenFilename()
    {
        // TODO: combine with another function here that does similar
        openFileDialog.InitialDirectory = Project?.ProjectFileName ?? "";
        return openFileDialog.ShowDialog() == DialogResult.OK ? openFileDialog.FileName : "";
    }

    private static void ShowExportResults(LogCreatorOutput.OutputResult result)
    {
        if (result.FatalErrorMsg != "")
            PromptDialog.Show($"Internal exporter error in Diz: {result.FatalErrorMsg}", "Internal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        else if (result.ErrorCount > 0)
            PromptDialog.Show("Disassembly exported with warnings, see generated errors.txt file", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
            PromptDialog.Show("Disassembly files exported successfully!", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task<bool> PromptForOpenProjectFilename()
    {
        if (!await PromptContinueEvenIfUnsavedChanges())
            return false;

        openProjectFile.InitialDirectory = Project?.ProjectFileName;
        return openProjectFile.ShowDialog() == DialogResult.OK;
    }

    private void viewHelpToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var helpUrl = "https://github.com/IsoFrieze/DiztinGUIsh/blob/master/Diz.App.Winforms/dist/docs/HELP.md";
        try
        {
            // System.Diagnostics.Process.Start(Directory.GetCurrentDirectory() + "/help.html");
            WinformsGuiUtil.OpenExternalProcess(helpUrl);
        }
        catch (Exception)
        {
            PromptDialog.Show("Failed to open help url:\r\n"+helpUrl+"", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void githubToolStripMenuItem_Click(object sender, EventArgs e) =>
        WinformsGuiUtil.OpenExternalProcess("https://github.com/Isofrieze/DiztinGUIsh");

    private async Task<string> PromptOpenBizhawkCDLFile()
    {
        openCDLDialog.InitialDirectory = Project.ProjectFileName;
        if (openCDLDialog.ShowDialog() != DialogResult.OK)
            return "";

        return !await PromptContinueEvenIfUnsavedChanges() ? "" : openCDLDialog.FileName;
    }

    private static void ReportNumberFlagsModified(long numModifiedFlags, int numFiles = 1)
    {
        PromptDialog.Show($"Modified total {numModifiedFlags} flags from {numFiles} files!",
            "Done",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private bool PromptForImportBSNESTraceLogFile()
    {
        openTraceLogDialog.Multiselect = true;
        return openTraceLogDialog.ShowDialog() == DialogResult.OK;
    }
        
    private static void ShowOffsetOutOfRangeMsg()
    {
        ShowError("That offset is out of range.", "Error");
    }

    /// <summary>
    /// Ask the user where to go. Returns the ROM file offset, or -1 for "nowhere" -- which
    /// covers cancelling, no ROM loaded, and any state the window refused to confirm.
    ///
    /// Async because the goto window is a per-toolkit view service and not every toolkit can
    /// offer a blocking modal call. The WinForms one does, so on that backend this method still
    /// runs start to finish without ever yielding to the message loop.
    /// </summary>
    private async Task<int> PromptForGotoOffset()
    {
        if (!RomDataPresent())
            return -1;

        var snesData = Project.Data.GetSnesApi()
                       ?? throw new InvalidOperationException("No snes data present");

        // no notification marshaller: this ViewModel does no background work, so every
        // notification it raises is a direct consequence of a widget event already on the UI
        // thread.
        var viewModel = new GotoViewModel(
            snesData,
            snesData.GetRomSize(),
            startPcOffset: ViewOffset + table.CurrentCell?.RowIndex ?? 0);

        // which window shows up is a per-toolkit registration; the ViewModel is the whole
        // contract. Awaiting the WinForms implementation continues synchronously, because its
        // modal call has already finished by the time it returns a task.
        var confirmed = await viewFactory.GetGotoView().EditAsync(
            viewModel,
            initiallySelectSnesAddr: !Project.ProjectUserSettings.DisplayOffsetsInGrid);

        return confirmed ? viewModel.ResultPcOffset ?? -1 : -1;
    }

    private static void ShowError(string errorMsg, string caption = "Error")
    {
        PromptDialog.Show(errorMsg, caption);
    }

    /// <summary>
    /// Ask the user which run of bytes to decode as instructions. Returns the command describing
    /// that request, or null for "step nothing" -- which covers cancelling and any range the
    /// window refused to confirm. Applying the command is the caller's job.
    ///
    /// Async because the harsh-auto-step window is a per-toolkit view service and not every
    /// toolkit can offer a blocking modal call. The WinForms one does, so on that backend this
    /// method still runs start to finish without ever yielding to the message loop.
    /// </summary>
    private async Task<AutoStepHarshCommand?> PromptHarshAutoStep(int offset)
    {
        var snesData = Project.Data.GetSnesApi()
                       ?? throw new InvalidOperationException("No snes data present");

        // no notification marshaller: this ViewModel does no background work, so every
        // notification it raises is a direct consequence of a widget event already on the UI
        // thread.
        var viewModel = new HarshAutoStepViewModel(
            snesData,
            snesData.GetRomSize(),
            startPcOffset: offset);

        // which window shows up is a per-toolkit registration; the ViewModel is the whole
        // contract. Awaiting the WinForms implementation continues synchronously, because its
        // modal call has already finished by the time it returns a task.
        return !await viewFactory.GetHarshAutoStepView().EditAsync(viewModel)
            ? null
            : viewModel.BuildAutoStepHarshCommand();
    }
    
    // -----------------------
    
    // what the user picked in the mark-many window last time, remembered for the rest of the
    // session so repeat marking doesn't mean re-typing the same choices.
    private MarkManySettings savedMarkManySettings = new();

    private async Task<MarkCommand?> PromptBuildMarkManyCommand(int offset, int count, MarkCommand.MarkManyProperty? property = null)
    {
        var snesData = Project.Data.GetSnesApi()
                       ?? throw new InvalidOperationException("No snes data present");

        var settingToUse = savedMarkManySettings;
        if (property != null)
            settingToUse.SelectedProperty = property.Value;

        // no notification marshaller: this ViewModel does no background work, so every
        // notification it raises is a direct consequence of a widget event already on the UI
        // thread.
        var viewModel = new MarkManyViewModel<ISnesData>(snesData, startOffset: offset, count: count);
        viewModel.RestoreSettings(settingToUse);

        // which window shows up is a per-toolkit registration; the ViewModel is the whole
        // contract. Awaiting the WinForms implementation continues synchronously, because its
        // modal call has already finished by the time it returns a task.
        if (!await viewFactory.GetMarkManyView().EditAsync(viewModel))
            return null;

        // save a copy of previous UI settings, so we can restore them next time
        savedMarkManySettings = viewModel.CaptureSettings();
        return viewModel.BuildMarkCommand();
    }

    /// <summary>
    /// Offer the misaligned-flags report and ask whether to repair what it lists. Returns true
    /// when the user asked for the repair; applying it is the caller's job.
    ///
    /// Async because the misaligned-flags window is a per-toolkit view service and not every
    /// toolkit can offer a blocking modal call. The WinForms one does, so on that backend this
    /// method still runs start to finish without ever yielding to the message loop.
    /// </summary>
    private async Task<bool> PromptForMisalignmentCheck()
    {
        if (!RomDataPresent())
            return false;

        // the sweep lives in Diz.Cpu.65816, which the ViewModel assembly may not reference, so
        // it is handed in as a delegate. A project with no SNES api attached has nothing to
        // sweep: the legacy window silently showed an empty report rather than throwing, and
        // this keeps that.
        // no notification marshaller: this ViewModel does no background work, so every
        // notification it raises is a direct consequence of a widget event already on the UI
        // thread.
        var viewModel = new MisalignmentCheckerViewModel(
            () => Project.Data.GetSnesApi() is { } snesApi
                ? snesApi.GenerateMisalignmentReport()
                : (0, ""));

        // which window shows up is a per-toolkit registration; the ViewModel is the whole
        // contract. Awaiting the WinForms implementation continues synchronously, because its
        // modal call has already finished by the time it returns a task.
        return await viewFactory.GetMisalignmentCheckerView().RunAsync(viewModel);
    }

    private static void ShowInfo(string s, string caption)
    {
        PromptDialog.Show(s, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Ask whether to rescan every instruction for in/out/end/read points. Returns true when the
    /// user said yes; running the rescan is the caller's job.
    ///
    /// Async because the rescan confirmation is a per-toolkit view service and not every toolkit
    /// can offer a blocking modal call. The WinForms one does, so on that backend this method
    /// still runs start to finish without ever yielding to the message loop.
    /// </summary>
    private async Task<bool> PromptForInOutChecking()
    {
        if (!RomDataPresent())
            return false;

        // no ViewModel: this window has no state and no inputs, so the seam is the whole
        // contract. Awaiting the WinForms implementation continues synchronously, because its
        // modal call has already finished by the time it returns a task.
        return await viewFactory.GetInOutPointCheckerView().ConfirmAsync();
    }

    public string AskToSelectNewRomFilename(string promptSubject, string promptText)
    {
        // Called from the background project-open Task (RunLongRunningTaskAsync runs work on the
        // thread pool). The confirm dialog AND OpenFileDialog are modal WinForms UI and must run
        // on the STA UI thread -- marshal back to it or they throw ThreadStateException (swallowed
        // by ProjectController's catch-all, so the picker silently never appears).
        if (InvokeRequired)
            return (string)Invoke(new Func<string>(() => AskToSelectNewRomFilename(promptSubject, promptText)));

        string initialDir = null; // TODO: Project.ProjectFileName
        return WinformsGuiUtil.PromptToConfirmAction(promptSubject, promptText,
            () => WinformsGuiUtil.PromptToSelectFile(initialDir)
        ) ?? "";
    }

    public void OnProjectOpenWarnings(IEnumerable<string> warnings)
    {
        foreach (var warningMsg in warnings) {
            // --acceptProjectOpenWarnings: OK-only notices with nothing to decide, so an unattended
            // launch accepts them rather than sitting on a modal box forever. Still recorded.
            if (StartupPromptOptions.AcceptProjectOpenWarnings)
            {
                Diz.Core.util.StartupTrace.Log($"auto-accepted project open warning: {warningMsg}");
                continue;
            }

            // Use PromptDialog (like every sibling prompt in this file) rather than a raw
            // ownerless MessageBox.Show: PromptDialog's ctor calls BringWinFormToTop(), so it
            // self-tops and can't hide behind the main window (which an ownerless MessageBox does).
            PromptDialog.Show(warningMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}