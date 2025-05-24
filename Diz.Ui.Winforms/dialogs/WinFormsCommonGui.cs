using Diz.Controllers.interfaces;
using Diz.Ui.Winforms.util;

namespace Diz.Ui.Winforms.dialogs;

// ReSharper disable once ClassNeverInstantiated.Global
public class WinFormsCommonGui : ICommonGui
{
    public bool PromptToConfirmAction(string msg) => 
        WinformsGuiUtil.PromptToConfirmAction("Warning", msg, () => true);

    public void ShowError(string msg) => 
        MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    
    public void ShowWarning(string msg) => 
        MessageBox.Show(msg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    
    public void ShowMessage(string msg) => 
        MessageBox.Show(msg, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
}