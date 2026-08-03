using System.ComponentModel;
using Diz.Core.Interfaces;
using Diz.Core.util;
using Diz.Cpu._65816.import;
using Diz.Ui.ViewModels.ImportRom;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// Modal host for <see cref="SnesImportRomViewModel"/>: how to read a SNES ROM when turning it
/// into a new project -- which memory mapping, which interrupt vectors get labels, and which
/// extra data to synthesize up front. Everything that decides what those choices mean lives in
/// the ViewModel; this file is widget wiring only.
///
/// The window never reads the ROM and never asks the user to confirm anything. Re-reading the
/// vector table after a map-mode change is a delegate the ViewModel was handed, and the
/// "are you sure?" question a risky mapping warrants is put by the caller once this window closes.
/// </summary>
public partial class SnesImportRomDialog : Form
{
    /// <summary>
    /// Which widgets show which vector. Vectors the ViewModel carries but the designer has no
    /// controls for are absent -- those are the CPU-reserved slots the user gets no say over --
    /// and rows are matched by NAME, never by position.
    /// </summary>
    private readonly IReadOnlyList<(string VectorName, CheckBox Check, TextBox Text)> vectorWidgets;

    private readonly SnesImportRomViewModel viewModel;

    // true while widget values are being written FROM the ViewModel; the input handlers below bail
    // out then, so a ViewModel-driven refresh can't be mistaken for the user clicking. Without it
    // the checkbox and combo handlers echo every refresh straight back into the ViewModel.
    private bool updatingWidgets;

    /// <param name="viewModel">Holds the import choices, and re-reads the ROM when the mapping changes.</param>
    public SnesImportRomDialog(SnesImportRomViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        this.viewModel = viewModel;

        InitializeComponent();

        // the emulation row LABELLED "BRK" in the designer is really a reserved slot that is not a
        // BRK vector at all. The display text stays as it is; the name here is what a generated
        // label would carry, and that is what the ViewModel matches on.
        vectorWidgets =
        [
            (SnesVectorNames.Native_COP, checkboxNativeCOP, textNativeCOP),
            (SnesVectorNames.Native_BRK, checkboxNativeBRK, textNativeBRK),
            (SnesVectorNames.Native_ABORT, checkboxNativeABORT, textNativeABORT),
            (SnesVectorNames.Native_NMI, checkboxNativeNMI, textNativeNMI),
            (SnesVectorNames.Native_RESET__ignored, checkboxNativeRESET, textNativeRESET),
            (SnesVectorNames.Native_IRQ, checkboxNativeIRQ, textNativeIRQ),

            (SnesVectorNames.Emulation_COP, checkboxEmuCOP, textEmuCOP),
            (SnesVectorNames.Emulation_Reserved3__ignored, checkboxEmuReseved3Ignored, textEmuReseved3Ignored),
            (SnesVectorNames.Emulation_ABORT, checkboxEmuABORT, textEmuABORT),
            (SnesVectorNames.Emulation_NMI, checkboxEmuNMI, textEmuNMI),
            (SnesVectorNames.Emulation_RESET, checkboxEmuRESET, textEmuRESET),
            (SnesVectorNames.Emulation_IRQBRK, checkboxEmuIRQBRK, textEmuIRQBRK),
        ];

        foreach (var (vectorName, check, _) in vectorWidgets)
        {
            check.Tag = vectorName;
            check.CheckedChanged += vectorCheckbox_CheckedChanged;
        }

        // the picker holds raw enum values but must SHOW the friendly text ("SA - 1 ROM", not
        // "Sa1Rom"): the ViewModel deliberately carries no display strings.
        cmbRomMapMode.DisplayMember = nameof(MapModeChoice.Description);
        cmbRomMapMode.ValueMember = nameof(MapModeChoice.Value);
        cmbRomMapMode.DataSource = viewModel.RomMapModeChoices
            .Select(mode => new MapModeChoice(mode, Util.GetEnumDescription(mode)))
            .ToList();

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        foreach (var row in viewModel.Vectors)
            row.PropertyChanged += VectorRow_PropertyChanged;

        FormClosed += (_, _) =>
        {
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            foreach (var row in viewModel.Vectors)
                row.PropertyChanged -= VectorRow_PropertyChanged;
        };

        RefreshAllWidgets();
    }

