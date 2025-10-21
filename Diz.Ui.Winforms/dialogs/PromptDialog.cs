using Diz.Ui.Winforms.util;

namespace Diz.Ui.Winforms.dialogs;

public partial class PromptDialog : Form
{
    private DialogResult dialogResult = DialogResult.Cancel;

    private PromptDialog()
    {
        InitializeComponent();
        this.BringWinFormToTop();
    }

    public static DialogResult Show(string message, string title = "Message", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information)
    {
        using var dialog = new PromptDialog();
        dialog.Text = title;
        dialog.SetMessage(message);
        dialog.SetButtons(buttons);
        dialog.SetIcon(icon);

        return dialog.ShowDialog();
    }

    private void SetMessage(string message)
    {
        var normalizedMessage = message.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
        txtMessage.Text = normalizedMessage;
    }

    private void SetButtons(MessageBoxButtons buttons)
    {
        // Hide all buttons initially
        btnOk.Visible = false;
        btnYes.Visible = false;
        btnNo.Visible = false;
        ResetButtons();

        switch (buttons)
        {
            case MessageBoxButtons.OK:
                ConfigureSingleButton(btnOk, "OK", DialogResult.OK);
                AcceptButton = btnOk;
                break;

            case MessageBoxButtons.OKCancel:
                ConfigureTwoButtons(btnYes, "OK", DialogResult.OK, btnNo, "Cancel", DialogResult.Cancel);
                AcceptButton = btnYes;
                CancelButton = btnNo;
                break;

            case MessageBoxButtons.YesNo:
                ConfigureTwoButtons(btnYes, "Yes", DialogResult.Yes, btnNo, "No", DialogResult.No);
                AcceptButton = btnYes;
                CancelButton = btnNo;
                break;

            case MessageBoxButtons.YesNoCancel:
                ConfigureThreeButtons(btnOk, "Yes", DialogResult.Yes, btnYes, "No", DialogResult.No, btnNo, "Cancel", DialogResult.Cancel);
                AcceptButton = btnOk;
                CancelButton = btnNo;
                break;

            case MessageBoxButtons.RetryCancel:
                ConfigureTwoButtons(btnYes, "Retry", DialogResult.Retry, btnNo, "Cancel", DialogResult.Cancel);
                AcceptButton = btnYes;
                CancelButton = btnNo;
                break;

            case MessageBoxButtons.AbortRetryIgnore:
                ConfigureThreeButtons(btnOk, "Abort", DialogResult.Abort, btnYes, "Retry", DialogResult.Retry, btnNo, "Ignore", DialogResult.Ignore);
                AcceptButton = btnYes;
                CancelButton = btnOk;
                break;

            case MessageBoxButtons.CancelTryContinue:
                ConfigureThreeButtons(btnOk, "Cancel", DialogResult.Cancel, btnYes, "Try Again", DialogResult.TryAgain, btnNo, "Continue", DialogResult.Continue);
                AcceptButton = btnYes;
                CancelButton = btnOk;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null);
        }
    }

    private void ResetButtons()
    {
        btnOk.Text = "OK";
        btnYes.Text = "Yes";
        btnNo.Text = "No";
        btnOk.Tag = DialogResult.OK;
        btnYes.Tag = DialogResult.Yes;
        btnNo.Tag = DialogResult.No;
    }

    private void ConfigureSingleButton(Button button, string text, DialogResult result)
    {
        button.Visible = true;
        button.Text = text;
        button.Tag = result;
        button.Location = button.Location with { X = Width / 2 - button.Width / 2 };
    }

    private void ConfigureTwoButtons(Button button1, string text1, DialogResult result1, Button button2, string text2, DialogResult result2)
    {
        button1.Visible = true;
        button2.Visible = true;
        
        button1.Text = text1;
        button1.Tag = result1;
        button2.Text = text2;
        button2.Tag = result2;

        var spacing = 10;
        var totalWidth = button1.Width + button2.Width + spacing;
        var startX = (Width - totalWidth) / 2;

        button1.Location = button1.Location with { X = startX };
        button2.Location = button2.Location with { X = startX + button1.Width + spacing };
    }

    private void ConfigureThreeButtons(Button button1, string text1, DialogResult result1, 
        Button button2, string text2, DialogResult result2, 
        Button button3, string text3, DialogResult result3)
    {
        button1.Visible = true;
        button2.Visible = true;
        button3.Visible = true;
        
        button1.Text = text1;
        button1.Tag = result1;
        button2.Text = text2;
        button2.Tag = result2;
        button3.Text = text3;
        button3.Tag = result3;

        const int spacing = 10;
        var totalWidth = button1.Width + button2.Width + button3.Width + (spacing * 2);
        var startX = (Width - totalWidth) / 2;

        button1.Location = button1.Location with { X = startX };
        button2.Location = button2.Location with { X = startX + button1.Width + spacing };
        button3.Location = button3.Location with { X = startX + button1.Width + button2.Width + (spacing * 2) };
    }

    private void SetIcon(MessageBoxIcon icon)
    {
        var systemIcon = icon switch
        {
            MessageBoxIcon.Error => SystemIcons.Error,
            MessageBoxIcon.Warning => SystemIcons.Warning,
            MessageBoxIcon.Information => SystemIcons.Information,
            MessageBoxIcon.Question => SystemIcons.Question,
            _ => SystemIcons.Information
        };

        picIcon.Image = systemIcon.ToBitmap();
    }

    private void btnOk_Click(object sender, EventArgs e)
    {
        dialogResult = (DialogResult)(btnOk.Tag ?? DialogResult.OK);
        DialogResult = dialogResult;
        Close();
    }

    private void btnYes_Click(object sender, EventArgs e)
    {
        dialogResult = (DialogResult)(btnYes.Tag ?? DialogResult.Yes);
        DialogResult = dialogResult;
        Close();
    }

    private void btnNo_Click(object sender, EventArgs e)
    {
        dialogResult = (DialogResult)(btnNo.Tag ?? DialogResult.No);
        DialogResult = dialogResult;
        Close();
    }
}