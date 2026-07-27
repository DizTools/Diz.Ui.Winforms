using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Diz.Ui.ViewModels.MisalignmentChecker;
using Diz.Ui.Winforms.dialogs;
using FluentAssertions;
using Xunit;
using WinFormsLabel = System.Windows.Forms.Label;

namespace Diz.Ui.Winforms.Test.Tests;

/// <summary>
/// MisalignmentCheckerDialog is a thin host over MisalignmentCheckerViewModel: Scan asks the
/// ViewModel to run, and every widget shows what the ViewModel says afterwards. These tests
/// construct the real form (never shown -- no window handle is needed to read control state) and
/// drive it with a fake sweep, so nothing here needs a ROM.
/// </summary>
public class MisalignmentCheckerDialogBindingTests
{
    /// <summary>A stand-in for the ROM sweep: hands back canned answers, one per call.</summary>
    private sealed class FakeScan
    {
        private readonly Queue<(int found, string reportText)> results;
        public int TimesRun { get; private set; }

        public FakeScan(params (int found, string reportText)[] results) =>
            this.results = new Queue<(int, string)>(results);

        public (int found, string reportText) Run()
        {
            ++TimesRun;
            return results.Count == 1 ? results.Peek() : results.Dequeue();
        }
    }

    private static (MisalignmentCheckerDialog dialog, MisalignmentCheckerViewModel viewModel) MakeDialog(
        FakeScan scan)
    {
        var viewModel = new MisalignmentCheckerViewModel(scan.Run);
        return (new MisalignmentCheckerDialog(viewModel), viewModel);
    }

    private static T Widget<T>(Control parent, string name) where T : Control =>
        parent.Controls.Find(name, searchAllChildren: true).OfType<T>().Single();

