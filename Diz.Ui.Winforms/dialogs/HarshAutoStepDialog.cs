using System.ComponentModel;
using Diz.Ui.ViewModels.HarshAutoStep;
using Diz.Ui.ViewModels.MarkMany;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// Modal host for <see cref="HarshAutoStepViewModel"/>: pick a run of bytes and press Go, and
/// every one of them gets decoded as an instruction whether or not execution ever reaches it.
/// Everything that decides which bytes those are -- range math, address conversion,
/// hex/decimal parsing, validation -- lives in the ViewModel. This file is widget wiring only,
/// and the dialog never steps anything: the caller reads <c>BuildAutoStepHarshCommand()</c> off
/// the ViewModel after a DialogResult.OK and applies it.
/// </summary>
public partial class HarshAutoStepDialog : Form
{
    private readonly HarshAutoStepViewModel viewModel;

    // this window has no error label of its own, so the reason Go is unavailable is shown as an
    // icon beside the field that is wrong. Created here rather than in the designer, so the
    // generated layout stays exactly what it was.
    private readonly ErrorProvider validationErrors = new()
    {
        BlinkStyle = ErrorBlinkStyle.NeverBlink,
    };

    // true while widget values are being written FROM the ViewModel; the input handlers below
    // bail out then, so a ViewModel-driven refresh can't be mistaken for the user typing.
    private bool updatingWidgets;

    // the control the user is currently typing into. Text is never pushed back into it while it
    // holds the caret -- reformatting a field under the caret fights the user.
    private Control? controlBeingEdited;

    /// <param name="viewModel">Holds the range, and decides whether it is worth stepping through.</param>
    public HarshAutoStepDialog(HarshAutoStepViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        this.viewModel = viewModel;

        InitializeComponent();

        validationErrors.ContainerControl = this;

        // the range is the whole editable surface here, so it is the only thing to listen to.
        viewModel.Range.PropertyChanged += Range_PropertyChanged;

        FormClosed += (_, _) =>
        {
            viewModel.Range.PropertyChanged -= Range_PropertyChanged;
            validationErrors.Dispose();
        };

        RefreshAllWidgets();
    }

    // ------------------------------------------------------------------ ViewModel -> widgets

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
                WriteWidgets(() =>
                {
                    radioHex.Checked = viewModel.Range.UseHexadecimal;
                    radioDec.Checked = !viewModel.Range.UseHexadecimal;
                });
                break;

            case nameof(AddressRangeViewModel.UseSnesAddresses):
                WriteWidgets(() =>
                {
                    radioSNES.Checked = viewModel.Range.UseSnesAddresses;
                    radioPC.Checked = !viewModel.Range.UseSnesAddresses;
                });
                break;
        }

        RefreshValidation();
    }

    private void RefreshAllWidgets()
    {
        WriteWidgets(() =>
        {
            radioSNES.Checked = viewModel.Range.UseSnesAddresses;
            radioPC.Checked = !viewModel.Range.UseSnesAddresses;
            radioHex.Checked = viewModel.Range.UseHexadecimal;
            radioDec.Checked = !viewModel.Range.UseHexadecimal;

            textStart.Text = viewModel.Range.StartText;
            textEnd.Text = viewModel.Range.EndText;
            textCount.Text = viewModel.Range.CountText;
        });

        RefreshValidation();
    }

    /// <summary>
    /// Go is available only when the ViewModel can actually build a command, and the reason it
    /// can't is attached to the byte count -- the only thing that can be wrong here. The view
    /// never re-derives validity; it only displays the answer.
    /// </summary>
    private void RefreshValidation()
    {
        var result = viewModel.Validate();

        go.Enabled = result.IsValid;
        validationErrors.SetError(textCount, result.IsValid ? null : result.Error);
    }

    // ------------------------------------------------------------------ widgets -> ViewModel

    private void textStart_TextChanged(object sender, EventArgs e) =>
        PushToViewModel(textStart, () => viewModel.Range.StartText = textStart.Text);

    private void textEnd_TextChanged(object sender, EventArgs e) =>
        PushToViewModel(textEnd, () => viewModel.Range.EndText = textEnd.Text);

    private void textCount_TextChanged(object sender, EventArgs e) =>
        PushToViewModel(textCount, () => viewModel.Range.CountText = textCount.Text);

    // both radio pairs report through the control that is losing its check as well as the one
    // gaining it, so one handler per pair covers both directions.
    private void radioROM_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(null, () => viewModel.Range.UseSnesAddresses = radioSNES.Checked);

    private void radioHex_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(null, () => viewModel.Range.UseHexadecimal = radioHex.Checked);

    private void go_Click(object sender, EventArgs e) => Finish();

    private void cancel_Click(object sender, EventArgs e) => Close();

    /// <summary>
    /// Confirm. Refused while the range holds no bytes, so neither the button nor Enter -- which
    /// clicks that same button, this form's AcceptButton -- can start a step over nothing.
    /// </summary>
    private void Finish()
    {
        if (!viewModel.CanBuildAutoStepCommand)
            return;

        DialogResult = DialogResult.OK;
    }

    // ------------------------------------------------------------------ plumbing

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

    /// <summary>
    /// Push text into a box, EXCEPT the one being typed into. Retyping a field under the user's
    /// caret fights every keystroke -- and that would happen constantly here, because a SNES
    /// address typed in a mirrored bank converts back to the canonical bank.
    /// </summary>
    private void WriteText(TextBox textBox, string text)
    {
        if (ReferenceEquals(textBox, controlBeingEdited))
            return;

        WriteWidgets(() => textBox.Text = text);
    }
}
