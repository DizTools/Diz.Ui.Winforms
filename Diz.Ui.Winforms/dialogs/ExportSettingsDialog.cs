using System.ComponentModel;
using Diz.Controllers.interfaces;
using Diz.Core.export;
using Diz.Ui.ViewModels.ExportSettings;
using Diz.Ui.Winforms.util;

namespace Diz.Ui.Winforms.dialogs;

/// <summary>
/// Modal host for <see cref="ExportSettingsViewModel"/>: how each line of the generated assembly is
/// written, where it goes, and which optional pieces are included. Everything that decides what
/// those choices mean lives in the ViewModel; this file is widget wiring plus the two things a
/// ViewModel is not allowed to do -- put a question to the user, and open a file picker.
///
/// THE COMBO BOXES CARRY THEIR OWN DISPLAY TEXT. The two enums have no descriptions attached, so
/// there is nothing to render them from generically; the item lists are laid out in the designer
/// and mapped here by position. The mapping is written out explicitly rather than cast from the
/// selected index, so re-ordering a designer item list can no longer silently change what a
/// stored setting means.
///
/// THE DISK IS ASKED ABOUT THE OUTPUT PATH ONLY WHEN THE PATH SETTLES -- on Browse, on leaving the
/// path box, and when the export is started. Validation runs on every keystroke, but against the
/// remembered answer.
/// </summary>
public partial class ExportSettingsDialog : Form
{
    /// <summary>The picker filter used when the export goes to one file rather than one per bank.</summary>
    private const string SingleFileFilter = "Assembly Files|*.asm|All Files|*.*";

    /// <summary>Designer item order for <c>comboStructure</c>.</summary>
    private static readonly LogWriterSettings.FormatStructure[] StructureItems =
    [
        LogWriterSettings.FormatStructure.SingleFile,
        LogWriterSettings.FormatStructure.OneBankPerFile,
    ];

    /// <summary>Designer item order for <c>comboUnlabeled</c>.</summary>
    private static readonly LogWriterSettings.FormatUnlabeled[] UnlabeledItems =
    [
        LogWriterSettings.FormatUnlabeled.ShowAll,
        LogWriterSettings.FormatUnlabeled.ShowInPoints,
        LogWriterSettings.FormatUnlabeled.ShowNone,
    ];

    private readonly ExportSettingsViewModel viewModel;
    private readonly IFileDialogService fileDialogService;

    // true while widget values are being written FROM the ViewModel; the input handlers below bail
    // out then, so a ViewModel-driven refresh can't be mistaken for the user typing. Without it
    // every refresh echoes straight back into the ViewModel.
    private bool updatingWidgets;

    /// <param name="viewModel">Holds the settings being edited, and everything that validates them.</param>
    /// <param name="fileDialogService">Opens the file/folder picker behind the Browse button.</param>
    public ExportSettingsDialog(ExportSettingsViewModel viewModel, IFileDialogService fileDialogService)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(fileDialogService);

        this.viewModel = viewModel;
        this.fileDialogService = fileDialogService;

        InitializeComponent();

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        FormClosed += (_, _) => viewModel.PropertyChanged -= ViewModel_PropertyChanged;

