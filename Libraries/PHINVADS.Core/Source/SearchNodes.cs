/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define NILPROPOGATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using VadsClient;

namespace Alphora.Dataphor.PHINVADS.Core
{
	// operator SatisfiesSearchParam(const AResource : generic, const AParamName : string, const AParamValue : string) : boolean
	public class SatisfiesSearchParamNode : TernaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null || argument3 == null)
				return null;
			#endif

			throw new InvalidOperationException("Data exposed by the service does not permit evaluating this criteria outside the service.");
		}
	}

	public class UnaryCurrentNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return program.Stack.Peek((int)argument1);
		}
	}

	public class NilaryCurrentNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			return program.Stack.Peek(0);
		}
	}
}
