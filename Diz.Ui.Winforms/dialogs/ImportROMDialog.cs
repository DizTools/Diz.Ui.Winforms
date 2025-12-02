using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Diz.Controllers.controllers;
using Diz.Controllers.interfaces;
using Diz.Core.util;
using Diz.Cpu._65816.import;
using Diz.Ui.Winforms.util;
using System.ComponentModel;
using Diz.Core.Interfaces;
using Diz.Cpu._65816;

namespace Diz.Ui.Winforms.dialogs;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class ImportRomDialog : Form, IImportRomDialogView
{
    public record VectorControls(string Name, CheckBox? Check, TextBox? Text);

    // NOTES:
    // - all this could be converted to use databinding and be easier to deal with, but,
    //   probably more work than its worth. this is fine, if a bit manual. it's unlikely to ever need to change.
    // - there's a few hidden entries that we won't put in the GUI
    private List<VectorControls> GetVectorGuiMappings()
    {
        return
        [
            // 1. these need to be kept in VECTOR TABLE ORDER.
            // 2. and ALL 16 vector entries must be present (even if there's no corresponding GUI for them) 
            
            new VectorControls(SnesVectorNames.Native_Reserved1__ignored, null, null),  // no GUI
            new VectorControls(SnesVectorNames.Native_Reserved2__ignored, null, null),  // no GUI
            new VectorControls(SnesVectorNames.Native_COP, checkboxNativeCOP, textNativeCOP),
            new VectorControls(SnesVectorNames.Native_BRK, checkboxNativeBRK, textNativeBRK),
            new VectorControls(SnesVectorNames.Native_ABORT, checkboxNativeABORT, textNativeABORT),
            new VectorControls(SnesVectorNames.Native_NMI, checkboxNativeNMI, textNativeNMI),
            new VectorControls(SnesVectorNames.Native_RESET__ignored, checkboxNativeRESET, textNativeRESET),
            new VectorControls(SnesVectorNames.Native_IRQ, checkboxNativeIRQ, textNativeIRQ),
            
            new VectorControls(SnesVectorNames.Emulation_Reserved1__ignored, null, null),  // no GUI
            new VectorControls(SnesVectorNames.Emulation_Reserved2__ignored, null, null),  // no GUI
            new VectorControls(SnesVectorNames.Emulation_COP, checkboxEmuCOP, textEmuCOP),
            new VectorControls(SnesVectorNames.Emulation_Reserved3__ignored, checkboxEmuReseved3Ignored, textEmuReseved3Ignored), // this is not BRK. it's reserved, it's not real. GUI row will still say BRK though
            new VectorControls(SnesVectorNames.Emulation_ABORT, checkboxEmuABORT, textEmuABORT),
            new VectorControls(SnesVectorNames.Emulation_NMI, checkboxEmuNMI, textEmuNMI),
            new VectorControls(SnesVectorNames.Emulation_RESET, checkboxEmuRESET, textEmuRESET),
            new VectorControls(SnesVectorNames.Emulation_IRQBRK, checkboxEmuIRQBRK, textEmuIRQBRK)
        ];
    }

    private IImportRomDialogController? controller;
    private readonly List<VectorControls> vectorTableGui;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IImportRomDialogController? Controller
    {
        get => controller;
        set
        {
            if (controller != null)
                controller.OnBuilderInitialized -= ControllerOnBuilderInitialized;

            controller = value;

            if (controller != null)
                controller.OnBuilderInitialized += ControllerOnBuilderInitialized;
        }
    }

    public ImportRomDialog()
    {
        InitializeComponent();
        
        vectorTableGui = GetVectorGuiMappings();
        foreach (var vector in vectorTableGui)
        {
            if (vector.Check == null)
                continue;
            
            vector.Check.CheckedChanged += OnVectorCheckboxCheckedChanged;
            vector.Check.Tag = vector;
        }
    }

    private void OnVectorCheckboxCheckedChanged(object? sender, EventArgs e)
    {
        if (sender is not CheckBox checkbox)
            return;

        if (checkbox.Tag is not VectorControls vector)
            return;
        
        Controller.Builder.OptionSetGenerateVectorTableLabelFor(vector.Name, vector.Check?.Checked ?? true);
    }

