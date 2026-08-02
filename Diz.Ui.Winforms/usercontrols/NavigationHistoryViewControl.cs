using System.ComponentModel;
using Diz.Controllers.interfaces;
using Diz.Core.model;
using Diz.Ui.ViewModels.Navigation;

namespace Diz.Ui.Winforms.usercontrols;

/// <summary>
/// Thin host over <see cref="NavigationHistoryViewModel"/>: the grid renders the ViewModel's
/// entries, the toolbar buttons and row activation call ViewModel commands, and nothing in this
/// file navigates or decides anything. The ViewModel is owned by the main window and OUTLIVES this
/// control -- back/forward are main-menu commands that work whether or not this window was ever
/// opened -- so this control only ever borrows it, and never disposes it.
///
/// RECORDING A POINT MUST NOT NAVIGATE TO IT. The control this replaces wired
/// BindingSource.CurrentChanged straight at "navigate", so appending an entry moved the position,
/// which re-navigated to the entry that had just been recorded -- and with no overshoot, undoing
/// the overshoot the real navigation had only just applied. That wiring is gone, and so is every
/// other route from "the grid's selection moved" to "navigate":
///
/// NAVIGATION IS DRIVEN BY ROW ACTIVATION ONLY -- double-click, or Enter on the focused row. Both
/// come from real user input and neither can be raised by the grid on its own, which is the whole
/// point. The grid moves its own selection all the time without being asked: when the first row
/// appears in an empty grid, and again when a bound grid materializes its rows on being realized
/// (that one lands on row 0 while the user is on the newest entry, so a selection-driven design
/// would jump the user to the OLDEST point in their history just for opening the window). Single-
/// clicking a row therefore only selects it now, where before it also navigated -- that was the
/// deleted CurrentChanged path, and it is not worth resurrecting a class of bug to keep.
///
/// The highlight follows the ViewModel the other way round, off three events: the ViewModel's
/// CurrentIndex, the BindingSource's ListChanged, and the grid finishing a binding pass. The list
/// events are needed because the ViewModel and the BindingSource watch the same underlying
/// BindingList and the ViewModel is subscribed first (it is built before any window exists), so
/// when CurrentIndex moves on an append the grid has no such row yet.
///
/// N3: this control is also the WinForms backend's <see cref="INavigationHistoryView"/> -- the
/// seam MainWindow resolves through IViewFactory. Same shape as the region editor: the CONTROL
/// implements the interface and the registration hands it out already sitting inside its host
/// form, so Show()/BringFormToTop() have a window to operate on.
/// </summary>
public partial class NavigationHistoryViewControl : UserControl, INavigationHistoryView
{
    private NavigationHistoryViewModel? viewModel;

    public NavigationHistoryViewControl()
    {
        InitializeComponent();

        dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGridView1.MultiSelect = false;

        // wired here rather than in the designer: these are behaviour, not widget properties.
        dataGridView1.KeyDown += Grid_KeyDown;
        dataGridView1.DataBindingComplete += Grid_DataBindingComplete;
        navigationEntryBindingSource.ListChanged += BindingSource_ListChanged;

        Disposed += (_, _) => ViewModel = null;
    }

    /// <summary>
    /// The history to show. A settable property rather than a constructor argument because the
    /// designer builds this control; the host assigns the ViewModel it already owns. Assigning
    /// null detaches without disposing -- the ViewModel belongs to the host, not to this window.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public NavigationHistoryViewModel? ViewModel
    {
        get => viewModel;
        set
        {
            if (ReferenceEquals(viewModel, value))
                return;

            if (viewModel != null)
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            viewModel = value;

            // with no ViewModel the grid falls back to the entry TYPE, which is what the designer
            // binds it to: the columns stay, the rows go.
            navigationEntryBindingSource.DataSource =
                (object?) viewModel?.BindableEntries ?? typeof(NavigationEntry);

            if (viewModel != null)
                viewModel.PropertyChanged += ViewModel_PropertyChanged;

            SyncGridSelectionFromViewModel();
        }
    }

    /// <summary>
    /// Overshoot the ← and → buttons ask for. Seeded by the host with the same number its own
    /// back/forward menu commands use, so "go back" means one thing however it is triggered. Row
    /// activation deliberately keeps <see cref="NavigationHistoryViewModel.NoOvershoot"/>, landing
    /// exactly on the row the user pointed at.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int BackForwardOvershoot { get; set; } = NavigationHistoryViewModel.NoOvershoot;

    // ------------------------------------------------------------------ INavigationHistoryView

    // declared, never raised: the host form hides on close, so from a caller's point of view this
    // "form" never closes. Identical to the region editor.
    public event EventHandler? OnFormClosed;

    // hides Control.Show(): callers of INavigationHistoryView.Show() expect the WINDOW to appear.
    public new void Show() => FindForm()?.Show();

    public void BringFormToTop() => FindForm()?.Focus();

    // ------------------------------------------------------------------ input

    private void btnBack_Click(object sender, EventArgs e) => viewModel?.MoveBack(BackForwardOvershoot);
    private void btnForward_Click(object sender, EventArgs e) => viewModel?.MoveForward(BackForwardOvershoot);
    private void btnClearHistory_Click(object sender, EventArgs e) => viewModel?.Clear();

    private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex < 0)
            return;

        // asks again even for the row already selected: double-clicking where you already are is
        // how the user re-centres the view on it.
        viewModel?.SelectEntry(e.RowIndex, NavigationHistoryViewModel.NoOvershoot);
    }

    /// <summary>Enter activates the focused row: the keyboard equivalent of double-clicking it.</summary>
    private void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter || viewModel == null)
            return;

        var index = dataGridView1.CurrentRow?.Index ?? NavigationHistoryViewModel.NoSelection;
        if (index < 0)
            return;

        // the grid's own Enter moves down a row; activating is what the user meant here.
        e.Handled = true;
        viewModel.SelectEntry(index, NavigationHistoryViewModel.NoOvershoot);
    }

    // ------------------------------------------------------------------ display

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NavigationHistoryViewModel.CurrentIndex))
            SyncGridSelectionFromViewModel();
    }

    private void BindingSource_ListChanged(object? sender, ListChangedEventArgs e) =>
        SyncGridSelectionFromViewModel();

    // the grid has just (re)built its rows -- on being realized, or on a rebind. It puts its own
    // caret on row 0 doing so; put it back where the ViewModel says the user is.
    private void Grid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e) =>
        SyncGridSelectionFromViewModel();

    private void SyncGridSelectionFromViewModel()
    {
        var index = viewModel?.CurrentIndex ?? NavigationHistoryViewModel.NoSelection;
        if (index < 0 || index >= navigationEntryBindingSource.Count)
            return;

        // safe to move freely: nothing turns a selection change into a navigation.
        navigationEntryBindingSource.Position = index;
    }
}
