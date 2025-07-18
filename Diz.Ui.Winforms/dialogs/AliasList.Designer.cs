namespace Diz.Ui.Winforms.dialogs;

partial class AliasList
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        labelsViewControl1 = new Diz.Ui.Winforms.usercontrols.LabelsViewControl();
        SuspendLayout();
        // labelsViewControl1
        // 
        labelsViewControl1.Dock = DockStyle.Fill;
        labelsViewControl1.Location = new Point(0, 0);
        labelsViewControl1.Margin = new Padding(4, 3, 4, 3);
        labelsViewControl1.MinimumSize = new Size(250, 282);
        labelsViewControl1.Name = "labelsViewControl1";
        labelsViewControl1.Size = new Size(757, 551);
        labelsViewControl1.TabIndex = 0;
        // 
        // AliasList
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(757, 551);
        Controls.Add(labelsViewControl1);
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        Margin = new Padding(4, 3, 4, 3);
        MinimumSize = new Size(250, 282);
        Name = "AliasList";
        ShowIcon = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Label List";
        FormClosing += AliasList_FormClosing;
        ResumeLayout(false);
    }

    #endregion
    private usercontrols.LabelsViewControl labelsViewControl1;
}