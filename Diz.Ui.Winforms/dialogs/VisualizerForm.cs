using Diz.Core.model;

namespace Diz.Ui.Winforms.dialogs;

public partial class VisualizerForm : Form
{
    private readonly IProject project;

    public VisualizerForm(IProject project)
    {
        this.project = project;
        InitializeComponent();
    }

    private void VisualizerForm_Load(object sender, System.EventArgs e)
    {
        // hack to make room for the scrollbar
        // I wish docking dealt with this, or maybe I set it up wrong...
        Width = romFullVisualizer1.Width + 40;

        romFullVisualizer1.Project = project;
    }

    // private void ProjectController_ProjectChanged(object sender, IProjectController.ProjectChangedEventArgs e)
    // {
    //     Project = e.Project;
    // }

    private void VisualizerForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;
        e.Cancel = true;
        Hide();
    }
}