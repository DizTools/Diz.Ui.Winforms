namespace Diz.Ui.Winforms.usercontrols;

partial class LabelsViewControl
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
        DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
        openFileDialog1 = new OpenFileDialog();
        saveFileDialog1 = new SaveFileDialog();
        dataGridView1 = new DataGridView();
        Address = new DataGridViewTextBoxColumn();
        Alias = new DataGridViewTextBoxColumn();
        Comment = new DataGridViewTextBoxColumn();
        menuStrip1 = new MenuStrip();
        dataToolStripMenuItem = new ToolStripMenuItem();
        importCSVAppendToolStripMenuItem = new ToolStripMenuItem();
        importCSVToolStripMenuItem = new ToolStripMenuItem();
        exportCSVToolStripMenuItem = new ToolStripMenuItem();
        tableLayoutPanel1 = new TableLayoutPanel();
        toolStrip1 = new ToolStrip();
        toolStripStatusLabel1 = new ToolStripLabel();
        toolStripLabel1 = new ToolStripLabel();
        flowLayoutPanel1 = new FlowLayoutPanel();
        btnJmp = new Button();
        btnNewFromCurrentIA = new Button();
        label1 = new Label();
        txtSearch = new TextBox();
        btnClearSearch = new Button();
        toolStripSeparator1 = new ToolStripSeparator();
        normalizeWRAMLabelsToolStripMenuItem = new ToolStripMenuItem();
        ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
        menuStrip1.SuspendLayout();
        tableLayoutPanel1.SuspendLayout();
        toolStrip1.SuspendLayout();
        flowLayoutPanel1.SuspendLayout();
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

        dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle4.BackColor = SystemColors.Window;
        dataGridViewCellStyle4.Font = new Font("Consolas", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        dataGridViewCellStyle4.ForeColor = SystemColors.ControlText;
        dataGridViewCellStyle4.SelectionBackColor = Color.CornflowerBlue;
        dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
        dataGridView1.DefaultCellStyle = dataGridViewCellStyle4;
        dataGridView1.Dock = DockStyle.Fill;
        dataGridView1.Location = new Point(0, 0);
        dataGridView1.Margin = new Padding(0);
        dataGridView1.Name = "dataGridView1";
        dataGridView1.RowHeadersVisible = false;
        dataGridView1.RowHeadersWidth = 4;
        dataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        dataGridView1.RowTemplate.Height = 15;
        dataGridView1.ScrollBars = ScrollBars.Vertical;
        dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
        dataGridView1.ShowCellErrors = false;
        dataGridView1.ShowCellToolTips = false;
        dataGridView1.ShowEditingIcon = false;
        dataGridView1.ShowRowErrors = false;
        dataGridView1.Size = new Size(757, 466);
        dataGridView1.TabIndex = 3;
        dataGridView1.TabStop = false;
        dataGridView1.KeyDown += new KeyEventHandler(table_KeyDown);
        dataGridView1.CellBeginEdit += dataGridView1_CellBeginEdit;
        dataGridView1.CellValidating += dataGridView1_CellValidating;
        dataGridView1.UserDeletingRow += dataGridView1_UserDeletingRow;
        
        // 
        // menuStrip1
        // 
        menuStrip1.Items.AddRange(new ToolStripItem[] { dataToolStripMenuItem });
        menuStrip1.Location = new Point(0, 0);
        menuStrip1.Name = "menuStrip1";
        menuStrip1.Size = new Size(757, 24);
        menuStrip1.TabIndex = 7;
        menuStrip1.Text = "menuStrip1";
        // 
        // dataToolStripMenuItem
        // 
        dataToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { importCSVAppendToolStripMenuItem, importCSVToolStripMenuItem, exportCSVToolStripMenuItem, toolStripSeparator1, normalizeWRAMLabelsToolStripMenuItem });
        dataToolStripMenuItem.Name = "dataToolStripMenuItem";
        dataToolStripMenuItem.Size = new Size(43, 20);
        dataToolStripMenuItem.Text = "Data";
        // 
        // importCSVAppendToolStripMenuItem
        // 
        importCSVAppendToolStripMenuItem.Name = "importCSVAppendToolStripMenuItem";
        importCSVAppendToolStripMenuItem.Size = new Size(204, 22);
        importCSVAppendToolStripMenuItem.Text = "Import CSV (Append) ...";
        importCSVAppendToolStripMenuItem.Click += importCSVAppendToolStripMenuItem_Click;
        // 
        // importCSVToolStripMenuItem
        // 
        importCSVToolStripMenuItem.Name = "importCSVToolStripMenuItem";
        importCSVToolStripMenuItem.Size = new Size(204, 22);
        importCSVToolStripMenuItem.Text = "Import CSV (Replace) ...";
        importCSVToolStripMenuItem.Click += importCSVToolStripMenuItem_Click;
        // 
        // exportCSVToolStripMenuItem
        // 
        exportCSVToolStripMenuItem.Name = "exportCSVToolStripMenuItem";
        exportCSVToolStripMenuItem.Size = new Size(204, 22);
        exportCSVToolStripMenuItem.Text = "Export CSV ...";
        exportCSVToolStripMenuItem.Click += exportCSVToolStripMenuItem_Click;
        // 
        // tableLayoutPanel1
        // 
        tableLayoutPanel1.ColumnCount = 1;
        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tableLayoutPanel1.Controls.Add(dataGridView1, 0, 0);
        tableLayoutPanel1.Controls.Add(toolStrip1, 0, 2);
        tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 1);
        tableLayoutPanel1.Dock = DockStyle.Fill;
        tableLayoutPanel1.Location = new Point(0, 24);
        tableLayoutPanel1.Name = "tableLayoutPanel1";
        tableLayoutPanel1.RowCount = 3;
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 91.98397F));
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 8.016032F));
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        tableLayoutPanel1.Size = new Size(757, 527);
        tableLayoutPanel1.TabIndex = 8;
        // 
        // toolStrip1
        // 
        toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripLabel1 });
        toolStrip1.Location = new Point(0, 506);
        toolStrip1.Name = "toolStrip1";
        toolStrip1.Size = new Size(757, 21);
        toolStrip1.TabIndex = 4;
        toolStrip1.Text = "toolStrip1";
        // 
        // toolStripStatusLabel1
        // 
        toolStripStatusLabel1.Name = "toolStripStatusLabel1";
        toolStripStatusLabel1.Size = new Size(118, 18);
        toolStripStatusLabel1.Text = "toolStripStatusLabel1";
        // 
        // toolStripLabel1
        // 
        toolStripLabel1.Name = "toolStripLabel1";
        toolStripLabel1.Size = new Size(0, 18);
        // 
        // flowLayoutPanel1
        // 
        flowLayoutPanel1.Controls.Add(btnJmp);
        flowLayoutPanel1.Controls.Add(btnNewFromCurrentIA);
        flowLayoutPanel1.Controls.Add(label1);
        flowLayoutPanel1.Controls.Add(txtSearch);
        flowLayoutPanel1.Controls.Add(btnClearSearch);
        flowLayoutPanel1.Dock = DockStyle.Fill;
        flowLayoutPanel1.Location = new Point(3, 469);
        flowLayoutPanel1.Name = "flowLayoutPanel1";
        flowLayoutPanel1.Size = new Size(751, 34);
        flowLayoutPanel1.TabIndex = 5;
        // 
        // btnJmp
        // 
        btnJmp.Location = new Point(4, 3);
        btnJmp.Margin = new Padding(4, 3, 4, 3);
        btnJmp.Name = "btnJmp";
        btnJmp.Size = new Size(74, 26);
        btnJmp.TabIndex = 1;
        btnJmp.Text = "Go To";
        btnJmp.UseVisualStyleBackColor = true;
        btnJmp.Click += btnJmp_Click;
        // 
        // btnNewFromCurrentIA
        // 
        btnNewFromCurrentIA.Location = new Point(86, 3);
        btnNewFromCurrentIA.Margin = new Padding(4, 3, 4, 3);
        btnNewFromCurrentIA.Name = "btnNewFromCurrentIA";
        btnNewFromCurrentIA.Size = new Size(148, 26);
        btnNewFromCurrentIA.TabIndex = 4;
        btnNewFromCurrentIA.Text = "New Label From IA";
        btnNewFromCurrentIA.UseVisualStyleBackColor = true;
        btnNewFromCurrentIA.Click += btnNewFromCurrentIA_Click;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(241, 0);
        label1.Name = "label1";
        label1.Size = new Size(45, 15);
        label1.TabIndex = 2;
        label1.Text = "Search:";
        // 
        // txtSearch
        // 
        txtSearch.Location = new Point(292, 3);
        txtSearch.Name = "txtSearch";
        txtSearch.Size = new Size(344, 23);
        txtSearch.TabIndex = 3;
        txtSearch.TextChanged += txtSearch_TextChanged;
        // 
        // btnClearSearch
        // 
        btnClearSearch.Location = new Point(643, 3);
        btnClearSearch.Margin = new Padding(4, 3, 4, 3);
        btnClearSearch.Name = "btnClearSearch";
        btnClearSearch.Size = new Size(92, 26);
        btnClearSearch.TabIndex = 5;
        btnClearSearch.Text = "Clear Search";
        btnClearSearch.UseVisualStyleBackColor = true;
        btnClearSearch.Click += btnClearSearch_Click;
        // 
        // toolStripSeparator1
        // 
        toolStripSeparator1.Name = "toolStripSeparator1";
        toolStripSeparator1.Size = new Size(201, 6);
        // 
        // normalizeWRAMLabelsToolStripMenuItem
        // 
        normalizeWRAMLabelsToolStripMenuItem.Name = "normalizeWRAMLabelsToolStripMenuItem";
        normalizeWRAMLabelsToolStripMenuItem.Size = new Size(204, 22);
        normalizeWRAMLabelsToolStripMenuItem.Text = "Normalize WRAM Labels";
        normalizeWRAMLabelsToolStripMenuItem.Click += normalizeWRAMLabelsToolStripMenuItem_Click;
        // 
        // AliasList
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(757, 551);
        Controls.Add(tableLayoutPanel1);
        Controls.Add(menuStrip1);
        Margin = new Padding(4, 3, 4, 3);
        MinimumSize = new Size(250, 282);
        Name = "AliasList";
        Text = "Label List";
        ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
        menuStrip1.ResumeLayout(false);
        menuStrip1.PerformLayout();
        tableLayoutPanel1.ResumeLayout(false);
        tableLayoutPanel1.PerformLayout();
        toolStrip1.ResumeLayout(false);
        toolStrip1.PerformLayout();
        flowLayoutPanel1.ResumeLayout(false);
        flowLayoutPanel1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();

    }

    #endregion
    private System.Windows.Forms.OpenFileDialog openFileDialog1;
    private System.Windows.Forms.SaveFileDialog saveFileDialog1;
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
    private ToolStripLabel toolStripLabel1;
    private FlowLayoutPanel flowLayoutPanel1;
    private Button btnJmp;
    private Label label1;
    private Button btnNewFromCurrentIA;
    private TextBox txtSearch;
    private Button btnClearSearch;
    private ToolStripSeparator toolStripSeparator1;
    private ToolStripMenuItem normalizeWRAMLabelsToolStripMenuItem;
}