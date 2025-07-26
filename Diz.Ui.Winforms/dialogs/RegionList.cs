using Diz.Controllers.interfaces;

namespace Diz.Ui.Winforms.dialogs;

public partial class RegionList : Form, IRegionListView
{
    public RegionList()
    {
        InitializeComponent();
        FormClosing += RegionList_FormClosing;
    }

    public new event EventHandler? OnFormClosed
    {
        add => regionListUserControl1.OnFormClosed += value;
        remove => regionListUserControl1.OnFormClosed -= value;
    }

    private void RegionList_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;
        e.Cancel = true;
        Hide();
    }
    

    public void BringFormToTop() => regionListUserControl1.BringFormToTop();
    public void SetProjectController(IProjectController? projectController) => regionListUserControl1.SetProjectController(projectController);
    public void RebindProject() => regionListUserControl1.RebindProject();
}