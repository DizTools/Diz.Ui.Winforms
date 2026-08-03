using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Diz.Controllers.interfaces;
using Diz.Core.export;
using Diz.Core.util;
using Diz.Ui.ViewModels.ExportSettings;
using Diz.Ui.Winforms.dialogs;
using FluentAssertions;
using Moq;
using Xunit;
using WinFormsLabel = System.Windows.Forms.Label;

namespace Diz.Ui.Winforms.Test.Tests;

/// <summary>
/// ExportSettingsDialog is a thin host over ExportSettingsViewModel: every widget reads and writes
/// the ViewModel and nothing else. These tests construct the real form (never shown -- no window
/// handle is needed to drive TextChanged / CheckedChanged / SelectedIndexChanged) and check that
/// traffic flows both ways, that the combo boxes map to the right enum members rather than to
/// whatever their designer position happens to be, and that the validation feedback the window
/// grew -- the problems line and the disabled Start Export button -- follows the ViewModel.
///
/// Nothing here reads a ROM or generates real assembly: the two delegates the ViewModel needs are
/// stubbed, exactly as the caller supplies them.
/// </summary>
public class ExportSettingsDialogBindingTests
{
    /// <summary>A path fragment the fake filesystem below reports as not existing.</summary>
    private const string MissingDirectory = "this_directory_is_not_there";

    private static IFilesystemService Filesystem(Mock<IFilesystemService>? preConfigured = null)
    {
        var fsMock = preConfigured ?? new Mock<IFilesystemService>();
        fsMock.Setup(x => x.DirectoryExists(It.IsAny<string>()))
            .Returns((string? name) => name?.Contains(MissingDirectory) != true);
        return fsMock.Object;
    }

    private static (ExportSettingsDialog dialog, ExportSettingsViewModel viewModel) MakeDialog(
        LogWriterSettings? settings = null,
        Func<string, bool>? isLineTemplateValid = null,
        IFileDialogService? fileDialogService = null,
        IFilesystemService? fs = null)
    {
        var viewModel = new ExportSettingsViewModel(
            settings ?? new LogWriterSettings { BaseOutputPath = @"C:\project" },
            fs ?? Filesystem(),
            isLineTemplateValid ?? (_ => true),
            _ => "SAMPLE ASSEMBLY");

        return (new ExportSettingsDialog(viewModel, fileDialogService ?? new Mock<IFileDialogService>().Object),
            viewModel);
    }

    private static T Widget<T>(Control parent, string name) where T : Control =>
        parent.Controls.Find(name, searchAllChildren: true).OfType<T>().Single();

