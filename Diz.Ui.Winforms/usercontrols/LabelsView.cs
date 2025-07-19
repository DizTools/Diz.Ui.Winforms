using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Diz.Controllers.controllers;
using Diz.Controllers.interfaces;
using Diz.Core.Interfaces;
using Diz.Core.model.snes;
using Diz.Core.util;
using Diz.Cpu._65816;
using Diz.Ui.Winforms.util;
using Label = Diz.Core.model.Label;

namespace Diz.Ui.Winforms.usercontrols;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class LabelsViewControl : UserControl, ILabelEditorView
{ 
    private string CurrentSearchTerm => txtSearch?.Text ?? "";
    
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IProjectController? ProjectController { get; set; }
    
    private Data? Data => ProjectController?.Project?.Data;
    private bool locked;
    private int currentlyEditing = -1;
    private DataTable? dataTable;
    
    public LabelsViewControl()
    {
        InitializeComponent();
        
        Load += AliasList_Load;
    }

    private void AliasList_Load(object? sender, EventArgs e)
    {
        SafeEndEdit();
        RepopulateFromData();
    }

    private void LabelsOnOnLabelChanged(object? sender, EventArgs e)
    {
        // this is a bit hacky and very costly for lots of labels at the moment.
        // be careful. better to replace with property notify change/etc later on.
        RepopulateFromData();
    }

