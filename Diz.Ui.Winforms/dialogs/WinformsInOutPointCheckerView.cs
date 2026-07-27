using System.Threading.Tasks;
using Diz.Controllers.interfaces;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// The WinForms in/out-point rescan confirmation. ShowDialog blocks until the user is done, so
/// the whole interaction happens inside <see cref="ConfirmAsync"/> and the returned task is
/// already completed -- awaiting it continues synchronously on the UI thread, which is what
/// keeps this backend's behavior identical to a plain blocking modal call.
/// </summary>
public sealed class WinformsInOutPointCheckerView : IInOutPointCheckerView
{
    public Task<bool> ConfirmAsync()
    {
        using var dialog = new InOutPointCheckerDialog();
        return Task.FromResult(dialog.ShowDialog() == DialogResult.OK);
    }
}
