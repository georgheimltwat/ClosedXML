﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ClosedXML.Excel
{
    
    internal delegate void RangeShiftedRowsDelegate(XLRange range, Int32 rowsShifted);
    internal delegate void RangeShiftedColumnsDelegate(XLRange range, Int32 columnsShifted);
    internal class XLWorksheet : XLRangeBase, IXLWorksheet
    {
        public event RangeShiftedRowsDelegate RangeShiftedRows;
        public event RangeShiftedColumnsDelegate RangeShiftedColumns;

        #region Constants

        public const Int32 MaxNumberOfRows = 1048576;
        public const Int32 MaxNumberOfColumns = 16384;

        #endregion

        private XLWorkbook workbook;
        public XLWorksheet(String sheetName, XLWorkbook workbook)
            : base((IXLRangeAddress)new XLRangeAddress(new XLAddress(1, 1), new XLAddress(MaxNumberOfRows, MaxNumberOfColumns)))
        {
            Worksheet = this;
            NamedRanges = new XLNamedRanges(workbook);
            SheetView = new XLSheetView();
            Tables = new XLTables();
            Hyperlinks = new XLHyperlinks();
            this.workbook = workbook;
            style = new XLStyle(this, workbook.Style);
            Internals = new XLWorksheetInternals(new XLCellCollection(), new XLColumnsCollection(), new XLRowsCollection(), new XLRanges(workbook, workbook.Style) , workbook);
            PageSetup = new XLPageSetup(workbook.PageOptions, this);
            Outline = new XLOutline(workbook.Outline);
            ColumnWidth = workbook.ColumnWidth;
            RowHeight = workbook.RowHeight;
            this.Name = sheetName;
            RangeShiftedRows += new RangeShiftedRowsDelegate(XLWorksheet_RangeShiftedRows);
            RangeShiftedColumns += new RangeShiftedColumnsDelegate(XLWorksheet_RangeShiftedColumns);
        }

        void XLWorksheet_RangeShiftedColumns(XLRange range, int columnsShifted)
        {
            var newMerge = new XLRanges(workbook, workbook.Style);
            foreach (var rngMerged in Internals.MergedRanges)
            {
                if (range.RangeAddress.FirstAddress.ColumnNumber <= rngMerged.RangeAddress.FirstAddress.ColumnNumber
                    && rngMerged.RangeAddress.FirstAddress.RowNumber >= range.RangeAddress.FirstAddress.RowNumber
                    && rngMerged.RangeAddress.LastAddress.RowNumber <= range.RangeAddress.LastAddress.RowNumber)
                {
                    var newRng = Range(
                        rngMerged.RangeAddress.FirstAddress.RowNumber,
                        rngMerged.RangeAddress.FirstAddress.ColumnNumber + columnsShifted,
                        rngMerged.RangeAddress.LastAddress.RowNumber,
                        rngMerged.RangeAddress.LastAddress.ColumnNumber + columnsShifted);
                    newMerge.Add(newRng);
                }
                else if (
                       !(range.RangeAddress.FirstAddress.ColumnNumber <= rngMerged.RangeAddress.FirstAddress.ColumnNumber
                        && range.RangeAddress.FirstAddress.RowNumber <= rngMerged.RangeAddress.LastAddress.RowNumber))
                {
                    newMerge.Add(rngMerged);
                }
            }
            Internals.MergedRanges = newMerge;
        }

        void XLWorksheet_RangeShiftedRows(XLRange range, int rowsShifted)
        {
            var newMerge = new XLRanges(workbook, workbook.Style);
            foreach (var rngMerged in Internals.MergedRanges)
            {
                if (range.RangeAddress.FirstAddress.RowNumber <= rngMerged.RangeAddress.FirstAddress.RowNumber
                    && rngMerged.RangeAddress.FirstAddress.ColumnNumber >= range.RangeAddress.FirstAddress.ColumnNumber
                    && rngMerged.RangeAddress.LastAddress.ColumnNumber <= range.RangeAddress.LastAddress.ColumnNumber)
                {
                    var newRng = Range(
                        rngMerged.RangeAddress.FirstAddress.RowNumber + rowsShifted,
                        rngMerged.RangeAddress.FirstAddress.ColumnNumber,
                        rngMerged.RangeAddress.LastAddress.RowNumber + rowsShifted,
                        rngMerged.RangeAddress.LastAddress.ColumnNumber);
                    newMerge.Add(newRng);
                }
                else if (!(range.RangeAddress.FirstAddress.RowNumber <= rngMerged.RangeAddress.FirstAddress.RowNumber
                    && range.RangeAddress.FirstAddress.ColumnNumber <= rngMerged.RangeAddress.LastAddress.ColumnNumber))
                {
                    newMerge.Add(rngMerged);
                }
            }
            Internals.MergedRanges = newMerge;
        }

        public void NotifyRangeShiftedRows(XLRange range, Int32 rowsShifted)
        {
            if (RangeShiftedRows != null)
                RangeShiftedRows(range, rowsShifted);
        }

        public void NotifyRangeShiftedColumns(XLRange range, Int32 columnsShifted)
        {
            if (RangeShiftedColumns != null)
                RangeShiftedColumns(range, columnsShifted);
        }

        public XLWorksheetInternals Internals { get; private set; }
        
        #region IXLStylized Members

        private IXLStyle style;
        public override IXLStyle Style
        {
            get
            {
                return style;
            }
            set
            {
                style = new XLStyle(this, value);
                foreach(var cell in Internals.CellsCollection.Values)
                    cell.Style = style;
            }
        }

        public override IEnumerable<IXLStyle> Styles
        {
            get 
            {
                UpdatingStyle = true;
                yield return style;
                foreach (var c in Internals.CellsCollection.Values)
                {
                    yield return c.Style;
                }
                UpdatingStyle = false;
            }
        }

        public override Boolean UpdatingStyle { get; set; }

        public override IXLStyle InnerStyle
        {
            get { return new XLStyle(new XLStylizedContainer(this.style, this), style); }
            set { style = new XLStyle(this, value); }
        }

        #endregion

        public Double ColumnWidth { get; set; }
        public Double RowHeight { get; set; }

        public String Name { get; set; }
        public Int32 SheetId { get; set; }
        public String RelId { get; set; }
        
        internal Int32 position;
        public Int32 Position 
        {
            get
            {
                return position;
            }
            set
            {
                if (value > workbook.Worksheets.Count() + 1)
                    throw new IndexOutOfRangeException("Index must be equal or less than the number of worksheets + 1.");

                if (value < position)
                    workbook.Worksheets.Where(w => ((XLWorksheet)w).Position >= value && ((XLWorksheet)w).Position < position).ForEach(w => ((XLWorksheet)w).position += 1);

                if (value > position)
                    workbook.Worksheets.Where(w => ((XLWorksheet)w).Position <= value && ((XLWorksheet)w).Position > position).ForEach(w => ((XLWorksheet)w).position -= 1);

                position = value;
            }
        }

        public IXLPageSetup PageSetup { get; private set; }
        public IXLOutline Outline { get; private set; }

        public IXLRow FirstRowUsed()
        {
            var rngRow = this.AsRange().FirstRowUsed();
            if (rngRow != null)
            {
                return this.Row(rngRow.RangeAddress.FirstAddress.RowNumber);
            }
            else
            {
                return null;
            }
        }
        public IXLRow LastRowUsed()
        {
            var rngRow = this.AsRange().LastRowUsed();
            if (rngRow != null)
            {
                return this.Row(rngRow.RangeAddress.LastAddress.RowNumber);
            }
            else
            {
                return null;
            }
        }

        public IXLColumn LastColumn()
        {
            return Column(MaxNumberOfColumns);
        }
        public IXLColumn FirstColumn()
        {
            return Column(1);
        }
        public IXLRow FirstRow()
        {
            return Row(1);
        }
        public IXLRow LastRow()
        {
            return Row(MaxNumberOfRows);
        }
        public IXLColumn FirstColumnUsed()
        {
            var rngColumn = this.AsRange().FirstColumnUsed();
            if (rngColumn != null)
            {
                return this.Column(rngColumn.RangeAddress.FirstAddress.ColumnNumber);
            }
            else
            {
                return null;
            }
        }
        public IXLColumn LastColumnUsed()
        {
            var rngColumn = this.AsRange().LastColumnUsed();
            if (rngColumn != null)
            {
                return this.Column(rngColumn.RangeAddress.LastAddress.ColumnNumber);
            }
            else
            {
                return null;
            }
        }

        public IXLColumns Columns()
        {
            var retVal = new XLColumns(this, true);
            var columnList = new List<Int32>();

            if (this.Internals.CellsCollection.Count > 0)
                columnList.AddRange(this.Internals.CellsCollection.Keys.Select(k => k.ColumnNumber).Distinct());

            if (this.Internals.ColumnsCollection.Count > 0)
                columnList.AddRange(this.Internals.ColumnsCollection.Keys.Where(c => !columnList.Contains(c)));

            foreach (var c in columnList)
            {
                retVal.Add((XLColumn)this.Column(c));
            }

            return retVal;
        }
        public IXLColumns Columns(String columns)
        {
            var retVal = new XLColumns(this);
            var columnPairs = columns.Split(',');
            foreach (var pair in columnPairs)
            {
                var tPair = pair.Trim();
                String firstColumn;
                String lastColumn;
                if (tPair.Contains(':') || tPair.Contains('-'))
                {
                    if (tPair.Contains('-'))
                        tPair = tPair.Replace('-', ':');

                    var columnRange = tPair.Split(':');
                    firstColumn = columnRange[0];
                    lastColumn = columnRange[1];
                }
                else
                {
                    firstColumn = tPair;
                    lastColumn = tPair;
                }

                Int32 tmp;
                if (Int32.TryParse(firstColumn, out tmp))
                    foreach (var col in this.Columns(Int32.Parse(firstColumn), Int32.Parse(lastColumn)))
                    {
                        retVal.Add((XLColumn)col);
                    }
                else
                    foreach (var col in this.Columns(firstColumn, lastColumn))
                    {
                        retVal.Add((XLColumn)col);
                    }
            }
            return retVal;
        }
        public IXLColumns Columns( String firstColumn, String lastColumn)
        {
            return this.Columns(XLAddress.GetColumnNumberFromLetter(firstColumn), XLAddress.GetColumnNumberFromLetter(lastColumn));
        }
        public IXLColumns Columns( Int32 firstColumn, Int32 lastColumn)
        {
            var retVal = new XLColumns(this);

            for (var co = firstColumn; co <= lastColumn; co++)
            {
                retVal.Add((XLColumn)this.Column(co));
            }
            return retVal;
        }

        public IXLRows Rows()
        {
            var retVal = new XLRows(this, true);
            var rowList = new List<Int32>();

            if (this.Internals.CellsCollection.Count > 0)
                rowList.AddRange(this.Internals.CellsCollection.Keys.Select(k => k.RowNumber).Distinct());

            if (this.Internals.RowsCollection.Count > 0)
                rowList.AddRange(this.Internals.RowsCollection.Keys.Where(r => !rowList.Contains(r)));

            foreach (var r in rowList)
            {
                retVal.Add((XLRow)this.Row(r));
            }

            return retVal;
        }
        public IXLRows Rows( String rows)
        {
            var retVal = new XLRows(this);
            var rowPairs = rows.Split(',');
            foreach (var pair in rowPairs)
            {
                var tPair = pair.Trim();
                String firstRow;
                String lastRow;
                if (tPair.Contains(':') || tPair.Contains('-'))
                {
                    if (tPair.Contains('-'))
                        tPair = tPair.Replace('-', ':');

                    var rowRange = tPair.Split(':');
                    firstRow = rowRange[0];
                    lastRow = rowRange[1];
                }
                else
                {
                    firstRow = tPair;
                    lastRow = tPair;
                }
                foreach (var row in this.Rows(Int32.Parse(firstRow), Int32.Parse(lastRow)))
                {
                    retVal.Add((XLRow)row);
                }
            }
            return retVal;
        }
        public IXLRows Rows( Int32 firstRow, Int32 lastRow)
        {
            var retVal = new XLRows(this);

            for (var ro = firstRow; ro <= lastRow; ro++)
            {
                retVal.Add((XLRow)this.Row(ro));
            }
            return retVal;
        }

        public IXLRow Row(Int32 row)
        {
            return Row(row, true);
        }
        public IXLRow Row(Int32 row, Boolean pingCells)
        {
            IXLStyle styleToUse;
            if (this.Internals.RowsCollection.ContainsKey(row))
            {
                styleToUse = this.Internals.RowsCollection[row].Style;
            }
            else
            {
                if (pingCells)
                {
                    // This is a new row so we're going to reference all 
                    // cells in columns of this row to preserve their formatting
                    var distinctColumns = new HashSet<Int32>();
                    foreach (var k in this.Internals.CellsCollection.Keys)
                    {
                        if (!distinctColumns.Contains(k.ColumnNumber))
                            distinctColumns.Add(k.ColumnNumber);
                    }

                    var usedColumns = from c in this.Internals.ColumnsCollection
                                      join dc in distinctColumns
                                        on c.Key equals dc
                                      where !this.Internals.CellsCollection.ContainsKey(new XLAddress(row, c.Key))
                                      select c.Key;

                    usedColumns.ForEach(c => Cell(row, c));
                }
                styleToUse = this.Style;
                this.Internals.RowsCollection.Add(row, new XLRow(row, new XLRowParameters(this, styleToUse, false)));
            }

            return new XLRow(row, new XLRowParameters(this, styleToUse, true));
        }
        public IXLColumn Column( Int32 column)
        {
            IXLStyle styleToUse;
            if (this.Internals.ColumnsCollection.ContainsKey(column))
            {
                styleToUse = this.Internals.ColumnsCollection[column].Style;
            }
            else
            {
                // This is a new row so we're going to reference all 
                // cells in this row to preserve their formatting
                this.Internals.RowsCollection.Keys.ForEach(r => Cell(r, column));
                styleToUse = this.Style;
                this.Internals.ColumnsCollection.Add(column, new XLColumn(column, new XLColumnParameters(this, this.Style, false)));
            }

            return new XLColumn(column, new XLColumnParameters(this, this.Style, true));
        }
        public IXLColumn Column( String column)
        {
            return this.Column(XLAddress.GetColumnNumberFromLetter(column));
        }

        public override IXLRange AsRange()
        {
            return Range(1, 1, XLWorksheet.MaxNumberOfRows, XLWorksheet.MaxNumberOfColumns);
        }

        public void CollapseRows()
        {
            Enumerable.Range(1, 8).ForEach(i => CollapseRows(i));
        }
        public void CollapseColumns()
        {
            Enumerable.Range(1, 8).ForEach(i => CollapseColumns(i));
        }
        public void ExpandRows()
        {
            Enumerable.Range(1, 8).ForEach(i => ExpandRows(i));
        }
        public void ExpandColumns()
        {
            Enumerable.Range(1, 8).ForEach(i => ExpandRows(i));
        }

        public void CollapseRows(Int32 outlineLevel)
        {
            if (outlineLevel < 1 || outlineLevel > 8)
                throw new ArgumentOutOfRangeException("Outline level must be between 1 and 8.");

            Internals.RowsCollection.Values.Where(r => r.OutlineLevel == outlineLevel).ForEach(r => r.Collapse());
        }
        public void CollapseColumns(Int32 outlineLevel)
        {
            if (outlineLevel < 1 || outlineLevel > 8)
                throw new ArgumentOutOfRangeException("Outline level must be between 1 and 8.");

            Internals.ColumnsCollection.Values.Where(c => c.OutlineLevel == outlineLevel).ForEach(c => c.Collapse());
        }
        public void ExpandRows(Int32 outlineLevel)
        {
            if (outlineLevel < 1 || outlineLevel > 8)
                throw new ArgumentOutOfRangeException("Outline level must be between 1 and 8.");

            Internals.RowsCollection.Values.Where(r => r.OutlineLevel == outlineLevel).ForEach(r => r.Expand());
        }
        public void ExpandColumns(Int32 outlineLevel)
        {
            if (outlineLevel < 1 || outlineLevel > 8)
                throw new ArgumentOutOfRangeException("Outline level must be between 1 and 8.");

            Internals.ColumnsCollection.Values.Where(c => c.OutlineLevel == outlineLevel).ForEach(c => c.Expand());
        }

        public void Delete()
        {
            workbook.Worksheets.Delete(Name);
        }
        public new void Clear()
        {
            Internals.CellsCollection.Clear();
            Internals.ColumnsCollection.Clear();
            Internals.MergedRanges.Clear();
            Internals.RowsCollection.Clear();
        }
        public IXLNamedRanges NamedRanges { get; private set; }
        public IXLSheetView SheetView { get; private set; }
        public IXLTables Tables { get; private set; }
        public IXLTable Table(String name)
        {
            return Tables.Table(name);
        }

        public IXLWorksheet CopyTo(String newSheetName)
        {
            return CopyTo(this.workbook, newSheetName, workbook.Worksheets.Count() + 1);
        }

        public IXLWorksheet CopyTo(String newSheetName, Int32 position)
        {
            return CopyTo(this.workbook, newSheetName, position);
        }

        public IXLWorksheet CopyTo(XLWorkbook workbook, String newSheetName)
        {
            return CopyTo(workbook, newSheetName, workbook.Worksheets.Count() + 1);
        }

        public IXLWorksheet CopyTo(XLWorkbook workbook, String newSheetName, Int32 position)
        {
            var ws = (XLWorksheet)workbook.Worksheets.Add(newSheetName, position);
            this.Internals.CellsCollection.ForEach(kp => ws.Internals.CellsCollection.Add(kp));
            this.Internals.ColumnsCollection.ForEach(kp => ws.Internals.ColumnsCollection.Add(kp));
            this.Internals.MergedRanges.ForEach(kp => ws.Internals.MergedRanges.Add(kp));
            this.Internals.RowsCollection.ForEach(kp => ws.Internals.RowsCollection.Add(kp));
            this.PageSetup = new XLPageSetup(this.PageSetup, ws);
            this.Outline = new XLOutline(this.Outline);
            this.SheetView = new XLSheetView(this.SheetView);
            this.NamedRanges.ForEach(r => ws.NamedRanges.Add(r.Name, r.Ranges));
            this.Tables.ForEach(t => ws.Tables.Add(t));
            return ws;
        }

        #region Outlines
        private Dictionary<Int32, Int32> columnOutlineCount = new Dictionary<Int32, Int32>();
        public void IncrementColumnOutline(Int32 level)
        {
            if (level > 0)
            {
                if (!columnOutlineCount.ContainsKey(level))
                    columnOutlineCount.Add(level, 0);

                columnOutlineCount[level]++;
            }
        }
        public void DecrementColumnOutline(Int32 level)
        {
            if (level > 0)
            {
                if (!columnOutlineCount.ContainsKey(level))
                    columnOutlineCount.Add(level, 0);

                if (columnOutlineCount[level] > 0)
                    columnOutlineCount[level]--;
            }
        }
        public Int32 GetMaxColumnOutline()
        {
            if (columnOutlineCount.Count == 0)
                return 0;
            else
                return columnOutlineCount.Where(kp => kp.Value > 0).Max(kp => kp.Key);
        }

        private Dictionary<Int32, Int32> rowOutlineCount = new Dictionary<Int32, Int32>();
        public void IncrementRowOutline(Int32 level)
        {
            if (level > 0)
            {
                if (!rowOutlineCount.ContainsKey(level))
                    rowOutlineCount.Add(level, 0);

                rowOutlineCount[level]++;
            }
        }
        public void DecrementRowOutline(Int32 level)
        {
            if (level > 0)
            {
                if (!rowOutlineCount.ContainsKey(level))
                    rowOutlineCount.Add(level, 0);

                if (rowOutlineCount[level] > 0)
                    rowOutlineCount[level]--;
            }
        }
        public Int32 GetMaxRowOutline()
        {
            if (rowOutlineCount.Count == 0)
                return 0;
            else
                return rowOutlineCount.Where(kp => kp.Value > 0).Max(kp => kp.Key);
        }
        #endregion

        public new IXLHyperlinks Hyperlinks { get; private set; }
    }
}