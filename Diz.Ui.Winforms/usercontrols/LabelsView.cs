using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Diz.Controllers.controllers;
using Diz.Controllers.interfaces;
using Diz.Core.Interfaces;
using Diz.Core.model;
using Diz.Core.model.snes;
using Diz.Cpu._65816;
using Diz.Ui.ViewModels.Labels;
using Diz.Ui.Winforms.util;

namespace Diz.Ui.Winforms.usercontrols;

// Step 3 of the new-ui plan: this control renders LabelEditorViewModel (Diz.Ui.ViewModels)
// instead of owning a DataTable. All label logic (validation, filtering, sorting, address
// math, import/export, WRAM normalization) lives in the VM; this file is widget wiring.
//
// Binding: VM Rows (ReadOnlyObservableCollection) -> ObservableBindingList adapter
// (plan finding 4: WinForms can't observe INCC) -> BindingSource -> DataGridView.
// One-way. Grid edits flow back through vm.ValidateEdit/CommitEdit, never list mutation.
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class LabelsViewControl : UserControl, ILabelEditorView
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IProjectController? ProjectController { get; set; }

    // step 4: file paths come from this seam now (WinForms impl wraps the classic dialogs).
    // default = the local toolkit implementation so the control works when constructed
    // outside DI (e.g. the designer); the composition root injects the container's instance.
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IFileDialogService FileDialogService { get; set; } = new WinformsFileDialogService();

    // same filter strings the old designer-instantiated openFileDialog1/saveFileDialog1 used
    private const string LabelImportFilter =
        "Comma Separated Value Files|*.csv|BSNES Symbols Map|*.cpu.sym|Text Files|*.txt|All Files|*.*";
    private const string LabelExportFilter =
        "Comma Separated Value Files|*.csv|Text Files|*.txt|All Files|*.*";

    private Data? Data => ProjectController?.Project?.Data;

    private ILabelEditorViewModel? viewModel;
    private ObservableBindingList<ILabelRowViewModel, LabelGridRow>? gridRows;
    private readonly BindingSource bindingSource = new();
    private bool syncingSelection;

    // details panel (right side): binds straight to the model label, exactly as before.
    // grid refresh on detail edits is automatic now (label INPC -> row VM -> ItemChanged).
    private IAnnotationLabel? selectedLabel;
    private BindingList<ContextMapping>? contextMappingsBindingList;
    private bool isUpdatingContextMappings;

    public LabelsViewControl()
    {
        InitializeComponent();
        SetupGrid();
        SetupLabelDetailsPanel();
        Load += (_, _) => { if (viewModel == null) RecreateViewModel(); };
        Disposed += (_, _) => TearDownViewModel();
    }

    // ------------------------------------------------------------------ VM lifecycle

    // the VM is project-scoped (it wraps the open project's label provider), so this view
    // composes it here rather than resolving it from the DI container. the IA-resolution
    // port is the composition-layer wiring for Diz.Cpu.65816, which the VM assembly is
    // forbidden to reference.
    private void RecreateViewModel()
    {
        SafeEndEdit();
        TearDownViewModel();

        var labels = Data?.Labels;
        if (labels == null)
            return;

        viewModel = new LabelEditorViewModel(
            labels,
            notificationMarshaller: RunOnUiThread,
            resolveRomOffsetToSnesIa: romOffset =>
                Data?.GetSnesApi()?.GetIntermediateAddress(romOffset, resolve: true) ?? -1);

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        viewModel.ErrorRaised += ViewModel_ErrorRaised;
        viewModel.NavigationRequested += ViewModel_NavigationRequested;

        // preserve the current search box contents across project rebinds (old behavior)
        if (txtSearch.Text.Length != 0)
            viewModel.SearchTerm = txtSearch.Text;

        gridRows = new ObservableBindingList<ILabelRowViewModel, LabelGridRow>(
            viewModel.Rows, row => new LabelGridRow(row));

        SuspendDrawingDuring(() => bindingSource.DataSource = gridRows);
        toolStripStatusLabel1.Text = viewModel.StatusText;
    }

    private void TearDownViewModel()
    {
        if (viewModel == null)
            return;

        bindingSource.DataSource = null;
        gridRows?.Dispose();
        gridRows = null;

        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        viewModel.ErrorRaised -= ViewModel_ErrorRaised;
        viewModel.NavigationRequested -= ViewModel_NavigationRequested;
        viewModel.Dispose();
        viewModel = null;
    }

    // VM marshaller contract: synchronous when already on the UI thread (send semantics).
    // off-thread notifications only occur during VM async work (e.g. ImportLabelsAsync).
    private void RunOnUiThread(Action action)
    {
        if (IsHandleCreated && InvokeRequired)
            Invoke(action);
        else
            action();
    }

    // ------------------------------------------------------------------ grid setup

    private void SetupGrid()
    {
        dataGridView1.AutoGenerateColumns = false;
        // rows are added/removed only through VM commands (plan review: the bound list is
        // read-only). adding still works via "New Label From IA" / Ctrl+Alt+L; deleting via
        // the Delete key below.
        dataGridView1.AllowUserToAddRows = false;
        dataGridView1.AllowUserToDeleteRows = false;
        dataGridView1.AllowUserToResizeColumns = true;

        dataGridView1.Columns.AddRange(
            NewColumn("Address", nameof(LabelGridRow.Address), 80),
            NewColumn("Name", nameof(LabelGridRow.Name), 200),
            NewColumn("Comment", nameof(LabelGridRow.Comment), 200),
            NewColumn("Contexts", nameof(LabelGridRow.Context), 200, readOnly: true));

        dataGridView1.DataSource = bindingSource;

        dataGridView1.CellValidating += Grid_CellValidating;
        dataGridView1.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
        dataGridView1.SelectionChanged += Grid_SelectionChanged;
        dataGridView1.KeyDown += Grid_KeyDown;
    }

    private static DataGridViewTextBoxColumn NewColumn(
        string header, string boundProperty, int width, bool readOnly = false) => new()
    {
        HeaderText = header,
        DataPropertyName = boundProperty,
        Width = width,
        ReadOnly = readOnly,
        // plan finding 5: native sorting stays OFF; header clicks route to the VM
        SortMode = DataGridViewColumnSortMode.Programmatic,
    };

    private static LabelField? FieldForColumn(int columnIndex) => columnIndex switch
    {
        0 => LabelField.Address,
        1 => LabelField.Name,
        2 => LabelField.Comment,
        _ => null, // Contexts column: read-only, and the VM dropped context sorting (step 2)
    };

    private LabelGridRow? GridRowAt(int rowIndex) =>
        gridRows != null && rowIndex >= 0 && rowIndex < gridRows.Count ? gridRows[rowIndex] : null;

    private ILabelRowViewModel? CurrentRowViewModel() =>
        GridRowAt(dataGridView1.CurrentCell?.RowIndex ?? -1)?.Row;

    private int IndexOfRow(ILabelRowViewModel row)
    {
        for (var i = 0; i < (gridRows?.Count ?? 0); i++)
            if (ReferenceEquals(gridRows![i].Row, row))
                return i;
        return -1;
    }

    // ------------------------------------------------------------------ cell editing

    private void Grid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        // CellValidating also fires on plain cell navigation; only act on real edits
        if (!dataGridView1.IsCurrentCellInEditMode)
            return;

        var row = GridRowAt(e.RowIndex)?.Row;
        var field = FieldForColumn(e.ColumnIndex);
        if (viewModel == null || row == null || field == null)
            return;

        var proposed = e.FormattedValue?.ToString() ?? "";
        var result = viewModel.ValidateEdit(row, field.Value, proposed);
        toolStripStatusLabel1.Text = result.Error ?? "";
        if (!result.IsValid)
        {
            e.Cancel = true; // stay in edit mode, exactly like the old grid
            return;
        }

        // valid: apply AFTER the grid finishes its whole commit sequence. CommitEdit
        // mutates the bound row list (remove+add), and doing that from inside a grid edit
        // event re-enters SetCurrentCellAddressCore -- the native-crash family SafeEndEdit
        // exists to paper over. BeginInvoke posts past the entire sequence; the grid's own
        // value push-back lands in LabelGridRow's no-op setters in between.
        BeginInvoke(() => viewModel?.CommitEdit(row, field.Value, proposed));
    }

    private void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Delete || dataGridView1.IsCurrentCellInEditMode)
            return;

        var row = CurrentRowViewModel();
        if (viewModel == null || row == null)
            return;

        viewModel.DeleteLabel(row.SnesAddress);
        e.Handled = true;
    }

    // ------------------------------------------------------------------ sorting / selection

    private void Grid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        var field = FieldForColumn(e.ColumnIndex);
        if (viewModel == null || field == null)
            return;

        SafeEndEdit();
        SuspendDrawingDuring(() =>
        {
            if (viewModel.SortField == field.Value)
            {
                viewModel.SortDescending = !viewModel.SortDescending;
            }
            else
            {
                viewModel.SortField = field.Value;
                viewModel.SortDescending = false;
            }
        });

        foreach (DataGridViewColumn column in dataGridView1.Columns)
            column.HeaderCell.SortGlyphDirection = SortOrder.None;
        dataGridView1.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection =
            viewModel.SortDescending ? SortOrder.Descending : SortOrder.Ascending;
    }

    private void Grid_SelectionChanged(object? sender, EventArgs e)
    {
        if (syncingSelection)
            return;

        var row = CurrentRowViewModel();
        if (viewModel != null)
        {
            syncingSelection = true;
            viewModel.SelectedRow = row;
            syncingSelection = false;
        }
        UpdateDetailsPanelFor(row);
    }

    private void SyncGridSelectionFromViewModel()
    {
        var row = viewModel?.SelectedRow;
        if (syncingSelection || row == null)
            return;

        var index = IndexOfRow(row);
        if (index < 0 || index >= dataGridView1.Rows.Count)
            return;

        var columnIndex = dataGridView1.CurrentCell?.ColumnIndex ?? 1;
        syncingSelection = true;
        try
        {
            dataGridView1.CurrentCell = dataGridView1.Rows[index].Cells[columnIndex];
        }
        catch (InvalidOperationException)
        {
            // mid-commit; the grid will catch up on the next selection change
        }
        finally
        {
            syncingSelection = false;
        }
        UpdateDetailsPanelFor(row);
    }

    // ------------------------------------------------------------------ VM events

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ILabelEditorViewModel.StatusText):
                toolStripStatusLabel1.Text = viewModel?.StatusText ?? "";
                break;
            case nameof(ILabelEditorViewModel.SelectedRow):
                SyncGridSelectionFromViewModel();
                break;
            case nameof(ILabelEditorViewModel.SearchTerm):
                // VM-side clears (e.g. FocusOrCreate*) must reach the search box too
                if (viewModel != null && txtSearch.Text != viewModel.SearchTerm)
                    txtSearch.Text = viewModel.SearchTerm;
                break;
        }
    }

    private void ViewModel_ErrorRaised(object? sender, string message) =>
        MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private void ViewModel_NavigationRequested(object? sender, int snesAddress)
    {
        if (ProjectController == null)
            return;

        var romOffset = Data?.ConvertSnesToPc(snesAddress) ?? -1;
        if (romOffset == -1)
            return;

        ProjectController.SelectOffset(romOffset,
            new ISnesNavigation.HistoryArgs { Description = "Jump To Label" });
    }

    // ------------------------------------------------------------------ toolbar / search

    private void btnJmp_Click(object sender, EventArgs e) =>
        viewModel?.JumpToSelectedInMainView();

    private void btnNewFromCurrentIA_Click(object sender, EventArgs e) =>
        FocusOrCreateLabelAtSelectedRomOffsetIa();

    private void txtSearch_TextChanged(object sender, EventArgs e)
    {
        if (viewModel == null)
            return;
        SafeEndEdit();
        SuspendDrawingDuring(() => viewModel.SearchTerm = txtSearch.Text);
    }

    private void btnClearSearch_Click(object sender, EventArgs e)
    {
        SafeEndEdit();
        txtSearch.Text = ""; // TextChanged pushes the empty term into the VM
    }

    // ------------------------------------------------------------------ menu commands

    private async void importCSVAppendToolStripMenuItem_Click(object sender, EventArgs e)
    {
        const string msg = "Info: Items in CSV will:\n" +
                   "1) CSV items will be added if their address doesn't already exist in this list\n" +
                   "2) CSV items will replace anything with the same address as items in the list\n" +
                   "3) any unmatched addresses in the list will be left alone\n" +
                   "\n" +
                   "Continue?\n";

        await ImportLabelsCsv(msg, replaceAll: false);
    }

    private async void importCSVToolStripMenuItem_Click(object sender, EventArgs e) =>
        await ImportLabelsCsv(
            "Info: All list items will be deleted and replaced with the CSV file.\n" +
            "\n" +
            "Continue?\n",
            replaceAll: true);

    // step 4: same flow the user always saw (warning prompt -> file dialog -> import),
    // but the view obtains the path itself and hands the controller a plain string.
    // the controller no longer touches the view: it surfaces parse errors via ICommonGui
    // (identical dialog), and the VM re-syncs from provider events instead of the old
    // RepopulateFromData callback.
    private async Task ImportLabelsCsv(string warningMsg, bool replaceAll)
    {
        if (ProjectController == null)
            return;

        if (!PromptWarning(warningMsg))
            return;

        // empty title = keep the OS default ("Open"), like the old openFileDialog1
        var importFilename = await FileDialogService.PromptOpenFileAsync("", LabelImportFilter);
        if (string.IsNullOrEmpty(importFilename))
            return;

        ProjectController.ImportLabelsCsv(importFilename, replaceAll);
    }

    private static bool PromptWarning(string msg) =>
        MessageBox.Show(msg, "Warning", MessageBoxButtons.OKCancel) == DialogResult.OK;

    private async void exportCSVToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (viewModel == null)
            return;

        // empty title = keep the OS default ("Save As"), like the old saveFileDialog1
        var exportFilename = await FileDialogService.PromptSaveFileAsync("", LabelExportFilter);
        if (string.IsNullOrEmpty(exportFilename))
            return;

        try
        {
            // step 1's exporter: same non-RFC-4180 dialect the importer reads; sanitizes
            // (and reports) what the old hand-rolled writer silently exported broken.
            await viewModel.ExportLabelsAsync(exportFilename);
        }
        catch (Exception)
        {
            MessageBox.Show("An error occurred while saving the file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void normalizeWRAMLabelsToolStripMenuItem_Click(object sender, EventArgs e) =>
        // the controller owns the confirm prompt; the label mutations stream back into the
        // VM via provider events, so no manual repopulate is needed anymore.
        SuspendDrawingDuring(() => ProjectController?.NormalizeWramLabels());

    // ------------------------------------------------------------------ ILabelEditorView
    // (step 4 dropped the prompt-shaped members PromptForCsvFilename/ShowLineItemError;
    // dialogs now go through FileDialogService / the controller's ICommonGui.)

    public void SetProjectController(IProjectController? projectController) =>
        ProjectController = projectController;

    public void RepopulateFromData() => RecreateViewModel();

    public void RebindProject() => RecreateViewModel();

    public event EventHandler? OnFormClosed; // never raised: the host form hides on close (as before)

    // hides Control.Show(): callers of ILabelEditorView.Show() expect the WINDOW to appear
    public new void Show() => FindForm()?.Show();

    public void BringFormToTop() => FindForm()?.Focus();

    public void FocusOrCreateLabelAtSelectedRomOffsetIa()
    {
        var selectedOffset = ProjectController?.ProjectView.SelectedOffset ?? -1;
        if (selectedOffset == -1)
        {
            MessageBox.Show("No offset selected in main form, or no project loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        FocusOrCreateLabelAtRomOffsetIa(selectedOffset);
    }

    public void FocusOrCreateLabelAtRomOffsetIa(int selectedOffset) =>
        BeginGridEditFor(viewModel?.FocusOrCreateAtRomOffsetIa(selectedOffset));

    public void FocusOrCreateLabelAtSnesAddress(int snesAddress) =>
        BeginGridEditFor(viewModel?.FocusOrCreateAtSnesAddress(snesAddress));

    private void BeginGridEditFor(ILabelRowViewModel? row)
    {
        if (row == null)
            return;

        var index = IndexOfRow(row);
        if (index < 0 || index >= dataGridView1.Rows.Count)
            return;

        dataGridView1.CurrentCell = dataGridView1.Rows[index].Cells[1]; // name cell
        dataGridView1.BeginEdit(true);
    }

    // ------------------------------------------------------------------ drawing helpers

    private void SafeEndEdit()
    {
        // not thrilled about this implementation, but necessary to prevent silent native crashes like:
        // System.InvalidOperationException: Operation did not succeed because the program cannot commit or quit a cell value change
        try
        {
            if (dataGridView1.IsCurrentCellInEditMode)
                dataGridView1.EndEdit();
        }
        catch (Exception ex)
        {
            try
            {
                dataGridView1.CancelEdit();
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"LabelView: Could not end/cancel edit (bad/weird situation now): {ex.Message}");
            }
        }
    }

    // CPU optimization for bulk row changes: prevent layout/repaint while the VM streams
    // thousands of row events (search, sort, rebind, normalize). hacky but effective.
    private void SuspendDrawingDuring(Action action)
    {
        WinformsGuiUtil.SuspendDrawing(dataGridView1);
        dataGridView1.SuspendLayout();
        SuspendLayout();
        try
        {
            action();
        }
        finally
        {
            ResumeLayout(performLayout: true);
            dataGridView1.ResumeLayout(performLayout: true);
            WinformsGuiUtil.ResumeDrawing(dataGridView1);
        }
    }

    // ------------------------------------------------------------------ details panel

    private void SetupLabelDetailsPanel()
    {
        dataGridContexts.AutoGenerateColumns = false;
        dataGridContexts.AllowUserToAddRows = true;
        dataGridContexts.AllowUserToDeleteRows = true;

        dataGridContexts.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Context",
            HeaderText = "Context",
            DataPropertyName = nameof(ContextMapping.Context),
            Width = 150
        });
        dataGridContexts.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "NameOverride",
            HeaderText = "Name Override",
            DataPropertyName = nameof(ContextMapping.NameOverride),
            Width = 200
        });
    }

    private void UpdateDetailsPanelFor(ILabelRowViewModel? row)
    {
        if (contextMappingsBindingList != null)
            contextMappingsBindingList.ListChanged -= ContextMappingsBindingList_ListChanged;

        selectedLabel = row != null ? Data?.Labels.GetLabel(row.SnesAddress) : null;

        txtDetailsLabelPrimaryName.DataBindings.Clear();
        txtDetailsLabelComment.DataBindings.Clear();

        if (selectedLabel == null)
        {
            dataGridContexts.DataSource = null;
            contextMappingsBindingList = null;
            lblPanelName.Text = "Label Details";
            groupBox1.Text = "Label Details";
            return;
        }

        txtDetailsLabelPrimaryName.DataBindings.Add(new Binding("Text", selectedLabel,
            nameof(selectedLabel.Name), formattingEnabled: false, DataSourceUpdateMode.OnPropertyChanged));

        var commentBinding = new Binding("Text", selectedLabel, nameof(selectedLabel.Comment),
            formattingEnabled: false, DataSourceUpdateMode.OnPropertyChanged);
        commentBinding.Format += (_, args) =>
        {
            if (args.Value is string text)
                args.Value = text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
        };
        commentBinding.Parse += (_, args) =>
        {
            if (args.Value is string text)
                args.Value = text.Replace(Environment.NewLine, "\n");
        };
        txtDetailsLabelComment.DataBindings.Add(commentBinding);

        contextMappingsBindingList = [];
        foreach (var mapping in selectedLabel.ContextMappings)
        {
            contextMappingsBindingList.Add(mapping as ContextMapping ?? new ContextMapping
            {
                Context = mapping.Context,
                NameOverride = mapping.NameOverride
            });
        }

        contextMappingsBindingList.AllowNew = true;
        contextMappingsBindingList.AllowRemove = true;
        contextMappingsBindingList.AllowEdit = true;
        contextMappingsBindingList.ListChanged += ContextMappingsBindingList_ListChanged;

        dataGridContexts.DataSource = contextMappingsBindingList;
        groupBox1.Text = $"Label Details - {row!.AddressText}";
    }

    private void ContextMappingsBindingList_ListChanged(object? sender, ListChangedEventArgs e)
    {
        if (selectedLabel?.ContextMappings == null || contextMappingsBindingList == null)
            return;

        if (e.ListChangedType == ListChangedType.Reset || isUpdatingContextMappings)
            return;

        try
        {
            isUpdatingContextMappings = true;

            // clear and rebuild the model's collection; the row VM relays the change into
            // the main grid's Contexts column automatically.
            selectedLabel.ContextMappings.Clear();
            foreach (var mapping in contextMappingsBindingList)
            {
                if (!string.IsNullOrWhiteSpace(mapping.Context))
                    selectedLabel.ContextMappings.Add(mapping);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing context mappings: {ex.Message}");
        }
        finally
        {
            isUpdatingContextMappings = false;
        }
    }
}
