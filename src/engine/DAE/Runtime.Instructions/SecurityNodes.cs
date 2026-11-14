/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System; 
using System.Text;
using System.Collections;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.Catalog;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	/// <remarks> <code>operator DecryptString(const AEncrypted : String) : String; </code>
	///  <para>Note: Decrypt is deterministic and repeatable because it always yields the same result.</para> </remarks>
	public class SystemDecryptStringNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif

			return Schema.SecurityUtility.DecryptString((String)arguments[0]);
		}
    }

	/// <remarks> <code>operator EncryptString(const AUnencrypted : String) : String; </code>
	///  <para>Note: Encrypt is not deterministic or repeatable because it includes a random SALT in the result.</para> </remarks>
	public class SystemEncryptStringNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif

			return Schema.SecurityUtility.EncryptString((string)arguments[0]);
		}
    }
}