    // PerformClick() is not usable here: it refuses on a control that isn't selectable, and
    // nothing on a form that was never shown is.
    private static void RaiseClick(Control control) =>
        typeof(Control)
            .GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, new object[] { EventArgs.Empty });

    // ------------------------------------------------------------------ before any scan

    [Fact]
    public void NothingIsShownUntilSomethingHasBeenScanned()
    {
        var (dialog, viewModel) = MakeDialog(new FakeScan((3, "three")));
        using var _ = dialog;

        Widget<TextBox>(dialog, "textLog").Text.Should().BeEmpty();
        Widget<WinFormsLabel>(dialog, "labelStatus").Text.Should().BeEmpty();
        viewModel.FoundCount.Should().BeNull();
    }

    [Fact]
    public void ConstructingTheWindowDoesNotSweepTheRom()
    {
        // the sweep walks the whole ROM. Opening the window must not start one -- that is what
        // the Scan button is for.
        var scan = new FakeScan((3, "three"));
        using var dialog = MakeDialog(scan).dialog;

        scan.TimesRun.Should().Be(0);
    }

    [Fact]
    public void TheInstructionParagraphIsLoadedFromTheResourceFile()
    {
        // the designer builds a ComponentResourceManager from typeof(MisalignmentCheckerDialog)
        // and reads this text out of MisalignmentCheckerDialog.resx. If the class and the .resx
        // ever stop agreeing on the name, the lookup silently yields nothing and this fails.
        using var dialog = MakeDialog(new FakeScan((0, ""))).dialog;

        Widget<WinFormsLabel>(dialog, "label1").Text.Should().StartWith("Check for misaligned flags");
    }

    [Fact]
    public void TheReportBoxIsForReadingOnly()
    {
        using var dialog = MakeDialog(new FakeScan((0, ""))).dialog;

        Widget<TextBox>(dialog, "textLog").ReadOnly.Should().BeTrue();
    }

    // ------------------------------------------------------------------ scanning

    [Fact]
    public void ScanRunsTheSweepAndShowsBothTheReportAndTheCount()
    {
        var scan = new FakeScan((2, "C00010: Operand without Opcode\r\nC00020: Operand without Opcode\r\n"));
        var (dialog, viewModel) = MakeDialog(scan);
        using var _ = dialog;

        RaiseClick(Widget<Button>(dialog, "buttonScan"));

        scan.TimesRun.Should().Be(1);
        Widget<TextBox>(dialog, "textLog").Text.Should().Be(viewModel.ReportText);
        Widget<TextBox>(dialog, "textLog").Text.Should().Contain("Operand without Opcode");

        // the count the legacy window discarded
        Widget<WinFormsLabel>(dialog, "labelStatus").Text.Should().Be("Found 2 misalignments");
    }

    [Fact]
    public void ACleanRomStillSaysSoRatherThanShowingNothing()
    {
        // "not scanned yet" and "scanned, and clean" looked identical before: both were an empty
        // report box.
        var (dialog, _) = MakeDialog(new FakeScan((0, "No misaligned flags found!")));
        using var _2 = dialog;

        RaiseClick(Widget<Button>(dialog, "buttonScan"));

        Widget<TextBox>(dialog, "textLog").Text.Should().Be("No misaligned flags found!");
        Widget<WinFormsLabel>(dialog, "labelStatus").Text.Should().Be("No misalignments found");
    }

    [Fact]
    public void AScanThatHitTheResultLimitSaysTheSweepStoppedEarly()
    {
        // the generator gives up once it has collected this many, so the ROM past that point was
        // never looked at and the window has to say so.
        var (dialog, _) = MakeDialog(
            new FakeScan((MisalignmentCheckerViewModel.FindingLimit, "lots")));
        using var _2 = dialog;

        RaiseClick(Widget<Button>(dialog, "buttonScan"));

        Widget<WinFormsLabel>(dialog, "labelStatus").Text
            .Should().Be(
                $"Found {MisalignmentCheckerViewModel.FindingLimit} misalignments " +
                $"(scan stopped at the {MisalignmentCheckerViewModel.FindingLimit}-result limit; " +
                "there may be more)");
    }

    [Fact]
    public void ScanningAgainReplacesEverythingTheLastScanShowed()
    {
        // fix something by hand, rescan, and only the new answer is on screen.
        var (dialog, _) = MakeDialog(new FakeScan((2, "two"), (0, "No misaligned flags found!")));
        using var _2 = dialog;

        var buttonScan = Widget<Button>(dialog, "buttonScan");

        RaiseClick(buttonScan);
        Widget<TextBox>(dialog, "textLog").Text.Should().Be("two");
        Widget<WinFormsLabel>(dialog, "labelStatus").Text.Should().Be("Found 2 misalignments");

        RaiseClick(buttonScan);
        Widget<TextBox>(dialog, "textLog").Text.Should().Be("No misaligned flags found!");
        Widget<WinFormsLabel>(dialog, "labelStatus").Text.Should().Be("No misalignments found");
    }

    [Fact]
    public void AScanTheWindowDidNotStartStillReachesTheWidgets()
    {
        // the widgets follow the ViewModel's notifications, not the button handler, so anything
        // else that drives the same ViewModel updates the window too.
        var (dialog, viewModel) = MakeDialog(new FakeScan((1, "one")));
        using var _ = dialog;

        viewModel.Scan();

        Widget<TextBox>(dialog, "textLog").Text.Should().Be("one");
        Widget<WinFormsLabel>(dialog, "labelStatus").Text.Should().Be("Found 1 misalignment");
    }

    [Fact]
    public void TheWindowStopsFollowingTheViewModelOnceItIsClosed()
    {
        var (dialog, viewModel) = MakeDialog(new FakeScan((1, "one")));
        using var _ = dialog;

        // OnFormClosed is what a real close raises; the form was never shown, so raise it directly
        typeof(Form)
            .GetMethod("OnFormClosed", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(dialog, new object[] { new FormClosedEventArgs(CloseReason.UserClosing) });

        viewModel.Scan();

        Widget<TextBox>(dialog, "textLog").Text.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ confirming

    [Fact]
    public void FixConfirmsWithoutRequiringAScanFirst()
    {
        // deliberate, and as old as the window: the instruction paragraph offers fixing as an
        // alternative to reading the report, not as a step after it.
        var scan = new FakeScan((5, "five"));
        var (dialog, _) = MakeDialog(scan);
        using var _2 = dialog;

        RaiseClick(Widget<Button>(dialog, "buttonFix"));

        dialog.DialogResult.Should().Be(DialogResult.OK);
        scan.TimesRun.Should().Be(0);
        Widget<Button>(dialog, "buttonFix").Enabled.Should().BeTrue();
    }

    [Fact]
    public void FixStillConfirmsAfterAScan()
    {
        var (dialog, _) = MakeDialog(new FakeScan((5, "five")));
        using var _2 = dialog;

        RaiseClick(Widget<Button>(dialog, "buttonScan"));
        RaiseClick(Widget<Button>(dialog, "buttonFix"));

        dialog.DialogResult.Should().Be(DialogResult.OK);
    }

    [Fact]
    public void CancelDoesNotConfirmAnything()
    {
        var (dialog, _) = MakeDialog(new FakeScan((5, "five")));
        using var _2 = dialog;

        var cancel = Widget<Button>(dialog, "cancel");

        // Escape is not wired to a key handler: it goes through the form's CancelButton, which
        // is this very button. Asserted before the click, because closing the window tears the
        // control tree down.
        dialog.CancelButton.Should().BeSameAs(cancel);

        RaiseClick(Widget<Button>(dialog, "buttonScan"));
        RaiseClick(cancel);

        dialog.DialogResult.Should().NotBe(DialogResult.OK);
    }

    [Fact]
    public void TheWindowRefusesToBeBuiltWithoutAViewModel()
    {
        var make = () => new MisalignmentCheckerDialog(null!);
        make.Should().Throw<ArgumentNullException>();
    }
}