    /// <summary>A map mode paired with the text to show for it. The combo binds to these, not to the bare enum.</summary>
    private sealed record MapModeChoice(RomMapMode Value, string Description);

    // ------------------------------------------------------------------ ViewModel -> widgets

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SnesImportRomViewModel.SelectedRomMapMode):
                WriteWidgets(() => cmbRomMapMode.SelectedValue = viewModel.SelectedRomMapMode);
                break;

            case nameof(SnesImportRomViewModel.CartridgeTitle):
                romtitle.Text = viewModel.CartridgeTitle;
                break;

            case nameof(SnesImportRomViewModel.StatusText):
                statusMessage.Text = viewModel.StatusText;
                break;
        }
    }

    private void VectorRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is SnesVectorRowViewModel row)
            RefreshVectorRow(row);
    }

    private void RefreshAllWidgets()
    {
        WriteWidgets(() =>
        {
            cmbRomMapMode.SelectedValue = viewModel.SelectedRomMapMode;
            checkHeader.Checked = viewModel.GenerateHeaderFlags;
            checkBankRegions.Checked = viewModel.GenerateBankRegions;

            detectMessage.Text = viewModel.DetectionMessage;
            romspeed.Text = viewModel.RomSpeedText;
            romtitle.Text = viewModel.CartridgeTitle;
            statusMessage.Text = viewModel.StatusText;

            foreach (var row in viewModel.Vectors)
                RefreshVectorRow(row);
        });
    }

    private void RefreshVectorRow(SnesVectorRowViewModel row)
    {
        var widgets = vectorWidgets.FirstOrDefault(entry => entry.VectorName == row.Name);
        if (widgets.Check == null)
            return;

        WriteWidgets(() =>
        {
            widgets.Text.Text = row.DisplayValue;
            widgets.Check.Checked = row.IsEnabled;
            widgets.Check.Enabled = row.IsSelectable;
        });
    }

    // ------------------------------------------------------------------ widgets -> ViewModel

    private void cmbRomMapMode_SelectedIndexChanged(object sender, EventArgs e) =>
        PushToViewModel(() =>
        {
            if (cmbRomMapMode.SelectedValue is RomMapMode mode)
                viewModel.SelectedRomMapMode = mode;
        });

    private void vectorCheckbox_CheckedChanged(object? sender, EventArgs e)
    {
        if (sender is not CheckBox { Tag: string vectorName } checkbox)
            return;

        PushToViewModel(() =>
        {
            var row = viewModel.Vectors.FirstOrDefault(entry => entry.Name == vectorName);
            if (row != null)
                row.IsEnabled = checkbox.Checked;
        });
    }

    private void checkHeader_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.GenerateHeaderFlags = checkHeader.Checked);

    private void checkBankRegions_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.GenerateBankRegions = checkBankRegions.Checked);

    // OK stays available whatever is on screen: whether the choices are risky is decided after
    // this window closes, and a warning the user is allowed to accept is not a reason to block
    // the button.
    private void okay_Click(object sender, EventArgs e) => DialogResult = DialogResult.OK;

    private void cancel_Click(object sender, EventArgs e) => Close();

    // ------------------------------------------------------------------ plumbing

    /// <summary>Run a user-input handler, unless the change came from the ViewModel in the first place.</summary>
    private void PushToViewModel(Action push)
    {
        if (updatingWidgets)
            return;

        push();
    }

    /// <summary>Write widget state without the input handlers treating it as user input.</summary>
    private void WriteWidgets(Action write)
    {
        var previous = updatingWidgets;
        updatingWidgets = true;
        try
        {
            write();
        }
        finally
        {
            updatingWidgets = previous;
        }
    }
}
