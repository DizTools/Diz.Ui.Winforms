using System.Threading.Tasks;
using Diz.Controllers.interfaces;
using Diz.Cpu._65816;
using Diz.Ui.ViewModels.MarkMany;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// The WinForms mark-many window. ShowDialog blocks until the user is done, so the whole
/// interaction happens inside <see cref="EditAsync"/> and the returned task is already
/// completed -- awaiting it continues synchronously on the UI thread, which is what keeps
/// this backend's behavior identical to a plain blocking modal call.
/// </summary>
public sealed class WinformsMarkManyView : IMarkManyView
{
    public Task<bool> EditAsync(MarkManyViewModel<ISnesData> viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        using var dialog = new MarkManyDialog(viewModel);
        return Task.FromResult(dialog.ShowDialog() == DialogResult.OK);
    }
}
