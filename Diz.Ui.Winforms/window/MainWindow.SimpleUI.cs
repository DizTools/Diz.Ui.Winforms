﻿#nullable enable

using Diz.Core.commands;
using Diz.Core.Interfaces;
using Diz.Core.util;
using Diz.Ui.Winforms.dialogs;

namespace Diz.Ui.Winforms.window;

public partial class MainWindow
{
    private void MainWindow_FormClosing(object sender, FormClosingEventArgs e) =>
        e.Cancel = !PromptContinueEvenIfUnsavedChanges();

    private void MainWindow_SizeChanged(object sender, EventArgs e) => UpdatePanels();
    private void MainWindow_ResizeEnd(object sender, EventArgs e) => UpdateDataGridView();
    private void MainWindow_Load(object sender, EventArgs e) => Init();
    private void newProjectToolStripMenuItem_Click(object sender, EventArgs e) => CreateNewProject();
    private void openProjectToolStripMenuItem_Click(object sender, EventArgs e) => OpenProject();

    private void saveProjectToolStripMenuItem_Click(object sender, EventArgs e) => 
        SaveProject(askFilenameIfNotSet: true, alwaysAsk: false); // save

    private void saveProjectAsToolStripMenuItem_Click(object sender, EventArgs e) => 
        SaveProject(askFilenameIfNotSet: true, alwaysAsk: true); // save as
        
    private bool EnsureProjectFileExistsOnDisk()
    {
        // must have saved the project at least once first
        // (otherwise relative export paths can get screwy).
        // does NOT MEAN we saved recently, just that it was ONCE ever saved.
        if (!string.IsNullOrEmpty(Project.ProjectFileName)) 
            return true;
            
        MessageBox.Show("Project file must be saved first before exporting. Please save it now.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            
        return SaveProject(askFilenameIfNotSet: true, alwaysAsk: true);
    }
        
    private void toolStrip_exportDisassemblyUseCurrentSettings_Click(object sender, System.EventArgs e)
    {
        if (!EnsureProjectFileExistsOnDisk())
            return;

        RunOperationWithUiHidden(() => 
            ProjectController.ExportAssemblyWithCurrentSettings()
        );
    }
    
    private void toolStrip_exportDisassemblyEditSettingsFirst_Click(object sender, EventArgs e)
    {
        if (!EnsureProjectFileExistsOnDisk())
            return;
            
        RunOperationWithUiHidden(() => 
            ProjectController.ConfirmSettingsThenExportAssembly()
        );
    }

    private bool RunOperationWithUiHidden(Func<bool> action)
    {
        // hide the UI so it doesn't try and update while we're doing intense stuff (like exporting)
        // this could mess up internal operations and iterate through collections being modified/etc.
        Hide();
        var result = action();
        Show();
        return result;
    }

    private void toolStrip_openExportDirectory_Click(object sender, EventArgs e) =>
        OpenExportDirectory();

    private void aboutToolStripMenuItem_Click(object sender, EventArgs e) =>
        viewFactory.GetAboutView().Show();
        
    private void exitToolStripMenuItem_Click(object sender, EventArgs e) => 
        Application.Exit();
        
    private void decimalToolStripMenuItem_Click(object sender, EventArgs e) => 
        UpdateBase(Util.NumberBase.Decimal);

    private void hexadecimalToolStripMenuItem_Click(object sender, EventArgs e) =>
        UpdateBase(Util.NumberBase.Hexadecimal);

    private void binaryToolStripMenuItem_Click(object sender, EventArgs e) => 
        UpdateBase(Util.NumberBase.Binary);
        
    private void importTraceLogBinary_Click(object sender, EventArgs e) => ImportBsnesBinaryTraceLog();
    private void addLabelToolStripMenuItem_Click(object sender, EventArgs e) => BeginAddingLabel();
    private void visualMapToolStripMenuItem_Click(object sender, EventArgs e) => ShowVisualizerForm();
    private void stepOverToolStripMenuItem_Click(object sender, EventArgs e) => Step(SelectedOffset);
    private void stepInToolStripMenuItem_Click(object sender, EventArgs e) => StepIn(SelectedOffset);
    private void autoStepSafeToolStripMenuItem_Click(object sender, EventArgs e) => AutoStepSafe(SelectedOffset);
    private void autoStepHarshToolStripMenuItem_Click(object sender, EventArgs e) => AutoStepHarsh(SelectedOffset);
    private void gotoToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var gotoOffset = PromptForGotoOffset();
        if (gotoOffset != -1)
            GoTo(gotoOffset);
    }

    private void gotoIntermediateAddressToolStripMenuItem_Click(object sender, EventArgs e) =>
        GoToIntermediateAddress(SelectedOffset);

    private void gotoFirstUnreachedToolStripMenuItem_Click(object sender, EventArgs e) => 
        GoToUnreached(true, true);

    private void gotoNearUnreachedToolStripMenuItem_Click(object sender, EventArgs e) =>
        GoToUnreached(false, false);

    private void gotoNextUnreachedToolStripMenuItem_Click(object sender, EventArgs e) => 
        GoToUnreached(false, true);

    private void gotoNextUnreachedInPointToolStripMenuItem_Click(object sender, EventArgs e) =>
        GoToNextUnreachedBranchPoint(SelectedOffset);

