using System.ComponentModel;
using System.Globalization;
using Diz.Core.commands;
using Diz.Core.Interfaces;
using Diz.Core.util;
using Diz.Cpu._65816;
using Diz.Ui.ViewModels.MarkMany;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// Modal host for <see cref="MarkManyViewModel{TDataSource}"/>: pick a property, a value and a
/// run of bytes, then press OK. Everything that decides what gets marked -- range math, address
/// conversion, hex/decimal parsing, validation, session memory -- lives in the ViewModel. This
/// file is widget wiring only, and the dialog never applies the command: the caller reads
/// <c>BuildMarkCommand()</c> off the ViewModel after a DialogResult.OK and applies it.
/// </summary>
public partial class MarkManyDialog : Form
{
    // combo box contents, in the order the designer lists them. The combos show display text;
    // these arrays are the model values behind each index, so no index arithmetic leaks into
    // the ViewModel. "CPU architecture" is deliberately absent from the property combo: the
    // ViewModel and the applier both support marking it, but this window has never offered it
    // and nothing reaches it today. If it ever becomes relevant, add MarkManyProperty.CpuArch
    // here and the matching display string to comboPropertyType.Items in the designer -- the
    // architecture combo it selects is already built and wired.
    private static readonly MarkCommand.MarkManyProperty[] PropertyComboValues =
    [
        MarkCommand.MarkManyProperty.Flag,
        MarkCommand.MarkManyProperty.DataBank,
        MarkCommand.MarkManyProperty.DirectPage,
        MarkCommand.MarkManyProperty.MFlag,
        MarkCommand.MarkManyProperty.XFlag,
    ];

    private static readonly FlagType[] FlagComboValues =
    [
        FlagType.Unreached, FlagType.Opcode, FlagType.Operand, FlagType.Data8Bit,
        FlagType.Graphics, FlagType.Music, FlagType.Empty, FlagType.Data16Bit,
        FlagType.Pointer16Bit, FlagType.Data24Bit, FlagType.Pointer24Bit,
        FlagType.Data32Bit, FlagType.Pointer32Bit, FlagType.Text,
    ];

    private static readonly Architecture[] ArchComboValues =
    [
        Architecture.Cpu65C816, Architecture.Apuspc700, Architecture.GpuSuperFx,
    ];

    // mxCombo: index 0 = "16-Bit", index 1 = "8-Bit"
    private const int MxComboIndex16Bit = 0;
    private const int MxComboIndex8Bit = 1;

    private readonly MarkManyViewModel<ISnesData> viewModel;

    private readonly ErrorProvider validationErrors = new()
    {
        BlinkStyle = ErrorBlinkStyle.NeverBlink,
    };

    // true while widget values are being written FROM the ViewModel; the input handlers below
    // bail out then, so a ViewModel-driven refresh can't be mistaken for the user typing.
    private bool updatingWidgets;

    // the control the user is currently typing into. Text is never pushed back into it while
    // it holds the caret -- reformatting a field under the caret fights the user. (The range
    // ViewModel already withholds notifications for the field being edited; this covers the
    // register value box, which shares no such rule.)
    private Control? controlBeingEdited;

    public MarkManyDialog(MarkManyViewModel<ISnesData> viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        this.viewModel = viewModel;

        InitializeComponent();

        validationErrors.ContainerControl = this;

        // value combos have no designer-wired handlers (the old dialog only read them when OK
        // was pressed); they push to the ViewModel now, so wire them here.
        flagCombo.SelectedIndexChanged += FlagCombo_SelectedIndexChanged;
        mxCombo.SelectedIndexChanged += MxCombo_SelectedIndexChanged;
        archCombo.SelectedIndexChanged += ArchCombo_SelectedIndexChanged;

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        viewModel.Range.PropertyChanged += Range_PropertyChanged;

        FormClosed += (_, _) =>
        {
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            viewModel.Range.PropertyChanged -= Range_PropertyChanged;
            validationErrors.Dispose();
        };

        RefreshAllWidgets();
    }

