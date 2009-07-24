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
using ColumnExpression = Alphora.Dataphor.DAE.Language.SQL.ColumnExpression;
using DropIndexStatement = Alphora.Dataphor.DAE.Language.PGSQL.DropIndexStatement;
using SelectStatement = Alphora.Dataphor.DAE.Language.SQL.SelectStatement;

namespace Alphora.Dataphor.Device.PGSQL
{

    #region Instruction Nodes

    public class PostgreSQLPostgreSQLBinaryCompareNode : InstructionNode
    {
        public static int Compare(Scalar ALeftValue, Scalar ARightValue)
        {
            Stream LLeftStream = ALeftValue.IsNative
                                     ? new MemoryStream(ALeftValue.AsByteArray, 0, ALeftValue.AsByteArray.Length, false,
                                                        true)
                                     : ALeftValue.OpenStream();
            try
            {
                Stream LRightStream = ARightValue.IsNative
                                          ? new MemoryStream(ARightValue.AsByteArray, 0, ARightValue.AsByteArray.Length,
                                                             false, true)
                                          : ARightValue.OpenStream();
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

        public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
        {
            if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
                return new DataVar(FDataType, null);
            else
                return new DataVar(FDataType,
                                   new Scalar(AProcess, (ScalarType)FDataType,
                                              Compare((Scalar)AArguments[0].Value, (Scalar)AArguments[1].Value)));
        }
    }

    public class PostgreSQLPostgreSQLBinaryEqualNode : InstructionNode
    {
        public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
        {
            if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
                return new DataVar(FDataType, null);
            else
                return new DataVar(FDataType,
                                   new Scalar(AProcess, (ScalarType)FDataType,
                                              PostgreSQLPostgreSQLBinaryCompareNode.Compare((Scalar)AArguments[0].Value,
                                                                                  (Scalar)AArguments[1].Value) == 0));
        }
    }

    public class PostgreSQLPostgreSQLBinaryNotEqualNode : InstructionNode
    {
        public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
        {
            if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
                return new DataVar(FDataType, null);
            else
                return new DataVar(FDataType,
                                   new Scalar(AProcess, (ScalarType)FDataType,
                                              PostgreSQLPostgreSQLBinaryCompareNode.Compare((Scalar)AArguments[0].Value,
                                                                                  (Scalar)AArguments[1].Value) != 0));
        }
    }

    public class PostgreSQLPostgreSQLBinaryLessNode : InstructionNode
    {
        public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
        {
            if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
                return new DataVar(FDataType, null);
            else
                return new DataVar(FDataType,
                                   new Scalar(AProcess, (ScalarType)FDataType,
                                              PostgreSQLPostgreSQLBinaryCompareNode.Compare((Scalar)AArguments[0].Value,
                                                                                  (Scalar)AArguments[1].Value) < 0));
        }
    }

    public class PostgreSQLPostgreSQLBinaryInclusiveLessNode : InstructionNode
    {
        public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
        {
            if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
                return new DataVar(FDataType, null);
            else
                return new DataVar(FDataType,
                                   new Scalar(AProcess, (ScalarType)FDataType,
                                              PostgreSQLPostgreSQLBinaryCompareNode.Compare((Scalar)AArguments[0].Value,
                                                                                  (Scalar)AArguments[1].Value) <= 0));
        }
    }

    public class PostgreSQLPostgreSQLBinaryGreaterNode : InstructionNode
    {
        public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
        {
            if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
                return new DataVar(FDataType, null);
            else
                return new DataVar(FDataType,
                                   new Scalar(AProcess, (ScalarType)FDataType,
                                              PostgreSQLPostgreSQLBinaryCompareNode.Compare((Scalar)AArguments[0].Value,
                                                                                  (Scalar)AArguments[1].Value) > 0));
        }
    }

    public class PostgreSQLPostgreSQLBinaryInclusiveGreaterNode : InstructionNode
    {
        public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
        {
            if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
                return new DataVar(FDataType, null);
            else
                return new DataVar(FDataType,
                                   new Scalar(AProcess, (ScalarType)FDataType,
                                              PostgreSQLPostgreSQLBinaryCompareNode.Compare((Scalar)AArguments[0].Value,
                                                                                  (Scalar)AArguments[1].Value) >= 0));
        }
    }

    #endregion
}