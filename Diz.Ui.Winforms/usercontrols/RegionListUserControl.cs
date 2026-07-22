using Diz.Controllers.interfaces;
using Diz.Core.Interfaces;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
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

        // FullRowSelect makes the built-in Ctrl+C copy the whole row; we want just the focused
        // cell, so disable the built-in copy and handle Ctrl+C ourselves (see RegionGridView_KeyDown).
        regionGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;

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
    
    // Unbound convenience column: Length is derived from Start/End rather than stored on IRegion.
    // Editing it moves EndSnesAddress (start stays put); editing either address recomputes it.
    // EndSnesAddress is INCLUSIVE across the codebase (see RegionRoles/RegionAssetExportService),
    // so the byte count shown here is end - start + 1.
    var lengthColumn = new DataGridViewTextBoxColumn
    {
        Name = "Length",
        HeaderText = "Length [hex]",
        ToolTipText = "Region size in bytes (hex), inclusive of the end address. " +
                      "Type here to move the End SNES Address; editing either address recomputes this.",
        ValueType = typeof(int),
        Width = 90
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
        lengthColumn,
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
        new DataGridViewTextBoxColumn
        {
            Name = "AssetOptions",
            DataPropertyName = "AssetOptions",
            HeaderText = "Asset Options (JSON)",
            ToolTipText = "Free-form JSON merged into the manifest under \"options\", overriding " +
                          "the \"gfx\" block. Leave blank normally. " +
                          "e.g. {\"cell_h\": 12} or {\"view\": {\"order\": \"column_major\", \"rows\": 12}}",
            Width = 200
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
    regionGridView.CellValueChanged += RegionGridView_CellValueChanged;
    regionGridView.KeyDown += RegionGridView_KeyDown;
}

// Copy ONLY the focused cell on Ctrl+C. The grid's own copy is disabled (ClipboardCopyMode.Disable
// in ConfigureDataGridView) because FullRowSelect would otherwise copy the entire row.
private void RegionGridView_KeyDown(object? sender, KeyEventArgs e)
{
    if (!e.Control || e.KeyCode != Keys.C)
        return;

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

    FormatLengthCell(e);
    ApplyAssetCellStyling(e);
}

// Length is unbound, so its displayed value is always recomputed from the row's addresses here
// rather than trusted from whatever the cell happens to be holding. That keeps it honest no
// matter which of the three cells the user last touched.
private void FormatLengthCell(DataGridViewCellFormattingEventArgs e)
{
    if (regionGridView.Columns[e.ColumnIndex].Name != "Length")
        return;

    if (e.RowIndex < 0 || e.RowIndex >= regionGridView.Rows.Count)
        return;

    var row = regionGridView.Rows[e.RowIndex];

    // a brand-new row has no addresses yet; showing "1" there would be noise.
    if (row.IsNewRow || !TryGetRegionLength(row, out var length))
    {
        e.Value = "";
        e.FormattingApplied = true;
        return;
    }

    e.Value = Util.NumberToBaseString(length, Util.NumberBase.Hexadecimal, 0, showPrefix: false);
    e.FormattingApplied = true;
}

// EndSnesAddress is inclusive (the last byte IN the region), so the byte count is end - start + 1.
private static bool TryGetRegionLength(DataGridViewRow row, out int length)
{
    length = 0;
    if (!TryGetAddressCell(row, "StartSnesAddress", out var start) ||
        !TryGetAddressCell(row, "EndSnesAddress", out var end))
        return false;

    length = end - start + 1;
    return true;
}

// address cells hold ints once bound/parsed, but can transiently hold the raw string mid-edit.
private static bool TryGetAddressCell(DataGridViewRow row, string columnName, out int value)
{
    value = 0;

    switch (row.Cells[columnName].Value)
    {
        case int intValue:
            value = intValue;
            return true;

        case string text when !string.IsNullOrWhiteSpace(text):
            var toParse = text;
            if (!ByteUtil.TryParseNum_Stripped(ref toParse, NumberStyles.HexNumber, out var parsed))
                return false;
            value = parsed;
            return true;

        default:
            return false;
    }
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
    columnName is "AssetType" or "AssetVersion" or "AssetName" or "AssetOptions";

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
    if (regionGridView.Columns[e.ColumnIndex].Name == "Length")
    {
        ParseLengthCell(e);
        return;
    }

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

// Length is typed in hex like the addresses. Parsing only turns the text into a number; the
// actual write-back to EndSnesAddress happens in CellValueChanged, once the value is committed.
private void ParseLengthCell(DataGridViewCellParsingEventArgs e)
{
    if (e.Value is not string stringValue)
        return;

    if (string.IsNullOrWhiteSpace(stringValue))
    {
        // blank means "leave the addresses alone" -- the display recomputes from them anyway.
        e.Value = null;
        e.ParsingApplied = true;
        return;
    }

    if (!ByteUtil.TryParseNum_Stripped(ref stringValue, NumberStyles.HexNumber, out var length))
    {
        ShowErrorMessage($"Invalid length: '{stringValue}'. Please enter a valid hexadecimal number.");
        e.ParsingApplied = false;
        return;
    }

    if (length < 1)
    {
        ShowErrorMessage("Length must be at least 1 (zero-length regions are not allowed).");
        e.ParsingApplied = false;
        return;
    }

    e.Value = length;
    e.ParsingApplied = true;
    HideErrorMessage();
}

// Keeps the Start/End/Length trio consistent after any one of them is committed:
//   - Length edited  -> move EndSnesAddress, keeping StartSnesAddress fixed
//   - Start/End edited -> just repaint, since Length is recomputed during formatting
private void RegionGridView_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
{
    if (e.RowIndex < 0 || e.RowIndex >= regionGridView.Rows.Count || e.ColumnIndex < 0)
        return;

    var columnName = regionGridView.Columns[e.ColumnIndex].Name;
    if (columnName is not ("Length" or "StartSnesAddress" or "EndSnesAddress"))
        return;

    var row = regionGridView.Rows[e.RowIndex];

    if (columnName == "Length")
    {
        ApplyLengthToEndAddress(row);
    }
    else
    {
        // An address moved, so the length the user last typed is now stale. Clear the stored
        // value (the display is derived, so nothing visibly changes) -- otherwise re-typing that
        // same number later wouldn't raise CellValueChanged and the end address wouldn't move.
        if (row.Cells["Length"].Value != null)
            row.Cells["Length"].Value = null;
    }

    // Length's displayed text is derived, so force a repaint of the row to pick up the new value.
    regionGridView.InvalidateRow(e.RowIndex);
}

private void ApplyLengthToEndAddress(DataGridViewRow row)
{
    if (row.Cells["Length"].Value is not int length || length < 1)
        return;

    if (!TryGetAddressCell(row, "StartSnesAddress", out var start))
        return;

    // inclusive end: a length of 1 means end == start.
    var newEnd = start + length - 1;
    if (TryGetAddressCell(row, "EndSnesAddress", out var currentEnd) && currentEnd == newEnd)
        return;

    row.Cells["EndSnesAddress"].Value = newEnd;
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

            // Route to the descriptor that owns this AssetType family, the same way the codec
            // dispatch does downstream (Diz.LogWriter BinaryAssetExporterBase / BuildToolBinding).
            var descriptor = AssetTypeUiValidators.FirstOrDefault(d => d.Matches(assetType));
            if (descriptor == null)
            {
                var known = string.Join(", ", AssetTypeUiValidators.SelectMany(d => d.ExampleTypes));
                ShowErrorMessage($"Asset Type is required when Export Type is 'Asset'. Expected one of: {known}.");
                e.Cancel = true;
                return;
            }

            // Asset Options is free-form and Diz doesn't own its vocabulary, so at this generic
            // layer we validate only that it parses as a JSON object; the descriptor reads
            // whatever type-specific keys (e.g. cell_h) it needs.
            var optionsText = row.Cells["AssetOptions"].Value?.ToString() ?? "";
            JsonObject? options = null;
            if (!string.IsNullOrWhiteSpace(optionsText))
            {
                JsonNode? parsed;
                try
                {
                    parsed = JsonNode.Parse(optionsText);
                }
                catch (JsonException ex)
                {
                    ShowErrorMessage($"Asset Options is not valid JSON: {ex.Message}");
                    e.Cancel = true;
                    return;
                }

                if (parsed is not JsonObject optionsObj)
                {
                    ShowErrorMessage("Asset Options must be a JSON object, e.g. {\"cell_h\": 12}.");
                    e.Cancel = true;
                    return;
                }

                options = optionsObj;
            }

            // EndSnesAddress is INCLUSIVE (the last byte IN the region), so the byte count is
            // end - start + 1. This must match what the exporter actually extracts --
            // RegionAssetExportService slices inclusively and the exporters validate against
            // request.Bytes.Length -- otherwise the UI rejects regions the build accepts (and
            // vice versa). Same number the grid's Length column shows.
            var context = new AssetTypeValidationContext(assetType, endSnesAddr - startSnesAddr + 1, options);
            var error = descriptor.Validate(context);
            if (error != null)
            {
                ShowErrorMessage(error);
                e.Cancel = true;
                return;
            }
        }
    }

    // ---- per-asset-type UI validation ------------------------------------------------------
    // Each descriptor owns a family of AssetType strings and knows how to sanity-check a region's
    // length + options for that family. Replaces the old gfx-only bpp table so a new type (e.g.
    // "audio."/BRR) is added by registering another descriptor rather than editing RowValidating.
    // Mirrors the codec dispatch in Diz.LogWriter (routing by AssetType prefix).

    /// <param name="RegionLength">
    /// Inclusive byte count (end - start + 1) -- the number of bytes the exporter will actually
    /// extract for this region.
    /// </param>
    private sealed record AssetTypeValidationContext(string AssetType, int RegionLength, JsonObject? Options);

    private sealed class AssetTypeUiValidator
    {
        /// <summary>Does this descriptor own the given AssetType string?</summary>
        public Func<string, bool> Matches { get; init; } = _ => false;

        /// <summary>Example type strings, surfaced in the "expected one of" error.</summary>
        public IReadOnlyList<string> ExampleTypes { get; init; } = [];

        /// <summary>Returns null when valid, else a user-facing error message.</summary>
        public Func<AssetTypeValidationContext, string?> Validate { get; init; } = _ => null;
    }

    private static readonly IReadOnlyList<AssetTypeUiValidator> AssetTypeUiValidators =
    [
        BuildGfxAssetValidator(),
        BuildBrrAssetValidator(),
        BuildTextAssetValidator(),
    ];

    // SNES BRR audio: audio.snes.brr. The stream is 9-byte ADPCM blocks (1 header + 8 data),
    // so its length must be a whole multiple of 9. Mirrors BrrRegionAssetExporter.Validate in
    // Diz.LogWriter. NOTE: the region must cover ONLY the BRR stream; if the sample has a
    // length/header prefix before the stream, that prefix stays in the parent region's assembly.
    private static AssetTypeUiValidator BuildBrrAssetValidator()
    {
        const string brrType = "audio.snes.brr";
        const int brrBlock = 9;

        return new AssetTypeUiValidator
        {
            Matches = t => string.Equals(t, brrType, StringComparison.Ordinal),
            ExampleTypes = [brrType],
            Validate = ctx =>
            {
                if (ctx.RegionLength <= 0 || ctx.RegionLength % brrBlock != 0)
                {
                    return $"Region length ({ctx.RegionLength} bytes) must be a whole multiple of " +
                           $"{brrBlock} bytes (one BRR ADPCM block) when Asset Type is '{brrType}'. " +
                           "The region must cover ONLY the BRR stream -- if the sample has a " +
                           "length/header prefix before the stream, exclude it.";
                }

                return null;
            },
        };
    }

    // SNES graphics: gfx.snes.{2,4,8}bpp. bpp/2 bitplane pairs, 2 bytes per row per pair,
    // cell_h rows; a partial cell at the end would silently produce garbage graphics, so reject.
    // Kept in sync (by shape) with RegionAssetUtil.ParseSnesGfxBpp in Diz.LogWriter.
    private static AssetTypeUiValidator BuildGfxAssetValidator()
    {
        var validBpp = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "gfx.snes.2bpp", 2 },
            { "gfx.snes.4bpp", 4 },
            { "gfx.snes.8bpp", 8 },
        };

        return new AssetTypeUiValidator
        {
            Matches = validBpp.ContainsKey,
            ExampleTypes = validBpp.Keys.ToList(),
            Validate = ctx =>
            {
                var bpp = validBpp[ctx.AssetType];

                var cellHeight = 8;
                if (ctx.Options != null
                    && ctx.Options.TryGetPropertyValue("cell_h", out var cellHeightNode)
                    && cellHeightNode != null)
                {
                    if (cellHeightNode.GetValueKind() != JsonValueKind.Number
                        || !cellHeightNode.AsValue().TryGetValue(out cellHeight)
                        || cellHeight < 1)
                    {
                        return "Asset Options: cell_h must be an integer >= 1.";
                    }
                }

                var cellSizeInBytes = bpp * cellHeight;
                if (ctx.RegionLength <= 0 || ctx.RegionLength % cellSizeInBytes != 0)
                {
                    var what = cellHeight == 8 ? $"one {bpp}bpp tile" : $"one {bpp}bpp 8x{cellHeight} cell";
                    return $"Region length ({ctx.RegionLength} bytes) must be a whole multiple of " +
                           $"{cellSizeInBytes} bytes ({what}) when Asset Type is '{ctx.AssetType}'.";
                }

                return null;
            },
        };
    }

    // Fixed-width name tables: text.ct.mapped. Mirrors TextRegionAssetExporter in Diz.LogWriter:
    // text assets REQUIRE options (tbl/record_width/pad have no defaults Diz could invent), and the
    // region must be a whole number of record_width-byte records -- the records carry no terminator,
    // so a ragged tail mis-frames every later record. Matched EXACTLY (like the gfx/brr validators),
    // not by "text." prefix: a near-miss such as "text.ct.mapped2" has no codec downstream and must
    // be rejected here, not accepted and then failed at build.
    private static AssetTypeUiValidator BuildTextAssetValidator()
    {
        const string mappedType = "text.ct.mapped";
        return new AssetTypeUiValidator
        {
            Matches = t => string.Equals(t, mappedType, StringComparison.Ordinal),
            ExampleTypes = [mappedType],
            Validate = ctx =>
            {
                if (ctx.Options == null)
                    return "Text assets require Asset Options, e.g. " +
                           "{\"tbl\": \"text/<table>.tbl\", \"record_width\": N, \"pad\": \"0xNN\"} " +
                           "(plus an optional \"tokens\" map).";

                if (!TryGetIntOption(ctx.Options, "record_width", out var recordWidth) || recordWidth < 1)
                    return "Asset Options: \"record_width\" must be an integer >= 1.";

                if (!TryGetNonEmptyStringOption(ctx.Options, "tbl", out _))
                    return "Asset Options: \"tbl\" must be a non-empty string path to the .tbl font map.";

                if (!TryGetNonEmptyStringOption(ctx.Options, "pad", out var pad) || !TryParseByteLiteral(pad, out _))
                    return "Asset Options: \"pad\" must be a byte literal like \"0xEF\" (0..255).";

                if (ctx.RegionLength <= 0 || ctx.RegionLength % recordWidth != 0)
                    return $"Region length ({ctx.RegionLength} bytes) must be a whole multiple of " +
                           $"record_width ({recordWidth}) when Asset Type is '{ctx.AssetType}'. " +
                           "Fixed-width records have no terminator, so a ragged tail mis-frames " +
                           "every later record -- adjust the bounds or record_width.";

                return null;
            },
        };
    }

    private static bool TryGetIntOption(JsonObject options, string key, out int value)
    {
        value = 0;
        return options.TryGetPropertyValue(key, out var node) && node != null
            && node.GetValueKind() == JsonValueKind.Number
            && node.AsValue().TryGetValue(out value);
    }

    private static bool TryGetNonEmptyStringOption(JsonObject options, string key, out string value)
    {
        value = "";
        if (!options.TryGetPropertyValue(key, out var node) || node == null
            || node.GetValueKind() != JsonValueKind.String)
            return false;
        value = node.GetValue<string>();
        return !string.IsNullOrWhiteSpace(value);
    }

    // A byte literal like "0xEF" or "239" (0..255). Mirrors TextRegionAssetExporter.TryParseByteLiteral.
    private static bool TryParseByteLiteral(string s, out int value)
    {
        s = s.Trim();
        var ok = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? int.TryParse(s.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)
            : int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        return ok && value is >= 0 and <= 0xFF;
    }

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