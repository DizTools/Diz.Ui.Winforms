using System.Threading.Tasks;
using Diz.Controllers.interfaces;
using Diz.Ui.ViewModels.MisalignmentChecker;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// The WinForms misaligned-flags window. ShowDialog blocks until the user is done, so the whole
/// interaction happens inside <see cref="RunAsync"/> and the returned task is already
/// completed -- awaiting it continues synchronously on the UI thread, which is what keeps
/// this backend's behavior identical to a plain blocking modal call.
/// </summary>
public sealed class WinformsMisalignmentCheckerView : IMisalignmentCheckerView
{
    public Task<bool> RunAsync(MisalignmentCheckerViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        using var dialog = new MisalignmentCheckerDialog(viewModel);
        return Task.FromResult(dialog.ShowDialog() == DialogResult.OK);
    }
}
