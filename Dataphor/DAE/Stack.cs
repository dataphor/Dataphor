/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE
{
	public class Stack<T> : Object
	{
		public const int CInitialCapacity = 0;
		public const int CDefaultMaxStackDepth = 32767;
		
		public Stack() : this(CDefaultMaxStackDepth, StackWindowList.CDefaultMaxCallDepth) { }
		public Stack(int AMaxStackDepth, int AMaxCallDepth) : base()
		{
			FMaxStackDepth = AMaxStackDepth;
			FWindows = new StackWindowList(AMaxCallDepth);
		}
		
		protected int FCount;
		public int Count { get { return (AllowExtraWindowAccess ? FCount : (FCount - Base)); } }
		public int FrameCount { get { return FCount - FWindows.CurrentStackWindow.FrameBase; } }
		
		private int FMaxStackDepth;
		public int MaxStackDepth
		{
			get { return FMaxStackDepth; }
			set
			{
				if (value < FCount)
					throw new BaseException(BaseException.Codes.StackDepthExceedsNewSetting, FCount, value);

				FMaxStackDepth = value;
			}
		}
		
		public int MaxCallDepth
		{
			get { return FWindows.MaxCallDepth; }
			set { FWindows.MaxCallDepth = value; }
		}
		
		public int CallDepth { get { return FWindows.Count; } }
		
		protected T[] FStack = new T[CInitialCapacity];
		protected StackWindowList FWindows;
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
				throw new BaseException(BaseException.Codes.StackOverflow, MaxStackDepth);
			EnsureCapacity(FCount);
			FStack[FCount] = AItem;
			FCount++;
		}
		
		public T Pop()
		{
			#if DEBUG
			if (FCount <= Base)
				throw new BaseException(BaseException.Codes.StackEmpty);
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
				throw new BaseException(BaseException.Codes.InvalidStackIndex, AOffset.ToString());
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
				throw new BaseException(BaseException.Codes.InvalidStackIndex, AOffset.ToString());
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
					throw new BaseException(BaseException.Codes.InvalidStackIndex, AIndex.ToString());
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
					throw new BaseException(BaseException.Codes.InvalidStackIndex, AIndex.ToString());
				FStack[LIndex] = value;
				#else
				FStack[FCount - 1 - AIndex] = value; 
				#endif
			}
		} // same code as peek and poke, duplicated for performance
		
		public virtual void PushWindow(int ACount)
		{
			FWindows.Push(new StackWindow(FCount - ACount));
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
			int LCeiling = AWindowIndex == 0 ? FCount : FWindows[AWindowIndex - 1].Base;
			
			object[] LResult = new object[LCeiling - LBase];
			for (int LIndex = LBase; LIndex < LCeiling; LIndex++)
				LResult[LResult.Length - 1 - (LIndex - LBase)] = FStack[LIndex];
			return LResult;
		}
	}
}