    private void AliasList_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) 
            return;

        e.Cancel = true;
        Hide();
    }

    private void btnJmp_Click(object sender, EventArgs e)
    {
        if (ProjectController == null || Data == null)
            return;
        
        if (!int.TryParse(dataGridView1.SelectedRows[0].Cells[0].Value as string, NumberStyles.HexNumber, null, out var val))
            return;

        var offset = Data.ConvertSnesToPc(val);
        if (offset < 0) 
            return;
        
        ProjectController.SelectOffset(
            offset,
            new ISnesNavigation.HistoryArgs { Description = "Jump To Label" }
        );
    }

    public string PromptForCsvFilename()
    {
        var result = openFileDialog1.ShowDialog();
        return result != DialogResult.OK || openFileDialog1.FileName == ""
            ? ""
            : openFileDialog1.FileName;
    }

    public void ShowLineItemError(string exMessage, int errLine)
    {
        WinformsGuiUtil.ShowLineItemError(exMessage, errLine);
    }

    public void SetProjectController(IProjectController? projectController)
    {
        ProjectController = projectController;
    }

    private void exportCSVToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var result = saveFileDialog1.ShowDialog();
        if (result != DialogResult.OK || saveFileDialog1.FileName == "")
            return;

        var fileName = saveFileDialog1.FileName;

        try
        {
            using var sw = new StreamWriter(fileName);

            // TODO: use a better CSV output tool for this. this probably doesn't escape strings properly/etc
            WriteLabelsToCsv(sw);
        }
        catch (Exception)
        {
            MessageBox.Show("An error occurred while saving the file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void WriteLabelsToCsv(TextWriter textWriter)
    {
        if (Data?.Labels?.Labels == null)
            return;
        
        foreach (var (snesOffset, label) in Data.Labels.Labels)
        {
            OutputCsvLine(textWriter, snesOffset, label);
        }
    }

    private static void OutputCsvLine(TextWriter sw, int labelSnesAddress, IReadOnlyLabel label)
    {
        var outputLine = $"{Util.ToHexString6(labelSnesAddress)},{label.Name},{label.Comment}";
        sw.WriteLine(outputLine);
    }

    private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
    {
        if (Data?.Labels == null)
            return;
        
        // When using DataTable, we need to get the value from the underlying DataRowView
        var rowView = e.Row?.DataBoundItem as DataRowView;
        var cellValue = rowView?["Address"] as string;
    
        if (string.IsNullOrEmpty(cellValue))
            return;

        if (!int.TryParse(cellValue, NumberStyles.HexNumber, null, out var val))
            return;

        locked = true;
        Data.Labels.RemoveLabel(val);
        locked = false;
    }


    private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
    {
        currentlyEditing = e.RowIndex;

        // start by entering an address first, not the label
        if (dataGridView1.Rows[e.RowIndex].IsNewRow && e.ColumnIndex == 1)
        {
            dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[0];
        }
    }

    private void dataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
    {
        if (Data?.Labels == null)
            return;
        
        if (dataGridView1.Rows[e.RowIndex].IsNewRow)
            return;
        
        var dataBoundItem = (DataRowView?)dataGridView1.Rows[e.RowIndex].DataBoundItem;
        if (dataBoundItem == null)
            return;
        
        var dataRow = dataBoundItem.Row;
        var existingSnesAddressStr = dataRow["Address"] as string;
        var existingName = dataRow["Name"] as string;
        var existingComment = dataRow["Comment"] as string;

        int.TryParse(existingSnesAddressStr, NumberStyles.HexNumber, null, out var existingSnesAddress);

        var newLabel = new Label
        {
            Name = existingName ?? "",
            Comment = existingComment ?? ""
        };

        toolStripStatusLabel1.Text = "";
        var newSnesAddress = -1;

        switch (e.ColumnIndex)
        {
            // TODO: don't use indices, use the string column name.
            case 0: // label's address
            {
                if (!int.TryParse(e.FormattedValue?.ToString() ?? "", NumberStyles.HexNumber, null, out newSnesAddress))
                {
                    e.Cancel = true;
                    toolStripStatusLabel1.Text = "Must enter a valid hex address.";
                    break;
                }

                if (existingSnesAddress == -1 && Data.Labels.GetLabel(newSnesAddress) != null)
                {
                    e.Cancel = true;
                    toolStripStatusLabel1.Text = "This address already has a label.";
                    break;
                }

                if (dataGridView1.EditingControl != null)
                {
                    dataGridView1.EditingControl.Text = Util.ToHexString6(newSnesAddress);
                }

                break;
            }
            case 1: // label name
            {
                newSnesAddress = existingSnesAddress;
                newLabel.Name = e.FormattedValue?.ToString() ?? "";
                // todo (validate for valid label characters)
                break;
            }
            case 2: // label comment
            {
                newSnesAddress = existingSnesAddress;
                newLabel.Comment = e.FormattedValue?.ToString() ?? "";
                // todo (validate for valid comment characters, if any)
                break;
            }
        }

        locked = true;
        if (currentlyEditing >= 0)
        {
            if (newSnesAddress >= 0)
                Data.Labels.RemoveLabel(existingSnesAddress);

            Data.Labels.AddLabel(newSnesAddress, newLabel, true);
        }

        locked = false;

        currentlyEditing = -1;
    }


    public void AddRow(int snesAddress, Label label)
    {
        if (locked)
            return;
        
        RawAdd(snesAddress, label);
        dataGridView1.Invalidate();
    }

    private void RawAdd(int snesAddress, IReadOnlyLabel label)
    {
        if (dataTable == null)
            InitializeDataTable();

        dataTable?.Rows.Add(Util.ToHexString6(snesAddress), label.Name, label.Comment);
    }

    public void RemoveRow(int address)
    {
        if (locked || dataTable == null)
            return;

        var addressStr = Util.ToHexString6(address);
    
        // Find and remove the row
        for (var index = dataTable.Rows.Count - 1; index >= 0; index--)
        {
            if (dataTable.Rows[index]["Address"] as string != addressStr) 
                continue;
            
            dataTable.Rows.RemoveAt(index);
            break;
        }
    }
    
    public void ClearAndInvalidateDataGrid()
    {
        dataTable?.Clear();
        dataGridView1.Invalidate();
    }

    private void importCSVAppendToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (ProjectController == null)
            return;
        
        const string msg = "Info: Items in CSV will:\n" +
                   "1) CSV items will be added if their address doesn't already exist in this list\n" +
                   "2) CSV items will replace anything with the same address as items in the list\n" +
                   "3) any unmatched addresses in the list will be left alone\n" +
                   "\n" +
                   "Continue?\n";

        if (!PromptWarning(msg))
            return;

        ProjectController.ImportLabelsCsv(this, false);
    }

    private void importCSVToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (ProjectController == null)
            return;
        
        if (!PromptWarning("Info: All list items will be deleted and replaced with the CSV file.\n" +
                   "\n" +
                   "Continue?\n"))
            return;

        ProjectController.ImportLabelsCsv(this, true);
    }

    private static bool PromptWarning(string msg) =>
        MessageBox.Show(msg, "Warning", MessageBoxButtons.OKCancel) == DialogResult.OK;

    public void RebindProject()
    {
        if (Data?.Labels != null)
            Data.Labels.OnLabelChanged += LabelsOnOnLabelChanged;

        SafeEndEdit();
        RepopulateFromData();

        // todo: eventually use databinding/datasource, probably.
        // Todo: modify observabledictionary wrapper to avoid having to do the .Dict call here.
        // tmp disabled // Data.Labels.PropertyChanged += Labels_PropertyChanged;
        // tmp disabled // Data.Labels.CollectionChanged += Labels_CollectionChanged;
    }

    private void txtSearch_TextChanged(object sender, EventArgs e)
    {
        SafeEndEdit();
        RepopulateFromData();
    }

    private void btnClearSearch_Click(object sender, EventArgs e)
    {
        SafeEndEdit();
        txtSearch.Text = "";    // this will kick off RepopulateFromData() via event
    }
    
    private void SafeEndEdit()
    {
        // not thrilled about this implementation, but necessary to prevent silent native crashes like:
        // System.InvalidOperationException: Operation did not succeed because the program cannot commit or quit a cell value change
        
        try {
            if (dataGridView1.IsCurrentCellInEditMode)
                dataGridView1.EndEdit();
        } catch (Exception ex) {
            // If we can't end the edit normally, try to cancel it
            try {
                dataGridView1.CancelEdit();
            } catch {
                System.Diagnostics.Debug.WriteLine($"LabelView: Could not end/cancel edit (bad/weird situation now): {ex.Message}");
            }
        }
    }

    
    private void InitializeDataTable()
    {
        dataTable = new DataTable();
        dataTable.Columns.Add("Address", typeof(string));
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Comment", typeof(string));
        
        // or, we can get weird crashes. happens at startup when we're editing a row on the grid by default.
        SafeEndEdit();

        dataGridView1.DataSource = dataTable;
        
        // Configure columns AFTER binding
        if (dataGridView1.Columns.Count < 3) 
            return; // big problem.
        
        dataGridView1.Columns[0].HeaderText = "Address";
        dataGridView1.Columns[0].Width = 80;
        
        dataGridView1.Columns[1].HeaderText = "Name";
        dataGridView1.Columns[1].Width = 200;
        
        dataGridView1.Columns[2].HeaderText = "Comment";
        dataGridView1.Columns[2].Width = 1000;

        // Enable sorting
        dataGridView1.Columns[0].SortMode = DataGridViewColumnSortMode.Automatic;
        dataGridView1.Columns[1].SortMode = DataGridViewColumnSortMode.Automatic;
        dataGridView1.Columns[2].SortMode = DataGridViewColumnSortMode.Automatic;
    }

    public void Optimization_SuspendDrawing(bool suspend)
    {
        // optional: CPU optimization:
        // prevent layout calculations and repainting while we're doing large data modifications
        // greatly speeds things up, but this is all hacky as hell.
        
        if (suspend)
        {
            WinformsGuiUtil.SuspendDrawing(dataGridView1);
            dataGridView1.Enabled = false;
            dataGridView1.Visible = false;
            dataGridView1.SuspendLayout();
            SuspendLayout();
        }
        else
        {
            dataGridView1.Enabled = true;
            dataGridView1.Visible = true;
            ResumeLayout(performLayout: true);
            dataGridView1.ResumeLayout(performLayout: true);
            WinformsGuiUtil.ResumeDrawing(dataGridView1);
        }
    }
    
    public void RepopulateFromData()
    {
        if (locked)
            return;
        
        // Safety check - make sure we have data before proceeding
        if (ProjectController == null || Data?.Labels?.Labels == null)
            return;

        if (dataTable == null)
        {
            InitializeDataTable();
            if (dataTable == null)
                return;
        }

        Optimization_SuspendDrawing(true);

        dataTable.Clear();

        var labelSearchConditions = new LabelSearchTerms(CurrentSearchTerm);
        var filteredLabels = Data.Labels.Labels
            .Where(x => labelSearchConditions.DoesLabelMatch(x.Key, x.Value));
        
        foreach (var (snesAddress, label) in filteredLabels)
        {
            RawAdd(snesAddress, label);
        }
        
        var dataView = dataTable.DefaultView;
        dataView.Sort = "Address ASC";

        Optimization_SuspendDrawing(false); // restore
    }

    // TODO: get this back online again
    // private void Labels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    // {
    //     if (e.NewItems != null)
    //     {
    //         foreach (KeyValuePair<int, Label> item in e.NewItems)
    //         {
    //             AddRow(item.Key, item.Value);
    //         }
    //     }
    //
    //     if (e.OldItems == null) 
    //         return;
    //     
    //     foreach (KeyValuePair<int, Label> item in e.OldItems)
    //     {
    //         RemoveRow(item.Key);
    //     }
    // }
    //
    // // TODO: get this back online again
    //
    // private void Labels_PropertyChanged(object sender, PropertyChangedEventArgs e)
    // {
    //     // if needed, catch any changes to label content here
    // }

    public event EventHandler? OnFormClosed;

    public void Close()
    {
        OnFormClosed?.Invoke(this, EventArgs.Empty);
    }

    public void BringFormToTop()
    {
        Focus();
    }

    private void btnNewFromCurrentIA_Click(object sender, EventArgs e) => 
        FocusOrCreateLabelAtSelectedRomOffsetIa();

    public void FocusOrCreateLabelAtSelectedRomOffsetIa()
    {
        var selectedOffset = ProjectController?.ProjectView.SelectedOffset ?? -1;
        if (selectedOffset == -1)
        {
            MessageBox.Show("No offset selected in main form, or no project loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
     
        // whatever IA is selected on the main form, let's start editing a new label with that.
        FocusOrCreateLabelAtRomOffsetIa(selectedOffset);
    }

    public void FocusOrCreateLabelAtRomOffsetIa(int selectedOffset)
    {
        var snesData = Data?.GetSnesApi();
        var snesIa = snesData?.GetIntermediateAddress(selectedOffset, resolve: true) ?? -1;
        if (snesIa == -1)
        {
            MessageBox.Show(
                "You have selected a row in the main grid that has no IA (Intermediate Address). Can't proceed",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        FocusOrCreateLabelAtSnesAddress(snesIa);
    }

    public void FocusOrCreateLabelAtSnesAddress(int snesAddress)
    {
        // optional: convert mirrored WRAM labels into un-mirrored address.
        // will change i.e. $00xxxx addresses to $7Exxxx
        snesAddress = RomUtil.NormalizeSnesWramAddress(snesAddress);

        // finally clear our search
        // for the code below to work, there must be NO FILTER or we could miss rows
        if (txtSearch.Text != "")
        {
            txtSearch.Clear();
            RepopulateFromData();
        }

        // does it already exist?
        var row = FindRowWithSnesAddress(snesAddress);
        
        if (row == -1)
        {
            // if not, create it
            AddRow(snesAddress, new Label { Name = "New Label" });
            row = FindRowWithSnesAddress(snesAddress);
        }

        dataGridView1.CurrentCell = dataGridView1.Rows[row].Cells[1]; // Select the name cell for editing
        dataGridView1.BeginEdit(true);
    }

    private int FindRowWithSnesAddress(int snesAddress)
    {
        var snesAddressHexStr = Util.ToHexString6(snesAddress);

        // Search through DataGridView rows instead of DataTable rows, to bypass all filtering/etc.
        for (var i = 0; i < dataGridView1.Rows.Count; i++)
        {
            if (dataGridView1.Rows[i].IsNewRow)
                continue;

            var cellValue = dataGridView1.Rows[i].Cells[0].Value as string;
            if (cellValue != snesAddressHexStr)
                continue;

            return i;
        }

        return -1;
    }

    private void normalizeWRAMLabelsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        locked = true; // optimization, don't auto-repopulate with every little change
        ProjectController?.NormalizeWramLabels();
        locked = false;
        RepopulateFromData();
    }
}