using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Diz.Core.Interfaces;
using Diz.Core.util;
using Diz.Cpu._65816.import;
using Diz.Ui.ViewModels.ImportRom;
using Diz.Ui.Winforms.dialogs;
using FluentAssertions;
using Xunit;
using WinFormsLabel = System.Windows.Forms.Label;

namespace Diz.Ui.Winforms.Test.Tests;

/// <summary>
/// SnesImportRomDialog is a thin host over SnesImportRomViewModel: every widget reads and writes
/// the ViewModel and nothing else. These tests construct the real form (never shown -- no window
/// handle is needed to drive CheckedChanged / SelectedIndexChanged) and check that traffic flows
/// both ways. No ROM is read: the snapshots below are handed straight to the ViewModel, exactly
/// as the importer would.
/// </summary>
public class SnesImportRomDialogBindingTests
{
    private const string HiRomTitle = "A DIFFERENT TITLE";

    private static readonly string[] AlwaysEnabled =
    [
        SnesVectorNames.Native_Reserved1__ignored,
        SnesVectorNames.Native_Reserved2__ignored,
        SnesVectorNames.Emulation_Reserved1__ignored,
        SnesVectorNames.Emulation_Reserved2__ignored,
    ];

    private static readonly string[] AllNames =
    [
        SnesVectorNames.Native_Reserved1__ignored,
        SnesVectorNames.Native_Reserved2__ignored,
        SnesVectorNames.Native_COP,
        SnesVectorNames.Native_BRK,
        SnesVectorNames.Native_ABORT,
        SnesVectorNames.Native_NMI,
        SnesVectorNames.Native_RESET__ignored,
        SnesVectorNames.Native_IRQ,
        SnesVectorNames.Emulation_Reserved1__ignored,
        SnesVectorNames.Emulation_Reserved2__ignored,
        SnesVectorNames.Emulation_COP,
        SnesVectorNames.Emulation_Reserved3__ignored,
        SnesVectorNames.Emulation_ABORT,
        SnesVectorNames.Emulation_NMI,
        SnesVectorNames.Emulation_RESET,
        SnesVectorNames.Emulation_IRQBRK,
    ];

    private static SnesVectorSnapshot Snapshot(string title, string displayValue, bool readable) =>
        new(title, readable, AllNames.Select(name => new SnesVectorValue(name, displayValue, readable)).ToList());

    private static (SnesImportRomDialog dialog, SnesImportRomViewModel viewModel) MakeDialog(
        bool detectionSucceeded = true, RomMapMode detected = RomMapMode.LoRom)
    {
        // reading the ROM at HiROM produces different values and a different title, so a map-mode
        // change is visible in every widget that depends on it.
        SnesVectorSnapshot SnapshotFor(RomMapMode mode) => mode == RomMapMode.HiRom
            ? Snapshot(HiRomTitle, "9ABC", readable: true)
            : Snapshot("SAMPLE TITLE", "8123", readable: true);

        var viewModel = new SnesImportRomViewModel(
            // seeded at the DETECTED mapping, which is what the importer hands over: the opening
            // screen shows the ROM read the way analysis read it.
            initialSnapshot: SnapshotFor(detected),
            detectedRomMapMode: detected,
            detectionSucceeded: detectionSucceeded,
            romSpeedText: "SlowROM",
            alwaysEnabledVectorNames: AlwaysEnabled,
            initiallyEnabledVectorNames: AllNames,
            recomputeForMapMode: SnapshotFor);

        return (new SnesImportRomDialog(viewModel), viewModel);
    }

    private static T Widget<T>(Control parent, string name) where T : Control =>
        parent.Controls.Find(name, searchAllChildren: true).OfType<T>().Single();

    // ------------------------------------------------------------------ seeding

