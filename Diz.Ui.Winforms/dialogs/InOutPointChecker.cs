namespace Diz.Ui.Winforms.dialogs;

public partial class InOutPointChecker : Form
{
    public InOutPointChecker() => InitializeComponent();

    private void cancel_Click(object sender, EventArgs e) => Close();

    private void rescan_Click(object sender, EventArgs e) => DialogResult = DialogResult.OK;
}