    private void markOneToolStripMenuItem_Click(object sender, EventArgs e) => 
        Mark(SelectedOffset);
        
    private void markManyToolStripMenuItem_Click(object sender, EventArgs e) => 
        MarkMany(SelectedOffset, MarkCommand.MarkManyProperty.Flag);
        
    private void setDataBankToolStripMenuItem_Click(object sender, EventArgs e) => 
        MarkMany(SelectedOffset, MarkCommand.MarkManyProperty.DataBank);
        
    private void setDirectPageToolStripMenuItem_Click(object sender, EventArgs e) => 
        MarkMany(SelectedOffset, MarkCommand.MarkManyProperty.DirectPage);

    private void toggleAccumulatorSizeMToolStripMenuItem_Click(object sender, EventArgs e) => 
        MarkMany(SelectedOffset, MarkCommand.MarkManyProperty.MFlag);

    private void toggleIndexSizeToolStripMenuItem_Click(object sender, EventArgs e) => 
        MarkMany(SelectedOffset, MarkCommand.MarkManyProperty.XFlag);
        
    private void addCommentToolStripMenuItem_Click(object sender, EventArgs e) => 
        BeginEditingComment();

    private void unreachedToolStripMenuItem_Click(object sender, EventArgs e) =>
        SetMarkerLabel(FlagType.Unreached);

    private void opcodeToolStripMenuItem_Click(object sender, EventArgs e) => 
        SetMarkerLabel(FlagType.Opcode);

    private void operandToolStripMenuItem_Click(object sender, EventArgs e) =>
        SetMarkerLabel(FlagType.Operand);

    private void bitDataToolStripMenuItem_Click(object sender, EventArgs e) =>
        SetMarkerLabel(FlagType.Data8Bit);

    private void graphicsToolStripMenuItem_Click(object sender, EventArgs e) =>
        SetMarkerLabel(FlagType.Graphics);

    private void musicToolStripMenuItem_Click(object sender, EventArgs e) => SetMarkerLabel(FlagType.Music);
    private void emptyToolStripMenuItem_Click(object sender, EventArgs e) => SetMarkerLabel(FlagType.Empty);

    private void bitDataToolStripMenuItem1_Click(object sender, EventArgs e) =>
        SetMarkerLabel(FlagType.Data16Bit);

    private void wordPointerToolStripMenuItem_Click(object sender, EventArgs e) =>
        SetMarkerLabel(FlagType.Pointer16Bit);

    private void bitDataToolStripMenuItem2_Click(object sender, EventArgs e) =>
        SetMarkerLabel(FlagType.Data24Bit);

    private void longPointerToolStripMenuItem_Click(object sender, EventArgs e) =>
        SetMarkerLabel(FlagType.Pointer24Bit);

    private void bitDataToolStripMenuItem3_Click(object sender, EventArgs e) =>
        SetMarkerLabel(FlagType.Data32Bit);

    private void dWordPointerToolStripMenuItem_Click(object sender, EventArgs e) =>
        SetMarkerLabel(FlagType.Pointer32Bit);

    private void textToolStripMenuItem_Click(object sender, EventArgs e) => SetMarkerLabel(FlagType.Text);

    private void fixMisalignedInstructionsToolStripMenuItem_Click(object sender, EventArgs e) =>
        UiFixMisalignedInstructions();

    private void moveWithStepToolStripMenuItem_Click(object sender, EventArgs e) => ToggleMoveWithStep();

    private void projectSettingsToolStripMenuItem_Click(object sender, EventArgs e) => ShowProjectSettings();

    private void openLastProjectAutomaticallyToolStripMenuItem_Click(object sender, EventArgs e) =>
        ToggleOpenLastProjectEnabled();

    private void closeProjectToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // TODO
    }

    private void importCDLToolStripMenuItem_Click_1(object sender, EventArgs e) => ImportBizhawkCDL();

    private void importBsnesTracelogText_Click(object sender, EventArgs e) => ImportBsnesTraceLogText();

    private void graphicsWindowToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // TODO
        // graphics view window
    }

    private void toolStripOpenLast_Click(object sender, EventArgs e)
    {
        OpenLastProject();
    }

    private void rescanForInOutPointsToolStripMenuItem_Click(object sender, EventArgs e) => UiRescanForInOut();
    private void importUsageMapToolStripMenuItem_Click_1(object sender, EventArgs e) => UiImportBsnesUsageMap();
    private void table_MouseWheel(object sender, MouseEventArgs e) => 
        ScrollTableBy(e.Delta != 0 
            ? e.Delta/0x18 
            : 0
        );

    public NavigationForm NavigationForm { get; }

    private void showHistoryToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (!NavigationForm.Visible)
            NavigationForm.Show();
        else
            NavigationForm.BringToFront();
    }

    private void goBackToolStripMenuItem_Click(object sender, EventArgs e) => 
        NavigationForm.Navigate(forwardDirection: false, 
            overshootAmount: standardOvershootAmount
        );

    private void goForwardToolStripMenuItem_Click(object sender, EventArgs e) => 
        NavigationForm.Navigate(forwardDirection: true, 
            overshootAmount: standardOvershootAmount
        );

    private void LabelsOnOnLabelChanged(object? sender, EventArgs e)
    {
        InvalidateTable();
    }
}