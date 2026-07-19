using System.Diagnostics.CodeAnalysis;

namespace Diz.Ui.Winforms.dialogs;

// Plain host window for LabelsViewControl, which is the actual ILabelEditorView
// implementation (registered via DizUiWinformsCompositionRoot). This form's only behavior
// of its own: hide instead of close, so reopening the editor is instant and state is kept.
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class LabelEditorForm : Form
{
    public LabelEditorForm()
    {
        InitializeComponent();
    }

    internal usercontrols.LabelsViewControl LabelEditor => labelsViewControl1;

    private void LabelEditorForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;
        e.Cancel = true;
        Hide();
    }
}
