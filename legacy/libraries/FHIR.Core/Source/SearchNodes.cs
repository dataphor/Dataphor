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
using Hl7.Fhir.Model;

namespace Alphora.Dataphor.FHIR.Core
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

			var searchParamName = ((string)argument2).ToLower();

			IRow row = argument1 as IRow;
			if (row != null)
			{
				var typeName = (string)row["TypeName"];
				switch (typeName)
				{
					case "Appointment":
						switch (searchParamName)
						{
							case "patient":
							case "practitioner":
								if (row.HasValue("Participant"))
								{
									foreach (Appointment.ParticipantComponent participant in row.GetValue("Participant") as ListValue)
									{
										// Return the id of the reference
										var index = participant.Actor.Reference.ToLower().IndexOf(searchParamName);
										if (index >= 0)
										{
											if (participant.Actor.Reference.Substring(index + searchParamName.Length + 1) == (string)argument3)
											{
												return true;
											}
										}
									}
									return false;
								}
								else
								{
									return null;
								}
							default: return null;
						}
						default: return null;
				}
			}
			else
			{
				var appointment = argument1 as Appointment;
				if (appointment != null)
				{
					switch (searchParamName)
					{
						case "patient":
						case "practitioner":
							foreach (Appointment.ParticipantComponent participant in appointment.Participant)
							{
								// Return the id of the reference
								var index = participant.Actor.Reference.ToLower().IndexOf(searchParamName);
								if (index >= 0)
								{
									if (participant.Actor.Reference.Substring(index + searchParamName.Length + 1) == (string)argument3)
									{
										return true;
									}
								}
							}
							return false;
						default: return null;
					}
				}

				return null;
			}
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
