using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Diz.Core.model;
using Diz.Ui.ViewModels.Navigation;
using Diz.Ui.Winforms.dialogs;
using Diz.Ui.Winforms.usercontrols;
using FluentAssertions;
using Xunit;

namespace Diz.Ui.Winforms.Test.Tests;

/// <summary>
/// NavigationHistoryViewControl / NavigationHistoryForm are a thin host over
/// NavigationHistoryViewModel. These tests construct the real control and the real window (never
/// shown -- no window handle is needed to read control state) and check three things the old
/// control got wrong or could not do at all:
///
/// 1. BACK AND FORWARD NEED NO WINDOW. They are main-menu commands; the history and the place in
///    it live in the ViewModel the main window owns, so they work with the history window closed
///    and with it never having been constructed.
///
/// 2. RECORDING A POINT DOES NOT NAVIGATE TO IT. The old control drove navigation from
///    BindingSource.CurrentChanged, so an append re-navigated to the entry just recorded. Several
///    tests here exist only to stop that being re-joined -- including through the grid's own
///    selection, which moves by itself when the first row appears.
///
/// 3. ONE OVERSHOOT FOR "GO BACK" (decision D4). The old ← → buttons passed 0 by defaulted
///    argument, and the menu's 12 was overwritten anyway because the row-select that followed the
///    navigation navigated a second time with 0. Both triggers now ask for the host's standard
///    overshoot; picking a row out of the list still lands exactly on it.
///
/// SNES addresses here convert to a ROM offset by dropping $C00000; anything below that is "not in
/// this ROM" and must produce no navigation at all.
/// </summary>
public class NavigationHistoryViewBindingTests
{
    /// <summary>What MainWindow seeds the window with (MainWindow.Actions.standardOvershootAmount).</summary>
    private const int StandardOvershoot = 12;

    private const int SnesBase = 0xC00000;

    private static int ConvertSnesToPc(int snesAddress) =>
        snesAddress >= SnesBase ? snesAddress - SnesBase : -1;

    // ------------------------------------------------------------------ fixture

    private sealed class Fixture : IDisposable
    {
        public required BindingList<NavigationEntry> History { get; init; }
        public required NavigationHistoryViewModel ViewModel { get; init; }
        public required List<NavigationRequest> Requests { get; init; }

        /// <summary>Null until <see cref="NavigationHistoryViewBindingTests.Attach"/> is called.</summary>
        public NavigationHistoryViewControl? Control { get; set; }

        /// <summary>Null until <see cref="NavigationHistoryViewBindingTests.OpenWindow"/> is called.</summary>
        public NavigationHistoryForm? Window { get; set; }

        public DataGridView Grid => Widget<DataGridView>(Control!, "dataGridView1");

        public BindingSource BindingSource =>
            (BindingSource) typeof(NavigationHistoryViewControl)
                .GetField("navigationEntryBindingSource", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(Control!)!;

        public ToolStripButton Button(string name) =>
            Widget<ToolStrip>(Control!, "toolStrip1").Items.OfType<ToolStripButton>()
                .Single(item => item.Name == name);

        public void Record(int snesAddress) => History.Add(Entry(snesAddress));

        public void Dispose()
        {
            Window?.Dispose();
            Control?.Dispose();
            ViewModel.Dispose();
        }
    }

    private static NavigationEntry Entry(int snesAddress) =>
        new(snesAddress, "went somewhere", "start", data: null);

    /// <summary>
    /// A ViewModel over a history list, and every navigation it asked for -- with NO window. This
    /// is the shape MainWindow builds in its constructor, before any window exists.
    /// </summary>
    private static Fixture Build(params int[] snesAddresses)
    {
        // built the way DizDocument builds it, flags and all.
        var history = new BindingList<NavigationEntry>
        {
            RaiseListChangedEvents = true,
            AllowNew = false,
            AllowRemove = false,
            AllowEdit = false,
        };

        foreach (var snesAddress in snesAddresses)
            history.Add(Entry(snesAddress));

        var viewModel = new NavigationHistoryViewModel(history, ConvertSnesToPc);
        var requests = new List<NavigationRequest>();
        viewModel.NavigationRequested += (_, request) => requests.Add(request);

        return new Fixture { History = history, ViewModel = viewModel, Requests = requests };
    }

