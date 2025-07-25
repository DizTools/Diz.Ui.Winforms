﻿using System.Globalization;
using Diz.Controllers.controllers;
using Diz.Core.Interfaces;
using Diz.Core.util;
using Diz.Cpu._65816;

namespace Diz.Ui.Winforms.window;

// Everything in here should probably go in its own usercontrol for JUST the table.
// It's a complicated little beast.
public partial class MainWindow
{
    // Get the PC offset IN THE ROM for the selected row in the GRID
    public int SelectedOffset => table.CurrentCell.RowIndex + ViewOffset;

    private int rowsToShow;
    private bool moveWithStep = true;

    public void InvalidateTable() => table.Invalidate();

    private void ScrollTableBy(int delta)
    {
        if (Project?.Data == null || Project.Data.GetRomSize() <= 0)
            return;

        ScrollToTargetRomOffset(ViewOffset - delta);
    }

    private void vScrollBar1_ValueChanged(object sender, EventArgs e)
    {
        if (table.CurrentCell == null)
            return;

        ScrollToTargetRomOffset(vScrollBar1.Value);
    }

    private void ScrollToTargetRomOffset(int targetOffset)
    {
        ViewOffset = targetOffset;
        
        UpdateDataGridView();
        var targetRomOffset = SelectedOffset;
        var clampedRow = GetClosestVisibleRowForRomOffset(targetRomOffset);
        SetRow(clampedRow);
        InvalidateTable();
    }

    private int GetClosestVisibleRowForRomOffset(int targetRomOffset)
    {
        if (targetRomOffset < ViewOffset)
        {
            targetRomOffset = ViewOffset;
        } 
        else if (targetRomOffset >= ViewOffset + rowsToShow)
        {
            targetRomOffset = ViewOffset + rowsToShow - 1;
        }

        var clampedRow = targetRomOffset - ViewOffset;
        return clampedRow;
    }

    private void SetRow(int rowIndex) => 
        table.CurrentCell = table.Rows[rowIndex].Cells[table.CurrentCell.ColumnIndex];


    private void table_MouseDown(object sender, MouseEventArgs e)
    {
        InvalidateTable();
    }

    private void table_SelectionChanged(object sender, EventArgs e)
    {
        SelectOffset(SelectedOffset, -1);
    }

    private void table_CellClick(object sender, DataGridViewCellEventArgs e)
    {

    }
    
    private void MoveNextColumn(int direction)
    {
        // for moving left/right in the grid
        
        var newColumnIndex = table.CurrentCell.ColumnIndex + direction;
        
        if (newColumnIndex < 0) {
            newColumnIndex = 0;
        } else if (newColumnIndex >= table.ColumnCount) {
            newColumnIndex = table.ColumnCount - 1;
        }

        table.CurrentCell = table.Rows[table.CurrentCell.RowIndex].Cells[newColumnIndex];
    }
    
    
    private void BeginEditingColumn(ColumnType columnType)
    {
        table.CurrentCell = GetCellInSelectedRowByColumnType(columnType);
        table.BeginEdit(true);
    }

    private DataGridViewCell GetCellInSelectedRowByColumnType(ColumnType columnType) => 
        table.Rows[table.CurrentCell.RowIndex].Cells[(int) columnType];

    private void ScrollVertically(int offset, int amount)
    {
        var romSize = Project.Data.GetRomSize();
        var newOffset = offset - amount;
        
        if (newOffset < 0) 
            newOffset = 0;
        
        if (newOffset >= romSize) 
            newOffset = romSize - 1;
        
        SelectOffset(newOffset, -1);
    }

