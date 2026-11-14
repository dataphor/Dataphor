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
        public static int Compare(Program program, object leftValue, object rightValue)
        {
            Stream leftStream =
                leftValue is byte[]
                    ? new MemoryStream((byte[])leftValue, 0, ((byte[])leftValue).Length, false, true)
                    : program.StreamManager.Open((StreamID)leftValue, LockMode.Exclusive);
            try
            {
                Stream rightStream =
                    rightValue is byte[]
                        ? new MemoryStream((byte[])rightValue, 0, ((byte[])rightValue).Length, false, true)
                        : program.StreamManager.Open((StreamID)rightValue, LockMode.Exclusive);
                try
                {
                    int leftByte;
                    int rightByte;

                    while (true)
                    {
                        leftByte = leftStream.ReadByte();
                        rightByte = rightStream.ReadByte();

                        if (leftByte != rightByte)
                            break;

                        if (leftByte == -1)
                            break;

                        if (rightByte == -1)
                            break;
                    }

                    return leftByte == rightByte ? 0 : leftByte > rightByte ? 1 : -1;
                }
                finally
                {
                    rightStream.Close();
                }
            }
            finally
            {
                leftStream.Close();
            }
        }

        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return Compare(program, argument1, argument2);
        }
    }

    public class PostgreSQLPostgreSQLBinaryEqualNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return PostgreSQLPostgreSQLBinaryCompareNode.Compare(program, argument1, argument2) == 0;
        }
    }

    public class PostgreSQLPostgreSQLBinaryNotEqualNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return PostgreSQLPostgreSQLBinaryCompareNode.Compare(program, argument1, argument2) != 0;
        }
    }

    public class PostgreSQLPostgreSQLBinaryLessNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return PostgreSQLPostgreSQLBinaryCompareNode.Compare(program, argument1, argument2) < 0;
        }
    }

    public class PostgreSQLPostgreSQLBinaryInclusiveLessNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return PostgreSQLPostgreSQLBinaryCompareNode.Compare(program, argument1, argument2) <= 0;
        }
    }

    public class PostgreSQLPostgreSQLBinaryGreaterNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return PostgreSQLPostgreSQLBinaryCompareNode.Compare(program, argument1, argument2) > 0;
        }
    }

    public class PostgreSQLPostgreSQLBinaryInclusiveGreaterNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return PostgreSQLPostgreSQLBinaryCompareNode.Compare(program, argument1, argument2) >= 0;
        }
    }

    
}