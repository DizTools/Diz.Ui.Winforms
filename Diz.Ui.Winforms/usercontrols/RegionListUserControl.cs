using Diz.Controllers.interfaces;
using Diz.Core.Interfaces;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Diz.Core.util;

namespace Diz.Ui.Winforms.usercontrols;

public partial class RegionListUserControl : UserControl, IRegionListView
{
    private IProjectController? projectController;
    private readonly BindingSource bindingSource = new();
    private Label errorLabel; // Add this field for displaying errors

    public RegionListUserControl()
    {
        InitializeComponent();
        
        errorLabel = new Label
        {
            Name = "errorLabel",
            Text = "",
            ForeColor = Color.Black, // Normal text color when no error
            BackColor = SystemColors.Control, // Normal background
            AutoSize = false,
            Height = 25,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(5, 2, 5, 2)
            // Always visible now, not hidden
        };
    
        Controls.Add(errorLabel);
    
        // Ensure the DataGridView docks below the error label
        if (regionGridView != null)
        {
            regionGridView.Dock = DockStyle.Fill; // This will fill the remaining space after the top-docked error label
        }

        ConfigureDataGridView();
        AttachEventHandlers();
    }

    private void ConfigureDataGridView()
    {
        regionGridView.AutoGenerateColumns = false;
        regionGridView.DataSource = bindingSource;
        regionGridView.AllowUserToAddRows = true;
        regionGridView.AllowUserToDeleteRows = true;
        regionGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        SetupColumns();
    }

private void SetupColumns()
{
    // Only add columns if they don't already exist
    if (regionGridView.Columns.Count > 0)
        return;
    
    var startAddressColumn = new DataGridViewTextBoxColumn
    {
        Name = "StartSnesAddress",
        DataPropertyName = "StartSnesAddress",
        HeaderText = "Start SNES Address [hex]",
        Width = 100
    };
    
    var endAddressColumn = new DataGridViewTextBoxColumn
    {
        Name = "EndSnesAddress", 
        DataPropertyName = "EndSnesAddress",
        HeaderText = "End SNES Address [hex]",
        Width = 100
    };
    
    // ExportType is an enum: bind the combobox items to the enum values themselves (not strings)
    // so the selected item round-trips straight back through the binding source without conversion.
    var exportTypeColumn = new DataGridViewComboBoxColumn
    {
        Name = "ExportType",
        DataPropertyName = "ExportType",
        HeaderText = "Export Type",
        Width = 90,
        ValueType = typeof(RegionExportType),
        DataSource = Enum.GetValues(typeof(RegionExportType)),
        FlatStyle = FlatStyle.Flat
    };

    regionGridView.Columns.AddRange(new DataGridViewColumn[]
    {
        startAddressColumn,
        endAddressColumn,
        new DataGridViewTextBoxColumn
        {
            Name = "RegionName",
            DataPropertyName = "RegionName", 
            HeaderText = "Region Name",
            Width = 150
        },
        new DataGridViewTextBoxColumn
        {
            Name = "ContextToApply",
            DataPropertyName = "ContextToApply",
            HeaderText = "Label Context To Apply",
            Width = 140
        },
        new DataGridViewTextBoxColumn
        {
            Name = "Priority",
            DataPropertyName = "Priority",
            HeaderText = "Priority", 
            Width = 80
        },
        new DataGridViewCheckBoxColumn
        {
            Name = "ExportSeparateFile",
            DataPropertyName = "ExportSeparateFile",
            HeaderText = "Export Separate File", 
            Width = 50 // for header
        },
        exportTypeColumn,
        new DataGridViewTextBoxColumn
        {
            Name = "AssetType",
            DataPropertyName = "AssetType",
            HeaderText = "Asset Type",
            Width = 110
        },
        new DataGridViewTextBoxColumn
        {
            Name = "AssetVersion",
            DataPropertyName = "AssetVersion",
            HeaderText = "Asset Version",
            Width = 80
        },
        new DataGridViewTextBoxColumn
        {
            Name = "AssetName",
            DataPropertyName = "AssetName",
            HeaderText = "Asset Name",
            Width = 150
        },
        new DataGridViewButtonColumn
        {
            Name = "Actions",
            HeaderText = "Actions",
            Text = "Delete",
            UseColumnTextForButtonValue = true,
            Width = 100
        }
    });
}

private void AttachEventHandlers()
{
    regionGridView.CellContentClick += RegionGridView_CellContentClick;
    regionGridView.UserDeletingRow += RegionGridView_UserDeletingRow;
    regionGridView.RowValidating += RegionGridView_RowValidating;
    regionGridView.DataError += RegionGridView_DataError;
    regionGridView.CellFormatting += RegionGridView_CellFormatting;
    regionGridView.CellParsing += RegionGridView_CellParsing;
}

private void RegionGridView_DataError(object? sender, DataGridViewDataErrorEventArgs e)
{
    // Handle data errors to replace the default dialog
    var columnName = regionGridView.Columns[e.ColumnIndex].HeaderText;
    var errorMessage = $"Data error in {columnName}: {e.Exception?.Message ?? "Invalid data format"}";
    
    ShowErrorMessage(errorMessage);
    
    // Prevent the default error dialog from showing
    e.ThrowException = false;
    System.Diagnostics.Debug.WriteLine($"DataGridView error: {errorMessage}");
}

private void ShowErrorMessage(string message)
{
    errorLabel.Text = message;
    errorLabel.ForeColor = Color.Red;
    errorLabel.BackColor = Color.LightYellow;
    
    // Auto-clear the error message after 5 seconds (but keep the label visible)
    var timer = new System.Windows.Forms.Timer();
    timer.Interval = 5000;
    timer.Tick += (s, e) =>
    {
        ClearErrorMessage();
        timer.Dispose();
    };
    timer.Start();
}

private void ClearErrorMessage()
{
    errorLabel.Text = "";
    errorLabel.ForeColor = Color.Black;
    errorLabel.BackColor = SystemColors.Control;
}

private void HideErrorMessage() {
    ClearErrorMessage();
}

private void RegionGridView_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
{
    // Convert int to hex string for display
    if ((regionGridView.Columns[e.ColumnIndex].Name == "StartSnesAddress" || 
         regionGridView.Columns[e.ColumnIndex].Name == "EndSnesAddress") && 
        e.Value is int intValue)
    {
        e.Value = Util.NumberToBaseString(intValue, Util.NumberBase.Hexadecimal, 6, showPrefix: false);
        e.FormattingApplied = true;
    }

    ApplyAssetCellStyling(e);
}

// the asset columns only mean anything when we're not exporting as plain inline assembly.
// grey them out + make them read-only per-row (rather than hiding the columns entirely,
// which would make it non-obvious that the feature exists).
private void ApplyAssetCellStyling(DataGridViewCellFormattingEventArgs e)
{
    if (!IsAssetColumn(regionGridView.Columns[e.ColumnIndex].Name))
        return;

    if (e.RowIndex < 0 || e.RowIndex >= regionGridView.Rows.Count)
        return;

    var row = regionGridView.Rows[e.RowIndex];
    var disabled = GetRowExportType(row) == RegionExportType.Assembly;

    row.Cells[e.ColumnIndex].ReadOnly = disabled;

    if (!disabled)
        return;

    e.CellStyle.BackColor = SystemColors.Control;
    e.CellStyle.ForeColor = SystemColors.GrayText;
}

private static bool IsAssetColumn(string columnName) =>
    columnName is "AssetType" or "AssetVersion" or "AssetName";

// read the ExportType cell back out as an enum. cells can hold either the enum or its
// string form depending on whether the user just edited it, so handle both.
private static RegionExportType GetRowExportType(DataGridViewRow row)
{
    var value = row.Cells["ExportType"].Value;
    if (value is RegionExportType exportType)
        return exportType;

    return Enum.TryParse<RegionExportType>(value?.ToString(), out var parsed)
        ? parsed
        : RegionExportType.Assembly;
}

private void RegionGridView_CellParsing(object? sender, DataGridViewCellParsingEventArgs e)
{
    // Convert hex or decimal string back to int for storage
    if ((regionGridView.Columns[e.ColumnIndex].Name == "StartSnesAddress" || 
         regionGridView.Columns[e.ColumnIndex].Name == "EndSnesAddress") && 
        e.Value is string stringValue)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                e.Value = 0;
                e.ParsingApplied = true;
            }
            else if (ByteUtil.TryParseNum_Stripped(ref stringValue, NumberStyles.HexNumber, out var result))
            {
                e.Value = result;
                e.ParsingApplied = true;
                HideErrorMessage(); // Hide any previous error messages on successful parsing
            }
            else
            {
                // Show error for invalid format
                ShowErrorMessage($"Invalid address format: '{stringValue}'. Please enter a valid hexadecimal number.");
                e.ParsingApplied = false;
            }
        }
        catch (Exception ex)
        {
            // Show error for parsing exceptions
            ShowErrorMessage($"Error parsing address: {ex.Message}");
            e.ParsingApplied = false;
        }
    }
}
    
    public void BringFormToTop() => Show();

    public void SetProjectController(IProjectController? controller)
    {
        projectController = controller;
        RebindProject();
    }

    public void RebindProject()
    {
        // Clean up previous event handler to avoid memory leaks
        bindingSource.AddingNew -= BindingSource_AddingNew;
        
        if (projectController?.Project?.Data?.Regions != null)
        {
            bindingSource.DataSource = projectController.Project.Data.Regions;
            bindingSource.AddingNew += BindingSource_AddingNew;
            bindingSource.AllowNew = true;
        }
        else
        {
            bindingSource.DataSource = null;
        }
        
        regionGridView.Refresh();
    }

    private void BindingSource_AddingNew(object? sender, AddingNewEventArgs e)
    {
        // Sync UI and data: Create a new region in our data when user tries to add a row to the list
        e.NewObject = projectController?.Project?.Data?.CreateNewRegion();
    }
    
    private void RegionGridView_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        var actionColumn = regionGridView.Columns["Actions"];
        if (actionColumn == null || e.ColumnIndex != actionColumn.Index || e.RowIndex < 0) 
            return;
        
        var result = MessageBox.Show("Are you sure you want to delete this region?", 
            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
        if (result == DialogResult.Yes) 
            DeleteRegion(e.RowIndex);
    }
    
    private void RegionGridView_UserDeletingRow(object? sender, DataGridViewRowCancelEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to delete this region?", 
            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
        e.Cancel = result != DialogResult.Yes;
    }
    
    [SuppressMessage("ReSharper", "InvertIf")]
    [SuppressMessage("ReSharper", "RedundantJumpStatement")]
    private void RegionGridView_RowValidating(object? sender, DataGridViewCellCancelEventArgs e)
    {
        var row = regionGridView.Rows[e.RowIndex];
        
        // Skip validation if this is still a new row
        if (row.IsNewRow)
            return;
        
        if (string.IsNullOrWhiteSpace(row.Cells["RegionName"].Value?.ToString()))
        {
            ShowErrorMessage("Region Name is required.");
            e.Cancel = true;
            return;
        }
        
        if (!int.TryParse(row.Cells["StartSnesAddress"].Value?.ToString(), out var startSnesAddr))
        {
            ShowErrorMessage("Start SNES address must be valid number");
            e.Cancel = true;
            return;
        }
        
        if (!int.TryParse(row.Cells["EndSnesAddress"].Value?.ToString(), out var endSnesAddr))
        {
            ShowErrorMessage("End SNES address must be valid number");
            e.Cancel = true;
            return;
        }

        if (startSnesAddr == endSnesAddr) {
            ShowErrorMessage("Start/end address must not overlap, zero-length regions not allowed");
            e.Cancel = true;
            return;
        }
        
        if (startSnesAddr > endSnesAddr) {
            ShowErrorMessage("Start address must be less than end address");
            e.Cancel = true;
            return;
        }
        
        if (startSnesAddr < 0 || endSnesAddr < 0) {
            ShowErrorMessage("Negative numbers not allowed in SNES addresses");
            e.Cancel = true;
            return;
        }
        
        if (startSnesAddr > 0xFFFFFF || endSnesAddr > 0xFFFFFF) {
            ShowErrorMessage("SNES address too large (max allowed: 24-bits: 0xFFFFFF)");
            e.Cancel = true;
            return;
        }
        
        if (bool.TryParse(row.Cells["ExportSeparateFile"].Value?.ToString(), out var exportAsSeparateFile) && exportAsSeparateFile)
        {
            // for "export as separate file" regions, we don't allow crossing banks. validate:
            var startBank = RomUtil.GetBankFromSnesAddress(startSnesAddr);
            var endBank = RomUtil.GetBankFromSnesAddress(endSnesAddr);
            if (startBank != endBank) {
                ShowErrorMessage("When 'Export As Separate Files' is on, Start/end address must be in the same bank.");
                e.Cancel = true;
                return;
            }
        }

        var exportType = GetRowExportType(row);

        // asset name is used as a relative path under the asset root, so don't let it escape.
        // empty is fine: the exporter falls back to RegionName.
        var assetName = row.Cells["AssetName"].Value?.ToString() ?? "";
        if (!string.IsNullOrWhiteSpace(assetName))
        {
            if (assetName.Contains('\\') || assetName.Contains("..") || assetName.StartsWith('/'))
            {
                ShowErrorMessage("Asset Name must be a relative path: no backslashes, no '..', and no leading '/'.");
                e.Cancel = true;
                return;
            }
        }

        if (exportType == RegionExportType.Asset)
        {
            var assetType = row.Cells["AssetType"].Value?.ToString() ?? "";
            if (!ValidAssetTypes.TryGetValue(assetType, out var bpp))
            {
                ShowErrorMessage($"Asset Type is required when Export Type is 'Asset'. Expected one of: {string.Join(", ", ValidAssetTypes.Keys)}.");
                e.Cancel = true;
                return;
            }

            // 8 rows, bpp/2 bitplane pairs, 2 bytes per row per pair = bpp*8 bytes per tile.
            // a partial tile at the end would silently produce garbage graphics, so reject it here.
            var tileSizeInBytes = bpp * 8;
            var regionLength = endSnesAddr - startSnesAddr;
            if (regionLength % tileSizeInBytes != 0) {
                ShowErrorMessage($"Region length ({regionLength} bytes) must be a whole multiple of {tileSizeInBytes} bytes (one {bpp}bpp tile) when Asset Type is '{assetType}'.");
                e.Cancel = true;
                return;
            }
        }
    }

    // asset types Diz knows how to write a manifest for, mapped to their bits-per-pixel.
    // keep in sync with RegionAssetUtil.ParseSnesGfxBpp in Diz.LogWriter.
    private static readonly Dictionary<string, int> ValidAssetTypes = new()
    {
        { "gfx.snes.2bpp", 2 },
        { "gfx.snes.4bpp", 4 },
        { "gfx.snes.8bpp", 8 },
    };

    private void DeleteRegion(int rowIndex)
    {
        try
        {
            if (rowIndex >= 0 && rowIndex < bindingSource.Count)
            {
                bindingSource.RemoveAt(rowIndex);
                HideErrorMessage(); // Hide any error messages on successful deletion
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Error deleting region: {ex.Message}");
        }
    }
    
    // Additional utility methods
    public void AddRegion(IRegion region)
    {
        try
        {
            bindingSource.Add(region);
            HideErrorMessage(); // Hide any error messages on successful addition
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Error adding region: {ex.Message}");
        }
    }
    
    public void RefreshGrid() => bindingSource.ResetBindings(false);
    
    public IRegion? GetSelectedRegion()
    {
        return regionGridView.CurrentRow is { IsNewRow: false } 
            ? bindingSource.Current as IRegion 
            : null;
    }

    public event EventHandler? OnFormClosed;
}