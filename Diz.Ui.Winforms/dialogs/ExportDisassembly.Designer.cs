using System.ComponentModel;

namespace Diz.Ui.Winforms.dialogs
{
    partial class LogCreatorSettingsEditorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            ComponentResourceManager resources = new ComponentResourceManager(typeof(LogCreatorSettingsEditorForm));
            cancel = new Button();
            disassembleButton = new Button();
            textFormat = new TextBox();
            textSample = new TextBox();
            comboStructure = new ComboBox();
            comboUnlabeled = new ComboBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            numData = new NumericUpDown();
            chkNewLine = new CheckBox();
            chkOutputExtraWhitespace = new CheckBox();
            chkGenerateFullLine = new CheckBox();
            chkPrintLabelSpecificComments = new CheckBox();
            chkIncludeUnusedLabels = new CheckBox();
            saveLogSingleFile = new SaveFileDialog();
            chooseLogFolder = new FolderBrowserDialog();
            label7 = new Label();
            txtExportPath = new TextBox();
            btnBrowseOutputPath = new Button();
            label8 = new Label();
            chkGeneratePlusMinusLabels = new CheckBox();
            ((ISupportInitialize)numData).BeginInit();
            SuspendLayout();
            // 
            // cancel
            // 
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Location = new Point(14, 648);
            cancel.Margin = new Padding(4, 3, 4, 3);
            cancel.Name = "cancel";
            cancel.Size = new Size(88, 27);
            cancel.TabIndex = 20;
            cancel.TabStop = false;
            cancel.Text = "Cancel";
            cancel.UseVisualStyleBackColor = true;
            cancel.Click += cancel_Click;
            // 
            // disassembleButton
            // 
            disassembleButton.Location = new Point(762, 648);
            disassembleButton.Margin = new Padding(4, 3, 4, 3);
            disassembleButton.Name = "disassembleButton";
            disassembleButton.Size = new Size(132, 27);
            disassembleButton.TabIndex = 21;
            disassembleButton.Text = "Start Export!";
            disassembleButton.UseVisualStyleBackColor = true;
            disassembleButton.Click += disassembleButton_Click;
            // 
            // textFormat
            // 
            textFormat.Location = new Point(103, 228);
            textFormat.Margin = new Padding(4, 3, 4, 3);
            textFormat.Name = "textFormat";
            textFormat.Size = new Size(791, 23);
            textFormat.TabIndex = 17;
            textFormat.TextChanged += textFormat_TextChanged;
            // 
            // textSample
            // 
            textSample.Font = new Font("Courier New", 8.25F);
            textSample.Location = new Point(14, 284);
            textSample.Margin = new Padding(4, 3, 4, 3);
            textSample.Multiline = true;
            textSample.Name = "textSample";
            textSample.ReadOnly = true;
            textSample.ScrollBars = ScrollBars.Both;
            textSample.Size = new Size(881, 358);
            textSample.TabIndex = 19;
            textSample.TabStop = false;
            textSample.WordWrap = false;
            // 
            // comboStructure
            // 
            comboStructure.FormattingEnabled = true;
            comboStructure.Items.AddRange(new object[] { "All in one file", "One bank per file" });
            comboStructure.Location = new Point(754, 39);
            comboStructure.Margin = new Padding(4, 3, 4, 3);
            comboStructure.Name = "comboStructure";
            comboStructure.Size = new Size(140, 23);
            comboStructure.TabIndex = 4;
            comboStructure.SelectedIndexChanged += comboStructure_SelectedIndexChanged;
            // 
            // comboUnlabeled
            // 
            comboUnlabeled.FormattingEnabled = true;
            comboUnlabeled.Items.AddRange(new object[] { "Create All", "In points only", "None" });
            comboUnlabeled.Location = new Point(754, 12);
            comboUnlabeled.Margin = new Padding(4, 3, 4, 3);
            comboUnlabeled.Name = "comboUnlabeled";
            comboUnlabeled.Size = new Size(140, 23);
            comboUnlabeled.TabIndex = 2;
            comboUnlabeled.SelectedIndexChanged += comboUnlabeled_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(8, 231);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(87, 15);
            label1.TabIndex = 16;
            label1.Text = "Output format:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(8, 265);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(90, 15);
            label2.TabIndex = 18;
            label2.Text = "Sample Output:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(613, 69);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(132, 15);
            label3.TabIndex = 5;
            label3.Text = "Max data bytes per line:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(659, 42);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(86, 15);
            label4.TabIndex = 3;
            label4.Text = "Bank structure:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(619, 15);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new Size(128, 15);
            label5.TabIndex = 1;
            label5.Text = "Unlabeled instructions:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label6.Location = new Point(14, 15);
            label6.Margin = new Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new Size(349, 75);
            label6.TabIndex = 0;
            label6.Text = resources.GetString("label6.Text");
            // 
            // numData
            // 
            numData.Location = new Point(753, 67);
            numData.Margin = new Padding(4, 3, 4, 3);
            numData.Maximum = new decimal(new int[] { 16, 0, 0, 0 });
            numData.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numData.Name = "numData";
            numData.Size = new Size(44, 23);
            numData.TabIndex = 6;
            numData.Value = new decimal(new int[] { 8, 0, 0, 0 });
            numData.ValueChanged += numData_ValueChanged;
            // 
            // chkNewLine
            // 
            chkNewLine.AutoSize = true;
            chkNewLine.Location = new Point(25, 97);
            chkNewLine.Margin = new Padding(4, 3, 4, 3);
            chkNewLine.Name = "chkNewLine";
            chkNewLine.Size = new Size(181, 19);
            chkNewLine.TabIndex = 7;
            chkNewLine.Text = "Print labels on their own lines";
            chkNewLine.UseVisualStyleBackColor = true;
            chkNewLine.CheckedChanged += chkNewLine_CheckedChanged;
            // 
            // chkOutputExtraWhitespace
            // 
            chkOutputExtraWhitespace.AutoSize = true;
            chkOutputExtraWhitespace.Checked = true;
            chkOutputExtraWhitespace.CheckState = CheckState.Checked;
            chkOutputExtraWhitespace.Location = new Point(281, 97);
            chkOutputExtraWhitespace.Margin = new Padding(4, 3, 4, 3);
            chkOutputExtraWhitespace.Name = "chkOutputExtraWhitespace";
            chkOutputExtraWhitespace.Size = new Size(249, 19);
            chkOutputExtraWhitespace.TabIndex = 8;
            chkOutputExtraWhitespace.Text = "Output extra whitespace in assembly code";
            chkOutputExtraWhitespace.UseVisualStyleBackColor = true;
            chkOutputExtraWhitespace.CheckedChanged += chkOutputExtraWhitespace_CheckedChanged;
            // 
            // chkGenerateFullLine
            // 
            chkGenerateFullLine.AutoSize = true;
            chkGenerateFullLine.Checked = true;
            chkGenerateFullLine.CheckState = CheckState.Checked;
            chkGenerateFullLine.Location = new Point(281, 114);
            chkGenerateFullLine.Margin = new Padding(4, 3, 4, 3);
            chkGenerateFullLine.Name = "chkGenerateFullLine";
            chkGenerateFullLine.Size = new Size(198, 19);
            chkGenerateFullLine.TabIndex = 9;
            chkGenerateFullLine.Text = "Generate full line on special lines";
            chkGenerateFullLine.UseVisualStyleBackColor = true;
            chkGenerateFullLine.CheckedChanged += chkGenerateFullLine_CheckedChanged;
            // 
            // chkPrintLabelSpecificComments
            // 
            chkPrintLabelSpecificComments.AutoSize = true;
            chkPrintLabelSpecificComments.Checked = true;
            chkPrintLabelSpecificComments.CheckState = CheckState.Checked;
            chkPrintLabelSpecificComments.Location = new Point(577, 97);
            chkPrintLabelSpecificComments.Margin = new Padding(4, 3, 4, 3);
            chkPrintLabelSpecificComments.Name = "chkPrintLabelSpecificComments";
            chkPrintLabelSpecificComments.Size = new Size(255, 19);
            chkPrintLabelSpecificComments.TabIndex = 10;
            chkPrintLabelSpecificComments.Text = "Print label-specific comments in labels.asm";
            chkPrintLabelSpecificComments.UseVisualStyleBackColor = true;
            chkPrintLabelSpecificComments.CheckedChanged += chkPrintLabelSpecificComments_CheckedChanged;
            // 
            // chkIncludeUnusedLabels
            // 
            chkIncludeUnusedLabels.AutoSize = true;
            chkIncludeUnusedLabels.Location = new Point(577, 114);
            chkIncludeUnusedLabels.Margin = new Padding(4, 3, 4, 3);
            chkIncludeUnusedLabels.Name = "chkIncludeUnusedLabels";
            chkIncludeUnusedLabels.Size = new Size(320, 19);
            chkIncludeUnusedLabels.TabIndex = 11;
            chkIncludeUnusedLabels.Text = "optional: Generate other label metadata (.txt, .csv, .sym)";
            chkIncludeUnusedLabels.UseVisualStyleBackColor = true;
            chkIncludeUnusedLabels.CheckedChanged += chkIncludeUnusedLabels_CheckedChanged;
            // 
            // saveLogSingleFile
            // 
            saveLogSingleFile.Filter = "Assembly Files|*.asm|All Files|*.*";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(13, 171);
            label7.Margin = new Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new Size(57, 45);
            label7.TabIndex = 12;
            label7.Text = "Output \r\ndirectory \r\nor file:";
            // 
            // txtExportPath
            // 
            txtExportPath.Location = new Point(103, 172);
            txtExportPath.Margin = new Padding(4, 3, 4, 3);
            txtExportPath.Name = "txtExportPath";
            txtExportPath.Size = new Size(717, 23);
            txtExportPath.TabIndex = 13;
            txtExportPath.TextChanged += txtExportPath_TextChanged;
            // 
            // btnBrowseOutputPath
            // 
            btnBrowseOutputPath.Location = new Point(819, 172);
            btnBrowseOutputPath.Name = "btnBrowseOutputPath";
            btnBrowseOutputPath.Size = new Size(75, 24);
            btnBrowseOutputPath.TabIndex = 14;
            btnBrowseOutputPath.Text = "Browse...";
            btnBrowseOutputPath.UseVisualStyleBackColor = true;
            btnBrowseOutputPath.Click += btnBrowseOutputPath_Click;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(103, 198);
            label8.Margin = new Padding(4, 0, 4, 0);
            label8.Name = "label8";
            label8.Size = new Size(482, 15);
            label8.TabIndex = 15;
            label8.Text = "(By default, path is relative to the project file's directory. it will be created if it doesn't exist)";
            // 
            // chkGeneratePlusMinusLabels
            // 
            chkGeneratePlusMinusLabels.AutoSize = true;
            chkGeneratePlusMinusLabels.Checked = true;
            chkGeneratePlusMinusLabels.CheckState = CheckState.Checked;
            chkGeneratePlusMinusLabels.Location = new Point(25, 114);
            chkGeneratePlusMinusLabels.Margin = new Padding(4, 3, 4, 3);
            chkGeneratePlusMinusLabels.Name = "chkGeneratePlusMinusLabels";
            chkGeneratePlusMinusLabels.Size = new Size(155, 19);
            chkGeneratePlusMinusLabels.TabIndex = 22;
            chkGeneratePlusMinusLabels.Text = "Generate +/- local labels";
            chkGeneratePlusMinusLabels.UseVisualStyleBackColor = true;
            chkGeneratePlusMinusLabels.CheckedChanged += chkGeneratePlusMinusLabels_CheckedChanged;
            // 
            // LogCreatorSettingsEditorForm
            // 
            AcceptButton = disassembleButton;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancel;
            ClientSize = new Size(907, 687);
            Controls.Add(chkGeneratePlusMinusLabels);
            Controls.Add(label8);
            Controls.Add(btnBrowseOutputPath);
            Controls.Add(label7);
            Controls.Add(txtExportPath);
            Controls.Add(chkNewLine);
            Controls.Add(chkOutputExtraWhitespace);
            Controls.Add(chkGenerateFullLine);
            Controls.Add(chkIncludeUnusedLabels);
            Controls.Add(chkPrintLabelSpecificComments);
            Controls.Add(numData);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(comboUnlabeled);
            Controls.Add(comboStructure);
            Controls.Add(textSample);
            Controls.Add(textFormat);
            Controls.Add(disassembleButton);
            Controls.Add(cancel);
            Controls.Add(label6);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            Name = "LogCreatorSettingsEditorForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Export Disassembly";
            ((ISupportInitialize)numData).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private Button cancel;
        private Button disassembleButton;
        private TextBox textFormat;
        private TextBox textSample;
        private ComboBox comboStructure;
        private ComboBox comboUnlabeled;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private NumericUpDown numData;
        private CheckBox chkNewLine;
        private CheckBox chkOutputExtraWhitespace;
        private CheckBox chkGenerateFullLine;
        private CheckBox chkPrintLabelSpecificComments;
        private CheckBox chkIncludeUnusedLabels;
        private SaveFileDialog saveLogSingleFile;
        private FolderBrowserDialog chooseLogFolder;
        private Label label7;
        private TextBox txtExportPath;
        private Button btnBrowseOutputPath;
        private Label label8;
        private CheckBox chkGeneratePlusMinusLabels;
    }
}