    private void table_KeyDown(object sender, KeyEventArgs e)
    {
        if (Project?.Data == null || Project.Data.GetRomSize() <= 0) 
            return;

        var offset = SelectedOffset;

        // Console.WriteLine(e.KeyCode);

        var snesData = Project.Data.GetSnesApi();
        if (snesData == null)
            return;
        
        switch (e.KeyCode)
        {
            case Keys.F3:
                GoToNextUnreachedBranchPoint(offset);
                e.Handled = true;
                break;
            
            // case Keys.Home: case Keys.End: // I don't like these because they mess with text editing, disabling.
            case Keys.PageUp: case Keys.PageDown: 
            case Keys.Up: case Keys.Down:
                ScrollVertically(offset, e.KeyCode switch
                {
                    Keys.Up => 1,
                    Keys.Down => -1,
                    Keys.PageUp => 16,
                    Keys.PageDown => -16,
                    Keys.Home => 256,
                    Keys.End => -256,
                    _ => 0,
                });
                e.Handled = true;
                break;
            case Keys.Left:
                MoveNextColumn(-1);
                e.Handled = true;
                break;
            case Keys.Right:
                MoveNextColumn(1);
                e.Handled = true;
                break;
            case Keys.S:
                Step(offset);
                e.Handled = true;
                break;
            case Keys.I:
                StepIn(offset);
                e.Handled = true;
                break;
            case Keys.A:
                AutoStepSafe(offset);
                e.Handled = true;
                break;
            case Keys.T:
                GoToIntermediateAddress(offset);
                e.Handled = true;
                break;
            case Keys.U:
                GoToUnreached(true, true);
                e.Handled = true;
                break;
            case Keys.H:
                GoToUnreached(false, false);
                e.Handled = true;
                break;
            case Keys.N:
                GoToUnreached(false, true);
                e.Handled = true;
                break;
            
            case Keys.K:
                Mark(offset);
                e.Handled = true;
                break;
            
            case Keys.L:
                BeginEditingColumn(ColumnType.Label);
                e.Handled = true;
                break;
            case Keys.B:
                BeginEditingColumn(ColumnType.DataBank);
                e.Handled = true;
                break;
            case Keys.D:
                BeginEditingColumn(ColumnType.DirectPage);
                e.Handled = true;
                break;
            
            // I don't like this because it messes with CTRL+C
            // case Keys.C:
            //     BeginEditingColumn(ColumnType.Comment);
            //     e.Handled = true;
            //     break;
            
            case Keys.M:
                snesData.SetMFlag(offset, !snesData.GetMFlag(offset));
                e.Handled = true;
                break;
            case Keys.X:
                snesData.SetXFlag(offset, !snesData.GetXFlag(offset));
                e.Handled = true;
                break;
            
            case Keys.Enter:
                table.BeginEdit(true);
                e.Handled = true;
                break;
            
            case Keys.Delete:
                if (table.CurrentCell.ColumnIndex == (int)ColumnType.Label)
                {
                    // if editing a label, delete this label (don't just set to empty or we'll end up with blank labels)
                    var labels = snesData.Data.Labels;
                    var snesAddressOfSelectedRow = Project.Data.ConvertPCtoSnes(SelectedOffset);

                    if (labels.GetLabel(snesAddressOfSelectedRow) != null)
                        labels.RemoveLabel(snesAddressOfSelectedRow);
                    return;
                }
                
                // for everything else BUT labels, this is fine:
                table.CurrentCell.Value = null;
                e.Handled = true;
                break;
        }
        
        InvalidateTable();
    }