    [Fact]
    public void TheWindowStartsOutShowingWhatTheViewModelWasSeededWith()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        Widget<WinFormsLabel>(dialog, "detectMessage").Text.Should().Be(viewModel.DetectionMessage);
        Widget<WinFormsLabel>(dialog, "romspeed").Text.Should().Be("SlowROM");
        Widget<WinFormsLabel>(dialog, "romtitle").Text.Should().Be("SAMPLE TITLE");
        Widget<TextBox>(dialog, "textNativeNMI").Text.Should().Be("8123");
        Widget<CheckBox>(dialog, "checkboxNativeNMI").Checked.Should().BeTrue();
        Widget<CheckBox>(dialog, "checkboxNativeNMI").Enabled.Should().BeTrue();
    }

    /// <summary>
    /// Filling the map-mode picker must not count as the user choosing something.
    ///
    /// Handing a combo box its list selects the first entry straight away and raises the same
    /// event a click does. Unsuppressed, that pushes the FIRST mapping into the ViewModel and
    /// overwrites the detected one during construction, so the window opens describing the ROM
    /// read the wrong way: wrong mapping selected, and with it the wrong cartridge title and
    /// vector words.
    ///
    /// The detected mapping here is deliberately NOT the first one in the enum. Every other test
    /// in this file detects LoROM, which is first, so the overwrite wrote back the value that was
    /// already there and left no trace.
    /// </summary>
    [Fact]
    public void FillingTheMapModePickerDoesNotOverwriteTheDetectedMapMode()
    {
        var (dialog, viewModel) = MakeDialog(detected: RomMapMode.HiRom);
        using var _ = dialog;

        viewModel.SelectedRomMapMode.Should().Be(RomMapMode.HiRom,
            "constructing the window must not move the ViewModel off what was detected");
        Widget<ComboBox>(dialog, "cmbRomMapMode").SelectedValue.Should().Be(RomMapMode.HiRom);

        // the knock-on effect that made this visible: everything read at the mapping follows it.
        Widget<WinFormsLabel>(dialog, "romtitle").Text.Should().Be(HiRomTitle);
        Widget<TextBox>(dialog, "textNativeNMI").Text.Should().Be("9ABC");
    }

    [Fact]
    public void TheMapModePickerShowsTheDescriptionTextNotTheEnumMemberName()
    {
        // the ViewModel hands over raw RomMapMode values on purpose, so rendering them is the
        // host's job. Without it the picker reads "Sa1Rom".
        var (dialog, _) = MakeDialog();
        using var _2 = dialog;

        var combo = Widget<ComboBox>(dialog, "cmbRomMapMode");
        var shown = combo.Items.Cast<object>()
            .Select(item => item.GetType().GetProperty("Description")!.GetValue(item) as string)
            .ToList();

        shown.Should().Contain(Util.GetEnumDescription(RomMapMode.Sa1Rom));
        shown.Should().NotContain(nameof(RomMapMode.Sa1Rom));
        shown.Should().HaveCount(((IReadOnlyList<RomMapMode>)Enum.GetValues<RomMapMode>()).Count);
        combo.SelectedValue.Should().Be(RomMapMode.LoRom);
    }

    [Fact]
    public void BothGenerationCheckboxesStartOn()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        Widget<CheckBox>(dialog, "checkHeader").Checked.Should().BeTrue();
        viewModel.GenerateHeaderFlags.Should().BeTrue();

        // D4: bank-region synthesis used to have no control at all, and happened on every import.
        Widget<CheckBox>(dialog, "checkBankRegions").Checked.Should().BeTrue();
        viewModel.GenerateBankRegions.Should().BeTrue();
    }

    [Fact]
    public void DetectionFailureIsSaidOutLoud()
    {
        var (dialog, _) = MakeDialog(detectionSucceeded: false);
        using var _2 = dialog;

        Widget<WinFormsLabel>(dialog, "detectMessage").Text
            .Should().Be(SnesImportRomViewModel.DetectionFailedMessage);
    }

    // ------------------------------------------------------------------ widgets -> ViewModel

    [Fact]
    public void UntickingAVectorTakesItOutOfTheGeneratedLabels()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        Widget<CheckBox>(dialog, "checkboxNativeNMI").Checked = false;

        viewModel.EnabledVectorNames.Should().NotContain(SnesVectorNames.Native_NMI);
        viewModel.EnabledVectorNames.Should().Contain(SnesVectorNames.Native_IRQ);
    }

    [Fact]
    public void UntickingTheTwoGenerationBoxesReachesTheViewModel()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        Widget<CheckBox>(dialog, "checkHeader").Checked = false;
        Widget<CheckBox>(dialog, "checkBankRegions").Checked = false;

        viewModel.GenerateHeaderFlags.Should().BeFalse();
        viewModel.GenerateBankRegions.Should().BeFalse();
    }

    [Fact]
    public void PickingADifferentMapModeReReadsEveryValueOnScreen()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        Widget<ComboBox>(dialog, "cmbRomMapMode").SelectedValue = RomMapMode.HiRom;

        viewModel.SelectedRomMapMode.Should().Be(RomMapMode.HiRom);
        Widget<WinFormsLabel>(dialog, "romtitle").Text.Should().Be(HiRomTitle);
        Widget<TextBox>(dialog, "textNativeNMI").Text.Should().Be("9ABC");
    }

    // ------------------------------------------------------------------ ViewModel -> widgets

    [Fact]
    public void TheViewModelChangingTheMapModeMovesThePickerAndTheValues()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        viewModel.SelectedRomMapMode = RomMapMode.HiRom;

        Widget<ComboBox>(dialog, "cmbRomMapMode").SelectedValue.Should().Be(RomMapMode.HiRom);
        Widget<WinFormsLabel>(dialog, "romtitle").Text.Should().Be(HiRomTitle);
        Widget<TextBox>(dialog, "textEmuRESET").Text.Should().Be("9ABC");
    }

    [Fact]
    public void AMapModeTheVectorsCannotBeReadAtShowsPlaceholdersAndSaysWhy()
    {
        var viewModel = new SnesImportRomViewModel(
            initialSnapshot: Snapshot("SAMPLE TITLE", "8123", readable: true),
            detectedRomMapMode: RomMapMode.LoRom,
            detectionSucceeded: true,
            romSpeedText: "SlowROM",
            alwaysEnabledVectorNames: AlwaysEnabled,
            initiallyEnabledVectorNames: AllNames,
            recomputeForMapMode: _ => SnesVectorSnapshot.Unreadable(AllNames));

        using var dialog = new SnesImportRomDialog(viewModel);

        Widget<WinFormsLabel>(dialog, "statusMessage").Text.Should().BeEmpty();

        viewModel.SelectedRomMapMode = RomMapMode.HiRom;

        Widget<TextBox>(dialog, "textNativeNMI").Text.Should().Be(SnesVectorSnapshot.UnreadablePlaceholder);
        Widget<CheckBox>(dialog, "checkboxNativeNMI").Enabled.Should().BeFalse();
        Widget<CheckBox>(dialog, "checkboxNativeNMI").Checked.Should().BeFalse();
        Widget<WinFormsLabel>(dialog, "statusMessage").Text
            .Should().Be(SnesImportRomViewModel.VectorsUnreadableMessage);
    }

    // ------------------------------------------------------------------ D2

    [Fact]
    public void TheFourReservedVectorsHaveNoWidgetsAndStayOnRegardless()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        foreach (var name in AlwaysEnabled)
            dialog.Controls.Find(name, searchAllChildren: true).Should().BeEmpty();

        // switch off everything the window can reach
        foreach (var checkbox in dialog.Controls.Find("groupBox2", true).Single()
                     .Controls.OfType<CheckBox>())
            checkbox.Checked = false;

        viewModel.EnabledVectorNames.Should().BeEquivalentTo(AlwaysEnabled);
    }
}
