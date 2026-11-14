/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.DAE.Server
{
	public class ServerScript : ServerChildObject, IServerScript
	{
		internal ServerScript(ServerProcess process, string script, DebugLocator locator) : base()
		{
			_process = process;
			_batches = new ServerBatches();
			_messages = new ParserMessages();
			_sourceContext = new SourceContext(script, locator);
			try
			{
				_script = _process.ParseScript(script, _messages);
			}
			catch (SyntaxException e)
			{
				e.SetLocator(locator);
				throw;
			}
			_messages.SetLocator(locator);
			if ((_script is Block) && (((Block)_script).Statements.Count > 0))
			{
				Block block = (Block)_script;
						
				for (int index = 0; index < block.Statements.Count; index++)
					_batches.Add(new ServerBatch(this, block.Statements[index]));
			}
			else
				_batches.Add(new ServerBatch(this, _script));
		}
		
		protected void UnprepareBatches()
		{
			Exception exception = null;
			while (_batches.Count > 0)
			{
				try
				{
					_batches.DisownAt(0).Dispose();
				}
				catch (Exception E)
				{
					exception = E;
				}			
			}
			if (exception != null)
				throw exception;
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (_batches != null)
				{
					UnprepareBatches();
					_batches.Dispose();
					_batches = null;
				}
			}
			finally
			{
				_script = null;
				_messages = null;
				_process = null;
			
				base.Dispose(disposing);
			}
		}

		private Statement _script;
		
		private SourceContext _sourceContext;
		public SourceContext SourceContext { get { return _sourceContext; } }
		
		// Process        
		private ServerProcess _process;
		public ServerProcess Process { get { return _process; } }
		
		IServerProcess IServerScript.Process { get { return _process; } }
		
		// Used by the ExecuteAsync node to indicate whether the process was implicitly created to run this script
		private bool _shouldCleanupProcess = false;
		public bool ShouldCleanupProcess { get { return _shouldCleanupProcess; } set { _shouldCleanupProcess = value; } }

		// Messages
		private ParserMessages _messages;
		public ParserMessages Messages { get { return _messages; } }
		
		public void CheckParsed()
		{
			if (_messages.HasErrors())
				throw _messages.FirstError;
			// TODO: throw an AggregateException if there is more than 1 error
		}
		
		// Batches
		private ServerBatches _batches; 
		public ServerBatches Batches { get { return _batches; } }

		IServerBatches IServerScript.Batches { get { return _batches; } }
		
		public void Execute(DataParams paramsValue)
		{
			foreach (ServerBatch batch in _batches)
				batch.Execute(paramsValue);
		}
	}
	
	// ServerScripts
	public class ServerScripts : ServerChildObjects
	{		
		protected override void Validate(ServerChildObject objectValue)
		{
			if (!(objectValue is ServerScript))
				throw new ServerException(ServerException.Codes.ServerScriptContainer);
		}
		
		public new ServerScript this[int index]
		{
			get { return (ServerScript)base[index]; } 
			set { base[index] = value; }
		}
	}
}
