using System.Diagnostics.CodeAnalysis;

namespace Diz.Ui.Winforms.dialogs;

// Plain host window for RegionListViewControl, which is the actual IRegionListView
// implementation (registered via DizUiWinformsCompositionRoot). This form's only behavior of its
// own: hide instead of close, so reopening the region editor is instant and the grid's sort
// order, selection and scroll position are kept.
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class RegionListForm : Form
{
    public RegionListForm()
    {
        InitializeComponent();
    }

    internal usercontrols.RegionListViewControl RegionEditor => regionListViewControl1;

    private void RegionListForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;
        e.Cancel = true;
        Hide();
    }
}
