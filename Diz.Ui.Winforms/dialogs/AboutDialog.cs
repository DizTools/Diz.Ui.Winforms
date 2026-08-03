using Diz.Core.Interfaces;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// Version and build information about the running application: two strings from the version
/// source, a logo, and an OK button. No state, no input, no backend calls of its own -- so no
/// ViewModel.
///
/// Hides instead of closing, so the host can re-show this same window rather than stacking a new
/// copy behind the old one every time Help -> About is picked.
/// </summary>
internal partial class AboutDialog : Form
{
    private IAppVersionInfo AppVersionInfo { get; }

    public AboutDialog(IAppVersionInfo appVersionInfo)
    {
        AppVersionInfo = appVersionInfo;

        InitializeComponent();
        Init();
    }

    private void Init()
    {
        var versionInfo = AppVersionInfo.GetVersionInfo(IAppVersionInfo.AppVersionInfoType.Version);
        var fullDescription = AppVersionInfo.GetVersionInfo(IAppVersionInfo.AppVersionInfoType.FullDescription);

        Text = $"About Diz {versionInfo}";
        labelProductName.Text = "DiztinGUIsh";
        labelVersion.Text = $"Version: {versionInfo}";
        textBoxDescription.Text = fullDescription;
    }

    // Hide, not Close: a programmatic Close() does not report itself as a user closing the
    // window, so FormClosing below would let it through and dispose the one window the host
    // keeps. Escape reaches here too -- okButton is the form's CancelButton.
    private void okButton_Click(object sender, EventArgs e) => Hide();

    private void AboutDialog_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;
        e.Cancel = true;
        Hide();
    }
}
