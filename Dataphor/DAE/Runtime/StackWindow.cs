/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Runtime
{
	public class StackWindow : System.Object
	{
		public StackWindow(int ABase, PlanNode AOriginator) : base()
		{
			Base = ABase;
			Originator = AOriginator;
		}
		
		public PlanNode Originator;
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
		public const int CInitialCapacity = 0;
		private int FCount;
		private StackWindow[] FStackWindows = new StackWindow[CInitialCapacity];

        private void EnsureCapacity(int ARequiredCapacity)
        {
			if (FStackWindows.Length <= ARequiredCapacity)
			{
				StackWindow[] LNewStackWindows = new StackWindow[Math.Max(FStackWindows.Length, 1) * 2];
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
        
        /// <summary>
        /// Returns the current call stack.
        /// </summary>
		/// <remarks>
		/// This method is not thread safe, synchronization is the responsibility of the caller.
		/// This method returns the actual stack windows. The method is expected to be used read
		/// only, but a copy is not taken for performance reasons. Modifications to these structures
		/// will corrupt the stack of the running process.
		/// </remarks>
        public List<StackWindow> GetCallStack()
        {
			List<StackWindow> LResult = new List<StackWindow>();
			for (int LIndex = 0; LIndex < FCount; LIndex++)
				LResult.Add(FStackWindows[LIndex]);
			return LResult;
        }
	}
}
