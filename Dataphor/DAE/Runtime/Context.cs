/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;

	using Alphora.Dataphor;	
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	public class Frame
	{
		public Frame(int ABase, bool ARowContext)
		{
			Base = ABase;
			RowContext = ARowContext;
		}
		
		public int Base;
		public bool RowContext;
	}
	
	public class FrameList
	{
		public const int CInitialCapacity = 4;
		private int FCount;
		public int Count { get { return FCount; } }
		
		private Frame[] FFrames = new Frame[CInitialCapacity];

        private void EnsureCapacity(int ARequiredCapacity)
        {
			if (FFrames.Length <= ARequiredCapacity)
			{
				Frame[] LNewFrames = new Frame[FFrames.Length * 2];
				for (int LIndex = 0; LIndex < FFrames.Length; LIndex++)
					LNewFrames[LIndex] = FFrames[LIndex];
				FFrames = LNewFrames;
			}
        }
        
        public void Push(Frame AFrame)
        {
			EnsureCapacity(FCount);
			FFrames[FCount] = AFrame;
			FCount++;
        }
        
        public Frame Pop()
        {
			FCount--;
			Frame LResult = FFrames[FCount];
			FFrames[FCount] = null;
			return LResult;
        }
        
        public Frame CurrentFrame { get { return FFrames[FCount - 1]; } }
        
        public int RowBase
        {
			get
			{
				int LIndex;
				for (LIndex = FCount - 1; LIndex >= 0; LIndex--)
					if (!FFrames[LIndex].RowContext)
						break;

				LIndex++;						
				if ((LIndex >= FCount) || (LIndex < 0))
					return -1;
				
				return FFrames[LIndex].Base;
			}
        }
	}
	
	public class StackWindow : System.Object
	{
		public StackWindow(int ABase) : base()
		{
			Base = ABase;
		}
		
		public int Base;
		public int FrameBase { get { return FFrames.CurrentFrame.Base; } }
		public int FrameRowBase { get { return FFrames.RowBase; } }
		
		private FrameList FFrames = new FrameList();

		public void PushFrame(int ACount)
		{
			FFrames.Push(new Frame(ACount, FFrames.Count == 0 ? false : FFrames.CurrentFrame.RowContext));
		}
		
		public void PushFrame(int ACount, bool ARowContext)
		{
			FFrames.Push(new Frame(ACount, ARowContext));
		}
		
		public int PopFrame()
		{
			return FFrames.Pop().Base;
		}
	}
	
	public class StackWindowList
	{
		public const int CInitialCapacity = 4;
		private int FCount;
		private StackWindow[] FStackWindows = new StackWindow[CInitialCapacity];

        private void EnsureCapacity(int ARequiredCapacity)
        {
			if (FStackWindows.Length <= ARequiredCapacity)
			{
				StackWindow[] LNewStackWindows = new StackWindow[FStackWindows.Length * 2];
				for (int LIndex = 0; LIndex < FStackWindows.Length; LIndex++)
					LNewStackWindows[LIndex] = FStackWindows[LIndex];
				FStackWindows = LNewStackWindows;
			}
        }
        
        public void Push(StackWindow AStackWindow)
        {
			EnsureCapacity(FCount);
			FStackWindows[FCount] = AStackWindow;
			FCount++;
        }
        
        public StackWindow Pop()
        {
			FCount--;
			StackWindow LResult = FStackWindows[FCount];
			FStackWindows[FCount] = null;
			return LResult;
        }
        
        public StackWindow CurrentStackWindow { get { return FStackWindows[FCount - 1]; } }
        
        // Returns the highest frame row base in the stack, ragardless of windows
        public int FrameRowBase
        {
			get
			{
				int LFrameRowBase = -1;
				for (int LIndex = 0; LIndex < FCount; LIndex++)
				{
					LFrameRowBase = FStackWindows[LIndex].FrameRowBase;
					if (LFrameRowBase >= 0)
						break;
				}
				return LFrameRowBase;					
			}
        }
	}
	
	public class Context : Disposable
	{
		public const int CInitialCapacity = 4;
		
		public Context() : base()
		{
			MaxStackDepth = Server.CDefaultMaxStackDepth;
			MaxCallDepth = Server.CDefaultMaxCallDepth;
			FWindows.Push(new StackWindow(0));
		}
		
		public Context(int AMaxStackDepth, int AMaxCallDepth) : base()
		{
			MaxStackDepth = AMaxStackDepth;
			MaxCallDepth = AMaxCallDepth;
			FWindows.Push(new StackWindow(0));
		}
		
		private int FCount;
		public int Count { get { return (AllowExtraWindowAccess ? FCount : (FCount - Base)); } }
		public int FrameCount { get { return FCount - FWindows.CurrentStackWindow.FrameBase; } }
		
		public int MaxStackDepth;
		public int MaxCallDepth;
		
		private int FCallDepth;
		public void IncCallDepth()
		{
			if (FCallDepth >= MaxCallDepth)
				throw new RuntimeException(RuntimeException.Codes.CallOverflow, MaxCallDepth);
			FCallDepth++;
		}
		
		public void DecCallDepth()
		{
			FCallDepth--;
		}

		private DataVar[] FStack = new DataVar[CInitialCapacity];
		private StackWindowList FWindows = new StackWindowList();
		private int Base { get { return FWindows.CurrentStackWindow.Base; } }

        private void EnsureCapacity(int ARequiredCapacity)
        {
			if (FStack.Length <= ARequiredCapacity)
			{
				DataVar[] FNewStack = new DataVar[FStack.Length * 2];
				for (int LIndex = 0; LIndex < FStack.Length; LIndex++)
					FNewStack[LIndex] = FStack[LIndex];
				FStack = FNewStack;
			}
        }
        
		public void Push(DataVar ADataVar)
		{
			if (FCount >= MaxStackDepth)
				throw new RuntimeException(RuntimeException.Codes.StackOverflow, MaxStackDepth);
			EnsureCapacity(FCount);
			FStack[FCount] = ADataVar;
			FCount++;
		}
		
		public DataVar Pop()
		{
			#if DEBUG
			if (FCount <= Base)
				throw new RuntimeException(RuntimeException.Codes.StackEmpty);
			#endif

			FCount--;
			DataVar LResult = FStack[FCount];
			FStack[FCount] = null;
			return LResult;
		}
		
		private int FExtraWindowAccess = 0;
		
		public bool AllowExtraWindowAccess
		{
			get { return FExtraWindowAccess > 0; }
			set { if (value) FExtraWindowAccess++; else FExtraWindowAccess--; }
		}
		
		public bool InRowContext { get { return FWindows.CurrentStackWindow.FrameRowBase >= 0; } }
		
		public DataVar Peek(int AOffset)
		{
			#if DEBUG 
			int LIndex = FCount - 1 - AOffset;
			if ((LIndex >= FCount) || (!AllowExtraWindowAccess && (LIndex < Base)))
				throw new RuntimeException(RuntimeException.Codes.InvalidStackIndex, AOffset.ToString());
			return FStack[LIndex];
			#else
			return FStack[FCount - 1 - AOffset]; // same code as the indexer, duplicated for performance
			#endif
		}
		
		public DataVar this[int AIndex] 
		{ 
			get 
			{ 
				#if DEBUG 
				int LIndex = FCount - 1 - AIndex;
				if ((LIndex >= FCount) || (!AllowExtraWindowAccess && (LIndex < Base)))
					throw new RuntimeException(RuntimeException.Codes.InvalidStackIndex, AIndex.ToString());
				return FStack[LIndex];
				#else
				return FStack[FCount - 1 - AIndex]; 
				#endif
			} 
		} // same code as peek, duplicated for performance
		
		public void PushWindow(int ACount)
		{
			FWindows.Push(new StackWindow(FCount - ACount));
		}
		
		public void PopWindow()
		{
			int LBase = FWindows.Pop().Base;
			for (int LIndex = LBase; LIndex < FCount; LIndex++)
				FStack[LIndex] = null;
			FCount = LBase;
		}

		public void PushFrame()
		{
			FWindows.CurrentStackWindow.PushFrame(FCount);
		}
		
		public void PushFrame(bool ARowContext)
		{
			FWindows.CurrentStackWindow.PushFrame(FCount, ARowContext);
		}
		
		public void PopFrame()
		{
			int LBase = FWindows.CurrentStackWindow.PopFrame();
			for (int LIndex = LBase; LIndex < FCount; LIndex++)
				FStack[LIndex] = null;
			FCount = LBase;
		}
		
		public bool IsValidVariableIdentifier(string AIdentifier, StringCollection ANames)
		{
			// Returns true if the given identifier is a valid identifier in the current stack window.
			// If the return value is false, ANames will contain the offending identifier.
			// This only validates top-level variable names.  It is legitimate to declare a row variable
			// that contains a column name that effectively hides a variable further down the stack.
			for (int LIndex = FCount - 1; LIndex >= (AllowExtraWindowAccess ? 0 : Base); LIndex--)
			{
				#if DISALLOWAMBIGUOUSNAMES
				if (Schema.Object.NamesEqual(FStack[LIndex].Name, AIdentifier) || Schema.Object.NamesEqual(AIdentifier, FStack[LIndex].Name))
				{
					ANames.Add(FStack[LIndex].Name);
					return false;
				}
				#else
				if (String.Compare(FStack[LIndex].Name, AIdentifier) == 0)
				{
					ANames.Add(FStack[LIndex].Name);
					return false;
				}
				#endif
				
			}
			return true;
		}
		
		public int ResolveVariableIdentifier(string AIdentifier, out int AColumnIndex, StringCollection ANames)
		{
			AColumnIndex = -1;
			int LVariableIndex = -1;
			int LRowBase = AllowExtraWindowAccess ? FWindows.FrameRowBase : FWindows.CurrentStackWindow.FrameRowBase;
			if (LRowBase < 0)
				LRowBase = FCount;
			for (int LIndex = FCount - 1; LIndex >= (AllowExtraWindowAccess ? 0 : Base); LIndex--)
			{
				// if it's a row type check each of the columns
				if ((LIndex >= LRowBase) && (FStack[LIndex].DataType is Schema.RowType))
				{
					AColumnIndex = ((Schema.RowType)FStack[LIndex].DataType).Columns.IndexOf(AIdentifier, ANames);
					if (AColumnIndex >= 0)
					{
						LVariableIndex = FCount - 1 - LIndex;
						break;
					}
					else
						if (ANames.Count > 0)
							break;
				}

				// check the object itself
				if (Schema.Object.NamesEqual(FStack[LIndex].Name, AIdentifier))
				{
					if (LVariableIndex >= 0)
					{
						ANames.Add(this[LVariableIndex].Name);
						ANames.Add(FStack[LIndex].Name);
						LVariableIndex = -1;
						break;
					}
					LVariableIndex = FCount - 1 - LIndex;

					// If AllowExtraWindowAccess is true, we are binding a known good aggregate call, so allow variable hiding.
					if (AllowExtraWindowAccess)
						break;
				}
			}

			return LVariableIndex;
		}
		
		public bool HasCursorTypeVariables()
		{
			for (int LIndex = FCount - 1; LIndex >= (AllowExtraWindowAccess ? 0 : Base); LIndex--)
				if (FStack[LIndex].DataType is Schema.ICursorType)
					return true;
			return false;
		}
		
		protected DataVar FErrorVar;
		public DataVar ErrorVar
		{
			get { return FErrorVar; }
			set { FErrorVar = value; }
		}
	}
}

