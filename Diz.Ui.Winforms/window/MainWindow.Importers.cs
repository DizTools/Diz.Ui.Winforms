using Diz.Cpu._65816;
using Diz.Import.bsnes.tracelog;
using Diz.Ui.Winforms.dialogs;

namespace Diz.Ui.Winforms.window;

public partial class MainWindow
{
    private void ImportBizhawkCdl()
    {
        var filename = PromptOpenCdlFile("Open Bizhawk SNES CDL File");
        if (filename == "") 
            return;

        try {
            ProjectController.ImportBizHawkCdl(filename);
        } catch (Exception ex)  {
            ShowError(ex.Message);
        }

        UpdateSomeUI2();
    }

    private void ImportMesen2Cdl()
    {
        var filename = PromptOpenCdlFile("Open Mesen2 (NES) CDL File");
        if (filename == "") 
            return;

        try {
            ProjectController.ImportMesen2Cdl(filename);
        } catch (Exception ex) {
            ShowError(ex.Message);
        }

        UpdateSomeUI2();
    }

    private void ImportBsnesTraceLogText()
    {
        if (!PromptForImportBsnesTraceLogFile()) 
            return;
            
        var (numModifiedFlags, numFiles) = ImportBsnesTraceLogs();
            
        RefreshUi();
        ReportNumberFlagsModified(numModifiedFlags, numFiles);
    }

    private void UiImportBsnesUsageMap()
    {
        if (openUsageMapFile.ShowDialog() != DialogResult.OK)
            return;

        var numModifiedFlags = ProjectController.ImportBsnesUsageMap(openUsageMapFile.FileName);
            
        RefreshUi();
        ShowInfo($"Modified total {numModifiedFlags} flags!", "Done");
    }

    private (long numBytesModified, int numFiles) ImportBsnesTraceLogs()
    {
        var numBytesModified = ProjectController.ImportBsnesTraceLogs(openTraceLogDialog.FileNames);
        return (numBytesModified, openTraceLogDialog.FileNames.Length);
    }

    private void ImportBsnesBinaryTraceLog()
    {
        var snesData = Project.Data.GetSnesApi();
        if (snesData == null)
            return;
            
        var captureController = new BsnesTraceLogCaptureController(snesData);
        new BsnesTraceLogBinaryMonitorForm(captureController).ShowDialog();
            
        RefreshUi();
    }

    private void OnImportedProjectSuccess()
    {
        UpdateSaveOptionStates(saveEnabled: false, saveAsEnabled: true, closeEnabled: true);
        RefreshUi();
    }
}