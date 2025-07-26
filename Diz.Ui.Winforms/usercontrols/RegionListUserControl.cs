using Diz.Controllers.interfaces;
using Diz.Core.Interfaces;
using System.ComponentModel;

namespace Diz.Ui.Winforms.usercontrols;

public partial class RegionListUserControl : UserControl, IRegionListView
{
    private IProjectController? projectController;
    private readonly BindingSource bindingSource = new();

    public RegionListUserControl()
    {
        InitializeComponent();
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
        
        regionGridView.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn
            {
                Name = "StartSnesAddress",
                DataPropertyName = "StartSnesAddress",
                HeaderText = "Start Address",
                Width = 100
            },
            new DataGridViewTextBoxColumn
            {
                Name = "EndSnesAddress", 
                DataPropertyName = "EndSnesAddress",
                HeaderText = "End Address",
                Width = 100
            },
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

    private void BindingSource_AddingNew(object sender, AddingNewEventArgs e)
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
            MessageBox.Show("Region Name is required.", "Validation Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }
        
        // Validate address range
        if (!int.TryParse(row.Cells["StartSnesAddress"].Value?.ToString(), out var start) ||
            !int.TryParse(row.Cells["EndSnesAddress"].Value?.ToString(), out var end)) 
            return;

        if (start < end) 
            return;
        
        MessageBox.Show("Start address must be less than end address.", 
            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        e.Cancel = true;
    }
    
    private void DeleteRegion(int rowIndex)
    {
        try
        {
            if (rowIndex >= 0 && rowIndex < bindingSource.Count)
            {
                bindingSource.RemoveAt(rowIndex);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting region: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    // Additional utility methods
    public void AddRegion(IRegion region)
    {
        try
        {
            bindingSource.Add(region);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding region: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
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