    /// <summary>
    /// Press a button. PerformClick() is no use here: these forms are never shown, and a control
    /// on an invisible form reports itself as unselectable and swallows the click, so the handler
    /// is invoked directly. Both handlers are async void over an already-completed picker task, so
    /// they run to completion before this returns.
    /// </summary>
    private static void Press(ExportSettingsDialog dialog, string handlerName) =>
        typeof(ExportSettingsDialog)
            .GetMethod(handlerName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(dialog, [dialog, EventArgs.Empty]);

    // ------------------------------------------------------------------ seeding

    [Fact]
    public void TheWindowStartsOutShowingWhatTheViewModelWasSeededWith()
    {
        var (dialog, viewModel) = MakeDialog(new LogWriterSettings
        {
            BaseOutputPath = @"C:\project",
            Format = "%label% %code%",
            DataPerLine = 3,
            Unlabeled = LogWriterSettings.FormatUnlabeled.ShowNone,
            Structure = LogWriterSettings.FormatStructure.OneBankPerFile,
            NewLine = true,
            OutputExtraWhitespace = false,
            GenerateFullLine = false,
            IncludeUnusedLabels = true,
            PrintLabelSpecificComments = true,
            GeneratePlusMinusLabels = false,
            GenerateAssetLabels = false,
            FileOrFolderOutPath = "somewhere",
            ExcludedLabelAuthorsList = "bob,alice",
        });
        using var _ = dialog;

        Widget<TextBox>(dialog, "textFormat").Text.Should().Be("%label% %code%");
        Widget<NumericUpDown>(dialog, "numData").Value.Should().Be(3);
        Widget<ComboBox>(dialog, "comboUnlabeled").SelectedIndex.Should().Be(2);
        Widget<ComboBox>(dialog, "comboStructure").SelectedIndex.Should().Be(1);
        Widget<CheckBox>(dialog, "chkNewLine").Checked.Should().BeTrue();
        Widget<CheckBox>(dialog, "chkOutputExtraWhitespace").Checked.Should().BeFalse();
        Widget<CheckBox>(dialog, "chkGenerateFullLine").Checked.Should().BeFalse();
        Widget<CheckBox>(dialog, "chkIncludeUnusedLabels").Checked.Should().BeTrue();
        Widget<CheckBox>(dialog, "chkPrintLabelSpecificComments").Checked.Should().BeTrue();
        Widget<CheckBox>(dialog, "chkGeneratePlusMinusLabels").Checked.Should().BeFalse();
        Widget<CheckBox>(dialog, "chkGenerateAssetLabels").Checked.Should().BeFalse();
        Widget<TextBox>(dialog, "txtExportPath").Text.Should().Be("somewhere");
        Widget<TextBox>(dialog, "txtExcludeLabelAuthors").Text.Should().Be("alice, bob");
        Widget<TextBox>(dialog, "textSample").Text.Should().Be("SAMPLE ASSEMBLY");

        viewModel.LineTemplate.Should().Be("%label% %code%");
    }

    // ------------------------------------------------------------------ widgets -> ViewModel

    [Fact]
    public void EveryWidgetWritesItsSettingBackToTheViewModel()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        Widget<TextBox>(dialog, "textFormat").Text = "%bytes%";
        Widget<NumericUpDown>(dialog, "numData").Value = 12;
        Widget<ComboBox>(dialog, "comboUnlabeled").SelectedIndex = 0;
        Widget<CheckBox>(dialog, "chkNewLine").Checked = true;
        Widget<CheckBox>(dialog, "chkOutputExtraWhitespace").Checked = false;
        Widget<CheckBox>(dialog, "chkGenerateFullLine").Checked = false;
        Widget<CheckBox>(dialog, "chkIncludeUnusedLabels").Checked = true;
        Widget<CheckBox>(dialog, "chkPrintLabelSpecificComments").Checked = true;
        Widget<CheckBox>(dialog, "chkGeneratePlusMinusLabels").Checked = false;
        Widget<CheckBox>(dialog, "chkGenerateAssetLabels").Checked = false;
        Widget<TextBox>(dialog, "txtExportPath").Text = "elsewhere";

        viewModel.LineTemplate.Should().Be("%bytes%");
        viewModel.DataPerLine.Should().Be(12);
        viewModel.Unlabeled.Should().Be(LogWriterSettings.FormatUnlabeled.ShowAll);
        viewModel.NewLine.Should().BeTrue();
        viewModel.OutputExtraWhitespace.Should().BeFalse();
        viewModel.GenerateFullLine.Should().BeFalse();
        viewModel.IncludeUnusedLabels.Should().BeTrue();
        viewModel.PrintLabelSpecificComments.Should().BeTrue();
        viewModel.GeneratePlusMinusLabels.Should().BeFalse();
        viewModel.GenerateAssetLabels.Should().BeFalse();
        viewModel.OutputPath.Should().Be("elsewhere");
    }

    /// <summary>
    /// The two pickers hold plain display strings, so what a selection MEANS is the item order in
    /// the designer. Casting the selected index straight to the enum works only as long as the two
    /// orders agree by accident; these pin the mapping so re-ordering the designer list can no
    /// longer silently change what a stored project means.
    /// </summary>
    [Theory]
    [InlineData(0, LogWriterSettings.FormatStructure.SingleFile)]
    [InlineData(1, LogWriterSettings.FormatStructure.OneBankPerFile)]
    public void TheStructurePickerMapsToTheRightMode(int index, LogWriterSettings.FormatStructure expected)
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        Widget<ComboBox>(dialog, "comboStructure").SelectedIndex = index;

