using System;
using System.Globalization;
using System.IO;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Language.SQL;
using Alphora.Dataphor.DAE.Language.PGSQL;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using ColumnExpression = Alphora.Dataphor.DAE.Language.SQL.ColumnExpression;
using DropIndexStatement = Alphora.Dataphor.DAE.Language.PGSQL.DropIndexStatement;
using SelectStatement = Alphora.Dataphor.DAE.Language.SQL.SelectStatement;

namespace Alphora.Dataphor.DAE.Device.PGSQL
{

    public class PostgreSQLPostgreSQLBinaryCompareNode : BinaryInstructionNode
    {
        public static int Compare(Program AProgram, object ALeftValue, object ARightValue)
        {
            Stream LLeftStream =
                ALeftValue is byte[]
                    ? new MemoryStream((byte[])ALeftValue, 0, ((byte[])ALeftValue).Length, false, true)
                    : AProgram.StreamManager.Open((StreamID)ALeftValue, LockMode.Exclusive);
            try
            {
                Stream LRightStream =
                    ARightValue is byte[]
                        ? new MemoryStream((byte[])ARightValue, 0, ((byte[])ARightValue).Length, false, true)
                        : AProgram.StreamManager.Open((StreamID)ARightValue, LockMode.Exclusive);
                try
                {
                    int LLeftByte;
                    int LRightByte;

                    while (true)
                    {
                        LLeftByte = LLeftStream.ReadByte();
                        LRightByte = LRightStream.ReadByte();

                        if (LLeftByte != LRightByte)
                            break;

                        if (LLeftByte == -1)
                            break;

                        if (LRightByte == -1)
                            break;
                    }

                    return LLeftByte == LRightByte ? 0 : LLeftByte > LRightByte ? 1 : -1;
                }
                finally
                {
                    LRightStream.Close();
                }
            }
            finally
            {
                LLeftStream.Close();
            }
        }

        public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return Compare(AProgram, AArgument1, AArgument2);
        }
    }

    public class PostgreSQLPostgreSQLBinaryEqualNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return PostgreSQLPostgreSQLBinaryCompareNode.Compare(AProgram, AArgument1, AArgument2) == 0;
        }
    }

    public class PostgreSQLPostgreSQLBinaryNotEqualNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return PostgreSQLPostgreSQLBinaryCompareNode.Compare(AProgram, AArgument1, AArgument2) != 0;
        }
    }

    public class PostgreSQLPostgreSQLBinaryLessNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return PostgreSQLPostgreSQLBinaryCompareNode.Compare(AProgram, AArgument1, AArgument2) < 0;
        }
    }

    public class PostgreSQLPostgreSQLBinaryInclusiveLessNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return PostgreSQLPostgreSQLBinaryCompareNode.Compare(AProgram, AArgument1, AArgument2) <= 0;
        }
    }

    public class PostgreSQLPostgreSQLBinaryGreaterNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return PostgreSQLPostgreSQLBinaryCompareNode.Compare(AProgram, AArgument1, AArgument2) > 0;
        }
    }

    public class PostgreSQLPostgreSQLBinaryInclusiveGreaterNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return PostgreSQLPostgreSQLBinaryCompareNode.Compare(AProgram, AArgument1, AArgument2) >= 0;
        }
    }

    
}