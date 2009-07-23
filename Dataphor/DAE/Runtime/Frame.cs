/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Runtime
{
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
		public const int CInitialCapacity = 0;
		private int FCount;
		public int Count { get { return FCount; } }
		
		private Frame[] FFrames = new Frame[CInitialCapacity];

        private void EnsureCapacity(int ARequiredCapacity)
        {
			if (FFrames.Length <= ARequiredCapacity)
			{
				Frame[] LNewFrames = new Frame[Math.Max(FFrames.Length, 1) * 2];
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
}
