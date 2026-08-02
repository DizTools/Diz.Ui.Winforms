using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Diz.Controllers.interfaces;
using Diz.Core.Interfaces;
using Diz.Core.model.snes;
using Diz.Ui.ViewModels.Regions;
using Diz.Ui.Winforms.util;

namespace Diz.Ui.Winforms.usercontrols;

// This control renders a RegionListViewModel instead of binding the grid straight at the
// project's region collection. Everything that is a rule rather than a widget -- address
// parsing, per-region validation, the whole-list problem report, sort order, add and delete --
// lives in the ViewModel, so the WinForms grid and the other toolkit's window cannot drift
// apart. This file is widget wiring.
//
// Binding: VM Rows (ReadOnlyObservableCollection) -> ObservableBindingList adapter (WinForms
// binding does not observe INotifyCollectionChanged) -> BindingSource -> DataGridView. ONE-WAY.
// Grid edits flow back through vm.CommitField, never by mutating the bound list -- which is also
// why a region added from anywhere else (bank-region synthesis during an import, a save-format
// migration) now shows up in an already-open window without a rebind.
//
// Validation never blocks. A refused edit is not written to the region, the row keeps the text
// the user typed, the row is flagged, and the message goes to the status line -- the user is
// free to leave the cell, the row and the window at any point.
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class RegionListViewControl : UserControl, IRegionListView
{
    private const string DeleteConfirmationMessage = "Are you sure you want to delete this region?";
    private const string DeleteConfirmationCaption = "Confirm Delete";
    private const string ActionsColumnName = "Actions";

    // pale red: readable with the default grid text, and distinct from the grey used for the
    // asset cells a row does not use.
    private static readonly Color InvalidRowBackColor = Color.FromArgb(255, 224, 224);

    private IProjectController? projectController;

    private IRegionListViewModel? viewModel;
    private ObservableBindingList<IRegionRowViewModel, RegionGridRow>? gridRows;
    private readonly BindingSource bindingSource = new();

    private bool syncingSelection;
    private bool problemsCollapsed;
    private readonly int expandedProblemsHeight;

    // what the current ViewModel was built over, so a rebind can tell "same project again" from
    // "a different project" without rebuilding either way.
    private System.Collections.ObjectModel.ObservableCollection<IRegion>? boundRegions;

    /// <summary>
    /// How the user is asked to confirm a delete. Deleting a region is destructive and the
    /// question belongs to the toolkit, not the ViewModel, so the host asks it and only then
    /// calls the command. Replaceable so the wiring can be exercised without a message box on
    /// screen; the default is the same question the window has always asked.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string, bool> ConfirmDelete { get; set; } = message =>
        MessageBox.Show(message, DeleteConfirmationCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        == DialogResult.Yes;

    public RegionListViewControl()
    {
        InitializeComponent();

        expandedProblemsHeight = problemsPanel.Height;

        SetupGrid();
        problemsToggle.Click += ProblemsToggle_Click;
        addRegionButton.Click += AddRegionButton_Click;
        UpdateProblemsHeader(0);

        Load += (_, _) => { if (viewModel == null) RecreateViewModel(); };
        Disposed += (_, _) =>
        {
            TearDownViewModel();
            bindingSource.Dispose();
        };
    }

    private Data? Data => projectController?.Project?.Data;

    // ------------------------------------------------------------------ VM lifecycle

    // the ViewModel is project-scoped (it wraps the open project's regions), so this view builds
    // it here rather than resolving it from the DI container: a different project means a
    // different ViewModel, not a reconfigured one.
    private void RecreateViewModel()
    {
        var regionProvider = Data;

        // Every project change reaches here, including a plain Save. When the regions are still
        // the same collection, rebuilding would throw away the sort order, the selection and the
        // scroll position for nothing -- and it is unnecessary, because the rows follow the
        // collection's own change notification. So only rebuild when the regions really are a
        // different collection (a different project, or a project that was just deserialized
        // over the top of this one).
        if (viewModel != null && regionProvider != null && ReferenceEquals(boundRegions, regionProvider.Regions))
            return;

        SafeEndEdit();
        TearDownViewModel();

        if (regionProvider == null)
        {
            bindingSource.DataSource = null;
            statusLabel.Text = "";
            RefreshProblems();
            return;
        }

        boundRegions = regionProvider.Regions;
        viewModel = new RegionListViewModel(regionProvider, RunOnUiThread);
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        viewModel.RegionsChanged += ViewModel_RegionsChanged;
        ((INotifyCollectionChanged)viewModel.Problems).CollectionChanged += Problems_CollectionChanged;

        gridRows = new ObservableBindingList<IRegionRowViewModel, RegionGridRow>(
            viewModel.Rows, row => new RegionGridRow(row));

        SuspendDrawingDuring(() => bindingSource.DataSource = gridRows);

        UpdateSortGlyphs();
        statusLabel.Text = viewModel.StatusText;
        RefreshProblems();
    }

    private void TearDownViewModel()
    {
        if (viewModel == null)
            return;

        bindingSource.DataSource = null;
        gridRows?.Dispose();
        gridRows = null;
        boundRegions = null;

        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        viewModel.RegionsChanged -= ViewModel_RegionsChanged;
        ((INotifyCollectionChanged)viewModel.Problems).CollectionChanged -= Problems_CollectionChanged;
        viewModel.Dispose();
        viewModel = null;
    }

    // VM marshaller contract: synchronous when already on the UI thread (send semantics).
    // off-thread notifications occur when regions are created away from the UI -- an import
    // synthesizing bank regions on a worker thread.
    private void RunOnUiThread(Action action)
    {
        if (IsHandleCreated && InvokeRequired)
            Invoke(action);
        else
            action();
    }

    // Committing a field can move the row: when the committed field is the one being sorted on,
    // the ViewModel repositions it, which mutates the bound list. Doing that from inside a grid
    // edit event re-enters the grid's own current-cell bookkeeping, so the commit is posted past
    // the whole edit sequence. Without a window handle there is no message loop to post to, so
    // the work runs inline instead.
    private void PostEdit(Action action)
    {
        if (IsHandleCreated)
            BeginInvoke(action);
        else
            action();
    }

    // ------------------------------------------------------------------ grid setup

    private void SetupGrid()
    {
        regionGridView.AutoGenerateColumns = false;

        // Regions are added and removed through ViewModel commands only. The grid's own phantom
        // "new row" appends, and the rows here are in sort order rather than storage order, so
        // there is no correct place for it to land; the Add Region button replaces it. Row
        // deletion likewise goes through the command, so the confirmation is asked exactly once
        // however the user asked for it.
        regionGridView.AllowUserToAddRows = false;
        regionGridView.AllowUserToDeleteRows = false;
        regionGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        // FullRowSelect makes the built-in Ctrl+C copy the whole row; we want just the focused
        // cell, so disable the built-in copy and handle Ctrl+C ourselves (see Grid_KeyDown).
        regionGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;

        // the row header carries the per-row error marker (see RowErrorTextNeeded)
        regionGridView.RowHeadersVisible = true;
        regionGridView.ShowRowErrors = true;

        SetupColumns();

        regionGridView.DataSource = bindingSource;

        regionGridView.CellContentClick += Grid_CellContentClick;
        regionGridView.CellValidating += Grid_CellValidating;
        regionGridView.CellFormatting += Grid_CellFormatting;
        regionGridView.RowErrorTextNeeded += Grid_RowErrorTextNeeded;
        regionGridView.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
        regionGridView.SelectionChanged += Grid_SelectionChanged;
        regionGridView.KeyDown += Grid_KeyDown;
        regionGridView.DataError += Grid_DataError;
    }

    private void SetupColumns()
    {
        if (regionGridView.Columns.Count > 0)
            return;

        var lengthColumn = TextColumn(nameof(RegionGridRow.Length), "Length [hex]", 90);
        // Length is not stored on a region: it is derived from the two addresses, and typing here
        // moves the END address while the start stays put. The end address is INCLUSIVE across
        // the codebase, so the byte count is end - start + 1 and a length of 1 means end == start.
        lengthColumn.ToolTipText =
            "Region size in bytes (hex), inclusive of the end address. " +
            "Type here to move the End SNES Address; editing either address recomputes this.";

        var exportSeparateFileColumn = new DataGridViewCheckBoxColumn
        {
            Name = nameof(RegionGridRow.ExportSeparateFile),
            DataPropertyName = nameof(RegionGridRow.ExportSeparateFile),
            HeaderText = "Export Separate File",
            Width = 50, // for header
            SortMode = DataGridViewColumnSortMode.Programmatic,
        };

        // ExportType is an enum: bind the combobox items to the enum values themselves (not
        // strings) so the selected item round-trips without conversion.
        var exportTypeColumn = new DataGridViewComboBoxColumn
        {
            Name = nameof(RegionGridRow.ExportType),
            DataPropertyName = nameof(RegionGridRow.ExportType),
            HeaderText = "Export Type",
            Width = 90,
            ValueType = typeof(RegionExportType),
            DataSource = Enum.GetValues(typeof(RegionExportType)),
            FlatStyle = FlatStyle.Flat,
            SortMode = DataGridViewColumnSortMode.Programmatic,
        };

        var assetOptionsColumn = TextColumn(nameof(RegionGridRow.AssetOptions), "Asset Options (JSON)", 200);
        assetOptionsColumn.ToolTipText =
            "Free-form JSON merged into the manifest under \"options\", overriding " +
            "the \"gfx\" block. Leave blank normally. " +
            "e.g. {\"cell_h\": 12} or {\"view\": {\"order\": \"column_major\", \"rows\": 12}}";

        regionGridView.Columns.AddRange(
            TextColumn(nameof(RegionGridRow.StartSnesAddress), "Start SNES Address [hex]", 100),
            TextColumn(nameof(RegionGridRow.EndSnesAddress), "End SNES Address [hex]", 100),
            lengthColumn,
            TextColumn(nameof(RegionGridRow.RegionName), "Region Name", 150),
            TextColumn(nameof(RegionGridRow.ContextToApply), "Label Context To Apply", 140),
            TextColumn(nameof(RegionGridRow.Priority), "Priority", 80),
            exportSeparateFileColumn,
            exportTypeColumn,
            TextColumn(nameof(RegionGridRow.AssetType), "Asset Type", 110),
            TextColumn(nameof(RegionGridRow.AssetVersion), "Asset Version", 80),
            TextColumn(nameof(RegionGridRow.AssetName), "Asset Name", 150),
            assetOptionsColumn,
            new DataGridViewButtonColumn
            {
                Name = ActionsColumnName,
                HeaderText = "Actions",
                Text = "Delete",
                UseColumnTextForButtonValue = true,
                Width = 100,
                SortMode = DataGridViewColumnSortMode.Programmatic,
            });
    }

    // native sorting stays OFF on every column: header clicks set the ViewModel's sort state and
    // the already-sorted row list comes back through the binding.
    private static DataGridViewTextBoxColumn TextColumn(string name, string header, int width) => new()
    {
        Name = name,
        DataPropertyName = name,
        HeaderText = header,
        Width = width,
        SortMode = DataGridViewColumnSortMode.Programmatic,
    };

    private static RegionField? FieldForColumn(string columnName) => columnName switch
    {
        nameof(RegionGridRow.StartSnesAddress) => RegionField.Start,
        nameof(RegionGridRow.EndSnesAddress) => RegionField.End,
        nameof(RegionGridRow.Length) => RegionField.Length,
        nameof(RegionGridRow.RegionName) => RegionField.RegionName,
        nameof(RegionGridRow.ContextToApply) => RegionField.ContextToApply,
        nameof(RegionGridRow.Priority) => RegionField.Priority,
        nameof(RegionGridRow.ExportSeparateFile) => RegionField.ExportSeparateFile,
        nameof(RegionGridRow.ExportType) => RegionField.ExportType,
        nameof(RegionGridRow.AssetType) => RegionField.AssetType,
        nameof(RegionGridRow.AssetVersion) => RegionField.AssetVersion,
        nameof(RegionGridRow.AssetName) => RegionField.AssetName,
        nameof(RegionGridRow.AssetOptions) => RegionField.AssetOptions,
        _ => null, // the Actions button column edits nothing and sorts by nothing
    };

    private static bool IsAssetColumn(string columnName) =>
        columnName is nameof(RegionGridRow.AssetType) or nameof(RegionGridRow.AssetVersion)
            or nameof(RegionGridRow.AssetName) or nameof(RegionGridRow.AssetOptions);

    private RegionGridRow? GridRowAt(int rowIndex) =>
        gridRows != null && rowIndex >= 0 && rowIndex < gridRows.Count ? gridRows[rowIndex] : null;

    private IRegionRowViewModel? CurrentRowViewModel() =>
        GridRowAt(regionGridView.CurrentCell?.RowIndex ?? -1)?.Row;

    private int IndexOfRow(IRegionRowViewModel row)
    {
        for (var i = 0; i < (gridRows?.Count ?? 0); i++)
            if (ReferenceEquals(gridRows![i].Row, row))
                return i;
        return -1;
    }

    // ------------------------------------------------------------------ cell editing

    private void Grid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (viewModel == null || e.ColumnIndex < 0)
            return;

        var row = GridRowAt(e.RowIndex)?.Row;
        var field = FieldForColumn(regionGridView.Columns[e.ColumnIndex].Name);
        if (row == null || field == null)
            return;

        // CellValidating also fires on plain cell navigation, so an edit is only an edit when the
        // widget is holding something other than what the cell is ALREADY SHOWING. Without this
        // every move through the grid would re-commit unchanged values and wipe the status line.
        //
        // What a cell is showing is not always the row's current text: the checkbox and the combo
        // display the COMMITTED value, because they snap back when an edit is refused. Comparing
        // those against the refused text instead would swallow the user's second attempt at the
        // very value that was just rejected -- the one case where they are most likely to retry.
        var proposed = e.FormattedValue?.ToString() ?? "";
        var displayed = field.Value.DisplaysTypedText()
            ? row.TextFor(field.Value)
            : row.LastGoodTextFor(field.Value);

        if (proposed == displayed)
            return;

        // NOT cancelled, ever: an invalid value is refused by the ViewModel (the region is left
        // exactly as it was) and the row is flagged, but the user is never held in the cell.
        //
        // The commit is deferred (see PostEdit), so by the time it runs the world may have moved:
        // the project may have been rebound onto a different ViewModel, or the region may have
        // been deleted. Both are checked here rather than let the ViewModel throw at a user who
        // did nothing wrong -- an edit aimed at something that no longer exists is simply dropped.
        var editedViewModel = viewModel;
        PostEdit(() =>
        {
            if (!ReferenceEquals(editedViewModel, viewModel) || IndexOfRow(row) < 0)
                return;

            editedViewModel.CommitField(row, field.Value, proposed);
        });
    }

    private void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.C)
        {
            CopyFocusedCell(e);
            return;
        }

        if (e.KeyCode != Keys.Delete || regionGridView.IsCurrentCellInEditMode)
            return;

        var rows = SelectedRowViewModels();
        if (viewModel == null || rows.Count == 0)
            return;

        e.Handled = true;
        DeleteWithConfirmation(rows);
    }

    // Copy ONLY the focused cell. The grid's own copy is disabled (ClipboardCopyMode.Disable in
    // SetupGrid) because FullRowSelect would otherwise copy the entire row.
    private void CopyFocusedCell(KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;

        var text = regionGridView.CurrentCell?.Value?.ToString() ?? "";
        try
        {
            if (string.IsNullOrEmpty(text))
                Clipboard.Clear();
            else
                Clipboard.SetText(text);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            // clipboard momentarily locked by another process -- ignore rather than crash the editor.
        }
    }

    /// <summary>
    /// Every row the Delete key should act on. The grid selects whole rows and allows more than
    /// one, so acting on the current row alone would quietly delete one of the three a user had
    /// highlighted.
    /// </summary>
    private List<IRegionRowViewModel> SelectedRowViewModels()
    {
        var rows = regionGridView.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => r.Index)
            .OrderBy(index => index)
            .Select(index => GridRowAt(index)?.Row)
            .OfType<IRegionRowViewModel>()
            .ToList();

        if (rows.Count != 0)
            return rows;

        // nothing is selected as a row, but a cell has focus -- that row is what the user means.
        var current = CurrentRowViewModel();
        return current == null ? [] : [current];
    }

    // ------------------------------------------------------------------ display

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.RowIndex < 0 || e.RowIndex >= regionGridView.Rows.Count)
            return;

        var gridRow = GridRowAt(e.RowIndex);
        if (gridRow == null)
            return;

        var columnName = regionGridView.Columns[e.ColumnIndex].Name;

        if (IsAssetColumn(columnName))
        {
            // the asset columns only mean anything when the region's bytes are not emitted as
            // plain inline assembly. Grey them out and make them read-only per row, rather than
            // hiding the columns entirely, which would make it non-obvious that the feature
            // exists. Nothing is cleared: switching the export type back restores what was typed.
            var disabled = !gridRow.AssetFieldsEnabled;
            regionGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly = disabled;
            if (disabled)
            {
                e.CellStyle.BackColor = SystemColors.Control;
                e.CellStyle.ForeColor = SystemColors.GrayText;
                return;
            }
        }

        if (gridRow.HasError)
            e.CellStyle.BackColor = InvalidRowBackColor;
    }

    // The per-row bad-row marker: the grid's native row-header error icon, whose tooltip is the
    // rule the row breaks. Paired with the tinted row background above, so a bad row is obvious
    // at a glance and the reason is one hover away.
    private void Grid_RowErrorTextNeeded(object? sender, DataGridViewRowErrorTextNeededEventArgs e) =>
        e.ErrorText = GridRowAt(e.RowIndex)?.ErrorText ?? "";

    // With every value coming from the ViewModel as text the grid never holds something it cannot
    // convert, so this should not fire; if it somehow does, say so on the status line rather than
    // throwing a modal dialog at the user mid-edit.
    private void Grid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        var columnName = e.ColumnIndex >= 0 && e.ColumnIndex < regionGridView.Columns.Count
            ? regionGridView.Columns[e.ColumnIndex].HeaderText
            : "grid";

        statusLabel.Text = $"Data error in {columnName}: {e.Exception?.Message ?? "Invalid data format"}";
        e.ThrowException = false;
    }

    // ------------------------------------------------------------------ sorting / selection

    private void Grid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (viewModel == null || e.ColumnIndex < 0)
            return;

        var field = FieldForColumn(regionGridView.Columns[e.ColumnIndex].Name);
        if (field == null)
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

        UpdateSortGlyphs();
    }

    private void UpdateSortGlyphs()
    {
        foreach (DataGridViewColumn column in regionGridView.Columns)
            column.HeaderCell.SortGlyphDirection = SortOrder.None;

        if (viewModel == null)
            return;

        var sortedColumn = regionGridView.Columns
            .Cast<DataGridViewColumn>()
            .FirstOrDefault(c => FieldForColumn(c.Name) == viewModel.SortField);

        if (sortedColumn != null)
            sortedColumn.HeaderCell.SortGlyphDirection =
                viewModel.SortDescending ? SortOrder.Descending : SortOrder.Ascending;
    }

    private void Grid_SelectionChanged(object? sender, EventArgs e)
    {
        if (syncingSelection || viewModel == null)
            return;

        syncingSelection = true;
        try
        {
            viewModel.SelectedRow = CurrentRowViewModel();
        }
        finally
        {
            syncingSelection = false;
        }
    }

    private void SyncGridSelectionFromViewModel()
    {
        var row = viewModel?.SelectedRow;
        if (syncingSelection || row == null)
            return;

        var index = IndexOfRow(row);
        if (index < 0 || index >= regionGridView.Rows.Count)
            return;

        var columnIndex = regionGridView.CurrentCell?.ColumnIndex ?? 0;
        syncingSelection = true;
        try
        {
            regionGridView.CurrentCell = regionGridView.Rows[index].Cells[columnIndex];
        }
        catch (InvalidOperationException)
        {
            // mid-commit; the grid will catch up on the next selection change
        }
        finally
        {
            syncingSelection = false;
        }
    }

    // ------------------------------------------------------------------ commands

    private void AddRegionButton_Click(object? sender, EventArgs e)
    {
        if (viewModel == null)
            return;

        // the new region is named and is already a legal one-byte range, so it is never a row the
        // user cannot leave. It lands wherever the current sort order puts it, which is why the
        // grid selects it afterwards rather than assuming it is at the bottom.
        var row = viewModel.AddRegion();
        viewModel.SelectedRow = row;
        SyncGridSelectionFromViewModel();
    }

    private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        if (regionGridView.Columns[e.ColumnIndex].Name != ActionsColumnName)
            return;

        var row = GridRowAt(e.RowIndex)?.Row;
        if (row != null)
            DeleteWithConfirmation([row]);
    }

    private void DeleteWithConfirmation(IReadOnlyList<IRegionRowViewModel> rows)
    {
        if (viewModel == null || rows.Count == 0)
            return;

        // one question however many regions are going, and it says how many so a stray
        // multi-select is caught before anything is destroyed rather than after.
        var question = rows.Count == 1
            ? DeleteConfirmationMessage
            : $"Are you sure you want to delete {rows.Count} regions?";

        if (!ConfirmDelete(question))
            return;

        SafeEndEdit();

        // BY ROW, never by row index: the grid shows regions in sort order, so a row index is not
        // an index into the stored collection and deleting by one removes the wrong region.
        foreach (var row in rows)
            viewModel.DeleteRegion(row);
    }

    // ------------------------------------------------------------------ VM events

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IRegionListViewModel.StatusText):
                // persistent: the message stays until the next action replaces it.
                statusLabel.Text = viewModel?.StatusText ?? "";
                break;
            case nameof(IRegionListViewModel.SelectedRow):
                SyncGridSelectionFromViewModel();
                break;
            case nameof(IRegionListViewModel.SortField):
            case nameof(IRegionListViewModel.SortDescending):
                UpdateSortGlyphs();
                break;
        }
    }

    // region data changed (an add, a delete, or a committed field edit), so the project has
    // unsaved work in it. Re-sorting and selecting deliberately do not raise this.
    private void ViewModel_RegionsChanged(object? sender, EventArgs e) =>
        projectController?.MarkChanged();

    private void Problems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        RefreshProblems();

    // ------------------------------------------------------------------ problem panel

    // Problems are relationships BETWEEN regions -- crossing file-producing regions, overlapping
    // asset regions, a region claiming two output roles, duplicate names -- so they cannot be
    // shown on any one row. Deliberately display-only: the ViewModel's problem entries do not
    // carry the region they are about, so there is nothing to navigate to and offering it would
    // mean guessing from the message text.
    private void RefreshProblems()
    {
        problemsList.BeginUpdate();
        try
        {
            problemsList.Items.Clear();
            if (viewModel != null)
            {
                foreach (var problem in viewModel.Problems)
                    problemsList.Items.Add(DescribeProblem(problem));
            }
        }
        finally
        {
            problemsList.EndUpdate();
        }

        UpdateProblemsHeader(problemsList.Items.Count);
    }

    private static string DescribeProblem(RegionProblem problem) =>
        problem.Severity == RegionProblemSeverity.Warning
            ? $"Warning: {problem.Message}"
            : $"Error: {problem.Message}";

    private void ProblemsToggle_Click(object? sender, EventArgs e)
    {
        problemsCollapsed = !problemsCollapsed;
        problemsList.Visible = !problemsCollapsed;
        problemsPanel.Height = problemsCollapsed ? problemsToggle.Height : expandedProblemsHeight;
        UpdateProblemsHeader(problemsList.Items.Count);
    }

    private void UpdateProblemsHeader(int count) =>
        problemsToggle.Text = $"{(problemsCollapsed ? "▶" : "▼")} Problems ({count})";

    // ------------------------------------------------------------------ IRegionListView

    public void SetProjectController(IProjectController? controller)
    {
        projectController = controller;
        RebindProject();
    }

    public void RebindProject() => RecreateViewModel();

    public event EventHandler? OnFormClosed; // never raised: the host form hides on close (as before)

    // hides Control.Show(): callers of IRegionListView.Show() expect the WINDOW to appear
    public new void Show() => FindForm()?.Show();

    public void BringFormToTop() => FindForm()?.Focus();

    // ------------------------------------------------------------------ drawing helpers

    private void SafeEndEdit()
    {
        // not thrilled about this implementation, but necessary to prevent silent native crashes like:
        // System.InvalidOperationException: Operation did not succeed because the program cannot commit or quit a cell value change
        try
        {
            if (regionGridView.IsCurrentCellInEditMode)
                regionGridView.EndEdit();
        }
        catch (Exception ex)
        {
            try
            {
                regionGridView.CancelEdit();
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine(
                    $"RegionListViewControl: Could not end/cancel edit (bad/weird situation now): {ex.Message}");
            }
        }
    }

    // prevent layout/repaint while the ViewModel restreams the whole row list (a re-sort, a
    // rebind). hacky but effective.
    private void SuspendDrawingDuring(Action action)
    {
        WinformsGuiUtil.SuspendDrawing(regionGridView);
        regionGridView.SuspendLayout();
        SuspendLayout();
        try
        {
            action();
        }
        finally
        {
            ResumeLayout(performLayout: true);
            regionGridView.ResumeLayout(performLayout: true);
            WinformsGuiUtil.ResumeDrawing(regionGridView);
        }
    }
}
