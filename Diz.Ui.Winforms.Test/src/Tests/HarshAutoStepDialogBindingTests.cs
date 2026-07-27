using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Diz.Core.Interfaces;
using Diz.Core.model;
using Diz.Core.model.snes;
using Diz.Cpu._65816;
using Diz.Ui.ViewModels.HarshAutoStep;
using Diz.Ui.Winforms.dialogs;
using FluentAssertions;
using Xunit;
using WinFormsLabel = System.Windows.Forms.Label;

namespace Diz.Ui.Winforms.Test.Tests;

/// <summary>
/// HarshAutoStepDialog is a thin host over HarshAutoStepViewModel: every widget reads and writes
/// the ViewModel and nothing else. These tests construct the real form (never shown -- no window
/// handle is needed to drive TextChanged / CheckedChanged) and check that traffic flows both
/// ways. A click is invoked through its protected raiser, because PerformClick() refuses on a
/// control that isn't selectable and nothing on a form that was never shown is.
///
/// The ROM is a $100-byte HiROM, so ROM file offset $10 is SNES address $C00010 and the last
/// byte is offset $FF / $C000FF.
/// </summary>
public class HarshAutoStepDialogBindingTests
{
    private const int RomSize = 0x100;
    private const int SeedOffset = 0x10;

    // the range seeded at SeedOffset can only reach the end of this small ROM, so the default
    // $100 bytes clamp down to this.
    private const int SeededCount = RomSize - SeedOffset;

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

    private static (HarshAutoStepDialog dialog, HarshAutoStepViewModel viewModel, ISnesData rom) MakeDialog(
        int startPcOffset = SeedOffset)
    {
        var rom = MakeRom();
        var viewModel = new HarshAutoStepViewModel(rom, rom.GetRomSize(), startPcOffset);
        return (new HarshAutoStepDialog(viewModel), viewModel, rom);
    }

    private static T Widget<T>(Control parent, string name) where T : Control =>
        parent.Controls.Find(name, searchAllChildren: true).OfType<T>().Single();

