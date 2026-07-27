using System.ComponentModel;
using Diz.Ui.ViewModels.Goto;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// Modal host for <see cref="GotoViewModel"/>: type a SNES address or a ROM file offset and
/// press Go. Everything that decides where "there" is -- address conversion, hex/decimal
/// parsing, label stripping, validation -- lives in the ViewModel. This file is widget wiring
/// only, and the dialog never navigates: the caller reads <c>ResultPcOffset</c> off the
/// ViewModel after a DialogResult.OK and moves the view itself.
/// </summary>
public partial class GotoDialog : Form
{
    private readonly GotoViewModel viewModel;
    private readonly bool initiallySelectSnesAddr;

    // true while widget values are being written FROM the ViewModel; the input handlers below
    // bail out then, so a ViewModel-driven refresh can't be mistaken for the user typing.
    private bool updatingWidgets;

    /// <param name="viewModel">Holds both address projections and decides which are valid.</param>
    /// <param name="initiallySelectSnesAddr">
    /// Which box's text starts out selected, so typing replaces it. True selects the SNES
    /// ADDRESS box (widget <c>textROM</c> -- legacy widget name, it holds the SNES address);
    /// false selects the ROM FILE OFFSET box (<c>textPC</c>). This is the deliberate 2026-07-26
    /// un-inversion of a legacy quirk (the flag used to mean the opposite of its name); see
    /// <see cref="Diz.Controllers.interfaces.IGotoView"/> for the contract.
    /// </param>
    public GotoDialog(GotoViewModel viewModel, bool initiallySelectSnesAddr = true)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        this.viewModel = viewModel;
        this.initiallySelectSnesAddr = initiallySelectSnesAddr;

        InitializeComponent();

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        FormClosed += (_, _) => viewModel.PropertyChanged -= ViewModel_PropertyChanged;

        RefreshAllWidgets();
    }

    private void GotoDialog_Load(object sender, EventArgs e)
    {
        if (initiallySelectSnesAddr)
            textROM.SelectAll();
        else
            textPC.SelectAll();

        RefreshValidation();
    }

    // ------------------------------------------------------------------ ViewModel -> widgets

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(GotoViewModel.SnesText):
                WriteText(textROM, viewModel.SnesText);
                break;

            case nameof(GotoViewModel.PcText):
                WriteText(textPC, viewModel.PcText);
                break;

            case nameof(GotoViewModel.UseHexadecimal):
                WriteWidgets(() =>
                {
                    radioHex.Checked = viewModel.UseHexadecimal;
                    radioDec.Checked = !viewModel.UseHexadecimal;
                });
                break;
        }

        RefreshValidation();
    }

    private void RefreshAllWidgets()
    {
        WriteWidgets(() =>
        {
            textROM.Text = viewModel.SnesText;
            textPC.Text = viewModel.PcText;
            radioHex.Checked = viewModel.UseHexadecimal;
            radioDec.Checked = !viewModel.UseHexadecimal;
        });

        RefreshValidation();
    }

    /// <summary>
    /// Go is available only while the ViewModel names a real destination, and the reason it
    /// doesn't is shown verbatim. The view never re-derives validity; it only displays the
    /// answer.
    /// </summary>
    private void RefreshValidation()
    {
        go.Enabled = viewModel.CanConfirm;
        lblError.Text = viewModel.ValidationMessage;
    }

    // ------------------------------------------------------------------ widgets -> ViewModel

    private void textROM_TextChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.SnesText = textROM.Text);

    private void textPC_TextChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.PcText = textPC.Text);

    // the radio pair reports through the button losing its check as well as the one gaining
    // it, so this one handler covers both directions.
    private void radioHex_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.UseHexadecimal = radioHex.Checked);

    private void go_Click(object sender, EventArgs e) => Finish();

    private void cancel_Click(object sender, EventArgs e) => Close();

    private void textROM_KeyDown(object sender, KeyEventArgs e) => OnTextKeydown(e);

    private void textPC_KeyDown(object sender, KeyEventArgs e) => OnTextKeydown(e);

    private void OnTextKeydown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
            Finish();
    }

    /// <summary>
    /// Confirm. Refused while the boxes don't name a place to go, so Enter can't act on a
    /// destination the user never successfully typed.
    /// </summary>
    private void Finish()
    {
        if (!viewModel.CanConfirm)
            return;

        DialogResult = DialogResult.OK;
    }

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

    /// <summary>
    /// Push text into a box, INCLUDING the one being typed into. That is deliberate here: the
    /// ViewModel already withholds the notification for text it accepted unchanged, so the only
    /// time the edited box is written is when accepting rewrote it -- pasting the label
    /// "CODE_C012AB" leaves "C012AB" behind -- and that rewrite has to be visible.
    /// </summary>
    private void WriteText(TextBox textBox, string text) => WriteWidgets(() => textBox.Text = text);
}
