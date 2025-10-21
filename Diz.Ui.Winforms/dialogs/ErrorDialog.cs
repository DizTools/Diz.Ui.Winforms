namespace Diz.Ui.Winforms.dialogs;

public class ErrorDialog : Form
{
    private readonly Button okButton;

    public ErrorDialog(string errorMessage, string caption = "Error")
    {
        InitializeComponent();
        
        Text = caption;
        Icon = SystemIcons.Error;
        BackColor = SystemColors.Control;

        var iconPictureBox = new PictureBox
        {
            Image = SystemIcons.Error.ToBitmap(),
            Location = new Point(12, 12),
            Size = new Size(32, 32),
            SizeMode = PictureBoxSizeMode.StretchImage
        };

        // Create multiline textbox for error message
        var errorTextBox1 = new TextBox
        {
            Location = new Point(60, 12),
            Size = new Size(418, 200),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = errorMessage,
            Font = new Font("Consolas", 9F, FontStyle.Regular), // Monospace font for better readability
            BackColor = Color.White
        };

        // Create OK button
        okButton = new Button
        {
            Location = new Point(403, 230),
            Size = new Size(75, 23),
            Text = "OK",
            UseVisualStyleBackColor = true,
            DialogResult = DialogResult.OK
        };

        // Add event handlers
        okButton.Click += (_, _) => Close();
        AcceptButton = okButton; // Allow Enter key to close dialog
        
        Controls.AddRange(iconPictureBox, errorTextBox1, okButton);
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        
        // Form properties
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(500, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;  // This ensures it shows in taskbar
        StartPosition = FormStartPosition.CenterParent;
        
        ResumeLayout(false);
    }

    public static DialogResult ShowError(string errorMessage, string caption = "Error")
    {
        using var errorDialog = new ErrorDialog(errorMessage, caption);
        return errorDialog.ShowDialog();
    }
    
    public static DialogResult ShowError(IWin32Window owner, string errorMessage, string caption = "Error")
    {
        using var errorDialog = new ErrorDialog(errorMessage, caption);
        return errorDialog.ShowDialog(owner);
    }
}