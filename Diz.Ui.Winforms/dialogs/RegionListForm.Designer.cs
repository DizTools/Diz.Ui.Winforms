namespace Diz.Ui.Winforms.dialogs
{
    partial class RegionListForm
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
            regionListViewControl1 = new Diz.Ui.Winforms.usercontrols.RegionListViewControl();
            SuspendLayout();
            //
            // regionListViewControl1
            //
            regionListViewControl1.Dock = DockStyle.Fill;
            regionListViewControl1.Location = new Point(0, 0);
            regionListViewControl1.Name = "regionListViewControl1";
            regionListViewControl1.Size = new Size(1000, 450);
            regionListViewControl1.TabIndex = 0;
            //
            // RegionListForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 450);
            Controls.Add(regionListViewControl1);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            MinimumSize = new Size(500, 250);
            Name = "RegionListForm";
            Text = "Regions List";
            FormClosing += RegionListForm_FormClosing;
            ResumeLayout(false);
        }

        #endregion

        private usercontrols.RegionListViewControl regionListViewControl1;
    }
}
