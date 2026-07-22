using System.Threading.Tasks;
using Diz.Controllers.controllers;
using Diz.Controllers.interfaces;
using Diz.Core.commands;
using Diz.Cpu._65816;
using Diz.LogWriter;
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

    private int PromptForGotoOffset()
    {
        if (!RomDataPresent())
            return -1;

        var go = new GotoDialog(
            ViewOffset + table.CurrentCell?.RowIndex ?? 0, 
            Project.Data, 
            initiallySelectSnesAddr: !Project.ProjectUserSettings.DisplayOffsetsInGrid 
        );
        
        var result = go.ShowDialog();
        if (result != DialogResult.OK)
            return -1;
            
        return go.GetPcOffset();
    }

    private static void ShowError(string errorMsg, string caption = "Error")
    {
        PromptDialog.Show(errorMsg, caption);
    }

    private bool PromptHarshAutoStep(int offset, out int newOffset, out int count)
    {
        newOffset = count = -1;
            
        var harsh = new HarshAutoStep(offset, Project.Data);
        if (harsh.ShowDialog() != DialogResult.OK)
            return false;
            
        newOffset = harsh.StartRomOffset;
        count = harsh.Count;
        return true;
    }
    
    // -----------------------
    
    private MarkManyViewSettings savedMarkManySettings = new();

    private MarkCommand? PromptBuildMarkManyCommand(int offset, int count, MarkCommand.MarkManyProperty? property = null)
    {
        var settingToUse = savedMarkManySettings;
        if (property != null)
            settingToUse.SelectedProperty = property.Value;
        
        var markManyController = CreateMarkManyController();
        var markCommand = markManyController.Show(startOffset: offset, count: count, inputSettings: settingToUse);

        if (markCommand == null)
            return null;
            
        // save a copy of previous UI settings, so we can restore them next time
        savedMarkManySettings = markManyController.GetCurrentSettings();
        return markCommand;
    }
        
    private MarkManyController<ISnesData> CreateMarkManyController()
    {
        // TODO: replace view creation with dependency injection
        var view = new MarkManyView<ISnesData>();
        var snesData = Project.Data.GetSnesApi();
        
        return snesData == null 
            ? throw new InvalidOperationException("No snes data present") 
            : new MarkManyController<ISnesData>(snesData, view);
    }

    private bool PromptForMisalignmentCheck()
    {
        if (!RomDataPresent())
            return false;

        return new MisalignmentChecker(Project.Data).ShowDialog() == DialogResult.OK;
    }

    private static void ShowInfo(string s, string caption)
    {
        PromptDialog.Show(s, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private bool PromptForInOutChecking()
    {
        if (!RomDataPresent())
            return false;

        return new InOutPointChecker().ShowDialog() == DialogResult.OK;
    }

    public string AskToSelectNewRomFilename(string promptSubject, string promptText)
    {
        string initialDir = null; // TODO: Project.ProjectFileName
        return WinformsGuiUtil.PromptToConfirmAction(promptSubject, promptText, 
            () => WinformsGuiUtil.PromptToSelectFile(initialDir)
        ) ?? "";
    }

    public void OnProjectOpenWarnings(IEnumerable<string> warnings)
    {
        foreach (var warningMsg in warnings) {
            // Use PromptDialog (like every sibling prompt in this file) rather than a raw
            // ownerless MessageBox.Show: PromptDialog's ctor calls BringWinFormToTop(), so it
            // self-tops and can't hide behind the main window (which an ownerless MessageBox does).
            PromptDialog.Show(warningMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}