    /// <summary>
    /// The real window over the fixture's ViewModel, with its handle realized -- a bound
    /// DataGridView only materializes its rows once it has one. Nothing is ever shown.
    ///
    /// Realizing the handle is also the point: it is the moment the grid puts its own caret on row
    /// 0 while the user is on the NEWEST entry, which is the selection change a selection-driven
    /// design would have mistaken for the user asking to go to the oldest point in their history.
    /// </summary>
    private static Fixture Attach(Fixture fixture)
    {
        OpenWindow(fixture);

        fixture.Control = fixture.Window!.Controls.Find("navigationCtrl", searchAllChildren: true)
            .OfType<NavigationHistoryViewControl>().Single();

        // CreateControl() is a no-op while the hierarchy is invisible; reading Handle creates the
        // window unconditionally, which is all the binding needs.
        _ = fixture.Window.Handle;
        _ = fixture.Control.Handle;
        _ = fixture.Grid.Handle;

        return fixture;
    }

    /// <summary>Construct the real window over the fixture's ViewModel, exactly as MainWindow does.</summary>
    private static Fixture OpenWindow(Fixture fixture)
    {
        fixture.Window = new NavigationHistoryForm
        {
            ViewModel = fixture.ViewModel,
            BackForwardOvershoot = StandardOvershoot,
        };
        return fixture;
    }

    // ------------------------------------------------------------------ event helpers

    private static T Widget<T>(Control parent, string name) where T : Control =>
        parent.Controls.Find(name, searchAllChildren: true).OfType<T>().Single();

    private static void Raise(object target, string method, EventArgs args) =>
        target.GetType()
            .GetMethod(method,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, [args.GetType()], null)!
            .Invoke(target, [args]);

    // PerformClick() depends on the item being available on a shown parent; nothing on a window
    // that was never shown is. The protected raiser is what PerformClick would reach anyway.
    private static void Click(ToolStripItem item) =>
        typeof(ToolStripItem)
            .GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(item, [EventArgs.Empty]);

    private static void DoubleClickRow(Fixture fixture, int rowIndex) =>
        Raise(fixture.Grid, "OnCellMouseDoubleClick", new DataGridViewCellMouseEventArgs(
            0, rowIndex, 1, 1, new MouseEventArgs(MouseButtons.Left, 2, 0, 0, 0)));

    /// <summary>Move the grid's selection the way a user's single click on a row does.</summary>
    private static void SelectRow(Fixture fixture, int rowIndex) =>
        fixture.Grid.CurrentCell = fixture.Grid.Rows[rowIndex].Cells[0];

    /// <summary>Activate the focused row from the keyboard.</summary>
    private static void PressEnter(Fixture fixture) =>
        Raise(fixture.Grid, "OnKeyDown", new KeyEventArgs(Keys.Enter));

    // ------------------------------------------------------------------ no window at all

    [Fact]
    public void BackAndForwardWorkWhenNoWindowWasEverConstructed()
    {
        using var fixture = Build(0xC00010, 0xC00020, 0xC00030);

        // this is the state MainWindow is in until the user opens the history window -- which they
        // may never do. Nothing below touches a form.
        fixture.Window.Should().BeNull();

        fixture.ViewModel.MoveBack(StandardOvershoot);
        fixture.ViewModel.MoveBack(StandardOvershoot);

        fixture.Requests.Select(r => r.PcOffset).Should().Equal(0x20, 0x10);
        fixture.ViewModel.CurrentIndex.Should().Be(0);

        fixture.ViewModel.MoveForward(StandardOvershoot);
        fixture.Requests.Last().PcOffset.Should().Be(0x20);
    }

    [Fact]
    public void ThePlaceInTheHistorySurvivesTheWindowBeingOpenedAndDisposed()
    {
        using var fixture = Build(0xC00010, 0xC00020, 0xC00030);

        fixture.ViewModel.MoveBack(StandardOvershoot); // now on index 1

        OpenWindow(fixture);
        fixture.Window!.Dispose();
        fixture.Window = null;

        fixture.ViewModel.CurrentIndex.Should().Be(1, "the history and the place in it are not the window's");

        fixture.ViewModel.MoveBack(StandardOvershoot);
        fixture.Requests.Last().PcOffset.Should().Be(0x10);
    }

    // ------------------------------------------------------------------ recording is not navigating

    [Fact]
    public void RecordingTheFirstEntryWithTheHistoryOnScreenNavigatesNowhere()
    {
        // THE ONE THAT USED TO BREAK, and the hardest case: the grid picks its own selection when
        // a row appears in an empty grid, which is not a selection change the control makes.
        using var fixture = Attach(Build());

        fixture.Record(0xC00010);

        fixture.Requests.Should().BeEmpty();
        fixture.ViewModel.CurrentIndex.Should().Be(0, "the next 'back' has to leave the point just recorded");
    }

