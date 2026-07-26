using System.Linq;
using System.Windows.Forms;
using Diz.Core.commands;
using Diz.Core.Interfaces;
using Diz.Core.model;
using Diz.Core.model.snes;
using Diz.Core.util;
using Diz.Cpu._65816;
using Diz.Ui.ViewModels.MarkMany;
using Diz.Ui.Winforms.dialogs;
using FluentAssertions;
using Xunit;

namespace Diz.Ui.Winforms.Test.Tests;

/// <summary>
/// MarkManyDialog is a thin host over MarkManyViewModel: every widget reads and writes the
/// ViewModel and nothing else. These tests construct the real form (never shown -- no window
/// handle is needed to drive the widget events) and check that traffic flows both ways.
/// Control visibility is deliberately not asserted: Control.Visible reads false for everything
/// on a form that was never shown, so it says nothing here.
/// </summary>
public class MarkManyDialogBindingTests
{
    private const int RomSize = 0x100;

    private static ISnesData MakeRom()
    {
        var romBytes = new RomBytes();
        for (var i = 0; i < RomSize; ++i)
            romBytes.Add(new RomByte { Rom = (byte) i });

        var data = new Data
        {
            RomMapMode = RomMapMode.HiRom,
            RomSpeed = RomSpeed.FastRom,
            RomBytes = romBytes,
        };
        data.Apis.AddIfDoesntExist(new SnesApi(data));
        return data.GetSnesApi()!;
    }

    private static (MarkManyDialog dialog, MarkManyViewModel<ISnesData> viewModel) MakeDialog(
        int start = 0x10,
        int count = 0x10,
        MarkCommand.MarkManyProperty? property = null)
    {
        var viewModel = new MarkManyViewModel<ISnesData>(MakeRom(), start, count);
        if (property != null)
            viewModel.SelectedProperty = property.Value;

        return (new MarkManyDialog(viewModel), viewModel);
    }

    private static T Widget<T>(Control parent, string name) where T : Control =>
        parent.Controls.Find(name, searchAllChildren: true).OfType<T>().Single();

    // ------------------------------------------------------------------ ViewModel -> widgets

    [Fact]
    public void TheWidgetsStartOutShowingWhatTheViewModelHolds()
    {
        var (dialog, viewModel) = MakeDialog(start: 0x20, count: 8);
        using var _ = dialog;

        Widget<ComboBox>(dialog, "comboPropertyType").SelectedIndex.Should().Be(0); // Flag
        Widget<ComboBox>(dialog, "flagCombo").SelectedIndex.Should().Be(3); // Data (8-Bit)
        Widget<ComboBox>(dialog, "mxCombo").SelectedIndex.Should().Be(0); // 16-Bit
        Widget<ComboBox>(dialog, "archCombo").SelectedIndex.Should().Be(0); // 65C816

        Widget<TextBox>(dialog, "textStart").Text.Should().Be(viewModel.Range.StartText);
        Widget<TextBox>(dialog, "textEnd").Text.Should().Be(viewModel.Range.EndText);
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("8");
    }

    [Fact]
    public void APreselectedPropertyIsShownInTheComboAndSizesTheValueBox()
    {
        var (dialog, _) = MakeDialog(property: MarkCommand.MarkManyProperty.DataBank);
        using var _2 = dialog;

        Widget<ComboBox>(dialog, "comboPropertyType").SelectedIndex.Should().Be(1);
        Widget<TextBox>(dialog, "regValue").MaxLength.Should().Be(3);
    }

    [Fact]
    public void SelectingDirectPageWidensTheValueBoxAndReseedsItFromTheRom()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        Widget<ComboBox>(dialog, "comboPropertyType").SelectedIndex = 2; // Direct Page

