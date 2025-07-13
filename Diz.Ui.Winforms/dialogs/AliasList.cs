#nullable enable

using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Diz.Controllers.controllers;
using Diz.Controllers.interfaces;
using Diz.Core.Interfaces;
using Diz.Core.model.snes;
using Diz.Core.util;
using Diz.Cpu._65816;
using Label = Diz.Core.model.Label;

namespace Diz.Ui.Winforms.dialogs;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class AliasList : Form, ILabelEditorView
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IProjectController? ProjectController { get; set; }
    private Data Data => ProjectController?.Project?.Data!;

    private bool locked;
    private int currentlyEditing = -1;
    
    public AliasList()
    {
        Closed += (sender, args) => OnFormClosed?.Invoke(sender, args);

        InitializeComponent();
    }

    private void LabelsOnOnLabelChanged(object? sender, EventArgs e)
    {
        // this is a bit hacky and very costly for lots of labels at the moment.
        // be careful. better to replace with property notify change/etc later on.
        RepopulateFromData();
    }

    private void AliasList_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;
        e.Cancel = true;
        Hide();
    }

    private void AliasList_Resize(object sender, EventArgs e) { }

    private void btnJmp_Click(object sender, EventArgs e)
    {
        if (!int.TryParse(dataGridView1.SelectedRows[0].Cells[0].Value as string, NumberStyles.HexNumber, null, out var val))
            return;

        var offset = Data.ConvertSnesToPc(val);
        if (offset >= 0)
        {
            ProjectController!.SelectOffset(
                offset,
                new ISnesNavigation.HistoryArgs { Description = "Jump To Label" }
            );
        }
    }

    public string PromptForCsvFilename()
    {
        var result = openFileDialog1.ShowDialog();
        return result != DialogResult.OK || openFileDialog1.FileName == ""
            ? ""
            : openFileDialog1.FileName;
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

    private void WriteLabelsToCsv(TextWriter sw)
    {
        foreach (var (snesOffset, label) in Data.Labels.Labels)
        {
            OutputCsvLine(sw, snesOffset, label);
        }
    }

    private static void OutputCsvLine(TextWriter sw, int labelSnesAddress, IReadOnlyLabel label)
    {
        var outputLine = $"{Util.ToHexString6(labelSnesAddress)},{label.Name},{label.Comment}";
        sw.WriteLine(outputLine);
    }

    private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
    {
        var cellValue = e.Row != null ? (dataGridView1.Rows[e.Row.Index].Cells[0].Value as string) : "";
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
        if (dataGridView1.Rows[e.RowIndex].IsNewRow)
            return;

        var existingSnesAddressStr = dataGridView1.Rows[e.RowIndex].Cells[0].Value as string;
        var existingName = dataGridView1.Rows[e.RowIndex].Cells[1].Value as string;
        var existingComment = dataGridView1.Rows[e.RowIndex].Cells[2].Value as string;
        int.TryParse(existingSnesAddressStr, NumberStyles.HexNumber, null, out var existingSnesAddress);

        var newLabel = new Label
        {
            Name = existingName,
            Comment = existingComment
        };

        toolStripStatusLabel1.Text = "";
        var newSnesAddress = -1;

        switch (e.ColumnIndex)
        {
            case 0: // label's address
                {
                    if (!int.TryParse(e.FormattedValue?.ToString() ?? "", NumberStyles.HexNumber, null, out newSnesAddress))
                    {
                        e.Cancel = true;
                        toolStripStatusLabel1.Text = "Must enter a valid hex address.";
                        break;
                    }

                    if (existingSnesAddress == -1 && Data.Labels?.GetLabel(newSnesAddress) != null)
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
                Data.Labels?.RemoveLabel(existingSnesAddress);

            Data.Labels?.AddLabel(newSnesAddress, newLabel, true);
        }
        locked = false;

        currentlyEditing = -1;
    }

    public void AddRow(int address, Label alias)
    {
        if (locked)
            return;
        RawAdd(address, alias);
        dataGridView1.Invalidate();
    }

    private void RawAdd(int address, IReadOnlyLabel alias)
    {
        dataGridView1.Rows.Add(Util.ToHexString6(address), alias.Name, alias.Comment);
    }

    public void RemoveRow(int address)
    {
        if (locked)
            return;

        for (var index = 0; index < dataGridView1.Rows.Count; index++)
        {
            if (dataGridView1.Rows[index].Cells[0].Value as string != Util.ToHexString6(address))
                continue;

            dataGridView1.Rows.RemoveAt(index);
            dataGridView1.Invalidate();
            break;
        }
    }

    public void ClearAndInvalidateDataGrid()
    {
        dataGridView1.Rows.Clear();
        dataGridView1.Invalidate();
    }

    private void importCSVAppendToolStripMenuItem_Click(object sender, EventArgs e)
    {
        const string msg = "Info: Items in CSV will:\n" +
                   "1) CSV items will be added if their address doesn't already exist in this list\n" +
                   "2) CSV items will replace anything with the same address as items in the list\n" +
                   "3) any unmatched addresses in the list will be left alone\n" +
                   "\n" +
                   "Continue?\n";

        if (!PromptWarning(msg))
            return;

        ProjectController!.ImportLabelsCsv(this, false);
    }

    private void importCSVToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (!PromptWarning("Info: All list items will be deleted and replaced with the CSV file.\n" +
                   "\n" +
                   "Continue?\n"))
            return;

        ProjectController!.ImportLabelsCsv(this, true);
    }

    public static bool PromptWarning(string msg) =>
        MessageBox.Show(msg, "Warning", MessageBoxButtons.OKCancel) == DialogResult.OK;

    public void RebindProject()
    {
        if (Data.Labels != null)
            Data.Labels.OnLabelChanged += LabelsOnOnLabelChanged;

        RepopulateFromData();

        // todo: eventually use databinding/datasource, probably.
        // Todo: modify observabledictionary wrapper to avoid having to do the .Dict call here.
        // tmp disabled // Data.Labels.PropertyChanged += Labels_PropertyChanged;
        // tmp disabled // Data.Labels.CollectionChanged += Labels_CollectionChanged;
    }

    public string CurrentSearchTerm => txtSearch.Text;

    private void txtSearch_TextChanged(object sender, EventArgs e)
    {
        RepopulateFromData();
    }

    private void btnClearSearch_Click(object sender, EventArgs e)
    {
        txtSearch.Text = "";
        RepopulateFromData();
    }

    public void RepopulateFromData()
    {
        if (locked)
            return;

        ClearAndInvalidateDataGrid();

        var searchTerm = CurrentSearchTerm.ToUpper();
        var filteredLabels = Data.Labels.Labels
            .Where(
                x =>
                    Util.ToHexString6(x.Key).ToUpper().Contains(searchTerm) ||        // remember: search the HEX STRING or it gets weird :)
                    x.Value.Name.ToUpper().Contains(searchTerm) ||
                    x.Value.Comment.ToUpper().Contains(searchTerm)
            );

        // TODO: replace with winforms databinding eventually
        foreach (var (snesAddress, label) in filteredLabels)
        {
            RawAdd(snesAddress, label);
        }

        dataGridView1.Invalidate();

        // sort by SNES address by default. user can override this by clicking on buttons/etc
        dataGridView1.Sort(dataGridView1.Columns[0], ListSortDirection.Ascending);
    }

    public void ShowLineItemError(string msg, int errLine)
    {
        MessageBox.Show(
            "An error occurred while parsing the file.\n" + msg +
            (errLine > 0 ? $" (Check line {errLine}.)" : ""),
            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    // TODO: get this back online again

    private void Labels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (KeyValuePair<int, Label> item in e.NewItems)
            {
                AddRow(item.Key, item.Value);
            }
        }

        if (e.OldItems != null)
        {
            foreach (KeyValuePair<int, Label> item in e.OldItems)
            {
                RemoveRow(item.Key);
            }
        }
    }

    // TODO: get this back online again

    private void Labels_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // if needed, catch any changes to label content here
    }

    public event EventHandler? OnFormClosed;

    public void BringFormToTop()
    {
        Focus();
    }

    private void btnNewFromCurrentIA_Click(object sender, EventArgs e)
    {
        var snesData = Data.GetSnesApi();
        if (snesData == null || ProjectController == null)
            return;

        // whatever IA is selected on the main form, let's start editing a new label with that.
        var selectedOffset = ProjectController.ProjectView.SelectedOffset;
        if (selectedOffset == -1)
        {
            MessageBox.Show("No offset selected in main form.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var snesIa = snesData.GetIntermediateAddress(selectedOffset, resolve: true);
        if (snesIa == -1)
        {
            MessageBox.Show("You have selected a row in the main grid that has no IA (Intermediate Address). Can't proceed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // finally clear our search
        if (txtSearch.Text != "")
        {
            txtSearch.Clear();
            RepopulateFromData();
        }

        // now ready to start editing:
        var rowFound = false;
        var rowIndex = -1;

        for (var i = 0; i < dataGridView1.Rows.Count; i++)
        {
            if (dataGridView1.Rows[i].Cells[0].Value as string != Util.ToHexString6(snesIa))
                continue;

            rowFound = true;
            rowIndex = i;
            break;
        }

        if (!rowFound)
        {
            // Add a new row with the address and start editing it
            var newLabel = new Label { Name = "New Label" };
            AddRow(snesIa, newLabel);

            // Find the newly added row (should be the last one with our address)
            for (var i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Cells[0].Value as string != Util.ToHexString6(snesIa))
                    continue;

                rowIndex = i;
                break;
            }

            if (rowIndex < 0)
                return;
        }

        dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex].Cells[1]; // Select the name cell for editing
        dataGridView1.BeginEdit(true);
    }

    private void normalizeWRAMLabelsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        locked = true; // optimization, don't auto-repopulate with every little change
        ProjectController?.NormalizeWramLabels();
        locked = false;
        RepopulateFromData();
    }
}