    private void table_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
    {
        var row = e.RowIndex + ViewOffset;
        if (row >= Project.Data.GetRomSize()) 
            return;
        
        var romByte = Project.Data.GetRomByte(row);
        var snesData = Project.Data.GetSnesApi();
        if (romByte == null || snesData == null)
            return;
        
        switch ((ColumnType) e.ColumnIndex)
        {
            case ColumnType.Label:
                e.Value = Project.Data.Labels.GetLabelName(Project.Data.ConvertPCtoSnes(row));
                break;
            case ColumnType.Offset:
                e.Value = Util.NumberToBaseString(Project.Data.ConvertPCtoSnes(row), Util.NumberBase.Hexadecimal, 6);
                break;
            case ColumnType.AsciiCharRep:
                e.Value = (char)romByte;
                break;
            case ColumnType.NumericRep:
                e.Value = Util.NumberToBaseString((int)romByte, displayBase);
                break;
            case ColumnType.Point:
                e.Value = RomUtil.PointToString(snesData.GetInOutPoint(row));
                break;
            case ColumnType.Instruction:
                var len = snesData.GetInstructionLength(row);
                e.Value = row + len <= Project.Data.GetRomSize() ? snesData.GetInstructionStr(row) : "";
                break;
            case ColumnType.IA:
                var ia = snesData.GetIntermediateAddressOrPointer(row);
                e.Value = ia >= 0 ? Util.NumberToBaseString(ia, Util.NumberBase.Hexadecimal, 6) : "";
                break;
            case ColumnType.TypeFlag:
                e.Value = Util.GetEnumDescription(snesData.GetFlag(row));
                break;
            case ColumnType.DataBank:
                e.Value = Util.NumberToBaseString(snesData.GetDataBank(row), Util.NumberBase.Hexadecimal, 2);
                break;
            case ColumnType.DirectPage:
                e.Value = Util.NumberToBaseString(snesData.GetDirectPage(row), Util.NumberBase.Hexadecimal, 4);
                break;
            case ColumnType.MFlag:
                e.Value = RomUtil.BoolToSize(snesData.GetMFlag(row));
                break;
            case ColumnType.XFlag:
                e.Value = RomUtil.BoolToSize(snesData.GetXFlag(row));
                break;
            case ColumnType.Comment:
                e.Value = Project.Data.GetCommentText(Project.Data.ConvertPCtoSnes(row));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void table_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
    {
        var value = e.Value as string;
        int result;
        var row = e.RowIndex + ViewOffset;
        if (row >= Project.Data.GetRomSize()) 
            return;
        
        var romByte = Project.Data.GetRomByte(row);
        var snesData = Project.Data.GetSnesApi();
        if (romByte == null || snesData == null)
            return;
        
        switch ((ColumnType) e.ColumnIndex)
        {
            case ColumnType.Label:
                Project.Data.Labels.AddLabel(Project.Data.ConvertPCtoSnes(row), new Diz.Core.model.Label { Name = value ?? "" }, true);
                break; // todo (validate for valid label characters)
            case ColumnType.DataBank:
                if (int.TryParse(value, NumberStyles.HexNumber, null, out result)) snesData.SetDataBank(row, result);
                break;
            case ColumnType.DirectPage:
                if (int.TryParse(value, NumberStyles.HexNumber, null, out result)) snesData.SetDirectPage(row, result);
                break;
            case ColumnType.MFlag:
                snesData.SetMFlag(row, value is "8" or "M");
                break;
            case ColumnType.XFlag:
                snesData.SetXFlag(row, value is "8" or "X");
                break;
            case ColumnType.Comment:
                Project.Data.AddComment(Project.Data.ConvertPCtoSnes(row), value, true);
                break;
        }

        table.InvalidateRow(e.RowIndex);
    }

    private void PaintCell(int offset, DataGridViewCellStyle style, int column, int selOffset)
    {
        // editable cells show up green
        if (column is (int) ColumnType.Label or (int) ColumnType.DataBank or (int) ColumnType.DirectPage or (int) ColumnType.Comment) 
            style.SelectionBackColor = Color.Chartreuse;
        
        var snesData = Project.Data.GetSnesApi();
        if (snesData == null)
            return;

        switch (snesData.GetFlag(offset))
        {
            case FlagType.Unreached:
                style.BackColor = Color.LightGray;
                style.ForeColor = Color.DarkSlateGray;
                break;
            case FlagType.Opcode: ;
                var color = GetDisplayColorForRowFlaggedAsOpcode(offset, column, snesData);
                if (color != null)
                    style.BackColor = color.Value;
                break;
            case FlagType.Operand:
                style.ForeColor = Color.LightGray;
                break;
            case FlagType.Graphics:
                style.BackColor = Color.LightPink;
                break;
            case FlagType.Music:
                style.BackColor = Color.PowderBlue;
                break;
            case FlagType.Data8Bit:
            case FlagType.Data16Bit:
            case FlagType.Data24Bit:
            case FlagType.Data32Bit:
                style.BackColor = Color.NavajoWhite;
                break;
            case FlagType.Pointer16Bit:
            case FlagType.Pointer24Bit:
            case FlagType.Pointer32Bit:
                style.BackColor = Color.Orchid;
                break;
            case FlagType.Text:
                style.BackColor = Color.Aquamarine;
                break;
            case FlagType.Empty:
                style.BackColor = Color.DarkSlateGray;
                style.ForeColor = Color.LightGray;
                break;
        }

        if (selOffset < 0 || selOffset >= Project.Data.GetRomSize()) 
            return;
        
        switch (column)
        {
            //&& (Project.Data.GetFlag(selOffset) == Data.FlagType.Opcode || Project.Data.GetFlag(selOffset) == Data.FlagType.Unreached)
            case (int) ColumnType.Offset 
                when Project.Data.ConvertSnesToPc(snesData.GetIntermediateAddressOrPointer(selOffset)) == offset:
                
            //&& (Project.Data.GetFlag(offset) == Data.FlagType.Opcode || Project.Data.GetFlag(offset) == Data.FlagType.Unreached)
            case (int) ColumnType.IA
                when Project.Data.ConvertSnesToPc(snesData.GetIntermediateAddressOrPointer(offset)) == selOffset:
                
                style.BackColor = Color.DeepPink;
                break;
        }
    }

    private Color? GetDisplayColorForRowFlaggedAsOpcode(int offset, int column, ISnesData snesData)
    {
        int opcode = Project.Data.GetRomByte(offset) ?? 0x0;
        var whichColumn = (ColumnType)column;
        switch (whichColumn)
        {
            case ColumnType.Point:
                var point = snesData.GetInOutPoint(offset);
                int r = 255, g = 255, b = 255;
                if ((point & (InOutPoint.EndPoint | InOutPoint.OutPoint)) != 0) 
                    g -= 50;
                if ((point & InOutPoint.InPoint) != 0) 
                    r -= 50;
                if ((point & InOutPoint.ReadPoint) != 0) 
                    b -= 50;
                
                return Color.FromArgb(r, g, b);
            case ColumnType.Instruction:
                if (opcode is 
                    0x40 or 0xCB or 0xDB or 0xF8 or  // RTI WAI STP SED 
                    0xFB or 0x00 or 0x02 or 0x42     // XCE BRK COP WDM
                   ) 
                    return Color.Yellow;
                break;
            case ColumnType.DataBank:
                switch (opcode)
                {
                    case 0xAB:
                    case 0x44:
                    // PLB MVP MVN
                    case 0x54:
                        return Color.OrangeRed;
                    // PHB
                    case 0x8B:
                        return Color.Yellow;
                }
                break;
            case ColumnType.DirectPage:
                switch (opcode)
                {
                    case 0x2B:
                    case 0x5B: // PLD TCD
                        return Color.OrangeRed;
                    case 0x0B:
                    case 0x7B: // PHD TDC
                        return Color.Yellow;
                }

                break;
            case ColumnType.MFlag:
            case ColumnType.XFlag:
                var mask = column == 10 ? 0x20 : 0x10;
                switch (opcode)
                {
                    // PLP SEP REP
                    case 0x28:
                    case 0xC2 or 0xE2 when (Project.Data.GetRomByte(offset + 1) & mask) != 0: // if: relevant bit set
                        return Color.OrangeRed;
                    // PHP
                    case 0x08:
                        return Color.Yellow;
                }

                break;
        }

        return null;
    }

    private void table_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
    {
        var row = e.RowIndex + ViewOffset;
        if (row < 0 || row >= Project.Data.GetRomSize()) 
            return;
        
        PaintCell(row, e.CellStyle, e.ColumnIndex, SelectedOffset);
    }

    public void MarkHistoryPoint(int pcOffset, ISnesNavigation.HistoryArgs? historyArgs, string position)
    {
        if (historyArgs == null) 
            return;

        historyArgs.Position = position;
        
        RememberNavigationPoint(SelectedOffset, historyArgs); // save old position
    }

    public void SelectOffsetWithOvershoot(int pcOffset, int overshootAmount = 0)
        => SelectOffset(pcOffset, -1, null, overshootAmount);

    public void SelectOffset(int pcOffset, ISnesNavigation.HistoryArgs? historyArgs = null)
        => SelectOffset(pcOffset, -1, historyArgs);

    public void SelectOffset(int pcOffset, int column = -1, ISnesNavigation.HistoryArgs? historyArgs = null, int overshootAmount=0)
    {
        if (pcOffset == -1)
            return;
        
        MarkHistoryPoint(SelectedOffset, historyArgs, "start");

        // purely visual. allows this offset to appear more in the middle of the screen, instead of at the very bottom
        // you typically want to be presented with a view that shows stuff of interest you jumped to 
        // visible and not having to scroll down a bit then back up.
        //
        // THIS IS 100% OPTIONAL.
        if (overshootAmount > 0)
        {
            // NEW
            // which way are we jumping?
            var direction = SelectedOffset < pcOffset ? 1 : -1; 
            
            // ideally, we'd calculate this number to be at the center or top. for now, we'll just pick an arbitrary amount
            var proposedNewOffset = pcOffset + overshootAmount * direction; // note: may be out of range
            
            // clamp to actual range
            var overshotOffset = Util.Clamp(proposedNewOffset, 0, Project.Data.GetRomSize() - 1);
            InternalSelectOffset(overshotOffset, column);
            
            // ----
            
            // ORIGINAL
            // ideally, we'd calculate this number to be at the center or top. for now, we'll just pick an arbitrary amount
            // var overshotOffset = Math.Min(Project.Data.GetRomSize()-1, pcOffset + overshootAmount);
            // InternalSelectOffset(overshotOffset, column);
            
            // now, the view is scrolled so we've overshot where we really want to go.
            // when we next call InternalSelectOffset() with the real address, it won't need to scroll, it'll just select something already in view.
        }
        
        // do the real thing
        InternalSelectOffset(pcOffset, column);
        
        MarkHistoryPoint(pcOffset, historyArgs, "end");

        InvalidateTable();
    }

    private void InternalSelectOffset(int pcOffset, int column)
    {
        var col = column == -1 ? table.CurrentCell.ColumnIndex : column;
        if (pcOffset < ViewOffset)
        {
            ViewOffset = pcOffset;
            UpdateDataGridView();
            table.CurrentCell = table.Rows[0].Cells[col];
        }
        else if (pcOffset >= ViewOffset + rowsToShow)
        {
            ViewOffset = pcOffset - rowsToShow + 1;
            UpdateDataGridView();
            table.CurrentCell = table.Rows[rowsToShow - 1].Cells[col];
        }
        else
        {
            table.CurrentCell = table.Rows[pcOffset - ViewOffset].Cells[col];
        }
    }

    private void InitMainTable()
    {
        table.CellValueNeeded += table_CellValueNeeded;
        table.CellValuePushed += table_CellValuePushed;
        table.CellPainting += table_CellPainting;

        rowsToShow = ((table.Height - table.ColumnHeadersHeight) / table.RowTemplate.Height);

        // https://stackoverflow.com/a/1506066
        typeof(DataGridView).InvokeMember(
            "DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.SetProperty,
            null,
            table,
            new object[] {true});
    }

    private void BeginEditingComment()
    {
        BeginEditingColumn(ColumnType.Comment);
    }

    private void BeginAddingLabel()
    {
        BeginEditingColumn(ColumnType.Label);
    }
}