        viewModel.SelectedProperty.Should().Be(MarkCommand.MarkManyProperty.DirectPage);
        Widget<TextBox>(dialog, "regValue").MaxLength.Should().Be(5);
        Widget<TextBox>(dialog, "regValue").Text.Should().Be(
            Util.NumberToBaseString(viewModel.DirectPageValue, Util.NumberBase.Hexadecimal, 0));
    }

    // ------------------------------------------------------------------ widgets -> ViewModel

    [Fact]
    public void PickingAFlagTypePushesItToTheViewModel()
    {
        var (dialog, viewModel) = MakeDialog();
        using var _ = dialog;

        Widget<ComboBox>(dialog, "flagCombo").SelectedIndex = 13; // Text

        viewModel.FlagValue.Should().Be(FlagType.Text);
        viewModel.BuildMarkCommand()!.Value.Should().Be(FlagType.Text);
    }

    [Fact]
    public void PickingTheRegisterWidthPushesItToTheViewModel()
    {
        var (dialog, viewModel) = MakeDialog(property: MarkCommand.MarkManyProperty.MFlag);
        using var _ = dialog;

        Widget<ComboBox>(dialog, "mxCombo").SelectedIndex = 1; // 8-Bit

        viewModel.RegisterWidthIs8Bit.Should().BeTrue();
    }

    [Fact]
    public void EditingTheStartAddressMovesTheRangeAndUpdatesTheOtherFieldsOnly()
    {
        var (dialog, viewModel) = MakeDialog(start: 0x10, count: 0x10);
        using var _ = dialog;

        var textStart = Widget<TextBox>(dialog, "textStart");

        // lowercase, unpadded: whatever the user typed must survive untouched in the field
        // they are typing into, while the other fields follow along.
        var typed = Util.NumberToBaseString(
            viewModel.Range.StartSnesAddress + 8, Util.NumberBase.Hexadecimal, 0).ToLowerInvariant();
        textStart.Text = typed;

        viewModel.Range.StartIndex.Should().Be(0x18);
        viewModel.Range.EndIndex.Should().Be(0x1F); // end held still
        textStart.Text.Should().Be(typed);
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("8");
    }

    [Fact]
    public void EditingTheByteCountMovesTheEndOfTheRange()
    {
        var (dialog, viewModel) = MakeDialog(start: 0x10, count: 0x10);
        using var _ = dialog;

        Widget<TextBox>(dialog, "textCount").Text = "4";

        viewModel.Range.StartIndex.Should().Be(0x10);
        viewModel.Range.Count.Should().Be(4);
        Widget<TextBox>(dialog, "textEnd").Text.Should().Be(viewModel.Range.EndText);
    }

    [Fact]
    public void EditingTheRegisterValuePushesItToTheSelectedRegister()
    {
        var (dialog, viewModel) = MakeDialog(property: MarkCommand.MarkManyProperty.DataBank);
        using var _ = dialog;

        Widget<TextBox>(dialog, "regValue").Text = "7F";

        viewModel.DataBankValue.Should().Be(0x7F);
        viewModel.BuildMarkCommand()!.Value.Should().Be(0x7F);
    }

    [Fact]
    public void SwitchingToDecimalReformatsEveryNumberOnScreen()
    {
        var (dialog, viewModel) = MakeDialog(start: 0x10, count: 0x10,
            property: MarkCommand.MarkManyProperty.DataBank);
        using var _ = dialog;

        Widget<RadioButton>(dialog, "radioDec").Checked = true;

        viewModel.Range.UseHexadecimal.Should().BeFalse();
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("16");
        Widget<TextBox>(dialog, "textStart").Text.Should().Be(
            viewModel.Range.StartSnesAddress.ToString());
        Widget<TextBox>(dialog, "regValue").Text.Should().Be(viewModel.DataBankValue.ToString());
    }

    // ------------------------------------------------------------------ validation gating

    [Fact]
    public void OkIsRefusedWhileTheDataBankIsOutOfRange()
    {
        var (dialog, viewModel) = MakeDialog(property: MarkCommand.MarkManyProperty.DataBank);
        using var _ = dialog;

        var okay = Widget<Button>(dialog, "okay");
        okay.Enabled.Should().BeTrue();

        Widget<TextBox>(dialog, "regValue").Text = "1FF"; // > $FF

        okay.Enabled.Should().BeFalse();
        viewModel.BuildMarkCommand().Should().BeNull();

        Widget<TextBox>(dialog, "regValue").Text = "FF";

        okay.Enabled.Should().BeTrue();
        viewModel.BuildMarkCommand().Should().NotBeNull();
    }

    [Fact]
    public void OkIsRefusedWhileTheRangeIsEmpty()
    {
        var (dialog, _) = MakeDialog();
        using var _2 = dialog;

        var okay = Widget<Button>(dialog, "okay");

        Widget<TextBox>(dialog, "textCount").Text = "0";

        okay.Enabled.Should().BeFalse();

        Widget<TextBox>(dialog, "textCount").Text = "1";

        okay.Enabled.Should().BeTrue();
    }
}
