using System.Threading.Tasks;
using Diz.Cpu._65816;
using Diz.Import.bsnes.tracelog;
using Diz.Ui.Winforms.dialogs;

namespace Diz.Ui.Winforms.window;

public partial class MainWindow
{
    private async Task ImportBizhawkCDL()
    {
        var filename = await PromptOpenBizhawkCDLFile();
        if (filename != null && filename == "") return;
        ImportBizHawkCdl(filename);
        UpdateSomeUI2();
    }

    private void ImportBizHawkCdl(string filename)
    {
        try
        {
            ProjectController.ImportBizHawkCdl(filename);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message, "Error");
        }
    }

    private async Task ImportBsnesTraceLogText()
    {
        if (!PromptForImportBSNESTraceLogFile())
            return;

        var (numModifiedFlags, numFiles) = await ImportBsnesTraceLogs();

        RefreshUi();
        ReportNumberFlagsModified(numModifiedFlags, numFiles);
    }

    private async Task UiImportBsnesUsageMap()
    {
        if (openUsageMapFile.ShowDialog() != DialogResult.OK)
            return;

        var numModifiedFlags = await ProjectController.ImportBsnesUsageMapAsync(openUsageMapFile.FileName);

        RefreshUi();
        ShowInfo($"Modified total {numModifiedFlags} flags!", "Done");
    }

    private async Task<(long numBytesModified, int numFiles)> ImportBsnesTraceLogs()
    {
        var numBytesModified = await ProjectController.ImportBsnesTraceLogsAsync(openTraceLogDialog.FileNames);
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