    private void DataBind()
    {
        // this is the better way to do this but... we need better hooks for knowing when stuff changes, it's a mess
        WinformsGuiUtil.BindListControlToEnum<RomMapMode>(cmbRomMapMode, 
            Controller.Builder, 
            nameof(ISnesRomImportSettingsBuilder.OptionSelectedRomMapMode));
        
        checkHeader.Checked = Controller.Builder.OptionGenerateHeaderFlags;
    }

    public bool ShowAndWaitForUserToConfirmSettings()
    {
        return ShowDialog() == DialogResult.OK;
    }

    private void ControllerOnBuilderInitialized()
    {
        DataBind();
        RefreshUi();
    }

    public void RefreshUi()
    {
        if (Controller == null)
            return;
        
        UpdateTextboxes();
        UpdateOkayButtonEnabled();
        detectMessage.Text = Controller.GetDetectionMessage();
    }

    private void UpdateOkayButtonEnabled() =>
        // validation settings AFTER this will give a warning, so just leave this always enabled
        okay.Enabled = true;

    private void UpdateTextboxes()
    {
        if (Controller.IsProbablyValidDetection())
        {
            try
            {
                UpdateUiFromDetectedValues();
                return;
            }
            catch (Exception)
            {
                // fall through
            }
        }

        SetDefaultsIfDetectionFailed();
    }

    private void SetDefaultsIfDetectionFailed()
    {
        romspeed.Text = "????";
        romtitle.Text = "?????????????????????";
        
        foreach (var (_, checkBox, textBox) in vectorTableGui)
        {
            if (checkBox != null)
            {
                checkBox.Checked = false;
                checkBox.Enabled = false;
            }

            if (textBox != null)
                textBox.Text = "????";
        }
    }

    public List<string> EnabledVectorTableEntries 
    {
        get 
        {
            var enabledVectors = new List<string>();

            foreach (var (vectorName, checkBox, _) in vectorTableGui)
            {
                if (checkBox?.Checked ?? true)
                    enabledVectors.Add(vectorName);
            }

            return enabledVectors;
        }
    }

    // caution: things can go wrong here if we didn't guess settings correctly,
    // you usually want to call this function with a try/catch around it.
    private void UpdateUiFromDetectedValues()
    {
        SyncGuiVectorTableEntriesFromController();
        romspeed.Text = Controller.RomSpeedText;
        romtitle.Text = Controller.CartridgeTitle;
    }

    private void SyncGuiVectorTableEntriesFromController()
    {
        var guiMappedVectorEntries = 
            vectorTableGui
                .Select((vectorControl, index) => new
                {
                    VectorTableOffset = index*2,
                    VectorControl = vectorControl
                })
                .Where(x => x.VectorControl is { Check: not null, Text: not null })
                .ToList();
        
        foreach (var guiMappedVectorEntry in guiMappedVectorEntries)
        {
            // read the word value of this entry from the ROM
            var vectorRomWordValue = Controller.ReadRomVectorTableEntryValueWord(guiMappedVectorEntry.VectorTableOffset);
            SetGuiForVectorEntry(vectorRomWordValue, guiMappedVectorEntry.VectorControl.Text!, guiMappedVectorEntry.VectorControl.Check!);
        }
    }

    private static void SetGuiForVectorEntry(int vectorValue, Control textBox, CheckBox checkBox)
    {
        textBox.Text = Util.NumberToBaseString(vectorValue, Util.NumberBase.Hexadecimal, 4);

        var enabled = vectorValue >= 0x8000;
        checkBox.Checked = checkBox.Enabled = enabled;
    }

    private void ImportROMDialog_Load(object sender, EventArgs e) => 
        RefreshUi();

    private void okay_Click(object sender, EventArgs e)
    {
        if (!Controller.Submit())
            return;

        SetFinished();
    }

    private void SetFinished() => 
        DialogResult = DialogResult.OK;

    private void cancel_Click(object sender, EventArgs e) => 
        Close();

    // todo: databind this instead.
    private void checkHeader_CheckedChanged(object sender, EventArgs e) => 
        Controller.Builder.OptionGenerateHeaderFlags = checkHeader.Checked;

    private void ImportRomDialog_FormClosing(object sender, FormClosingEventArgs e) => 
        controller = null;
}