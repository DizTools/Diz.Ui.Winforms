using System.ComponentModel;
using Diz.Ui.ViewModels.MisalignmentChecker;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// Modal host for <see cref="MisalignmentCheckerViewModel"/>: press Scan to sweep the ROM for
/// bytes whose flags contradict the instruction or data item they belong to, and read the list
/// it produces. The sweep itself is the ViewModel's caller-seeded delegate, so this file is
/// widget wiring only and never touches project data.
///
/// The window does not fix anything either: Fix only confirms, and whoever opened the window
/// applies the repair. That is why Fix works before anything has been scanned, exactly as it
/// always did -- the instruction paragraph offers fixing as an alternative to reading the
/// report, not as a step after it.
/// </summary>
public partial class MisalignmentCheckerDialog : Form
{
    private readonly MisalignmentCheckerViewModel viewModel;

    /// <param name="viewModel">Runs the scan and holds what it found.</param>
    public MisalignmentCheckerDialog(MisalignmentCheckerViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        this.viewModel = viewModel;

        InitializeComponent();

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        FormClosed += (_, _) => viewModel.PropertyChanged -= ViewModel_PropertyChanged;

        RefreshAllWidgets();
    }

    // ------------------------------------------------------------------ ViewModel -> widgets

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MisalignmentCheckerViewModel.ReportText):
                textLog.Text = viewModel.ReportText;
                break;

            case nameof(MisalignmentCheckerViewModel.StatusText):
                labelStatus.Text = viewModel.StatusText;
                break;
        }
    }

    private void RefreshAllWidgets()
    {
        textLog.Text = viewModel.ReportText;
        labelStatus.Text = viewModel.StatusText;
    }

    // ------------------------------------------------------------------ widgets -> ViewModel

    private void buttonScan_Click(object sender, EventArgs e) => viewModel.Scan();

    private void buttonFix_Click(object sender, EventArgs e) => DialogResult = DialogResult.OK;

    private void cancel_Click(object sender, EventArgs e) => Close();
}
