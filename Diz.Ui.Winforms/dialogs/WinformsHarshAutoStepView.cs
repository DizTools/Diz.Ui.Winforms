using System.Threading.Tasks;
using Diz.Controllers.interfaces;
using Diz.Ui.ViewModels.HarshAutoStep;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// The WinForms harsh-auto-step window. ShowDialog blocks until the user is done, so the whole
/// interaction happens inside <see cref="EditAsync"/> and the returned task is already
/// completed -- awaiting it continues synchronously on the UI thread, which is what keeps
/// this backend's behavior identical to a plain blocking modal call.
/// </summary>
public sealed class WinformsHarshAutoStepView : IHarshAutoStepView
{
    public Task<bool> EditAsync(HarshAutoStepViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        using var dialog = new HarshAutoStepDialog(viewModel);
        return Task.FromResult(dialog.ShowDialog() == DialogResult.OK);
    }
}
