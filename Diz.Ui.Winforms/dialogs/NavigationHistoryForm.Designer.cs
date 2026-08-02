
using Diz.Ui.Winforms.usercontrols;

namespace Diz.Ui.Winforms.dialogs;

partial class NavigationHistoryForm
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
        this.navigationCtrl = new NavigationHistoryViewControl();
        this.SuspendLayout();
        // 
        // navigationCtrl
        // 
        this.navigationCtrl.Dock = System.Windows.Forms.DockStyle.Fill;
        this.navigationCtrl.Location = new System.Drawing.Point(0, 0);
        this.navigationCtrl.Name = "navigationCtrl";
        this.navigationCtrl.Size = new System.Drawing.Size(404, 450);
        this.navigationCtrl.TabIndex = 0;
        // 
        // NavigationHistoryForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(404, 450);
        this.Controls.Add(this.navigationCtrl);
        this.Name = "NavigationHistoryForm";
        this.Text = "Navigation";
        this.ResumeLayout(false);

    }

    #endregion

    private NavigationHistoryViewControl navigationCtrl;
}