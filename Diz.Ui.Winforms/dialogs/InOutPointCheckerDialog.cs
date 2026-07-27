namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// Modal host asking whether to rescan every instruction for in/out/end/read points. No
/// ViewModel, on purpose: the window has no state, no inputs and no backend calls of its own --
/// it explains what a rescan does and takes a yes or a no. Running the rescan belongs to
/// whoever opened it.
/// </summary>
public partial class InOutPointCheckerDialog : Form
{
    public InOutPointCheckerDialog() => InitializeComponent();

    private void cancel_Click(object sender, EventArgs e) => Close();

    private void rescan_Click(object sender, EventArgs e) => DialogResult = DialogResult.OK;
}
