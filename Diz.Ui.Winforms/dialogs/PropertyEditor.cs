namespace Diz.Ui.Winforms.dialogs;

public sealed class GenericPropertyEditorForm : Form
{
    private PropertyGrid propertyGrid;

    public GenericPropertyEditorForm(object items, string windowTitle = "Edit Properties")
    {
        Width = 800;
        Height = 600;
        Text = windowTitle;
        
        propertyGrid = new PropertyGrid
        {
            Dock = DockStyle.Fill,
            SelectedObject = items,
        };
        
        Controls.Add(propertyGrid);
        
        Load += Form_Load;
        Shown += Form_Shown;
    }

    private void Form_Load(object? sender, EventArgs e) { }
    private void Form_Shown(object? sender, EventArgs e) {
        ExpandAllGridItems();
    }
    
    private void ExpandAllGridItems()
    {
        var root = propertyGrid.SelectedGridItem;
        while (root.Parent != null) {
            root = root.Parent;
        }

        ExpandAllGridItems(root);
    }

    private static void ExpandAllGridItems(GridItem? gridItem)
    {
        if (gridItem == null)
            return;

        foreach (GridItem item in gridItem.GridItems)
        {
            if (item.GridItemType != GridItemType.Category && !item.Expandable) 
                continue;
            
            item.Expanded = true;
            ExpandAllGridItems(item);
        }
    }
}