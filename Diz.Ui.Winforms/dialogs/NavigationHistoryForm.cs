using System.ComponentModel;
using Diz.Ui.ViewModels.Navigation;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// Plain host window for <see cref="usercontrols.NavigationHistoryViewControl"/>, which is where
/// all the wiring lives. Named for the region-list precedent (RegionListForm hosting
/// RegionListViewControl): a long-lived window the main window resolves once and hides rather
/// than closes.
///
/// This window's only behaviour of its own: hide instead of close, so reopening the history is
/// instant and the scroll position is kept. Note that closing it never loses the history OR the
/// place in it -- both live in the ViewModel, which the main window owns; back and forward work
/// with this window closed, and with it never having been opened at all.
/// </summary>
public partial class NavigationHistoryForm : Form
{
    public NavigationHistoryForm()
    {
        InitializeComponent();
        FormClosing += NavigationHistoryForm_FormClosing;
    }

    /// <summary>The history being shown. Forwarded straight to the control; see it for the rules.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public NavigationHistoryViewModel? ViewModel
    {
        get => navigationCtrl.ViewModel;
        set => navigationCtrl.ViewModel = value;
    }

    /// <summary>
    /// Overshoot the in-window ← → buttons ask for; seeded by the host so it matches the
    /// back/forward menu commands.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int BackForwardOvershoot
    {
        get => navigationCtrl.BackForwardOvershoot;
        set => navigationCtrl.BackForwardOvershoot = value;
    }

    private void NavigationHistoryForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing)
            return;

        e.Cancel = true;
        Hide();
    }
}
