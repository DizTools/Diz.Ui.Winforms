using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Diz.Controllers.interfaces;
using Diz.Core.Interfaces;
using Diz.Core.model;
using Diz.Core.model.snes;
using Diz.Cpu._65816;
using Diz.Ui.ViewModels.Regions;
using Diz.Ui.Winforms.usercontrols;
using FluentAssertions;
using Moq;
using Xunit;
using SnesRegion = Diz.Core.model.snes.Region;

namespace Diz.Ui.Winforms.Test.Tests;

/// <summary>
/// RegionListViewControl is a thin host over RegionListViewModel: the grid renders the
/// ViewModel's already-sorted rows, and every edit, add, delete and re-sort goes back through a
/// ViewModel command. These tests construct the real control over a real project (never shown --
/// no window handle is needed to read control state) and check that traffic flows both ways.
///
/// The events a user raises that a hidden control does not raise on its own -- a cell being
/// validated, a header being clicked, a cell being formatted -- are invoked through their
/// protected raisers, so the wiring the control sets up is still what gets exercised.
/// </summary>
public class RegionListViewBindingTests
{
    private const BindingFlags AnyInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // ------------------------------------------------------------------ fixture

    private sealed class Fixture : IDisposable
    {
        public required RegionListViewControl Control { get; init; }
        public required Data Data { get; init; }
        public required List<bool> DeleteAnswers { get; init; }
        public int MarkChangedCount;
        public int DeleteConfirmationCount;

        public DataGridView Grid => Widget<DataGridView>(Control, "regionGridView");
        public ListBox Problems => Widget<ListBox>(Control, "problemsList");
        public Button ProblemsToggle => Widget<Button>(Control, "problemsToggle");
        public Button AddButton => Widget<Button>(Control, "addRegionButton");

        public string StatusText =>
            Widget<StatusStrip>(Control, "statusStrip").Items.OfType<ToolStripStatusLabel>().Single().Text ?? "";

        public void Dispose() => Control.Dispose();
    }

    private static Data MakeData()
    {
        var romBytes = new RomBytes();
        for (var i = 0; i < 0x100; ++i)
            romBytes.Add(new RomByte { Rom = (byte)i });

        var data = new Data
        {
            RomMapMode = RomMapMode.HiRom,
            RomSpeed = RomSpeed.FastRom,
            RomBytes = romBytes,
        };
        data.Apis.AddIfDoesntExist(new SnesApi(data));
        return data;
    }

    private static SnesRegion NewRegion(
        string name, int start, int end,
        RegionExportType exportType = RegionExportType.Assembly,
        bool separateFile = false,
        string assetType = "",
        string assetName = "",
        string assetOptions = "") =>
        new()
        {
            RegionName = name,
            StartSnesAddress = start,
            EndSnesAddress = end,
            ExportType = exportType,
            ExportSeparateFile = separateFile,
            AssetType = assetType,
            AssetVersion = "",
            AssetName = assetName,
            AssetOptions = assetOptions,
            ContextToApply = "",
        };

    private static Fixture MakeControl(params SnesRegion[] regions)
    {
        var data = MakeData();
        foreach (var region in regions)
            data.Regions.Add(region);

        var project = new Project { Data = data };
        var controller = new Mock<IProjectController>();
        controller.SetupGet(c => c.Project).Returns(project);

        var fixture = new Fixture
        {
            Control = new RegionListViewControl(),
            Data = data,
            DeleteAnswers = [],
        };

        controller.Setup(c => c.MarkChanged()).Callback(() => fixture.MarkChangedCount++);

        // the delete confirmation is a MessageBox in the shipping control; here it answers from a
        // queue so the wiring around it can be driven without a window on screen.
        fixture.Control.ConfirmDelete = _ =>
        {
            fixture.DeleteConfirmationCount++;
            if (fixture.DeleteAnswers.Count == 0)
                return true;
            var answer = fixture.DeleteAnswers[0];
            fixture.DeleteAnswers.RemoveAt(0);
            return answer;
        };

        fixture.Control.SetProjectController(controller.Object);
        return fixture;
    }

    // ------------------------------------------------------------------ widget + event helpers

    private static T Widget<T>(Control parent, string name) where T : Control =>
        parent.Controls.Find(name, searchAllChildren: true).OfType<T>().Single();

    private static void Raise(object target, string method, EventArgs args) =>
        target.GetType()
            .GetMethod(method, AnyInstance, null, [args.GetType()], null)!
            .Invoke(target, [args]);