        viewModel.Structure.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, LogWriterSettings.FormatUnlabeled.ShowAll)]
    [InlineData(1, LogWriterSettings.FormatUnlabeled.ShowInPoints)]
    [InlineData(2, LogWriterSettings.FormatUnlabeled.ShowNone)]
    public void TheUnlabeledPickerMapsToTheRightMode(int index, LogWriterSettings.FormatUnlabeled expected)
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        Widget<ComboBox>(dialog, "comboUnlabeled").SelectedIndex = index;

        viewModel.Unlabeled.Should().Be(expected);
    }

    /// <summary>
    /// Typing a comma must leave the comma alone. The settings record trims, de-duplicates and
    /// sorts this list, so normalizing per keystroke rewrites the box under the caret and eats the
    /// separator that was just typed -- making a second author impossible to start. Normalizing is
    /// deferred to the moment the settings are built.
    /// </summary>
    [Fact]
    public void TypingASecondAuthorIsPossible()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        var box = Widget<TextBox>(dialog, "txtExcludeLabelAuthors");
        foreach (var text in new[] { "z", "zed", "zed,", "zed, a", "zed, al" })
            box.Text = text;

        box.Text.Should().Be("zed, al");
        viewModel.ExcludedAuthorsText.Should().Be("zed, al");
        viewModel.BuildSettings().ExcludedLabelAuthors.Should().Equal("al", "zed");
    }

    /// <summary>
    /// The line template is lower-cased as it is typed -- the parser looks placeholders up by name
    /// -- but the caret must not jump to the end of the box while that happens.
    /// </summary>
    [Fact]
    public void TypingAnUpperCaseTemplateLowerCasesItWithoutRewritingUnderTheCaret()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        var box = Widget<TextBox>(dialog, "textFormat");
        box.Text = "%LABEL%";

        viewModel.LineTemplate.Should().Be("%label%");
        box.Text.Should().Be("%label%", "the box shows what the settings actually say");
        box.SelectionStart.Should().Be(0, "the echo puts the caret back where it found it");

        viewModel.BuildSettings().Format.Should().Be("%label%");
    }

    // ------------------------------------------------------------------ ViewModel -> widgets

    [Fact]
    public void TheStartExportButtonFollowsWhetherTheSettingsAreExportable()
    {
        var (dialog, viewModel) = MakeDialog(isLineTemplateValid: template => template.Length > 0);
        using var _ = dialog;

        var button = Widget<Button>(dialog, "disassembleButton");
        button.Enabled.Should().BeTrue();

        Widget<TextBox>(dialog, "textFormat").Text = "";
        button.Enabled.Should().BeFalse();
        Widget<WinFormsLabel>(dialog, "lblProblems").Text
            .Should().Be(ExportSettingsViewModel.InvalidLineTemplateMessage);

        Widget<TextBox>(dialog, "textFormat").Text = "%label%";
        button.Enabled.Should().BeTrue();
        viewModel.CanStartExport.Should().BeTrue();
    }

    [Fact]
    public void ValidatorComplaintsAreShown()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        Widget<TextBox>(dialog, "txtExportPath").Text = "";
        // the disk-reading rule only re-runs when the path settles, which is what leaving the box means
        viewModel.RefreshOutputPathStatus();

        viewModel.Problems.Should().NotBeEmpty();
        Widget<WinFormsLabel>(dialog, "lblProblems").Text
            .Should().Be(string.Join(Environment.NewLine, viewModel.Problems));
        Widget<Button>(dialog, "disassembleButton").Enabled.Should().BeFalse();
    }

    /// <summary>
    /// "All in one file" stays selectable -- a project that already stores it must remain editable
    /// -- but the assembly writer refuses that mode, so the window says so while it is selected.
    /// </summary>
    [Fact]
    public void PickingSingleFileOutputExplainsWhyThatWillNotWork()
    {
        var (dialog, _) = MakeDialog();
        using var __ = dialog;

        var warning = Widget<WinFormsLabel>(dialog, "lblStructureWarning");
        warning.Text.Should().BeEmpty();

        Widget<ComboBox>(dialog, "comboStructure").SelectedIndex = 0;
        warning.Text.Should().Be(ExportSettingsViewModel.SingleFileWarningText);

        Widget<ComboBox>(dialog, "comboStructure").SelectedIndex = 1;
        warning.Text.Should().BeEmpty();
    }

    [Fact]
    public void TheSampleBoxFollowsTheSettings()
    {
        var (dialog, _) = MakeDialog(isLineTemplateValid: template => template != "bad");
        using var _2 = dialog;

        Widget<TextBox>(dialog, "textFormat").Text = "bad";
        Widget<TextBox>(dialog, "textSample").Text
            .Should().Be(ExportSettingsViewModel.InvalidLineTemplateMessage);

        Widget<TextBox>(dialog, "textFormat").Text = "%label%";
        Widget<TextBox>(dialog, "textSample").Text.Should().Be("SAMPLE ASSEMBLY");
    }

    // ------------------------------------------------------------------ the file picker

    /// <summary>
    /// Which picker opens is decided by the output structure -- a save-file picker when everything
    /// goes into one file, a folder picker otherwise -- and the answer is stored relative to the
    /// project's own directory when it sits underneath it.
    /// </summary>
    [Fact]
    public async Task BrowsingForAFolderStoresAPathRelativeToTheProject()
    {
        var dialogServiceMock = new Mock<IFileDialogService>();
        dialogServiceMock
            .Setup(x => x.PromptSelectFolderAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(@"C:\project\somewhere\else");

        var (dialog, viewModel) = MakeDialog(fileDialogService: dialogServiceMock.Object);
        using var _ = dialog;

        Press(dialog, "btnBrowseOutputPath_Click");
        await Task.Yield();

        viewModel.OutputPath.Should().Be(@"somewhere\else");
        dialogServiceMock.Verify(
            x => x.PromptSaveFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never, "one file per bank means a folder is being chosen, not a file");
    }

    [Fact]
    public async Task BrowsingInSingleFileModeAsksForAFile()
    {
        var dialogServiceMock = new Mock<IFileDialogService>();
        dialogServiceMock
            .Setup(x => x.PromptSaveFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(@"C:\project\everything.asm");

        var (dialog, viewModel) = MakeDialog(fileDialogService: dialogServiceMock.Object);
        using var _ = dialog;

        Widget<ComboBox>(dialog, "comboStructure").SelectedIndex = 0;
        Press(dialog, "btnBrowseOutputPath_Click");
        await Task.Yield();

        viewModel.OutputPath.Should().Be("everything.asm");
        dialogServiceMock.Verify(
            x => x.PromptSaveFileAsync("", "Assembly Files|*.asm|All Files|*.*", It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// Leaving the path box is what tells the ViewModel the path has settled. Until then, live
    /// validation runs against the last answer the disk gave, so a half-typed path costs no
    /// filesystem calls and never flashes an error.
    /// </summary>
    [Fact]
    public void TypingAPathDoesNotReadTheDiskUntilTheBoxIsLeft()
    {
        var fsMock = new Mock<IFilesystemService>();
        var (dialog, _) = MakeDialog(fs: Filesystem(fsMock));
        using var __ = dialog;

        fsMock.Invocations.Clear();

        var box = Widget<TextBox>(dialog, "txtExportPath");
        foreach (var text in new[] { "o", "ou", "out", "outp", "outpu", "output" })
            box.Text = text;

        fsMock.Invocations.Should().BeEmpty("typing must not hit the filesystem");

        // Leave is what the real window raises when focus moves away; drive it directly, since
        // these forms are never shown and so never receive focus.
        typeof(Control)
            .GetMethod("OnLeave", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(box, [EventArgs.Empty]);

        fsMock.Invocations.Should().NotBeEmpty("leaving the box is when the path is checked");
    }
}