    [Fact]
    public void RecordingMoreEntriesWithTheHistoryOnScreenNavigatesNowhereAndFollowsTheNewestRow()
    {
        using var fixture = Attach(Build(0xC00010));

        fixture.Record(0xC00020);
        fixture.Record(0xC00030);

        fixture.Requests.Should().BeEmpty();
        fixture.ViewModel.CurrentIndex.Should().Be(2);
        fixture.BindingSource.Position.Should().Be(2, "the grid follows the newest entry, as it always did");
    }

    [Fact]
    public void TheCurrentChangedNavigationWiringIsGone()
    {
        // the exact mechanism of the old bug: moving the BindingSource's position fired
        // CurrentChanged, which navigated. Moving it directly must now do nothing at all.
        using var fixture = Attach(Build(0xC00010, 0xC00020, 0xC00030));

        fixture.ViewModel.CurrentIndex.Should().Be(2);
        fixture.BindingSource.Position = 0;

        fixture.Requests.Should().BeEmpty();
    }

    [Fact]
    public void ClearingTheHistoryEmptiesItAndNavigatesNowhere()
    {
        using var fixture = Attach(Build(0xC00010, 0xC00020));

        Click(fixture.Button("btnClearHistory"));

        fixture.History.Should().BeEmpty();
        fixture.Requests.Should().BeEmpty("the user is still looking at wherever they already were");
        fixture.ViewModel.CurrentIndex.Should().Be(NavigationHistoryViewModel.NoSelection);
    }

    [Fact]
    public void OpeningTheWindowOverAnExistingHistoryNavigatesNowhereAndHighlightsWhereTheUserIs()
    {
        using var fixture = Build(0xC00010, 0xC00020, 0xC00030);

        Attach(fixture); // realizes the grid, which selects row 0 all by itself

        fixture.Requests.Should().BeEmpty("opening a window is not asking to go anywhere");
        fixture.Grid.RowCount.Should().Be(3);
        fixture.BindingSource.Position.Should().Be(2, "the user is on the newest entry, not the oldest");
    }

    // ------------------------------------------------------------------ overshoot on each path (D4)

    [Fact]
    public void TheInWindowArrowsAskForTheSameOvershootTheMenuCommandsDo()
    {
        using var fixture = Attach(Build(0xC00010, 0xC00020, 0xC00030));

        Click(fixture.Button("btnBack"));

        // D4: the old buttons passed 0 by defaulted argument. "Go back" means one thing however it
        // is triggered.
        fixture.Requests.Should().ContainSingle().Which
            .Should().Be(new NavigationRequest(0x20, StandardOvershoot));

        Click(fixture.Button("btnForward"));

        fixture.Requests.Last().Should().Be(new NavigationRequest(0x30, StandardOvershoot));
    }

    [Fact]
    public void AnUnseededHostGetsNoOvershootRatherThanAGuessedOne()
    {
        // the property is what the host seeds; left alone it is the value that says "land exactly
        // here", never an invented number.
        using var fixture = Build(0xC00010, 0xC00020);
        using var window = new NavigationHistoryForm { ViewModel = fixture.ViewModel };

        window.BackForwardOvershoot.Should().Be(NavigationHistoryViewModel.NoOvershoot);
    }

    [Fact]
    public void DoubleClickingARowLandsExactlyOnItWithNoOvershoot()
    {
        using var fixture = Attach(Build(0xC00010, 0xC00020, 0xC00030));

        DoubleClickRow(fixture, 0);

        fixture.Requests.Should().ContainSingle().Which
            .Should().Be(new NavigationRequest(0x10, NavigationHistoryViewModel.NoOvershoot));
    }

    [Fact]
    public void DoubleClickingTheRowAlreadySelectedStillReCentresOnIt()
    {
        using var fixture = Attach(Build(0xC00010, 0xC00020, 0xC00030));

        fixture.ViewModel.CurrentIndex.Should().Be(2);
        DoubleClickRow(fixture, 2);

        fixture.Requests.Should().ContainSingle().Which.PcOffset.Should().Be(0x30);
    }

