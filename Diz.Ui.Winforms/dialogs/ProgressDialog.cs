using System.ComponentModel;
using Diz.Controllers.interfaces;
using Diz.Ui.Winforms.util;

namespace Diz.Ui.Winforms.dialogs;

public partial class ProgressDialog : Form, IProgressView
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsMarquee
    {
        get => isMarquee;
        set => this.InvokeIfRequired(() => UpdateProgressBarStyle(value));
    }
        
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? TextOverride
    {
        get => textOverride;
        set => this.InvokeIfRequired(() => UpdateTextOverride(value));
    }
        
    private bool isMarquee;
    private string? textOverride;

    public ProgressDialog()
    {
        InitializeComponent();
        progressBar1.Value = 0;
        progressBar1.Maximum = 100;
    }

    private void UpdateProgressBarStyle(bool isMarqueeType)
    {
        isMarquee = isMarqueeType;
        progressBar1.Style = isMarqueeType
            ? ProgressBarStyle.Marquee
            : ProgressBarStyle.Continuous;
    }

    private void UpdateTextOverride(string? value)
    {
        textOverride = value;
        UpdateProgressText();
    }
        
    public void Report(int i)
    {
        this.InvokeIfRequired(() =>
        {
            progressBar1.Value = i;
            UpdateProgressText();
        });
    }

    private void UpdateProgressText()
    {
        if (textOverride != null)
        {
            lblStatusText.Text = textOverride;
            return;
        }

        var percentDone = (int) (100 * (progressBar1.Value / (float) progressBar1.Maximum));
        lblStatusText.Text = $@"{percentDone}%";
    }

    public void SignalJobIsDone() => this.InvokeIfRequired(Close);
    public bool PromptDialog() => ShowDialog() == DialogResult.OK;
}