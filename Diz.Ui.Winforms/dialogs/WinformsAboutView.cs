using Diz.Controllers.interfaces;
using Diz.Core.Interfaces;
using Diz.Ui.Winforms.util;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// The WinForms "About Diz" window's view service.
///
/// No ViewModel comes in and none is built: the window displays two strings from the version
/// source and takes an OK (see <see cref="IAboutView"/>).
///
/// ONE window for the whole run. The window hides rather than closes and this service keeps it,
/// so picking Help -> About repeatedly brings the same window forward instead of stacking a new
/// copy behind the last one. That is why this seam is registered as a singleton. A disposed
/// window is still replaced -- the application can tear one down at shutdown, and Show() on a
/// disposed form throws.
/// </summary>
public sealed class WinformsAboutView : IAboutView
{
    private readonly IAppVersionInfo appVersionInfo;

    private AboutDialog? window;

    /// <param name="appVersionInfo">Supplies the version string and the full build description.</param>
    public WinformsAboutView(IAppVersionInfo appVersionInfo)
    {
        ArgumentNullException.ThrowIfNull(appVersionInfo);
        this.appVersionInfo = appVersionInfo;
    }

    public void Show()
    {
        if (window is not { IsDisposed: false })
            window = new AboutDialog(appVersionInfo);

        window.Show();
        window.BringWinFormToTop();
    }
}
