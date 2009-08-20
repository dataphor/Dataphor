/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Runtime
{
	public class Stack<T> : Object
	{
		public const int CInitialCapacity = 0;
		
		public Stack() : this(Server.Server.CDefaultMaxStackDepth, Server.Server.CDefaultMaxCallDepth) { }
		public Stack(int AMaxStackDepth, int AMaxCallDepth) : base()
		{
			MaxStackDepth = AMaxStackDepth;
			MaxCallDepth = AMaxCallDepth;
			FWindows.Push(new StackWindow(0, null, null));
		}
		
		protected int FCount;
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

		protected T[] FStack = new T[CInitialCapacity];
		protected StackWindowList FWindows = new StackWindowList();
		protected int Base { get { return FWindows.CurrentStackWindow.Base; } }

        private void EnsureCapacity(int ARequiredCapacity)
        {
			if (FStack.Length <= ARequiredCapacity)
			{
				T[] FNewStack = new T[Math.Max(FStack.Length, 1) * 2];
				for (int LIndex = 0; LIndex < FStack.Length; LIndex++)
					FNewStack[LIndex] = FStack[LIndex];
				FStack = FNewStack;
			}
        }
        
		public void Push(T AItem)
		{
			if (FCount >= MaxStackDepth)
				throw new RuntimeException(RuntimeException.Codes.StackOverflow, MaxStackDepth);
			EnsureCapacity(FCount);
			FStack[FCount] = AItem;
			FCount++;
		}
		
		public T Pop()
		{
			#if DEBUG
			if (FCount <= Base)
				throw new RuntimeException(RuntimeException.Codes.StackEmpty);
			#endif

			FCount--;
			T LResult = FStack[FCount];
			FStack[FCount] = default(T);
			return LResult;
		}
		
		private int FExtraWindowAccess = 0;
		
		public bool AllowExtraWindowAccess
		{
			get { return FExtraWindowAccess > 0; }
			set { if (value) FExtraWindowAccess++; else FExtraWindowAccess--; }
		}
		
		public bool InRowContext { get { return FWindows.CurrentStackWindow.FrameRowBase >= 0; } }
		
		public T Peek(int AOffset)
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
		
		public void Poke(int AOffset, T AItem)
		{
			#if DEBUG 
			int LIndex = FCount - 1 - AOffset;
			if ((LIndex >= FCount) || (!AllowExtraWindowAccess && (LIndex < Base)))
				throw new RuntimeException(RuntimeException.Codes.InvalidStackIndex, AOffset.ToString());
			FStack[LIndex] = AItem;
			#else
			FStack[FCount - 1 - AOffset] = AItem; // same code as the indexer, duplicated for performance
			#endif
		}
		
		public T this[int AIndex] 
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
			set
			{
				#if DEBUG 
				int LIndex = FCount - 1 - AIndex;
				if ((LIndex >= FCount) || (!AllowExtraWindowAccess && (LIndex < Base)))
					throw new RuntimeException(RuntimeException.Codes.InvalidStackIndex, AIndex.ToString());
				FStack[LIndex] = value;
				#else
				FStack[FCount - 1 - AIndex] = value; 
				#endif
			}
		} // same code as peek and poke, duplicated for performance
		
		public void PushWindow(int ACount, ServerPlan APlan, PlanNode AOriginator)
		{
			FWindows.Push(new StackWindow(FCount - ACount, APlan, AOriginator));
		}
		
		public void PushWindow(int ACount)
		{
			PushWindow(ACount, null, null);
		}
		
		public void PopWindow()
		{
			int LBase = FWindows.Pop().Base;
			for (int LIndex = LBase; LIndex < FCount; LIndex++)
				FStack[LIndex] = default(T);
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
				FStack[LIndex] = default(T);
			FCount = LBase;
		}
		
		public StackWindow CurrentStackWindow
		{
			get
			{
				return FWindows.CurrentStackWindow;
			}
		}
		
		public List<StackWindow> GetCallStack()
		{
			return FWindows.GetCallStack();
		}
		
		public object[] GetStack(int AWindowIndex)
		{
			int LBase = FWindows[AWindowIndex].Base;
			int LCeiling = AWindowIndex >= FWindows.Count ? FCount : FWindows[AWindowIndex + 1].Base;
			
			object[] LResult = new object[LCeiling - LBase];
			for (int LIndex = LBase; LIndex < LCeiling; LIndex++)
				LResult[LResult.Length - 1 - LIndex] = FStack[LIndex];
			return LResult;
		}
	}
}
