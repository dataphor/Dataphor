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
	
	public abstract class Operation : Disposable
	{
		public Operation(TableVar ATableVar) : base()
		{
			FTableVar = ATableVar;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			FTableVar = null;
			base.Dispose(ADisposing);
		}
		
		private TableVar FTableVar;
		public TableVar TableVar { get { return FTableVar; } }
	}
	
	public class Operations : TypedList
	{
		public Operations() : base(typeof(Operation)){}
		
		public new Operation this[int AIndex]
		{
			get { return (Operation)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class InsertOperation : Operation
	{
		public InsertOperation(TableVar ATableVar, Row ARow, BitArray AValueFlags) : base(ATableVar)
		{
			FRow = ARow;
			FValueFlags = AValueFlags;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FRow != null)
			{
				FRow.Dispose();
				FRow = null;
			}

			base.Dispose(ADisposing);
		}
		
		private Row FRow;
		public Row Row { get { return FRow; } }
		
		private BitArray FValueFlags;
		public BitArray ValueFlags { get { return FValueFlags; } }
	}
	
	public class UpdateOperation : Operation
	{
		public UpdateOperation(TableVar ATableVar, Row AOldRow, Row ANewRow, BitArray AValueFlags) : base(ATableVar)
		{
			FOldRow = AOldRow;
			FNewRow = ANewRow;
			FValueFlags = AValueFlags;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FOldRow != null)
			{
				FOldRow.Dispose();
				FOldRow = null;
			}
			
			if (FNewRow != null)
			{
				FNewRow.Dispose();
				FNewRow = null;
			}
			
			base.Dispose(ADisposing);
		}
		
		private Row FOldRow;
		public Row OldRow { get { return FOldRow; } }
		
		private Row FNewRow; 
		public Row NewRow { get { return FNewRow; } }
		
		private BitArray FValueFlags;
		public BitArray ValueFlags { get { return FValueFlags; } }
	}
	
	public class DeleteOperation : Operation
	{
		public DeleteOperation(TableVar ATableVar, Row ARow) : base(ATableVar)
		{
			FRow = ARow;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FRow != null)
			{
				FRow.Dispose();
				FRow = null;
			}

			base.Dispose(ADisposing);
		}
		
		private Row FRow;
		public Row Row { get { return FRow; } }
	}
}