    [Fact]
    public void DoubleClickingTheColumnHeadersNavigatesNowhere()
    {
        using var fixture = Attach(Build(0xC00010, 0xC00020));

        DoubleClickRow(fixture, -1); // row index -1 is the header row

        fixture.Requests.Should().BeEmpty();
    }

    [Fact]
    public void EnterOnTheFocusedRowActivatesItWithNoOvershoot()
    {
        using var fixture = Attach(Build(0xC00010, 0xC00020, 0xC00030));

        SelectRow(fixture, 0);
        PressEnter(fixture);

        fixture.Requests.Should().ContainSingle().Which
            .Should().Be(new NavigationRequest(0x10, NavigationHistoryViewModel.NoOvershoot));
        fixture.ViewModel.CurrentIndex.Should().Be(0);
    }

    [Fact]
    public void MerelySelectingARowNavigatesNowhere()
    {
        using var fixture = Attach(Build(0xC00010, 0xC00020, 0xC00030));

        // deliberate: the grid moves its own selection during binding and on an append, so
        // "selection moved" cannot be the trigger. Activation (double-click / Enter) is.
        SelectRow(fixture, 0);

        fixture.Requests.Should().BeEmpty();
    }

    [Fact]
    public void TheGridFollowingTheViewModelIsNotTheUserPickingARow()
    {
        using var fixture = Attach(Build(0xC00010, 0xC00020, 0xC00030));

        // back/forward move the ViewModel, and the highlight follows. That follow-up selection
        // must not be read as a fresh request, or every move would navigate twice -- which is what
        // used to overwrite the menu's overshoot with 0.
        Click(fixture.Button("btnBack"));

        fixture.Requests.Should().ContainSingle()
            .Which.OvershootAmount.Should().Be(StandardOvershoot);
        fixture.BindingSource.Position.Should().Be(1);
    }

    // ------------------------------------------------------------------ entries that map nowhere

    [Fact]
    public void AnEntryThatIsNotInTheOpenRomSelectsButNavigatesNowhere()
    {
        // history survives closing a project and opening a different one, so an entry can name an
        // address this ROM does not have. $7E0000 is SNES WRAM.
        using var fixture = Attach(Build(0xC00010, 0x7E0000));

        Click(fixture.Button("btnBack"));
        Click(fixture.Button("btnForward"));

        fixture.Requests.Should().ContainSingle().Which.PcOffset.Should().Be(0x10);
        fixture.ViewModel.CurrentIndex.Should().Be(1, "the selection still moves, or back/forward would strand");
    }

    // ------------------------------------------------------------------ the window itself

    [Fact]
    public void ClosingTheWindowHidesItInsteadOfDestroyingIt()
    {
        using var fixture = OpenWindow(Build(0xC00010));

        var args = new FormClosingEventArgs(CloseReason.UserClosing, cancel: false);
        Raise(fixture.Window!, "OnFormClosing", args);

        args.Cancel.Should().BeTrue("reopening the history must be instant, and it keeps its scroll position");
        fixture.Window!.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void TheApplicationShuttingDownReallyDoesCloseTheWindow()
    {
        using var fixture = OpenWindow(Build(0xC00010));

        var args = new FormClosingEventArgs(CloseReason.ApplicationExitCall, cancel: false);
        Raise(fixture.Window!, "OnFormClosing", args);

        args.Cancel.Should().BeFalse("only the user clicking the X is turned into a hide");
    }

    [Fact]
    public void TheWindowJustForwardsTheHostsSettingsToTheControl()
    {
        using var fixture = OpenWindow(Build(0xC00010));

        var hosted = fixture.Window!.Controls.Find("navigationCtrl", searchAllChildren: true)
            .OfType<NavigationHistoryViewControl>().Single();

        fixture.Window.ViewModel.Should().BeSameAs(fixture.ViewModel);
        hosted.ViewModel.Should().BeSameAs(fixture.ViewModel, "the window is a pass-through");
        hosted.BackForwardOvershoot.Should().Be(StandardOvershoot);
    }

    [Fact]
    public void DisposingTheWindowLetsGoOfTheViewModelWithoutDisposingIt()
    {
        // the ViewModel belongs to the main window and outlives this one; disposing the history
        // window must not stop the history being watched.
        using var fixture = OpenWindow(Build(0xC00010, 0xC00020));

        fixture.Window!.Dispose();
        fixture.Window = null;

        fixture.Record(0xC00030);
        fixture.ViewModel.CurrentIndex.Should().Be(2, "the ViewModel is still watching the history");
        fixture.Requests.Should().BeEmpty();
    }
}
