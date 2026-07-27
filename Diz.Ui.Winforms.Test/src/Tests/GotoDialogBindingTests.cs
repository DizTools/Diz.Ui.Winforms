using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Diz.Core.Interfaces;
using Diz.Core.model;
using Diz.Core.model.snes;
using Diz.Cpu._65816;
using Diz.Ui.ViewModels.Goto;
using Diz.Ui.Winforms.dialogs;
using FluentAssertions;
using Xunit;
using WinFormsLabel = System.Windows.Forms.Label;

namespace Diz.Ui.Winforms.Test.Tests;

/// <summary>
/// GotoDialog is a thin host over GotoViewModel: every widget reads and writes the ViewModel
/// and nothing else. These tests construct the real form (never shown -- no window handle is
/// needed to drive TextChanged / CheckedChanged) and check that traffic flows both ways.
/// The two events a user raises that a hidden form does not raise on its own -- the form's
/// Load and a key press -- are invoked through their protected raisers, so the wiring the
/// designer file sets up is still what gets exercised.
/// </summary>
public class GotoDialogBindingTests
{
    private const int RomSize = 0x100;
    private const int SeedOffset = 0x10;

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

    private static (GotoDialog dialog, GotoViewModel viewModel, ISnesData rom) MakeDialog(
        int startPcOffset = SeedOffset,
        bool initiallySelectSnesAddr = true)
    {
        var rom = MakeRom();
        var viewModel = new GotoViewModel(rom, rom.GetRomSize(), startPcOffset);
        return (new GotoDialog(viewModel, initiallySelectSnesAddr), viewModel, rom);
    }

    private static T Widget<T>(Control parent, string name) where T : Control =>
        parent.Controls.Find(name, searchAllChildren: true).OfType<T>().Single();

    private static void RaiseLoad(Form form) =>
        typeof(Form)
            .GetMethod("OnLoad", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(form, new object[] { EventArgs.Empty });

    private static void RaiseKeyDown(Control control, Keys key) =>
        typeof(Control)
            .GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, new object[] { new KeyEventArgs(key) });

