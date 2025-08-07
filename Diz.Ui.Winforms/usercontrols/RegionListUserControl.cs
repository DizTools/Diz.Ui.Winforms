using Diz.Controllers.interfaces;
using Diz.Core.Interfaces;
using System.ComponentModel;
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
        CreateErrorLabel();
        ConfigureDataGridView();
        AttachEventHandlers();
    }
    
    private void CreateErrorLabel()
    {
        errorLabel = new Label
        {
            Name = "errorLabel",
            Text = "",
            ForeColor = Color.Red,
            BackColor = Color.LightYellow,
            AutoSize = false,
            Height = 25,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(5, 0, 5, 0),
            Visible = false // Initially hidden
        };
        
        Controls.Add(errorLabel);
        errorLabel.BringToFront();
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
        HeaderText = "Start Address",
        Width = 100
    };
    
    var endAddressColumn = new DataGridViewTextBoxColumn
    {
        Name = "EndSnesAddress", 
        DataPropertyName = "EndSnesAddress",
        HeaderText = "End Address",
        Width = 100
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
            HeaderText = "Context",
            Width = 120
        },
        new DataGridViewTextBoxColumn
        {
            Name = "Priority",
            DataPropertyName = "Priority",
            HeaderText = "Priority", 
            Width = 80
        },
        new DataGridViewButtonColumn
        {
            Name = "Actions",
            HeaderText = "Actions",
            Text = "Delete",
            UseColumnTextForButtonValue = true,
            Width = 80
        }
    });
}

private void AttachEventHandlers()
{
    regionGridView.CellContentClick += RegionGridView_CellContentClick;
    regionGridView.UserDeletingRow += RegionGridView_UserDeletingRow;
    regionGridView.RowValidating += RegionGridView_RowValidating;
    regionGridView.DataError += RegionGridView_DataError; // Add this line
    
    // Add these event handlers for hex conversion
    regionGridView.CellFormatting += RegionGridView_CellFormatting;
    regionGridView.CellParsing += RegionGridView_CellParsing;
}

private void RegionGridView_DataError(object? sender, DataGridViewDataErrorEventArgs e)
{
    // Handle data errors to replace the default dialog
    var columnName = regionGridView.Columns[e.ColumnIndex].HeaderText;
    var errorMessage = $"Data error in {columnName}: {e.Exception?.Message ?? "Invalid data format"}";
    
    // Display error in our custom label
    ShowErrorMessage(errorMessage);
    
    // Prevent the default error dialog from showing
    e.ThrowException = false;
    
    // You can also log the error if needed
    System.Diagnostics.Debug.WriteLine($"DataGridView error: {errorMessage}");
}

private void ShowErrorMessage(string message)
{
    errorLabel.Text = message;
    errorLabel.Visible = true;
    
    // Auto-hide the error message after 5 seconds
    var timer = new System.Windows.Forms.Timer();
    timer.Interval = 5000; // 5 seconds
    timer.Tick += (s, e) =>
    {
        HideErrorMessage();
        timer.Dispose();
    };
    timer.Start();
}

private void HideErrorMessage()
{
    errorLabel.Visible = false;
    errorLabel.Text = "";
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
        // Create a new region when user tries to add a row
        e.NewObject = projectController?.Project?.Data?.CreateNewRegion();
    }
    
    private void RegionGridView_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        var actionColumn = regionGridView.Columns["Actions"];
        if (actionColumn == null || e.ColumnIndex != actionColumn.Index || e.RowIndex < 0) 
            return;
        
        // Handle delete button click
        var result = MessageBox.Show("Are you sure you want to delete this region?", 
            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
        if (result == DialogResult.Yes)
        {
            DeleteRegion(e.RowIndex);
        }
    }
    
    private void RegionGridView_UserDeletingRow(object? sender, DataGridViewRowCancelEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to delete this region?", 
            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
        e.Cancel = result != DialogResult.Yes;
    }
    
    private void RegionGridView_RowValidating(object? sender, DataGridViewCellCancelEventArgs e)
    {
        var row = regionGridView.Rows[e.RowIndex];
        
        // Skip validation for new row
        if (row.IsNewRow) return;
        
        // Validate required fields
        if (string.IsNullOrWhiteSpace(row.Cells["RegionName"].Value?.ToString()))
        {
            ShowErrorMessage("Region Name is required.");
            e.Cancel = true;
            return;
        }
        
        // Validate address range
        if (!int.TryParse(row.Cells["StartSnesAddress"].Value?.ToString(), out var start) ||
            !int.TryParse(row.Cells["EndSnesAddress"].Value?.ToString(), out var end)) 
            return;

        if (start < end) 
            return;
        
        ShowErrorMessage("Start address must be less than end address.");
        e.Cancel = true;
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