    // PerformClick() is not usable here: it refuses on a control that isn't selectable, and
    // nothing on a form that was never shown is.
    private static void RaiseClick(Control control) =>
        typeof(Control)
            .GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, new object[] { EventArgs.Empty });

    // the validation icon provider is created in code rather than by the designer, so it is only
    // reachable by reflection -- but it is what the user actually sees the message on.
    private static string ValidationErrorOn(HarshAutoStepDialog dialog, Control control) =>
        ((ErrorProvider) typeof(HarshAutoStepDialog)
            .GetField("validationErrors", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(dialog)!)
        .GetError(control);

    // ------------------------------------------------------------------ seeding

    [Fact]
    public void TheThreeBoxesStartOutShowingTheRangeTheCallerSeeded()
    {
        var (dialog, viewModel, _) = MakeDialog(startPcOffset: SeedOffset);
        using var _2 = dialog;

        // SNES addresses are padded to six digits; a byte count is never an address, so it isn't
        Widget<TextBox>(dialog, "textStart").Text.Should().Be("C00010");
        Widget<TextBox>(dialog, "textEnd").Text.Should().Be("C000FF");
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("F0");

        Widget<TextBox>(dialog, "textStart").Text.Should().Be(viewModel.Range.StartText);
        Widget<TextBox>(dialog, "textEnd").Text.Should().Be(viewModel.Range.EndText);
        Widget<TextBox>(dialog, "textCount").Text.Should().Be(viewModel.Range.CountText);

        viewModel.Range.Count.Should().Be(SeededCount);

        Widget<RadioButton>(dialog, "radioSNES").Checked.Should().BeTrue();
        Widget<RadioButton>(dialog, "radioPC").Checked.Should().BeFalse();
        Widget<RadioButton>(dialog, "radioHex").Checked.Should().BeTrue();
        Widget<RadioButton>(dialog, "radioDec").Checked.Should().BeFalse();
        Widget<Button>(dialog, "go").Enabled.Should().BeTrue();
    }

    [Fact]
    public void TheDefaultRangeIsTheFullDefaultCountEvenWhenItReachesTheLastByteOfTheRom()
    {
        // seeding at 0 in a $100-byte ROM asks for exactly the whole ROM. The legacy dialog
        // clamped the END to romSize - 1 and then recomputed COUNT from it, so it came back one
        // byte short ($FF); the range type clamps only when the range would genuinely leave the
        // ROM, so the full $100 survives.
        var (dialog, viewModel, _) = MakeDialog(startPcOffset: 0);
        using var _2 = dialog;

        Widget<TextBox>(dialog, "textStart").Text.Should().Be("C00000");
        Widget<TextBox>(dialog, "textEnd").Text.Should().Be("C000FF");
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("100");

        viewModel.Range.Count.Should().Be(HarshAutoStepViewModel.DefaultCount);
    }

    [Fact]
    public void TheWarningParagraphIsLoadedFromTheResourceFile()
    {
        // the designer builds a ComponentResourceManager from typeof(HarshAutoStepDialog) and
        // reads this text out of HarshAutoStepDialog.resx. If the class and the .resx ever stop
        // agreeing on the name, the lookup silently yields nothing and this fails.
        var (dialog, _, _) = MakeDialog();
        using var _2 = dialog;

        Widget<WinFormsLabel>(dialog, "label1").Text.Should().StartWith("WARNING:");
    }

    // ------------------------------------------------------------------ mutual updating

    [Fact]
    public void TypingAStartAddressMovesTheCountAndLeavesTheTypedTextExactlyAsTyped()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        var textStart = Widget<TextBox>(dialog, "textStart");

        // lowercase: whatever the user typed must survive untouched in the field they are typing
        // into, while the other fields follow along
        textStart.Text = "c00018";

        textStart.Text.Should().Be("c00018");
        viewModel.Range.StartIndex.Should().Be(0x18);
        viewModel.Range.EndIndex.Should().Be(0xFF, "moving the start leaves the end where it is");

        Widget<TextBox>(dialog, "textEnd").Text.Should().Be("C000FF");
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("E8");
    }

    [Fact]
    public void TypingAStartAddressInAMirrorBankStillLeavesTheTypedTextAlone()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        var textStart = Widget<TextBox>(dialog, "textStart");

        // A MIRROR BANK: HiROM ignores the top two bank bits, so $40:0018 and $C0:0018 are the
        // same ROM byte. The canonical form is what the ViewModel would render, so a host that
        // let text flow back into the box being typed in would retype the address under the
        // user's caret on every keystroke of a mirrored address.
        textStart.Text = "400018";

        textStart.Text.Should().Be("400018");
        viewModel.Range.StartIndex.Should().Be(0x18);
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("E8");
    }

    [Fact]
    public void TypingAnEndAddressIncludesThatByteInTheCount()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        var textEnd = Widget<TextBox>(dialog, "textEnd");
        textEnd.Text = "C0001F";

        // END IS INCLUSIVE: $10..$1F is $10 bytes. The legacy dialog treated End as exclusive
        // and would have said $F here.
        textEnd.Text.Should().Be("C0001F");
        viewModel.Range.StartIndex.Should().Be(SeedOffset, "moving the end leaves the start where it is");
        viewModel.Range.Count.Should().Be(0x10);

        Widget<TextBox>(dialog, "textStart").Text.Should().Be("C00010");
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("10");
    }

    [Fact]
    public void TypingAByteCountMovesTheEndOfTheRange()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        var textCount = Widget<TextBox>(dialog, "textCount");
        textCount.Text = "8";

        textCount.Text.Should().Be("8");
        viewModel.Range.StartIndex.Should().Be(SeedOffset);
        viewModel.Range.EndIndex.Should().Be(0x17);

        Widget<TextBox>(dialog, "textStart").Text.Should().Be("C00010");
        Widget<TextBox>(dialog, "textEnd").Text.Should().Be("C00017");
    }

    // ------------------------------------------------------------------ display toggles

    [Fact]
    public void SwitchingToDecimalReExpressesEveryBoxAndBackAgain()
    {
        var (dialog, viewModel, rom) = MakeDialog();
        using var _2 = dialog;

        Widget<RadioButton>(dialog, "radioDec").Checked = true;

        viewModel.Range.UseHexadecimal.Should().BeFalse();
        Widget<TextBox>(dialog, "textStart").Text.Should().Be(rom.ConvertPCtoSnes(SeedOffset).ToString());
        Widget<TextBox>(dialog, "textEnd").Text.Should().Be(rom.ConvertPCtoSnes(RomSize - 1).ToString());
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("240"); // $F0

        Widget<RadioButton>(dialog, "radioHex").Checked = true;

        viewModel.Range.UseHexadecimal.Should().BeTrue();
        Widget<TextBox>(dialog, "textStart").Text.Should().Be("C00010");
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("F0");
    }

    [Fact]
    public void SwitchingToRomFileOffsetsReExpressesTheAddressesButNotTheCount()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        // radioPC has no handler of its own: checking it unchecks radioSNES, and the pair reports
        // through the button losing its check too.
        Widget<RadioButton>(dialog, "radioPC").Checked = true;

        viewModel.Range.UseSnesAddresses.Should().BeFalse();
        Widget<TextBox>(dialog, "textStart").Text.Should().Be("10");
        Widget<TextBox>(dialog, "textEnd").Text.Should().Be("FF");
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("F0");

        Widget<RadioButton>(dialog, "radioSNES").Checked = true;

        viewModel.Range.UseSnesAddresses.Should().BeTrue();
        Widget<TextBox>(dialog, "textStart").Text.Should().Be("C00010");
        Widget<TextBox>(dialog, "textEnd").Text.Should().Be("C000FF");
    }

    [Fact]
    public void TheViewModelFlippingTheAddressSpaceMovesTheRadioButtons()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        viewModel.Range.UseSnesAddresses = false;

        Widget<RadioButton>(dialog, "radioPC").Checked.Should().BeTrue();
        Widget<RadioButton>(dialog, "radioSNES").Checked.Should().BeFalse();
        Widget<TextBox>(dialog, "textStart").Text.Should().Be("10");
    }

    // ------------------------------------------------------------------ addresses that map nowhere

    [Fact]
    public void ASnesStartAddressThatIsNotInThisRomIsIgnoredEntirely()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        var textStart = Widget<TextBox>(dialog, "textStart");

        // $7E0000 is SNES WRAM: a real address, but no byte of this ROM lives there. The legacy
        // dialog took the converter's -1 at face value and silently jumped the start to 0.
        textStart.Text = "7E0000";

        textStart.Text.Should().Be("7E0000", "the text the user typed is left alone");
        viewModel.Range.StartIndex.Should().Be(SeedOffset);
        Widget<TextBox>(dialog, "textEnd").Text.Should().Be("C000FF");
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("F0");
    }

    [Fact]
    public void ASnesEndAddressThatIsNotInThisRomIsIgnoredEntirely()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        var textEnd = Widget<TextBox>(dialog, "textEnd");
        textEnd.Text = "7E0000";

        textEnd.Text.Should().Be("7E0000");
        viewModel.Range.EndIndex.Should().Be(RomSize - 1);
        viewModel.Range.Count.Should().Be(SeededCount);
        Widget<TextBox>(dialog, "textCount").Text.Should().Be("F0");
    }

    // ------------------------------------------------------------------ validation gating

    [Fact]
    public void AnEmptyRangeDisablesGoAndTheCountBoxSaysWhy()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        var go = Widget<Button>(dialog, "go");
        var textCount = Widget<TextBox>(dialog, "textCount");

        go.Enabled.Should().BeTrue();
        ValidationErrorOn(dialog, textCount).Should().BeEmpty();

        textCount.Text = "0";

        go.Enabled.Should().BeFalse();
        viewModel.BuildAutoStepHarshCommand().Should().BeNull();
        ValidationErrorOn(dialog, textCount).Should().Be(HarshAutoStepViewModel.EmptyRangeMessage);

        textCount.Text = "10";

        go.Enabled.Should().BeTrue();
        viewModel.BuildAutoStepHarshCommand().Should().NotBeNull();
        ValidationErrorOn(dialog, textCount).Should().BeEmpty();
    }

    [Fact]
    public void GoRefusesToConfirmWhileTheRangeIsEmpty()
    {
        var (dialog, _, _) = MakeDialog();
        using var _2 = dialog;

        Widget<TextBox>(dialog, "textCount").Text = "0";
        RaiseClick(Widget<Button>(dialog, "go"));

        dialog.DialogResult.Should().Be(DialogResult.None);
    }

    [Fact]
    public void EnterCannotConfirmWhileTheRangeIsEmpty()
    {
        var (dialog, _, _) = MakeDialog();
        using var _2 = dialog;

        var go = Widget<Button>(dialog, "go");

        // Enter is not wired to a key handler here: it goes through the form's AcceptButton,
        // which clicks this very button. So "Enter can't confirm an empty range" is two facts --
        // Enter reaches `go` and nothing else, and `go` refuses -- and both are asserted. Driving
        // the real key path is not possible on a form that was never shown, because the button
        // isn't selectable and PerformClick() would no-op regardless of the range.
        dialog.AcceptButton.Should().BeSameAs(go);

        Widget<TextBox>(dialog, "textCount").Text = "0";

        go.Enabled.Should().BeFalse("a disabled AcceptButton is not clicked by Enter");
        RaiseClick(go);
        dialog.DialogResult.Should().Be(DialogResult.None);

        Widget<TextBox>(dialog, "textCount").Text = "10";

        go.Enabled.Should().BeTrue();
        RaiseClick(go);
        dialog.DialogResult.Should().Be(DialogResult.OK);
    }

    // ------------------------------------------------------------------ confirming

    [Fact]
    public void ConfirmingHandsBackACommandDescribingWhatIsOnScreen()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        Widget<TextBox>(dialog, "textStart").Text = "C00020";
        Widget<TextBox>(dialog, "textCount").Text = "40";

        RaiseClick(Widget<Button>(dialog, "go"));

        dialog.DialogResult.Should().Be(DialogResult.OK);

        var command = viewModel.BuildAutoStepHarshCommand();
        command.Should().NotBeNull();
        command!.Start.Should().Be(0x20);
        command.Count.Should().Be(0x40);
    }
}