        RefreshAllWidgets();
    }

    // ------------------------------------------------------------------ ViewModel -> widgets

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ExportSettingsViewModel.LineTemplate):
                WriteText(textFormat, viewModel.LineTemplate);
                break;

            case nameof(ExportSettingsViewModel.OutputPath):
                WriteText(txtExportPath, viewModel.OutputPath);
                break;

            case nameof(ExportSettingsViewModel.ExcludedAuthorsText):
                WriteText(txtExcludeLabelAuthors, viewModel.ExcludedAuthorsText);
                break;

            case nameof(ExportSettingsViewModel.SampleOutputText):
                textSample.Text = viewModel.SampleOutputText;
                break;

            case nameof(ExportSettingsViewModel.CanStartExport):
                disassembleButton.Enabled = viewModel.CanStartExport;
                break;

            case nameof(ExportSettingsViewModel.StructureWarningText):
                lblStructureWarning.Text = viewModel.StructureWarningText;
                break;

            case nameof(ExportSettingsViewModel.Problems):
            case nameof(ExportSettingsViewModel.StatusText):
                RefreshProblems();
                break;
        }
    }

    private void RefreshAllWidgets()
    {
        WriteWidgets(() =>
        {
            textFormat.Text = viewModel.LineTemplate;
            numData.Value = viewModel.DataPerLine;
            comboUnlabeled.SelectedIndex = Array.IndexOf(UnlabeledItems, viewModel.Unlabeled);
            comboStructure.SelectedIndex = Array.IndexOf(StructureItems, viewModel.Structure);
            chkNewLine.Checked = viewModel.NewLine;
            chkOutputExtraWhitespace.Checked = viewModel.OutputExtraWhitespace;
            chkGenerateFullLine.Checked = viewModel.GenerateFullLine;
            chkIncludeUnusedLabels.Checked = viewModel.IncludeUnusedLabels;
            chkPrintLabelSpecificComments.Checked = viewModel.PrintLabelSpecificComments;
            chkGeneratePlusMinusLabels.Checked = viewModel.GeneratePlusMinusLabels;
            chkGenerateAssetLabels.Checked = viewModel.GenerateAssetLabels;
            txtExportPath.Text = viewModel.OutputPath;
            txtExcludeLabelAuthors.Text = viewModel.ExcludedAuthorsText;

            textSample.Text = viewModel.SampleOutputText;
            disassembleButton.Enabled = viewModel.CanStartExport;
            lblStructureWarning.Text = viewModel.StructureWarningText;
            RefreshProblems();
        });
    }

    /// <summary>
    /// Say what is stopping the export. Every complaint is listed when there is more than one;
    /// otherwise the one-line summary is shown, which also covers an unparseable line template --
    /// not something the settings validator has an opinion about.
    /// </summary>
    private void RefreshProblems() =>
        lblProblems.Text = viewModel.Problems.Count > 0
            ? string.Join(Environment.NewLine, viewModel.Problems)
            : viewModel.StatusText;

    // ------------------------------------------------------------------ widgets -> ViewModel

    private void textFormat_TextChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.LineTemplate = textFormat.Text);

    private void numData_ValueChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.DataPerLine = (int)numData.Value);

    private void comboUnlabeled_SelectedIndexChanged(object sender, EventArgs e) =>
        PushToViewModel(() =>
        {
            if (comboUnlabeled.SelectedIndex >= 0)
                viewModel.Unlabeled = UnlabeledItems[comboUnlabeled.SelectedIndex];
        });

    private void comboStructure_SelectedIndexChanged(object sender, EventArgs e) =>
        PushToViewModel(() =>
        {
            if (comboStructure.SelectedIndex >= 0)
                viewModel.Structure = StructureItems[comboStructure.SelectedIndex];
        });

    private void chkNewLine_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.NewLine = chkNewLine.Checked);

    private void chkOutputExtraWhitespace_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.OutputExtraWhitespace = chkOutputExtraWhitespace.Checked);

    private void chkGenerateFullLine_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.GenerateFullLine = chkGenerateFullLine.Checked);

    private void chkPrintLabelSpecificComments_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.PrintLabelSpecificComments = chkPrintLabelSpecificComments.Checked);

    private void chkIncludeUnusedLabels_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.IncludeUnusedLabels = chkIncludeUnusedLabels.Checked);

    private void chkGeneratePlusMinusLabels_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.GeneratePlusMinusLabels = chkGeneratePlusMinusLabels.Checked);

    private void chkGenerateAssetLabels_CheckedChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.GenerateAssetLabels = chkGenerateAssetLabels.Checked);

    private void txtExportPath_TextChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.OutputPath = txtExportPath.Text);

    // the "does this directory exist?" rule reads the disk, so it waits until the path has settled.
    private void txtExportPath_Leave(object sender, EventArgs e) =>
        viewModel.RefreshOutputPathStatus();

    private void txtExcludeLabelAuthors_TextChanged(object sender, EventArgs e) =>
        PushToViewModel(() => viewModel.ExcludedAuthorsText = txtExcludeLabelAuthors.Text);

    // ------------------------------------------------------------------ the two questions

    // async void: these are event handlers, and the file picker is async because one toolkit's
    // picker has no blocking form. WinForms' does, so both complete before returning here.
    private async void btnBrowseOutputPath_Click(object sender, EventArgs e) =>
        await EnsureRealOutputDirectory(forcePickPath: true);

    private async void disassembleButton_Click(object sender, EventArgs e)
    {
        if (!await EnsureRealOutputDirectory(forcePickPath: false))
            return;

        DialogResult = DialogResult.OK;
    }

    private void cancel_Click(object sender, EventArgs e) => Close();

    /// <summary>
    /// Make sure the output directory is one that exists, asking as needed. Declining to create it
    /// is not a refusal to export -- it means "let me point somewhere else" -- so a new path gets
    /// picked and the question can come round a second time, for that path.
    /// </summary>
    /// <param name="forcePickPath">
    /// True when the user pressed Browse, so the picker opens even if the current path is fine.
    /// </param>
    /// <returns>true if the output directory now exists; false if the user backed out.</returns>
    private async Task<bool> EnsureRealOutputDirectory(bool forcePickPath)
    {
        var outcome = AskToCreateOutputDirectory(
            "Press YES to create and use this path, NO to select a new path instead.");

        if (outcome == CreateDirectoryOutcome.Created)
            return true;

        if ((forcePickPath || outcome == CreateDirectoryOutcome.Declined) && !await PickOutputPath())
            return false;

        return AskToCreateOutputDirectory() != CreateDirectoryOutcome.Declined;
    }

    private enum CreateDirectoryOutcome
    {
        AlreadyExists,
        Declined,
        Created,
    }

    private CreateDirectoryOutcome AskToCreateOutputDirectory(string extraMsg = "")
    {
        viewModel.RefreshOutputPathStatus();
        if (!viewModel.NeedsOutputDirectoryCreated)
            return CreateDirectoryOutcome.AlreadyExists;

        var wantsIt = WinformsGuiUtil.PromptToConfirmAction("Output Directory",
            "Output Directory does not exist.\nWould you like to create it now?\n" +
            $"{viewModel.OutputDirectoryToCreate}\n\n{extraMsg}",
            () => true);

        if (!wantsIt)
            return CreateDirectoryOutcome.Declined;

        viewModel.CreateOutputDirectory();
        return CreateDirectoryOutcome.Created;
    }

    /// <summary>
    /// Open the picker the current structure calls for -- a file when everything goes into one
    /// file, a folder when there is one file per bank -- and store the answer relative to the
    /// project's own directory when it sits underneath it.
    /// </summary>
    private async Task<bool> PickOutputPath()
    {
        var settings = viewModel.BuildSettings();
        var startingAt = settings.BuildFullOutputPath();

        var picked = settings.Structure == LogWriterSettings.FormatStructure.SingleFile
            ? await fileDialogService.PromptSaveFileAsync("", SingleFileFilter, startingAt)
            : await fileDialogService.PromptSelectFolderAsync("", startingAt);

        if (string.IsNullOrEmpty(picked))
            return false;

        viewModel.OutputPath = settings.WithPathRelativeTo(picked, settings.BaseOutputPath).FileOrFolderOutPath;
        viewModel.RefreshOutputPathStatus();
        return true;
    }

    // ------------------------------------------------------------------ plumbing

    /// <summary>Run a user-input handler, unless the change came from the ViewModel in the first place.</summary>
    private void PushToViewModel(Action push)
    {
        if (updatingWidgets)
            return;

        push();
    }

    /// <summary>
    /// Put text in a box without stealing the caret.
    ///
    /// One of these boxes is normalized as it is stored -- the line template is lower-cased,
    /// because the parser looks its placeholders up by name -- so a keystroke pushed into the
    /// ViewModel can come straight back as a DIFFERENT string, and a plain assignment to
    /// <see cref="TextBox.Text"/> then drops the caret at the end of the box. Typing an upper-case
    /// letter mid-string would jump the caret to the end after every single character. Writing only
    /// on a real difference and putting the caret back where it was leaves the box showing what the
    /// settings actually say without moving under the person typing.
    /// </summary>
    private void WriteText(TextBox box, string text)
    {
        if (box.Text == text)
            return;

        WriteWidgets(() =>
        {
            var caret = box.SelectionStart;
            var selectionLength = box.SelectionLength;
            box.Text = text;
            box.SelectionStart = Math.Min(caret, box.TextLength);
            box.SelectionLength = Math.Min(selectionLength, box.TextLength - box.SelectionStart);
        });
    }

    /// <summary>Write widget state without the input handlers treating it as user input.</summary>
    private void WriteWidgets(Action write)
    {
        var previous = updatingWidgets;
        updatingWidgets = true;
        try
        {
            write();
        }
        finally
        {
            updatingWidgets = previous;
        }
    }
}
