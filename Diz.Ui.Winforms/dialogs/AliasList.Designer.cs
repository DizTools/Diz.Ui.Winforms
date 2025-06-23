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
        DataGridViewCellStyle dataGridViewCellStyle8 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle7 = new DataGridViewCellStyle();
        openFileDialog1 = new OpenFileDialog();
        saveFileDialog1 = new SaveFileDialog();
        dataGridView1 = new DataGridView();
        Address = new DataGridViewTextBoxColumn();
        Alias = new DataGridViewTextBoxColumn();
        Comment = new DataGridViewTextBoxColumn();
        btnJmp = new Button();
        menuStrip1 = new MenuStrip();
        dataToolStripMenuItem = new ToolStripMenuItem();
        importCSVAppendToolStripMenuItem = new ToolStripMenuItem();
        importCSVToolStripMenuItem = new ToolStripMenuItem();
        exportCSVToolStripMenuItem = new ToolStripMenuItem();
        tableLayoutPanel1 = new TableLayoutPanel();
        toolStrip1 = new ToolStrip();
        toolStripStatusLabel1 = new ToolStripLabel();
        ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
        menuStrip1.SuspendLayout();
        tableLayoutPanel1.SuspendLayout();
        toolStrip1.SuspendLayout();
        SuspendLayout();
        // 
        // openFileDialog1
        // 
        openFileDialog1.Filter = "Comma Separated Value Files|*.csv|BSNES Symbols Map|*.cpu.sym|Text Files|*.txt|All Files|*.*";
        // 
        // saveFileDialog1
        // 
        saveFileDialog1.Filter = "Comma Separated Value Files|*.csv|Text Files|*.txt|All Files|*.*";
        // 
        // dataGridView1
        // 
        dataGridView1.AllowUserToResizeColumns = false;
        dataGridView1.AllowUserToResizeRows = false;
        dataGridView1.BorderStyle = BorderStyle.None;
        dataGridView1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Address, Alias, Comment });
        dataGridViewCellStyle8.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle8.BackColor = SystemColors.Window;
        dataGridViewCellStyle8.Font = new Font("Consolas", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        dataGridViewCellStyle8.ForeColor = SystemColors.ControlText;
        dataGridViewCellStyle8.SelectionBackColor = Color.CornflowerBlue;
        dataGridViewCellStyle8.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle8.WrapMode = DataGridViewTriState.False;
        dataGridView1.DefaultCellStyle = dataGridViewCellStyle8;
        dataGridView1.Dock = DockStyle.Fill;
        dataGridView1.Location = new Point(0, 0);
        dataGridView1.Margin = new Padding(0);
        dataGridView1.Name = "dataGridView1";
        dataGridView1.RowHeadersVisible = false;
        dataGridView1.RowHeadersWidth = 4;
        dataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        dataGridView1.RowTemplate.Height = 15;
        dataGridView1.ScrollBars = ScrollBars.Vertical;
        dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGridView1.ShowCellErrors = false;
        dataGridView1.ShowCellToolTips = false;
        dataGridView1.ShowEditingIcon = false;
        dataGridView1.ShowRowErrors = false;
        dataGridView1.Size = new Size(681, 455);
        dataGridView1.TabIndex = 3;
        dataGridView1.TabStop = false;
        dataGridView1.CellBeginEdit += dataGridView1_CellBeginEdit;
        dataGridView1.CellValidating += dataGridView1_CellValidating;
        dataGridView1.UserDeletingRow += dataGridView1_UserDeletingRow;
        // 
        // Address
        // 
        dataGridViewCellStyle5.Font = new Font("Consolas", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Address.DefaultCellStyle = dataGridViewCellStyle5;
        Address.HeaderText = "SnesAddr";
        Address.MaxInputLength = 6;
        Address.Name = "Address";
        Address.Width = 80;
        // 
        // Alias
        // 
        dataGridViewCellStyle6.Font = new Font("Arial Narrow", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Alias.DefaultCellStyle = dataGridViewCellStyle6;
        Alias.HeaderText = "Name";
        Alias.MaxInputLength = 60;
        Alias.Name = "Alias";
        Alias.Width = 200;
        // 
        // Comment
        // 
        dataGridViewCellStyle7.Font = new Font("Consolas", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Comment.DefaultCellStyle = dataGridViewCellStyle7;
        Comment.HeaderText = "Comment";
        Comment.MaxInputLength = 800;
        Comment.Name = "Comment";
        Comment.Width = 1000;
        // 
        // btnJmp
        // 
        btnJmp.Location = new Point(4, 458);
        btnJmp.Margin = new Padding(4, 3, 4, 3);
        btnJmp.Name = "btnJmp";
        btnJmp.Size = new Size(74, 26);
        btnJmp.TabIndex = 0;
        btnJmp.Text = "Go To";
        btnJmp.UseVisualStyleBackColor = true;
        btnJmp.Click += jump_Click;
        // 
        // menuStrip1
        // 
        menuStrip1.Items.AddRange(new ToolStripItem[] { dataToolStripMenuItem });
        menuStrip1.Location = new Point(0, 0);
        menuStrip1.Name = "menuStrip1";
        menuStrip1.Size = new Size(681, 24);
        menuStrip1.TabIndex = 7;
        menuStrip1.Text = "menuStrip1";
        // 
        // dataToolStripMenuItem
        // 
        dataToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { importCSVAppendToolStripMenuItem, importCSVToolStripMenuItem, exportCSVToolStripMenuItem });
        dataToolStripMenuItem.Name = "dataToolStripMenuItem";
        dataToolStripMenuItem.Size = new Size(43, 20);
        dataToolStripMenuItem.Text = "Data";
        // 
        // importCSVAppendToolStripMenuItem
        // 
        importCSVAppendToolStripMenuItem.Name = "importCSVAppendToolStripMenuItem";
        importCSVAppendToolStripMenuItem.Size = new Size(199, 22);
        importCSVAppendToolStripMenuItem.Text = "Import CSV (Append) ...";
        importCSVAppendToolStripMenuItem.Click += importCSVAppendToolStripMenuItem_Click;
        // 
        // importCSVToolStripMenuItem
        // 
        importCSVToolStripMenuItem.Name = "importCSVToolStripMenuItem";
        importCSVToolStripMenuItem.Size = new Size(199, 22);
        importCSVToolStripMenuItem.Text = "Import CSV (Replace) ...";
        importCSVToolStripMenuItem.Click += importCSVToolStripMenuItem_Click;
        // 
        // exportCSVToolStripMenuItem
        // 
        exportCSVToolStripMenuItem.Name = "exportCSVToolStripMenuItem";
        exportCSVToolStripMenuItem.Size = new Size(199, 22);
        exportCSVToolStripMenuItem.Text = "Export CSV ...";
        exportCSVToolStripMenuItem.Click += exportCSVToolStripMenuItem_Click;
        // 
        // tableLayoutPanel1
        // 
        tableLayoutPanel1.ColumnCount = 1;
        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tableLayoutPanel1.Controls.Add(dataGridView1, 0, 0);
        tableLayoutPanel1.Controls.Add(btnJmp, 0, 1);
        tableLayoutPanel1.Controls.Add(toolStrip1, 0, 2);
        tableLayoutPanel1.Dock = DockStyle.Fill;
        tableLayoutPanel1.Location = new Point(0, 24);
        tableLayoutPanel1.Name = "tableLayoutPanel1";
        tableLayoutPanel1.RowCount = 3;
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 93.31896F));
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 6.68103456F));
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        tableLayoutPanel1.Size = new Size(681, 508);
        tableLayoutPanel1.TabIndex = 8;
        // 
        // toolStrip1
        // 
        toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
        toolStrip1.Location = new Point(0, 487);
        toolStrip1.Name = "toolStrip1";
        toolStrip1.Size = new Size(681, 21);
        toolStrip1.TabIndex = 4;
        toolStrip1.Text = "toolStrip1";
        // 
        // toolStripStatusLabel1
        // 
        toolStripStatusLabel1.Name = "toolStripStatusLabel1";
        toolStripStatusLabel1.Size = new Size(118, 18);
        toolStripStatusLabel1.Text = "toolStripStatusLabel1";
        // 
        // AliasList
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(681, 532);
        Controls.Add(tableLayoutPanel1);
        Controls.Add(menuStrip1);
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        MainMenuStrip = menuStrip1;
        Margin = new Padding(4, 3, 4, 3);
        MaximumSize = new Size(697, 5763);
        MinimumSize = new Size(250, 282);
        Name = "AliasList";
        ShowIcon = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Label List";
        FormClosing += AliasList_FormClosing;
        Resize += AliasList_Resize;
        ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
        menuStrip1.ResumeLayout(false);
        menuStrip1.PerformLayout();
        tableLayoutPanel1.ResumeLayout(false);
        tableLayoutPanel1.PerformLayout();
        toolStrip1.ResumeLayout(false);
        toolStrip1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();

    }

    #endregion
    private System.Windows.Forms.OpenFileDialog openFileDialog1;
    private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    private Button btnJmp;
    private DataGridView dataGridView1;
    private MenuStrip menuStrip1;
    private ToolStripMenuItem dataToolStripMenuItem;
    private ToolStripMenuItem importCSVAppendToolStripMenuItem;
    private ToolStripMenuItem importCSVToolStripMenuItem;
    private ToolStripMenuItem exportCSVToolStripMenuItem;
    private DataGridViewTextBoxColumn Address;
    private DataGridViewTextBoxColumn Alias;
    private DataGridViewTextBoxColumn Comment;
    private TableLayoutPanel tableLayoutPanel1;
    private ToolStrip toolStrip1;
    private ToolStripLabel toolStripStatusLabel1;
}