    /// <summary>
    /// Hand the grid the text a user typed into a cell, exactly as the grid does when the cell
    /// loses focus. Returns the event args so the caller can check whether the control tried to
    /// hold the user in the cell.
    /// </summary>
    private static DataGridViewCellValidatingEventArgs Type(
        Fixture fixture, int rowIndex, string columnName, object? typedValue)
    {
        var columnIndex = fixture.Grid.Columns[columnName]!.Index;
        var args = (DataGridViewCellValidatingEventArgs)typeof(DataGridViewCellValidatingEventArgs)
            .GetConstructors(AnyInstance)
            .Single(c => c.GetParameters().Length == 3)
            .Invoke([columnIndex, rowIndex, typedValue]);

        Raise(fixture.Grid, "OnCellValidating", args);
        return args;
    }

    private static void ClickHeader(Fixture fixture, string columnName)
    {
        var columnIndex = fixture.Grid.Columns[columnName]!.Index;
        Raise(fixture.Grid, "OnColumnHeaderMouseClick", new DataGridViewCellMouseEventArgs(
            columnIndex, -1, 1, 1, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)));
    }

    private static void ClickCellContent(Fixture fixture, int rowIndex, string columnName)
    {
        var columnIndex = fixture.Grid.Columns[columnName]!.Index;
        var args = (DataGridViewCellEventArgs)typeof(DataGridViewCellEventArgs)
            .GetConstructors(AnyInstance)
            .Single(c => c.GetParameters().Length == 2)
            .Invoke([columnIndex, rowIndex]);

        Raise(fixture.Grid, "OnCellContentClick", args);
    }

    /// <summary>Run the control's cell-formatting pass for one cell and hand back what it decided
    /// -- the styling is where the asset-column greying and the bad-row tint live.</summary>
    private static DataGridViewCellFormattingEventArgs Format(Fixture fixture, int rowIndex, string columnName)
    {
        var columnIndex = fixture.Grid.Columns[columnName]!.Index;
        var args = new DataGridViewCellFormattingEventArgs(
            columnIndex, rowIndex,
            fixture.Grid.Rows[rowIndex].Cells[columnIndex].Value,
            typeof(string),
            new DataGridViewCellStyle());

        Raise(fixture.Grid, "OnCellFormatting", args);
        return args;
    }

    /// <summary>
    /// Give the control a real window handle, so work the control posts goes through the message
    /// loop instead of running inline. Nothing is ever shown: the window is realized and left
    /// invisible.
    /// </summary>
    private static Form RealizeHandle(Fixture fixture)
    {
        var form = new Form();
        form.Controls.Add(fixture.Control);

        // CreateControl() is a no-op while the hierarchy is invisible, and nothing here is ever
        // shown -- reading Handle creates the window unconditionally, which is all the deferred
        // path needs.
        _ = form.Handle;
        _ = fixture.Control.Handle;
        _ = fixture.Grid.Handle;

        fixture.Control.IsHandleCreated.Should().BeTrue("the deferred path only exists once there is a handle");
        return form;
    }

    private static void PumpMessageLoop()
    {
        for (var i = 0; i < 5; i++)
            Application.DoEvents();
    }

    /// <summary>
    /// Select exactly these rows. The grid always has one row selected -- binding a data source
    /// puts the caret on row 0 -- so a test that only adds to the selection would be testing a
    /// bigger selection than it meant to.
    /// </summary>
    private static void SelectRows(Fixture fixture, params int[] rowIndexes)
    {
        fixture.Grid.ClearSelection();
        foreach (var index in rowIndexes)
            fixture.Grid.Rows[index].Selected = true;

        fixture.Grid.SelectedRows.Count.Should().Be(rowIndexes.Length);
    }

    private static void PressDelete(Fixture fixture) =>
        Raise(fixture.Grid, "OnKeyDown", new KeyEventArgs(Keys.Delete));

    private static string CellText(Fixture fixture, int rowIndex, string columnName) =>
        fixture.Grid.Rows[rowIndex].Cells[columnName].Value?.ToString() ?? "";

    private static IRegion RegionNamed(Fixture fixture, string name) =>
        fixture.Data.Regions.Single(r => r.RegionName == name);

    // ------------------------------------------------------------------ columns

    [Fact]
    public void TheGridKeepsAllThirteenColumnsInOrderWithTheirHeaders()
    {
        // conversion, not redesign: the window still shows every field of a region in the order
        // it always did, including the Delete button column at the end.
        using var fixture = MakeControl();

        fixture.Grid.Columns.Cast<DataGridViewColumn>().Select(c => c.Name).Should().Equal(
            "StartSnesAddress", "EndSnesAddress", "Length", "RegionName", "ContextToApply",
            "Priority", "ExportSeparateFile", "ExportType", "AssetType", "AssetVersion",
            "AssetName", "AssetOptions", "Actions");

        fixture.Grid.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText).Should().Equal(
            "Start SNES Address [hex]", "End SNES Address [hex]", "Length [hex]", "Region Name",
            "Label Context To Apply", "Priority", "Export Separate File", "Export Type",
            "Asset Type", "Asset Version", "Asset Name", "Asset Options (JSON)", "Actions");
    }

    [Fact]
    public void NoColumnSortsItselfBehindTheViewModelsBack()
    {
        // the ViewModel owns the order, so the grid's own sorting must be off everywhere --
        // otherwise the rows and the ViewModel would disagree about what row 0 is.
        using var fixture = MakeControl();

        fixture.Grid.Columns.Cast<DataGridViewColumn>()
            .Should().OnlyContain(c => c.SortMode == DataGridViewColumnSortMode.Programmatic);
    }

    [Fact]
    public void TheGridDoesNotOfferItsOwnRowAddOrRowDelete()
    {
        // both go through ViewModel commands: the phantom "new row" appends, and rows here are in
        // sort order, so there is nowhere correct for it to land; deleting must ask first.
        using var fixture = MakeControl();

        fixture.Grid.AllowUserToAddRows.Should().BeFalse();
        fixture.Grid.AllowUserToDeleteRows.Should().BeFalse();
    }

    // ------------------------------------------------------------------ population

    [Fact]
    public void SettingTheProjectFillsTheGridFromTheProjectsRegions()
    {
        using var fixture = MakeControl(
            NewRegion("second", 0xC00100, 0xC001FF),
            NewRegion("first", 0xC00000, 0xC000FF));

        fixture.Grid.Rows.Count.Should().Be(2);
        CellText(fixture, 0, "RegionName").Should().Be("first");
        CellText(fixture, 0, "StartSnesAddress").Should().Be("C00000");
        CellText(fixture, 0, "EndSnesAddress").Should().Be("C000FF");
        CellText(fixture, 0, "Length").Should().Be("100");
    }

    [Fact]
    public void RowsStartOutSortedByStartAddressAscending()
    {
        // NOTE: a behavior change. The window used to show regions in whatever order they were
        // stored in; it now opens sorted by start address, with the glyph saying so.
        using var fixture = MakeControl(
            NewRegion("c", 0xC00200, 0xC0020F),
            NewRegion("a", 0xC00000, 0xC0000F),
            NewRegion("b", 0xC00100, 0xC0010F));

        RowNames(fixture).Should().Equal("a", "b", "c");
        fixture.Grid.Columns["StartSnesAddress"]!.HeaderCell.SortGlyphDirection
            .Should().Be(SortOrder.Ascending);
    }

    [Fact]
    public void ARegionAddedToTheProjectFromSomewhereElseShowsUpWithoutARebind()
    {
        // the bind used to be an ObservableCollection straight into a BindingSource, which raises
        // no ListChanged at all: bank-region synthesis at import and the save-format migration
        // that adds bank regions were invisible in an already-open window.
        using var fixture = MakeControl(NewRegion("existing", 0xC00000, 0xC0000F));

        fixture.Data.Regions.Add(NewRegion("arrived later", 0xC00100, 0xC0010F));

        fixture.Grid.Rows.Count.Should().Be(2);
        RowNames(fixture).Should().Equal("existing", "arrived later");
    }

    [Fact]
    public void ARegionRemovedFromTheProjectFromSomewhereElseLeavesTheGrid()
    {
        using var fixture = MakeControl(
            NewRegion("kept", 0xC00000, 0xC0000F),
            NewRegion("removed elsewhere", 0xC00100, 0xC0010F));

        fixture.Data.Regions.Remove(RegionNamed(fixture, "removed elsewhere"));

        RowNames(fixture).Should().Equal("kept");
    }

    private static List<string> RowNames(Fixture fixture) =>
        Enumerable.Range(0, fixture.Grid.Rows.Count)
            .Select(i => CellText(fixture, i, "RegionName"))
            .ToList();

    // ------------------------------------------------------------------ editing

    [Fact]
    public void TypingAHexStartAddressReachesTheRegion()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));

        Type(fixture, 0, "StartSnesAddress", "C00010");

        RegionNamed(fixture, "r").StartSnesAddress.Should().Be(0xC00010);
    }

    [Fact]
    public void TypingALengthMovesTheEndAddressAndLeavesTheStartAlone()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));

        Type(fixture, 0, "Length", "10");

        var region = RegionNamed(fixture, "r");
        region.StartSnesAddress.Should().Be(0xC00000);
        // the end address is INCLUSIVE, so 0x10 bytes ends at start + 0xF
        region.EndSnesAddress.Should().Be(0xC0000F);
        CellText(fixture, 0, "Length").Should().Be("10");
    }

    [Fact]
    public void ALengthOfOneIsLegalAndMakesTheEndAddressEqualTheStart()
    {
        // the old grid contradicted itself here: its Length column said "a length of 1 means
        // end == start", and its row validation then refused to let the user leave that row.
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));

        var args = Type(fixture, 0, "Length", "1");

        var region = RegionNamed(fixture, "r");
        region.EndSnesAddress.Should().Be(region.StartSnesAddress);
        args.Cancel.Should().BeFalse();
        fixture.StatusText.Should().BeEmpty();
    }

    [Fact]
    public void MovingAnAddressRecomputesTheLengthColumn()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));

        Type(fixture, 0, "EndSnesAddress", "C0000F");

        CellText(fixture, 0, "Length").Should().Be("10");
    }

    [Fact]
    public void TypingIntoACellWithoutChangingItCommitsNothing()
    {
        // CellValidating also fires on plain navigation; re-committing unchanged values would
        // wipe the status line every time the user moved through the grid.
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));

        Type(fixture, 0, "StartSnesAddress", "C00000");

        fixture.MarkChangedCount.Should().Be(0);
    }

    [Fact]
    public void FlippingTheExportTypeToAssetUngreysTheAssetCells()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));

        // an assembly region has nothing to say about assets
        var greyed = Format(fixture, 0, "AssetType");
        greyed.CellStyle.ForeColor.Should().Be(SystemColors.GrayText);
        fixture.Grid.Rows[0].Cells["AssetType"].ReadOnly.Should().BeTrue();

        Type(fixture, 0, "ExportType", "Binary");

        var live = Format(fixture, 0, "AssetType");
        live.CellStyle.ForeColor.Should().NotBe(SystemColors.GrayText);
        fixture.Grid.Rows[0].Cells["AssetType"].ReadOnly.Should().BeFalse();
    }

    [Fact]
    public void GoingBackToAssemblyGreysTheAssetCellsAgainWithoutClearingThem()
    {
        using var fixture = MakeControl(
            NewRegion("r", 0xC00000, 0xC00047, RegionExportType.Binary, assetType: "audio.snes.brr"));

        Type(fixture, 0, "ExportType", "Assembly");

        Format(fixture, 0, "AssetType").CellStyle.ForeColor.Should().Be(SystemColors.GrayText);
        // greyed out, not thrown away: flipping back restores what was typed
        RegionNamed(fixture, "r").AssetType.Should().Be("audio.snes.brr");
    }

    // ------------------------------------------------------------------ non-blocking validation

    [Fact]
    public void AnInvalidEditIsNeverWrittenToTheRegion()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));

        Type(fixture, 0, "RegionName", "   ");

        RegionNamed(fixture, "r").RegionName.Should().Be("r");
    }

    [Fact]
    public void AnInvalidEditNeverHoldsTheUserInTheCell()
    {
        // the old grid cancelled row validation, which trapped focus: a user could not look at
        // another row, or leave the window, until the row was valid or reverted.
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));

        var args = Type(fixture, 0, "RegionName", "");

        args.Cancel.Should().BeFalse();
    }

    [Fact]
    public void AnInvalidEditFlagsItsRowAndSaysWhy()
    {
        using var fixture = MakeControl(
            NewRegion("bad", 0xC00000, 0xC000FF),
            NewRegion("fine", 0xC00100, 0xC001FF));

        Type(fixture, 0, "EndSnesAddress", "BF0000");

        // the row-header marker the grid shows, and the tooltip behind it
        fixture.Grid.Rows[0].ErrorText.Should().Be("Start address must not be greater than end address.");
        fixture.Grid.Rows[1].ErrorText.Should().BeEmpty();

        // ... plus the offending row tinted, so a bad row is visible without hovering
        var badCell = Format(fixture, 0, "RegionName").CellStyle.BackColor;
        var goodCell = Format(fixture, 1, "RegionName").CellStyle.BackColor;
        badCell.Should().NotBe(goodCell);
        badCell.R.Should().BeGreaterThan(badCell.G); // a red tint, not just "some colour"

        // ... plus a persistent message on the status bar (no 5-second self-clearing banner)
        fixture.StatusText.Should().Be("Start address must not be greater than end address.");
    }

    [Fact]
    public void ARefusedEditKeepsTheTextTheUserTypedOnScreen()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));

        Type(fixture, 0, "EndSnesAddress", "BF0000");

        CellText(fixture, 0, "EndSnesAddress").Should().Be("BF0000");
        RegionNamed(fixture, "r").EndSnesAddress.Should().Be(0xC000FF);
    }

    [Fact]
    public void CorrectingARefusedEditClearsTheMarkerAndTheMessage()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));
        Type(fixture, 0, "EndSnesAddress", "BF0000");

        Type(fixture, 0, "EndSnesAddress", "C0001F");

        fixture.Grid.Rows[0].ErrorText.Should().BeEmpty();
        fixture.StatusText.Should().BeEmpty();
        RegionNamed(fixture, "r").EndSnesAddress.Should().Be(0xC0001F);
    }

    [Fact]
    public void AnAssetRegionWhoseLengthDoesNotFitItsCodecIsRefused()
    {
        // the same rule the build applies: a BRR stream is 9-byte blocks, so a region covering a
        // partial block would export garbage.
        using var fixture = MakeControl(
            NewRegion("r", 0xC00000, 0xC00047, RegionExportType.Asset, assetType: "audio.snes.brr"));

        Type(fixture, 0, "EndSnesAddress", "C00048");

        RegionNamed(fixture, "r").EndSnesAddress.Should().Be(0xC00047);
        fixture.StatusText.Should().Contain("whole multiple of 9 bytes");
    }

    // --------------------------------------- closed-value columns (checkbox / combo) snap back

    [Fact]
    public void TickingTheCheckboxOnARegionThatSpansTwoBanksCommits()
    {
        // The checkbox column's commit path, on the shape that used to be refused: a region that
        // emits its own .asm file and straddles a bank boundary. Nothing about banks constrains a
        // file-producing region -- the emitted assembly re-origins at the seam -- so the tick has
        // to reach the model and leave the row clean.
        using var fixture = MakeControl(NewRegion("crosses banks", 0xC00000, 0xC1000F));

        Type(fixture, 0, "ExportSeparateFile", true);

        RegionNamed(fixture, "crosses banks").ExportSeparateFile.Should().BeTrue();
        fixture.Grid.Rows[0].Cells["ExportSeparateFile"].Value.Should().Be(true);
        fixture.Grid.Rows[0].ErrorText.Should().BeEmpty();
        fixture.StatusText.Should().BeEmpty();
    }

    [Fact]
    public void ARefusedComboValueLeavesNoStaleMarkerOnTheRow()
    {
        // nothing the model refused is on screen afterwards -- the combo is showing the stored
        // value again -- so marking the row would be marking it over nothing.
        // 0x47 bytes is not a whole number of 9-byte BRR blocks.
        using var fixture = MakeControl(
            NewRegion("art", 0xC00000, 0xC00046, assetType: "audio.snes.brr"));

        Type(fixture, 0, "ExportType", RegionExportType.Asset);

        fixture.Grid.Rows[0].ErrorText.Should().BeEmpty();
        fixture.Grid.Rows[0].Cells["ExportType"].Value.Should().Be(RegionExportType.Assembly);
        // the reason is still told, on the status line
        fixture.StatusText.Should().Contain("whole multiple of 9 bytes");
    }

    [Fact]
    public void RetryingARefusedExportTypeAfterFixingTheRowActuallyCommits()
    {
        // the combo half of the same story: 0x10 bytes is not a whole number of 4bpp tiles.
        using var fixture = MakeControl(
            NewRegion("art", 0xC00000, 0xC0000F, assetType: "gfx.snes.4bpp"));

        Type(fixture, 0, "ExportType", RegionExportType.Asset);
        RegionNamed(fixture, "art").ExportType.Should().Be(RegionExportType.Assembly);

        Type(fixture, 0, "EndSnesAddress", "C0001F"); // 0x20 bytes = exactly one 4bpp tile
        Type(fixture, 0, "ExportType", RegionExportType.Asset);

        RegionNamed(fixture, "art").ExportType.Should().Be(RegionExportType.Asset);
        fixture.Grid.Rows[0].ErrorText.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ the deferred commit

    [Fact]
    public void ACommitPostedThroughTheMessageLoopLandsAndMovesTheRow()
    {
        // Commits are deferred on purpose: committing the column being sorted on repositions the
        // row, which mutates the bound list, and doing that inside a grid edit event re-enters
        // the grid's own current-cell bookkeeping. Every other test here runs without a window
        // handle, which takes the inline path -- this one exercises the real one.
        using var fixture = MakeControl(
            NewRegion("a", 0xC00000, 0xC00FFF), // roomy, so moving its start stays a legal range
            NewRegion("b", 0xC00100, 0xC0010F),
            NewRegion("c", 0xC00200, 0xC0020F));
        using var form = RealizeHandle(fixture);

        Type(fixture, 0, "StartSnesAddress", "C00500");

        // still queued: nothing has been written and nothing has moved yet
        RegionNamed(fixture, "a").StartSnesAddress.Should().Be(0xC00000);
        RowNames(fixture).Should().Equal("a", "b", "c");

        PumpMessageLoop();

        RegionNamed(fixture, "a").StartSnesAddress.Should().Be(0xC00500);
        RowNames(fixture).Should().Equal("b", "c", "a");
    }

    [Fact]
    public void ACommitStillQueuedWhenTheProjectIsReboundIsDroppedRatherThanThrowing()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));
        using var form = RealizeHandle(fixture);

        Type(fixture, 0, "RegionName", "renamed"); // queued

        var replacement = MakeData();
        replacement.Regions.Add(NewRegion("other", 0xC00200, 0xC0020F));
        var controller = new Mock<IProjectController>();
        controller.SetupGet(c => c.Project).Returns(new Project { Data = replacement });
        fixture.Control.SetProjectController(controller.Object);

        var pump = () => PumpMessageLoop();

        // the edit is aimed at a row that belongs to a ViewModel nobody is showing any more
        pump.Should().NotThrow();
        RowNames(fixture).Should().Equal("other");
    }

    [Fact]
    public void ACommitStillQueuedWhenItsRegionIsDeletedIsDroppedRatherThanThrowing()
    {
        using var fixture = MakeControl(
            NewRegion("doomed", 0xC00000, 0xC000FF),
            NewRegion("survivor", 0xC00100, 0xC001FF));
        using var form = RealizeHandle(fixture);

        Type(fixture, 0, "RegionName", "renamed"); // queued
        fixture.Data.Regions.Remove(RegionNamed(fixture, "doomed"));

        var pump = () => PumpMessageLoop();

        pump.Should().NotThrow();
        fixture.Data.Regions.Select(r => r.RegionName).Should().Equal("survivor");
    }

    // ------------------------------------------------------------------ adding

    [Fact]
    public void TheAddButtonCreatesOneNamedRegionAndSelectsIt()
    {
        using var fixture = MakeControl(NewRegion("existing", 0xC00100, 0xC0010F));

        Raise(fixture.AddButton, "OnClick", EventArgs.Empty);

        fixture.Data.Regions.Should().HaveCount(2);
        // named and already a legal one-byte range, so the new row is never one the user cannot
        // leave; it sorts to the top because its start address is 0.
        RowNames(fixture).Should().Equal(RegionListViewModel.DefaultRegionName, "existing");
        CellText(fixture, 0, "Length").Should().Be("1");

        // and the caret is put on it, so the user can start typing over the placeholder name
        // instead of hunting for wherever the sort order dropped it
        fixture.Grid.CurrentCell!.RowIndex.Should().Be(0);
        fixture.Grid.Rows[0].Selected.Should().BeTrue();
    }

    // ------------------------------------------------------------------ deleting

    [Fact]
    public void DeletingWhileSortedDescendingRemovesTheRegionOnThatRow()
    {
        // THE point of deleting by row rather than by index: with the grid sorted the other way
        // round, row 0 is the LAST region in storage order, and an index-based delete would take
        // the first one.
        using var fixture = MakeControl(
            NewRegion("lowest", 0xC00000, 0xC0000F),
            NewRegion("middle", 0xC00100, 0xC0010F),
            NewRegion("highest", 0xC00200, 0xC0020F));

        ClickHeader(fixture, "StartSnesAddress"); // already ascending -> flips to descending
        RowNames(fixture).Should().Equal("highest", "middle", "lowest");

        ClickCellContent(fixture, 0, "Actions");

        fixture.DeleteConfirmationCount.Should().Be(1);
        fixture.Data.Regions.Select(r => r.RegionName).Should().Equal("lowest", "middle");
    }

    [Fact]
    public void SayingNoToTheConfirmationKeepsTheRegion()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC0000F));
        fixture.DeleteAnswers.Add(false);

        ClickCellContent(fixture, 0, "Actions");

        fixture.DeleteConfirmationCount.Should().Be(1);
        fixture.Data.Regions.Should().HaveCount(1);
    }

    [Fact]
    public void DeletingASelectionOfRowsWhileSortedRemovesExactlyThoseRegions()
    {
        // the grid selects whole rows and allows more than one. Acting on the current row alone
        // would make a user who highlighted three rows and pressed Delete watch one of them
        // disappear -- and then have to work out which.
        using var fixture = MakeControl(
            NewRegion("lowest", 0xC00000, 0xC0000F),
            NewRegion("middle", 0xC00100, 0xC0010F),
            NewRegion("highest", 0xC00200, 0xC0020F));

        ClickHeader(fixture, "StartSnesAddress"); // ascending -> descending
        RowNames(fixture).Should().Equal("highest", "middle", "lowest");

        SelectRows(fixture, 0, 1);

        PressDelete(fixture);

        fixture.DeleteConfirmationCount.Should().Be(1, "one question, however many regions go");
        fixture.Data.Regions.Select(r => r.RegionName).Should().Equal("lowest");
    }

    [Fact]
    public void DeletingSeveralRowsSaysHowManyBeforeDoingIt()
    {
        using var fixture = MakeControl(
            NewRegion("a", 0xC00000, 0xC0000F),
            NewRegion("b", 0xC00100, 0xC0010F));
        var asked = new List<string>();
        fixture.Control.ConfirmDelete = message => { asked.Add(message); return true; };

        SelectRows(fixture, 0, 1);
        PressDelete(fixture);

        asked.Should().ContainSingle().Which.Should().Be("Are you sure you want to delete 2 regions?");
        fixture.Data.Regions.Should().BeEmpty();
    }

    [Fact]
    public void DeletingASingleRowStillAsksTheQuestionItAlwaysAsked()
    {
        using var fixture = MakeControl(
            NewRegion("a", 0xC00000, 0xC0000F),
            NewRegion("b", 0xC00100, 0xC0010F));
        var asked = new List<string>();
        fixture.Control.ConfirmDelete = message => { asked.Add(message); return true; };

        SelectRows(fixture, 1);
        PressDelete(fixture);

        asked.Should().ContainSingle().Which.Should().Be("Are you sure you want to delete this region?");
        fixture.Data.Regions.Select(r => r.RegionName).Should().Equal("a");
    }

    [Fact]
    public void SayingNoToAMultiRowDeleteKeepsEveryRegion()
    {
        using var fixture = MakeControl(
            NewRegion("a", 0xC00000, 0xC0000F),
            NewRegion("b", 0xC00100, 0xC0010F));
        fixture.DeleteAnswers.Add(false);

        SelectRows(fixture, 0, 1);
        PressDelete(fixture);

        fixture.DeleteConfirmationCount.Should().Be(1);
        fixture.Data.Regions.Should().HaveCount(2);
    }

    [Fact]
    public void ClickingSomeOtherColumnDoesNotDeleteAnything()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC0000F));

        ClickCellContent(fixture, 0, "RegionName");

        fixture.DeleteConfirmationCount.Should().Be(0);
        fixture.Data.Regions.Should().HaveCount(1);
    }

    // ------------------------------------------------------------------ sorting

    [Fact]
    public void ClickingAColumnHeaderReordersTheRows()
    {
        using var fixture = MakeControl(
            NewRegion("beta", 0xC00000, 0xC0000F),
            NewRegion("alpha", 0xC00100, 0xC0010F));

        ClickHeader(fixture, "RegionName");

        RowNames(fixture).Should().Equal("alpha", "beta");
        fixture.Grid.Columns["RegionName"]!.HeaderCell.SortGlyphDirection.Should().Be(SortOrder.Ascending);
        fixture.Grid.Columns["StartSnesAddress"]!.HeaderCell.SortGlyphDirection.Should().Be(SortOrder.None);
    }

    [Fact]
    public void ClickingTheSameHeaderAgainReversesTheOrder()
    {
        using var fixture = MakeControl(
            NewRegion("beta", 0xC00000, 0xC0000F),
            NewRegion("alpha", 0xC00100, 0xC0010F));

        ClickHeader(fixture, "RegionName");
        ClickHeader(fixture, "RegionName");

        RowNames(fixture).Should().Equal("beta", "alpha");
        fixture.Grid.Columns["RegionName"]!.HeaderCell.SortGlyphDirection.Should().Be(SortOrder.Descending);
    }

    [Fact]
    public void SortingIsDisplayOnlyAndLeavesTheStoredOrderAlone()
    {
        // stored order is what gets serialized and exported; a display sort must not touch it.
        using var fixture = MakeControl(
            NewRegion("second", 0xC00100, 0xC0010F),
            NewRegion("first", 0xC00000, 0xC0000F));

        ClickHeader(fixture, "StartSnesAddress");

        fixture.Data.Regions.Select(r => r.RegionName).Should().Equal("second", "first");
    }

    [Fact]
    public void ClickingTheActionsHeaderSortsNothing()
    {
        using var fixture = MakeControl(
            NewRegion("beta", 0xC00000, 0xC0000F),
            NewRegion("alpha", 0xC00100, 0xC0010F));

        ClickHeader(fixture, "Actions");

        RowNames(fixture).Should().Equal("beta", "alpha");
        fixture.Grid.Columns["StartSnesAddress"]!.HeaderCell.SortGlyphDirection.Should().Be(SortOrder.Ascending);
    }

    // ------------------------------------------------------------------ unsaved-changes flag

    [Fact]
    public void ACommittedEditMarksTheProjectAsHavingUnsavedChanges()
    {
        // region edits used to leave no trace: no asterisk, no prompt on exit, work lost.
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));

        Type(fixture, 0, "RegionName", "renamed");

        fixture.MarkChangedCount.Should().Be(1);
    }

    [Fact]
    public void ARefusedEditDoesNotMarkTheProjectChanged()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC000FF));

        Type(fixture, 0, "RegionName", "");

        fixture.MarkChangedCount.Should().Be(0);
    }

    [Fact]
    public void ReSortingDoesNotMarkTheProjectChanged()
    {
        using var fixture = MakeControl(
            NewRegion("beta", 0xC00000, 0xC0000F),
            NewRegion("alpha", 0xC00100, 0xC0010F));

        ClickHeader(fixture, "RegionName");
        ClickHeader(fixture, "RegionName");

        fixture.MarkChangedCount.Should().Be(0);
    }

    [Fact]
    public void AddingAndDeletingBothMarkTheProjectChanged()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00100, 0xC0010F));

        Raise(fixture.AddButton, "OnClick", EventArgs.Empty);
        fixture.MarkChangedCount.Should().Be(1);

        ClickCellContent(fixture, 0, "Actions");
        fixture.MarkChangedCount.Should().Be(2);
    }

    // ------------------------------------------------------------------ problem panel

    [Fact]
    public void TheProblemPanelListsProblemsThatOnlyExistBetweenRegions()
    {
        // two asset regions covering the same bytes: nothing wrong with either row on its own,
        // and completely invisible in the old window.
        using var fixture = MakeControl(
            NewRegion("art a", 0xC00000, 0xC0000F, RegionExportType.Binary, assetType: "gfx.snes.2bpp"),
            NewRegion("art b", 0xC00008, 0xC00017, RegionExportType.Binary, assetType: "gfx.snes.2bpp"));

        fixture.Problems.Items.Count.Should().Be(1);
        fixture.Problems.Items[0]!.ToString().Should().StartWith("Error: ")
            .And.Contain("overlap");
        fixture.ProblemsToggle.Text.Should().EndWith("Problems (1)");
    }

    [Fact]
    public void DuplicateRegionNamesAreAWarningRatherThanARefusal()
    {
        // existing projects may already contain duplicates and have to keep loading.
        using var fixture = MakeControl(
            NewRegion("same name", 0xC00000, 0xC0000F),
            NewRegion("same name", 0xC00100, 0xC0010F));

        fixture.Problems.Items.Count.Should().Be(1);
        fixture.Problems.Items[0]!.ToString().Should().StartWith("Warning: ");
        fixture.Data.Regions.Should().HaveCount(2);
    }

    [Fact]
    public void FixingTheProblemEmptiesThePanel()
    {
        using var fixture = MakeControl(
            NewRegion("art a", 0xC00000, 0xC0000F, RegionExportType.Binary, assetType: "gfx.snes.2bpp"),
            NewRegion("art b", 0xC00008, 0xC00017, RegionExportType.Binary, assetType: "gfx.snes.2bpp"));

        Type(fixture, 1, "StartSnesAddress", "C00010");

        fixture.Problems.Items.Count.Should().Be(0);
        fixture.ProblemsToggle.Text.Should().EndWith("Problems (0)");
    }

    [Fact]
    public void TheProblemPanelCollapsesAndReopens()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC0000F));
        var expandedHeight = Widget<Panel>(fixture.Control, "problemsPanel").Height;

        Raise(fixture.ProblemsToggle, "OnClick", EventArgs.Empty);

        fixture.Problems.Visible.Should().BeFalse();
        Widget<Panel>(fixture.Control, "problemsPanel").Height.Should().BeLessThan(expandedHeight);

        Raise(fixture.ProblemsToggle, "OnClick", EventArgs.Empty);

        fixture.Problems.Visible.Should().BeTrue();
        Widget<Panel>(fixture.Control, "problemsPanel").Height.Should().Be(expandedHeight);
    }

    // ------------------------------------------------------------------ rebinding

    [Fact]
    public void RebindingAfterTheProjectSwapsItsDataShowsTheNewRegions()
    {
        using var fixture = MakeControl(NewRegion("old", 0xC00000, 0xC0000F));

        var replacement = MakeData();
        replacement.Regions.Add(NewRegion("new", 0xC00200, 0xC0020F));
        var project = new Project { Data = replacement };
        var controller = new Mock<IProjectController>();
        controller.SetupGet(c => c.Project).Returns(project);

        fixture.Control.SetProjectController(controller.Object);

        RowNames(fixture).Should().Equal("new");
    }

    [Fact]
    public void RebindingTheSameProjectKeepsTheSortOrderTheUserChose()
    {
        // every project change rebinds -- including a plain Save. Rebuilding on a Save would
        // silently throw away the sort order and the selection while the user was working.
        using var fixture = MakeControl(
            NewRegion("beta", 0xC00000, 0xC0000F),
            NewRegion("alpha", 0xC00100, 0xC0010F));

        ClickHeader(fixture, "RegionName");
        fixture.Control.RebindProject();

        RowNames(fixture).Should().Equal("alpha", "beta");
        fixture.Grid.Columns["RegionName"]!.HeaderCell.SortGlyphDirection.Should().Be(SortOrder.Ascending);
    }

    [Fact]
    public void ClearingTheProjectEmptiesTheGridRatherThanThrowing()
    {
        using var fixture = MakeControl(NewRegion("r", 0xC00000, 0xC0000F));

        fixture.Control.SetProjectController(null);

        fixture.Grid.Rows.Count.Should().Be(0);
        fixture.Problems.Items.Count.Should().Be(0);
    }
}