    // ------------------------------------------------------------------ ViewModel -> widgets

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MarkManyViewModel<ISnesData>.SelectedProperty):
                WriteWidgets(() =>
                {
                    comboPropertyType.SelectedIndex = Array.IndexOf(PropertyComboValues, viewModel.SelectedProperty);
                });
                break;

            case nameof(MarkManyViewModel<ISnesData>.IsFlagValueUsed):
            case nameof(MarkManyViewModel<ISnesData>.IsRegisterValueUsed):
            case nameof(MarkManyViewModel<ISnesData>.IsRegisterWidthUsed):
            case nameof(MarkManyViewModel<ISnesData>.IsArchitectureValueUsed):
                RefreshValueWidgetVisibility();
                break;

            case nameof(MarkManyViewModel<ISnesData>.RegisterValueMaxTextLength):
                WriteWidgets(() => regValue.MaxLength = viewModel.RegisterValueMaxTextLength);
                break;

            case nameof(MarkManyViewModel<ISnesData>.DataBankValue):
            case nameof(MarkManyViewModel<ISnesData>.DirectPageValue):
                RefreshRegisterValueText();
                break;

            case nameof(MarkManyViewModel<ISnesData>.FlagValue):
                WriteWidgets(() =>
                {
                    flagCombo.SelectedIndex = Array.IndexOf(FlagComboValues, viewModel.FlagValue);
                });
                break;

            case nameof(MarkManyViewModel<ISnesData>.RegisterWidthIs8Bit):
                WriteWidgets(() =>
                {
                    mxCombo.SelectedIndex = viewModel.RegisterWidthIs8Bit ? MxComboIndex8Bit : MxComboIndex16Bit;
                });
                break;

            case nameof(MarkManyViewModel<ISnesData>.ArchitectureValue):
                WriteWidgets(() =>
                {
                    archCombo.SelectedIndex = Array.IndexOf(ArchComboValues, viewModel.ArchitectureValue);
                });
                break;
        }

        RefreshValidation();
    }

    private void Range_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AddressRangeViewModel.StartText):
                WriteText(textStart, viewModel.Range.StartText);
                break;

            case nameof(AddressRangeViewModel.EndText):
                WriteText(textEnd, viewModel.Range.EndText);
                break;

            case nameof(AddressRangeViewModel.CountText):
                WriteText(textCount, viewModel.Range.CountText);
                break;

            case nameof(AddressRangeViewModel.UseHexadecimal):
                // the register value box shares the range's number base
                WriteWidgets(() => radioHex.Checked = viewModel.Range.UseHexadecimal);
                RefreshRegisterValueText();
                break;

            case nameof(AddressRangeViewModel.UseSnesAddresses):
                WriteWidgets(() => radioSNES.Checked = viewModel.Range.UseSnesAddresses);
                break;
        }

        RefreshValidation();
    }

    private void RefreshAllWidgets()
    {
        WriteWidgets(() =>
        {
            comboPropertyType.SelectedIndex = Array.IndexOf(PropertyComboValues, viewModel.SelectedProperty);
            flagCombo.SelectedIndex = Array.IndexOf(FlagComboValues, viewModel.FlagValue);
            archCombo.SelectedIndex = Array.IndexOf(ArchComboValues, viewModel.ArchitectureValue);
            mxCombo.SelectedIndex = viewModel.RegisterWidthIs8Bit ? MxComboIndex8Bit : MxComboIndex16Bit;

            radioSNES.Checked = viewModel.Range.UseSnesAddresses;
            radioPC.Checked = !viewModel.Range.UseSnesAddresses;
            radioHex.Checked = viewModel.Range.UseHexadecimal;
            radioDec.Checked = !viewModel.Range.UseHexadecimal;

            textStart.Text = viewModel.Range.StartText;
            textEnd.Text = viewModel.Range.EndText;
            textCount.Text = viewModel.Range.CountText;

            regValue.MaxLength = viewModel.RegisterValueMaxTextLength;
            regValue.Text = RegisterValueText();
        });

        RefreshValueWidgetVisibility();
        RefreshValidation();
    }

    private void RefreshValueWidgetVisibility() =>
        WriteWidgets(() =>
        {
            flagCombo.Visible = viewModel.IsFlagValueUsed;
            regValue.Visible = viewModel.IsRegisterValueUsed;
            mxCombo.Visible = viewModel.IsRegisterWidthUsed;
            archCombo.Visible = viewModel.IsArchitectureValueUsed;
        });

    private void RefreshRegisterValueText() => WriteText(regValue, RegisterValueText());

    private string RegisterValueText() =>
        Util.NumberToBaseString(
            viewModel.SelectedProperty == MarkCommand.MarkManyProperty.DirectPage
                ? viewModel.DirectPageValue
                : viewModel.DataBankValue,
            NumberBase,
            0);

    /// <summary>
    /// OK is available only when the ViewModel can actually build a command. When it can't, the
    /// reason is attached to the input most likely responsible: an empty range is about the byte
    /// count, anything else while a register is selected is about the register value.
    /// </summary>
    private void RefreshValidation()
    {
        var result = viewModel.Validate();
        okay.Enabled = result.IsValid;

        validationErrors.SetError(textCount, null);
        validationErrors.SetError(regValue, null);
        validationErrors.SetError(comboPropertyType, null);

        if (result.IsValid)
            return;

        var offendingControl =
            viewModel.Range.Count <= 0 ? textCount :
            viewModel.IsRegisterValueUsed ? regValue :
            (Control) comboPropertyType;

        validationErrors.SetError(offendingControl, result.Error);
    }

    // ------------------------------------------------------------------ widgets -> ViewModel

    private void property_SelectedIndexChanged(object sender, EventArgs e) =>
        PushToViewModel(null, () =>
        {
            if (comboPropertyType.SelectedIndex >= 0)
                viewModel.SelectedProperty = PropertyComboValues[comboPropertyType.SelectedIndex];
        });

    private void FlagCombo_SelectedIndexChanged(object? sender, EventArgs e) =>
        PushToViewModel(null, () =>
        {
            if (flagCombo.SelectedIndex >= 0)
                viewModel.FlagValue = FlagComboValues[flagCombo.SelectedIndex];
        });

    private void MxCombo_SelectedIndexChanged(object? sender, EventArgs e) =>
        PushToViewModel(null, () =>
        {
            if (mxCombo.SelectedIndex >= 0)
                viewModel.RegisterWidthIs8Bit = mxCombo.SelectedIndex == MxComboIndex8Bit;
        });

    private void ArchCombo_SelectedIndexChanged(object? sender, EventArgs e) =>
        PushToViewModel(null, () =>
        {
            if (archCombo.SelectedIndex >= 0)
                viewModel.ArchitectureValue = ArchComboValues[archCombo.SelectedIndex];
        });

    private void textStart_TextChanged(object sender, EventArgs e) =>
        PushToViewModel(textStart, () => viewModel.Range.StartText = textStart.Text);

    private void textEnd_TextChanged(object sender, EventArgs e) =>
        PushToViewModel(textEnd, () => viewModel.Range.EndText = textEnd.Text);

    private void textCount_TextChanged(object sender, EventArgs e) =>
        PushToViewModel(textCount, () => viewModel.Range.CountText = textCount.Text);

    private void regValue_TextChanged(object sender, EventArgs e) =>
        PushToViewModel(regValue, () =>
        {
            // unparseable text is ignored outright, exactly as this box has always behaved:
            // the last good number stays in effect until something parseable is typed.
            if (!int.TryParse(regValue.Text, NumberStyle, CultureInfo.InvariantCulture, out var value))
                return;

            switch (viewModel.SelectedProperty)
            {
                case MarkCommand.MarkManyProperty.DataBank:
                    viewModel.DataBankValue = value;
                    break;
                case MarkCommand.MarkManyProperty.DirectPage:
                    viewModel.DirectPageValue = value;
                    break;
            }
        });

    // both radio pairs report through the control that is losing its check as well as the one
    // gaining it, so one handler per pair covers both directions.
    private void radioROM_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(null, () => viewModel.Range.UseSnesAddresses = radioSNES.Checked);

    private void radioHex_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(null, () => viewModel.Range.UseHexadecimal = radioHex.Checked);

    private void okay_Click(object sender, EventArgs e) => DialogResult = DialogResult.OK;

    private void cancel_Click(object sender, EventArgs e) => Close();

    // ------------------------------------------------------------------ plumbing

    private Util.NumberBase NumberBase =>
        viewModel.Range.UseHexadecimal ? Util.NumberBase.Hexadecimal : Util.NumberBase.Decimal;

    private NumberStyles NumberStyle =>
        viewModel.Range.UseHexadecimal ? NumberStyles.HexNumber : NumberStyles.Number;

    /// <summary>Run a user-input handler, unless the change came from the ViewModel in the first place.</summary>
    private void PushToViewModel(Control? sourceControl, Action push)
    {
        if (updatingWidgets)
            return;

        var previous = controlBeingEdited;
        controlBeingEdited = sourceControl;
        try
        {
            push();
        }
        finally
        {
            controlBeingEdited = previous;
        }
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

    private void WriteText(TextBox textBox, string text)
    {
        if (ReferenceEquals(textBox, controlBeingEdited))
            return;

        WriteWidgets(() => textBox.Text = text);
    }
}
