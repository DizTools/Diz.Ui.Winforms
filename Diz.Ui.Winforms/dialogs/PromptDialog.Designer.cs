namespace Diz.Ui.Winforms.dialogs;

partial class PromptDialog
{
    private System.ComponentModel.IContainer components = null;
    private TextBox txtMessage;
    private PictureBox picIcon;
    private Button btnOk;
    private Button btnYes;
    private Button btnNo;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        txtMessage = new TextBox();
        picIcon = new PictureBox();
        btnOk = new Button();
        btnYes = new Button();
        btnNo = new Button();
        ((System.ComponentModel.ISupportInitialize)picIcon).BeginInit();
        SuspendLayout();
            
        // 
        // picIcon
        // 
        picIcon.Location = new Point(12, 12);
        picIcon.Name = "picIcon";
        picIcon.Size = new Size(32, 32);
        picIcon.SizeMode = PictureBoxSizeMode.StretchImage;
        picIcon.TabIndex = 0;
        picIcon.TabStop = false;
            
        // 
        // txtMessage
        // 
        txtMessage.BackColor = SystemColors.Control;
        txtMessage.BorderStyle = BorderStyle.None;
        txtMessage.Location = new Point(54, 12);
        txtMessage.Multiline = true;
        txtMessage.Name = "txtMessage";
        txtMessage.ReadOnly = true;
        txtMessage.ScrollBars = ScrollBars.Vertical;
        txtMessage.Size = new Size(420, 315); // Increased height from 210 to 315 (50% more)
        txtMessage.TabIndex = 1;
        txtMessage.Text = "Message";
        txtMessage.TabStop = false;
        txtMessage.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        txtMessage.WordWrap = true; // Added to fix line breaks
            
        // 
        // btnOk
        // 
        btnOk.Location = new Point(212, 345); // Moved down from 240 to 345
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(75, 23);
        btnOk.TabIndex = 2;
        btnOk.Text = "OK";
        btnOk.UseVisualStyleBackColor = true;
        btnOk.Click += btnOk_Click;
            
        // 
        // btnYes
        // 
        btnYes.Location = new Point(162, 345); // Moved down from 240 to 345
        btnYes.Name = "btnYes";
        btnYes.Size = new Size(75, 23);
        btnYes.TabIndex = 3;
        btnYes.Text = "Yes";
        btnYes.UseVisualStyleBackColor = true;
        btnYes.Click += btnYes_Click;
            
        // 
        // btnNo
        // 
        btnNo.Location = new Point(252, 345); // Moved down from 240 to 345
        btnNo.Name = "btnNo";
        btnNo.Size = new Size(75, 23);
        btnNo.TabIndex = 4;
        btnNo.Text = "No";
        btnNo.UseVisualStyleBackColor = true;
        btnNo.Click += btnNo_Click;
            
        // 
        // PromptDialog
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(500, 405); // Increased height from 300 to 405 (35% more)
        Controls.Add(btnNo);
        Controls.Add(btnYes);
        Controls.Add(btnOk);
        Controls.Add(txtMessage);
        Controls.Add(picIcon);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "PromptDialog";
        ShowIcon = true;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Dialog";
        TopMost = true;
        ((System.ComponentModel.ISupportInitialize)picIcon).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}