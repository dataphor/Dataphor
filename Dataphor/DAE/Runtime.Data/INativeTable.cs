namespace Alphora.Dataphor.DAE.Runtime.Data
{
    public interface INativeTable
    {
        Schema.TableVar TableVar { get; set; }
        Schema.ITableType TableType { get; set; }
        Schema.IRowType RowType { get; set; }
        NativeRowTree ClusteredIndex { get; set; }
        NativeRowTreeList NonClusteredIndexes { get; set; }
        int Fanout { get; }
        int Capacity { get; }
        int RowCount { get; }
        void Drop(IValueManager AManager);

        /// <summary>Inserts the given row into all the indexes of the table value.</summary>
        /// <param name="ARow">The given row must conform to the structure of the table value.</param>
        void Insert(IValueManager AManager, Row ARow);

        bool HasRow(IValueManager AManager, Row ARow);
        void Update(IValueManager AManager, Row AOldRow, Row ANewRow);
        void Delete(IValueManager AManager, Row ARow);
        void Truncate(IValueManager AManager);
    }
}