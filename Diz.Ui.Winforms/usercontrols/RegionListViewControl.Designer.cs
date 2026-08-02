namespace Diz.Ui.Winforms.usercontrols
{
    partial class RegionListViewControl
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
            problemsPanel = new Panel();
            problemsList = new ListBox();
            problemsToggle = new Button();
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            toolbarPanel = new Panel();
            addRegionButton = new Button();
            ((System.ComponentModel.ISupportInitialize)regionGridView).BeginInit();
            problemsPanel.SuspendLayout();
            statusStrip.SuspendLayout();
            toolbarPanel.SuspendLayout();
            SuspendLayout();
            //
            // regionGridView
            //
            regionGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            regionGridView.Dock = DockStyle.Fill;
            regionGridView.Location = new Point(0, 30);
            regionGridView.Name = "regionGridView";
            regionGridView.Size = new Size(583, 155);
            regionGridView.TabIndex = 1;
            //
            // problemsList
            //
            problemsList.Dock = DockStyle.Fill;
            problemsList.FormattingEnabled = true;
            problemsList.IntegralHeight = false;
            problemsList.Location = new Point(0, 24);
            problemsList.Name = "problemsList";
            problemsList.SelectionMode = SelectionMode.One;
            problemsList.Size = new Size(583, 96);
            problemsList.TabIndex = 1;
            //
            // problemsToggle
            //
            problemsToggle.Dock = DockStyle.Top;
            problemsToggle.FlatStyle = FlatStyle.System;
            problemsToggle.Location = new Point(0, 0);
            problemsToggle.Name = "problemsToggle";
            problemsToggle.Size = new Size(583, 24);
            problemsToggle.TabIndex = 0;
            problemsToggle.Text = "Problems (0)";
            problemsToggle.TextAlign = ContentAlignment.MiddleLeft;
            problemsToggle.UseVisualStyleBackColor = true;
            //
            // problemsPanel
            //
            problemsPanel.Controls.Add(problemsList);
            problemsPanel.Controls.Add(problemsToggle);
            problemsPanel.Dock = DockStyle.Bottom;
            problemsPanel.Location = new Point(0, 185);
            problemsPanel.Name = "problemsPanel";
            problemsPanel.Size = new Size(583, 120);
            problemsPanel.TabIndex = 2;
            //
            // statusLabel
            //
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(0, 17);
            statusLabel.Text = "";
            //
            // statusStrip
            //
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
            statusStrip.Location = new Point(0, 305);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(583, 24);
            statusStrip.SizingGrip = false;
            statusStrip.TabIndex = 3;
            //
            // addRegionButton
            //
            addRegionButton.Dock = DockStyle.Left;
            addRegionButton.FlatStyle = FlatStyle.System;
            addRegionButton.Location = new Point(0, 2);
            addRegionButton.Name = "addRegionButton";
            addRegionButton.Size = new Size(110, 26);
            addRegionButton.TabIndex = 0;
            addRegionButton.Text = "Add Region";
            addRegionButton.UseVisualStyleBackColor = true;
            //
            // toolbarPanel
            //
            toolbarPanel.Controls.Add(addRegionButton);
            toolbarPanel.Dock = DockStyle.Top;
            toolbarPanel.Location = new Point(0, 0);
            toolbarPanel.Name = "toolbarPanel";
            toolbarPanel.Padding = new Padding(0, 2, 0, 2);
            toolbarPanel.Size = new Size(583, 30);
            toolbarPanel.TabIndex = 0;
            //
            // RegionListViewControl
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            // Docking is applied in reverse of the order controls are added here: the grid goes in
            // FIRST so it is docked LAST and takes whatever space the edge-docked strips leave.
            Controls.Add(regionGridView);
            Controls.Add(problemsPanel);
            Controls.Add(statusStrip);
            Controls.Add(toolbarPanel);
            Name = "RegionListViewControl";
            Size = new Size(583, 329);
            ((System.ComponentModel.ISupportInitialize)regionGridView).EndInit();
            problemsPanel.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            toolbarPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView regionGridView;
        private Panel problemsPanel;
        private ListBox problemsList;
        private Button problemsToggle;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private Panel toolbarPanel;
        private Button addRegionButton;
    }
}
