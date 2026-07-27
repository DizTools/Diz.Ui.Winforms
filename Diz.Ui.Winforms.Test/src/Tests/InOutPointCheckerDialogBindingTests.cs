using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Diz.Ui.Winforms.dialogs;
using FluentAssertions;
using Xunit;
using WinFormsLabel = System.Windows.Forms.Label;

namespace Diz.Ui.Winforms.Test.Tests;

/// <summary>
/// InOutPointCheckerDialog has no ViewModel and no state: it explains what a rescan does and
/// takes a yes or a no. So there is exactly one thing to pin -- which button means yes -- plus
/// the resource lookup the explanation depends on.
/// </summary>
public class InOutPointCheckerDialogBindingTests
{
    private static T Widget<T>(Control parent, string name) where T : Control =>
        parent.Controls.Find(name, searchAllChildren: true).OfType<T>().Single();

    // PerformClick() is not usable here: it refuses on a control that isn't selectable, and
    // nothing on a form that was never shown is.
    private static void RaiseClick(Control control) =>
        typeof(Control)
            .GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, new object[] { EventArgs.Empty });

    [Fact]
    public void NothingIsConfirmedUntilTheUserSaysSo()
    {
        using var dialog = new InOutPointCheckerDialog();

        dialog.DialogResult.Should().Be(DialogResult.None);
    }

    [Fact]
    public void RescanConfirms()
    {
        using var dialog = new InOutPointCheckerDialog();

        RaiseClick(Widget<Button>(dialog, "rescan"));

        dialog.DialogResult.Should().Be(DialogResult.OK);
    }

    [Fact]
    public void CancelDoesNotConfirm()
    {
        using var dialog = new InOutPointCheckerDialog();

        RaiseClick(Widget<Button>(dialog, "cancel"));

        dialog.DialogResult.Should().NotBe(DialogResult.OK);
    }

    [Fact]
    public void EnterConfirmsAndEscapeDoesNot()
    {
        // neither key is wired to a handler: they go through the form's AcceptButton and
        // CancelButton. Driving the real key path is not possible on a form that was never shown,
        // so what is asserted is where those keys land.
        using var dialog = new InOutPointCheckerDialog();

        dialog.AcceptButton.Should().BeSameAs(Widget<Button>(dialog, "rescan"));
        dialog.CancelButton.Should().BeSameAs(Widget<Button>(dialog, "cancel"));
    }

    [Fact]
    public void TheExplanationIsLoadedFromTheResourceFile()
    {
        // the designer builds a ComponentResourceManager from typeof(InOutPointCheckerDialog) and
        // reads this text out of InOutPointCheckerDialog.resx. If the class and the .resx ever
        // stop agreeing on the name, the lookup silently yields nothing and this fails.
        using var dialog = new InOutPointCheckerDialog();

        Widget<WinFormsLabel>(dialog, "label1").Text
            .Should().StartWith("Rescan all instructions for in points");
    }
}
