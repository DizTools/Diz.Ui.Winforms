using System.Reflection;
using Diz.Controllers.interfaces;
using Diz.Core.Interfaces;

namespace Diz.Ui.Winforms.dialogs;

internal partial class About : Form, IFormViewer
{
    protected IAppVersionInfo AppVersionInfo { get; }
    public event EventHandler? OnFormClosed;

    public About(IAppVersionInfo appVersionInfo)
    {
        AppVersionInfo = appVersionInfo;
        
        Closed += (sender, args) => OnFormClosed?.Invoke(sender, args);
        
        InitializeComponent();
        Init();
    }

    private void Init()
    {
        var versionInfo = AppVersionInfo.GetVersionInfo(IAppVersionInfo.AppVersionInfoType.Version);
        var fullDescription = AppVersionInfo.GetVersionInfo(IAppVersionInfo.AppVersionInfoType.FullDescription);
     
        Text = $"About Diz {versionInfo}";
        labelVersion.Text = $"Version: {versionInfo}";
        textBoxDescription.Text = fullDescription;
        labelCopyright.Text = ""; // garbage
        labelCompanyName.Text = "";  // garbage
    }
    
    private void okButton_Click(object sender, EventArgs e) => Close();
    public void BringFormToTop() => Focus();
}