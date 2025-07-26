namespace Diz.Ui.Winforms.usercontrols
{
    partial class RegionListUserControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            regionGridView = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)regionGridView).BeginInit();
            SuspendLayout();
            // 
            // regionGridView
            // 
            regionGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            regionGridView.Dock = DockStyle.Fill;
            regionGridView.Location = new Point(0, 0);
            regionGridView.Name = "regionGridView";
            regionGridView.Size = new Size(583, 329);
            regionGridView.TabIndex = 0;
            // 
            // RegionListUserControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(regionGridView);
            Name = "RegionListUserControl";
            Size = new Size(583, 329);
            ((System.ComponentModel.ISupportInitialize)regionGridView).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView regionGridView;
    }
}
