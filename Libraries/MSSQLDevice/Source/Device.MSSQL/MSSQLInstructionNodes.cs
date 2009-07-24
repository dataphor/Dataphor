/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USECONNECTIONPOOLING
#define USESQLOLEDB
//#define USEOLEDBCONNECTION
//#define USEADOCONNECTION


using System.IO;
using Alphora.Dataphor.DAE.Runtime;

using Alphora.Dataphor.DAE.Runtime.Instructions;

using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;

namespace Alphora.Dataphor.DAE.Device.MSSQL
{



    public class MSSQLMSSQLBinaryCompareNode : BinaryInstructionNode
    {
        public static int Compare(ServerProcess AProcess, object ALeftValue, object ARightValue)
        {
            Stream LLeftStream =
                ALeftValue is byte[]
                    ? new MemoryStream((byte[])ALeftValue, 0, ((byte[])ALeftValue).Length, false, true)
                    : AProcess.StreamManager.Open((StreamID)ALeftValue, LockMode.Exclusive);
            try
            {
                Stream LRightStream =
                    ARightValue is byte[]
                        ? new MemoryStream((byte[])ARightValue, 0, ((byte[])ARightValue).Length, false, true)
                        : AProcess.StreamManager.Open((StreamID)ARightValue, LockMode.Exclusive);
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

        public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return Compare(AProcess, AArgument1, AArgument2);
        }
    }

    public class MSSQLMSSQLBinaryEqualNode : BinaryInstructionNode
    {
        public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return MSSQLMSSQLBinaryCompareNode.Compare(AProcess, AArgument1, AArgument2) == 0;
        }
    }

    public class MSSQLMSSQLBinaryNotEqualNode : BinaryInstructionNode
    {
        public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return MSSQLMSSQLBinaryCompareNode.Compare(AProcess, AArgument1, AArgument2) != 0;
        }
    }

    public class MSSQLMSSQLBinaryLessNode : BinaryInstructionNode
    {
        public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return MSSQLMSSQLBinaryCompareNode.Compare(AProcess, AArgument1, AArgument2) < 0;
        }
    }

    public class MSSQLMSSQLBinaryInclusiveLessNode : BinaryInstructionNode
    {
        public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return MSSQLMSSQLBinaryCompareNode.Compare(AProcess, AArgument1, AArgument2) <= 0;
        }
    }

    public class MSSQLMSSQLBinaryGreaterNode : BinaryInstructionNode
    {
        public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return MSSQLMSSQLBinaryCompareNode.Compare(AProcess, AArgument1, AArgument2) > 0;
        }
    }

    public class MSSQLMSSQLBinaryInclusiveGreaterNode : BinaryInstructionNode
    {
        public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
        {
            if ((AArgument1 == null) || (AArgument2 == null))
                return null;
            else
                return MSSQLMSSQLBinaryCompareNode.Compare(AProcess, AArgument1, AArgument2) >= 0;
        }
    }

 
}