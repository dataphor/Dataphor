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

using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Device.MSSQL
{
    public class MSSQLMSSQLBinaryCompareNode : BinaryInstructionNode
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

    public class MSSQLMSSQLBinaryEqualNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return MSSQLMSSQLBinaryCompareNode.Compare(program, argument1, argument2) == 0;
        }
    }

    public class MSSQLMSSQLBinaryNotEqualNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return MSSQLMSSQLBinaryCompareNode.Compare(program, argument1, argument2) != 0;
        }
    }

    public class MSSQLMSSQLBinaryLessNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return MSSQLMSSQLBinaryCompareNode.Compare(program, argument1, argument2) < 0;
        }
    }

    public class MSSQLMSSQLBinaryInclusiveLessNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return MSSQLMSSQLBinaryCompareNode.Compare(program, argument1, argument2) <= 0;
        }
    }

    public class MSSQLMSSQLBinaryGreaterNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return MSSQLMSSQLBinaryCompareNode.Compare(program, argument1, argument2) > 0;
        }
    }

    public class MSSQLMSSQLBinaryInclusiveGreaterNode : BinaryInstructionNode
    {
        public override object InternalExecute(Program program, object argument1, object argument2)
        {
            if ((argument1 == null) || (argument2 == null))
                return null;
            else
                return MSSQLMSSQLBinaryCompareNode.Compare(program, argument1, argument2) >= 0;
        }
    }
}