using Diz.Controllers.interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Diz.Ui.Winforms.dialogs;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class AliasList : Form, ILabelEditorView
{
    // this class is mostly a wrapper for LabelsViewControl, which is a usercontrol on this form
    public event EventHandler? OnFormClosed;
    
    public AliasList()
    {
        InitializeComponent();
    }

    private void AliasList_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;
        e.Cancel = true;
        Hide();
    }

    public void BringFormToTop()
    {
        Focus();
    }

    // get rid of these 2 if we can:
    public string PromptForCsvFilename() => labelsViewControl1.PromptForCsvFilename();
    public void ShowLineItemError(string exMessage, int errLine) => labelsViewControl1.ShowLineItemError(exMessage, errLine);
    
    
    public void SetProjectController(IProjectController? projectController) => labelsViewControl1.SetProjectController(projectController);
    public void RepopulateFromData() => labelsViewControl1.RepopulateFromData();
    public void RebindProject() => labelsViewControl1.RebindProject();
    public void FocusOrCreateLabelAtSelectedRomOffsetIa() => labelsViewControl1.FocusOrCreateLabelAtSelectedRomOffsetIa();
    public void FocusOrCreateLabelAtRomOffsetIa(int selectedOffset) => labelsViewControl1.FocusOrCreateLabelAtRomOffsetIa(selectedOffset);
    public void FocusOrCreateLabelAtSnesAddress(int snesAddress) => labelsViewControl1.FocusOrCreateLabelAtSnesAddress(snesAddress);
}