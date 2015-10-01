/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using System;
	using System.Collections;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Schema;
using System.Collections.Generic;
	
	public abstract class Operation : Disposable
	{
		public Operation(TableVar tableVar) : base()
		{
			_tableVar = tableVar;
		}
		
		protected override void Dispose(bool disposing)
		{
			_tableVar = null;
			base.Dispose(disposing);
		}
		
		private TableVar _tableVar;
		public TableVar TableVar { get { return _tableVar; } }
	}

	#if USETYPEDLIST	
	public class Operations : TypedList
	{
		public Operations() : base(typeof(Operation)){}
		
		public new Operation this[int AIndex]
		{
			get { return (Operation)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	#else
	public class Operations : BaseList<Operation> { }
	#endif
	
	public class InsertOperation : Operation
	{
		public InsertOperation(TableVar tableVar, IRow row, BitArray valueFlags) : base(tableVar)
		{
			_row = row;
			_valueFlags = valueFlags;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_row != null)
			{
				_row.Dispose();
				_row = null;
			}

			base.Dispose(disposing);
		}
		
		private IRow _row;
		public IRow Row { get { return _row; } }
		
		private BitArray _valueFlags;
		public BitArray ValueFlags { get { return _valueFlags; } }
	}
	
	public class UpdateOperation : Operation
	{
		public UpdateOperation(TableVar tableVar, IRow oldRow, IRow newRow, BitArray valueFlags) : base(tableVar)
		{
			_oldRow = oldRow;
			_newRow = newRow;
			_valueFlags = valueFlags;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_oldRow != null)
			{
				_oldRow.Dispose();
				_oldRow = null;
			}
			
			if (_newRow != null)
			{
				_newRow.Dispose();
				_newRow = null;
			}
			
			base.Dispose(disposing);
		}
		
		private IRow _oldRow;
		public IRow OldRow { get { return _oldRow; } }
		
		private IRow _newRow; 
		public IRow NewRow { get { return _newRow; } }
		
		private BitArray _valueFlags;
		public BitArray ValueFlags { get { return _valueFlags; } }
	}
	
	public class DeleteOperation : Operation
	{
		public DeleteOperation(TableVar tableVar, IRow row) : base(tableVar)
		{
			_row = row;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_row != null)
			{
				_row.Dispose();
				_row = null;
			}

			base.Dispose(disposing);
		}
		
		private IRow _row;
		public IRow Row { get { return _row; } }
	}
}