    // PerformClick() is not usable here: it refuses on a control that isn't selectable, and
    // nothing on a form that was never shown is.
    private static void RaiseClick(Control control) =>
        typeof(Control)
            .GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, new object[] { EventArgs.Empty });

    // ------------------------------------------------------------------ seeding

    [Fact]
    public void BothBoxesStartOutShowingTheOffsetTheCallerSeeded()
    {
        var (dialog, viewModel, _) = MakeDialog(startPcOffset: 0x10);
        using var _2 = dialog;

        // SNES addresses are padded to six digits, ROM file offsets are not
        Widget<TextBox>(dialog, "textROM").Text.Should().Be("C00010");
        Widget<TextBox>(dialog, "textPC").Text.Should().Be("10");

        Widget<TextBox>(dialog, "textROM").Text.Should().Be(viewModel.SnesText);
        Widget<TextBox>(dialog, "textPC").Text.Should().Be(viewModel.PcText);

        Widget<RadioButton>(dialog, "radioHex").Checked.Should().BeTrue();
        Widget<RadioButton>(dialog, "radioDec").Checked.Should().BeFalse();
        Widget<Button>(dialog, "go").Enabled.Should().BeTrue();
        Widget<WinFormsLabel>(dialog, "lblError").Text.Should().BeEmpty();
    }

    [Fact]
    public void TheFlagBeingTrueSelectsTheSnesAddressBox()
    {
        var (dialog, _, _) = MakeDialog(initiallySelectSnesAddr: true);
        using var _2 = dialog;

        RaiseLoad(dialog);

        Widget<TextBox>(dialog, "textROM").SelectionLength.Should().Be("C00010".Length);
        Widget<TextBox>(dialog, "textPC").SelectionLength.Should().Be(0);
    }

    [Fact]
    public void TheFlagBeingFalseSelectsTheRomFileOffsetBox()
    {
        var (dialog, _, _) = MakeDialog(initiallySelectSnesAddr: false);
        using var _2 = dialog;

        RaiseLoad(dialog);

        Widget<TextBox>(dialog, "textPC").SelectionLength.Should().Be("10".Length);
        Widget<TextBox>(dialog, "textROM").SelectionLength.Should().Be(0);
    }

    // ------------------------------------------------------------------ mutual updating

    [Fact]
    public void TypingASnesAddressMovesTheOffsetBoxAndLeavesTheTypedTextExactlyAsTyped()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        var textROM = Widget<TextBox>(dialog, "textROM");

        // lowercase: whatever the user typed must survive untouched in the field they are
        // typing into, while the other field follows along
        textROM.Text = "c00018";

        textROM.Text.Should().Be("c00018");
        Widget<TextBox>(dialog, "textPC").Text.Should().Be("18");
        viewModel.ResultPcOffset.Should().Be(0x18);
    }

    [Fact]
    public void TypingAnAddressWhoseRoundTripIsNotTheIdentityStillLeavesTheTypedTextAlone()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        var textROM = Widget<TextBox>(dialog, "textROM");

        // A MIRROR BANK: HiROM ignores the top two bank bits, so $40:0018 and $C0:0018 are the
        // same ROM byte. The offset box therefore holds a number that converts BACK to the
        // canonical bank -- "C00018", not the bank that was typed. Only the other box may
        // follow; a host that let the offset flow back in would retype the address under the
        // user's caret, and the field would fight every keystroke of a mirrored address.
        textROM.Text = "400018";

        textROM.Text.Should().Be("400018");
        Widget<TextBox>(dialog, "textPC").Text.Should().Be("18");
        viewModel.ResultPcOffset.Should().Be(0x18);
    }

    [Fact]
    public void TypingARomFileOffsetMovesTheSnesBox()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        var textPC = Widget<TextBox>(dialog, "textPC");
        textPC.Text = "20";

        textPC.Text.Should().Be("20");
        Widget<TextBox>(dialog, "textROM").Text.Should().Be("C00020");
        viewModel.ResultPcOffset.Should().Be(0x20);
    }

    [Fact]
    public void PastingALabelRewritesTheEditedBoxToJustTheAddressInsideIt()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        var textROM = Widget<TextBox>(dialog, "textROM");
        textROM.Text = "CODE_C00018";

        // the one case where the box being edited IS rewritten: stripping changed the text
        textROM.Text.Should().Be("C00018");
        Widget<TextBox>(dialog, "textPC").Text.Should().Be("18");
        viewModel.ResultPcOffset.Should().Be(0x18);
    }

    [Fact]
    public void PastingALabelIntoTheOffsetBoxStripsItToo()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        var textPC = Widget<TextBox>(dialog, "textPC");
        textPC.Text = "CODE_000018";

        // the six digits the label carries are what is left behind, verbatim -- not reformatted
        textPC.Text.Should().Be("000018");
        Widget<TextBox>(dialog, "textROM").Text.Should().Be("C00018");
        viewModel.ResultPcOffset.Should().Be(0x18);
    }

    // ------------------------------------------------------------------ number base

    [Fact]
    public void SwitchingToDecimalReExpressesBothBoxesAndBackAgain()
    {
        var (dialog, viewModel, rom) = MakeDialog(startPcOffset: 0x10);
        using var _2 = dialog;

        Widget<RadioButton>(dialog, "radioDec").Checked = true;

        viewModel.UseHexadecimal.Should().BeFalse();
        Widget<TextBox>(dialog, "textROM").Text.Should().Be(rom.ConvertPCtoSnes(0x10).ToString());
        Widget<TextBox>(dialog, "textPC").Text.Should().Be("16");

        Widget<RadioButton>(dialog, "radioHex").Checked = true;

        viewModel.UseHexadecimal.Should().BeTrue();
        Widget<TextBox>(dialog, "textROM").Text.Should().Be("C00010");
        Widget<TextBox>(dialog, "textPC").Text.Should().Be("10");
    }

    [Fact]
    public void TheViewModelFlippingTheBaseMovesTheRadioButtons()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        viewModel.UseHexadecimal = false;

        Widget<RadioButton>(dialog, "radioDec").Checked.Should().BeTrue();
        Widget<RadioButton>(dialog, "radioHex").Checked.Should().BeFalse();
        Widget<TextBox>(dialog, "textPC").Text.Should().Be("16");
    }

    // ------------------------------------------------------------------ validation

    [Fact]
    public void UnparseableOffsetTextDisablesGoAndNamesTheOffsetBox()
    {
        var (dialog, _, _) = MakeDialog();
        using var _2 = dialog;

        var go = Widget<Button>(dialog, "go");
        var lblError = Widget<WinFormsLabel>(dialog, "lblError");

        Widget<TextBox>(dialog, "textPC").Text = "zz";

        go.Enabled.Should().BeFalse();
        lblError.Text.Should().Be(GotoViewModel.InvalidPcOffsetMessage);
        lblError.Text.Should().Be("Invalid ROM File Offset");

        Widget<TextBox>(dialog, "textPC").Text = "20";

        go.Enabled.Should().BeTrue();
        lblError.Text.Should().BeEmpty();
    }

    [Fact]
    public void ASnesAddressThatIsNotInThisRomNamesTheSnesBoxAndWinsOverTheOffsetMessage()
    {
        var (dialog, _, _) = MakeDialog();
        using var _2 = dialog;

        var go = Widget<Button>(dialog, "go");
        var lblError = Widget<WinFormsLabel>(dialog, "lblError");

        // break the offset box first, so both boxes are bad when the second edit lands
        Widget<TextBox>(dialog, "textPC").Text = "zz";
        lblError.Text.Should().Be(GotoViewModel.InvalidPcOffsetMessage);

        Widget<TextBox>(dialog, "textROM").Text = "qq";

        go.Enabled.Should().BeFalse();
        lblError.Text.Should().Be(GotoViewModel.InvalidSnesAddressMessage);
        lblError.Text.Should().Be("Invalid SNES Address");

        // one good SNES address fixes both boxes at once, since accepting it rewrites the other
        Widget<TextBox>(dialog, "textROM").Text = "C00030";

        go.Enabled.Should().BeTrue();
        lblError.Text.Should().BeEmpty();
        Widget<TextBox>(dialog, "textPC").Text.Should().Be("30");
    }

    // ------------------------------------------------------------------ confirming

    [Fact]
    public void EnterDoesNothingWhileTheBoxesDoNotNameAPlaceToGo()
    {
        var (dialog, _, _) = MakeDialog();
        using var _2 = dialog;

        Widget<TextBox>(dialog, "textROM").Text = "qq";
        Widget<Button>(dialog, "go").Enabled.Should().BeFalse();

        RaiseKeyDown(Widget<TextBox>(dialog, "textROM"), Keys.Enter);
        RaiseKeyDown(Widget<TextBox>(dialog, "textPC"), Keys.Enter);

        dialog.DialogResult.Should().Be(DialogResult.None);
    }

    [Fact]
    public void EnterConfirmsOnceTheBoxesAreValidAgain()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        Widget<TextBox>(dialog, "textROM").Text = "qq";
        RaiseKeyDown(Widget<TextBox>(dialog, "textROM"), Keys.Enter);
        dialog.DialogResult.Should().Be(DialogResult.None);

        Widget<TextBox>(dialog, "textROM").Text = "C00040";
        RaiseKeyDown(Widget<TextBox>(dialog, "textROM"), Keys.Enter);

        dialog.DialogResult.Should().Be(DialogResult.OK);
        viewModel.ResultPcOffset.Should().Be(0x40);
    }

    [Fact]
    public void TheGoButtonConfirmsTheSameWayEnterDoes()
    {
        var (dialog, viewModel, _) = MakeDialog();
        using var _2 = dialog;

        Widget<TextBox>(dialog, "textPC").Text = "48";
        RaiseClick(Widget<Button>(dialog, "go"));

        dialog.DialogResult.Should().Be(DialogResult.OK);
        viewModel.ResultPcOffset.Should().Be(0x48);
    }
}
