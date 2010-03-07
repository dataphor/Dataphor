using System;
using Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
    public class NativeTableCopy : System.Object, INativeTable
    {
        private INativeTable FNativeTable;
        private IValueManager FManager;
        private bool FReallyCopied;

        public NativeTableCopy(IValueManager AManager, INativeTable ATable)
        {
            FNativeTable = ATable;
            FManager = AManager;
            FReallyCopied = false;
        }


        private void ReallyCopy()
        {
            if (FReallyCopied)
                return;
            INativeTable LNewTable = new NativeTable(FManager, FNativeTable.TableVar);
            using (Scan LScan = new Scan(FManager, FNativeTable, FNativeTable.ClusteredIndex, ScanDirection.Forward, null, null))
            {
                LScan.Open();
                while (LScan.Next())
                {
                    using (Row LRow = LScan.GetRow())
                    {
                        LNewTable.Insert(FManager, LRow);
                    }
                }
            }            
            FNativeTable = LNewTable;
            FReallyCopied = true;
        }

        private INativeTable NativeTable
        {
            get
            {
                return this.FNativeTable;
            }
        }

        #region Implementation of INativeTable

        public TableVar TableVar
        {
            get { return this.NativeTable.TableVar; }
            set
            {
                ReallyCopy();
                this.NativeTable.TableVar=value;
            }
        }

        public ITableType TableType
        {
            get { return this.NativeTable.TableType; }
            set
            {
                ReallyCopy();
                this.NativeTable.TableType=value;
            }
        }

        public IRowType RowType
        {
            get { return this.NativeTable.RowType; }
            set
            {
                ReallyCopy();
                this.NativeTable.RowType = value;
            }
        }

        public NativeRowTree ClusteredIndex
        {
            get { return this.NativeTable.ClusteredIndex; }
            set
            {
                ReallyCopy();
                this.NativeTable.ClusteredIndex=value;
            }
        }

        public NativeRowTreeList NonClusteredIndexes
        {
            get { return this.NativeTable.NonClusteredIndexes; }
            set
            {
                ReallyCopy();
                this.NativeTable.NonClusteredIndexes = value;
            }
        }

        public int Fanout
        {
            get { return this.NativeTable.Fanout; }
        }

        public int Capacity
        {
            get { return this.NativeTable.Capacity; }
        }

        public int RowCount
        {
            get { return this.NativeTable.RowCount; }
        }

        public void Drop(IValueManager AManager)
        {
            ReallyCopy();
            this.NativeTable.Drop(AManager);
        }

        public void Insert(IValueManager AManager, Row ARow)
        {
            ReallyCopy();
            this.NativeTable.Insert(AManager,ARow);
        }

        public bool HasRow(IValueManager AManager, Row ARow)
        {
            ReallyCopy();
            return this.NativeTable.HasRow(AManager, ARow);
        }

        public void Update(IValueManager AManager, Row AOldRow, Row ANewRow)
        {
            ReallyCopy();
            this.NativeTable.Update(AManager,AOldRow,ANewRow);
        }

        public void Delete(IValueManager AManager, Row ARow)
        {
            ReallyCopy();
            this.NativeTable.Delete(AManager, ARow);
        }

        public void Truncate(IValueManager AManager)
        {
            ReallyCopy();
            this.NativeTable.Truncate(AManager);
        }

        #endregion
    }
}