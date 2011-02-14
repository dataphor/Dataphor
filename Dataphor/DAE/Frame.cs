/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

namespace Alphora.Dataphor.DAE
{
	public class Frame
	{
		public Frame(int baseValue, bool rowContext)
		{
			Base = baseValue;
			RowContext = rowContext;
		}
		
		public int Base;
		public bool RowContext;
	}
	
	public class FrameList
	{
		public const int InitialCapacity = 0;
		private int _count;
		public int Count { get { return _count; } }
		
		private Frame[] _frames = new Frame[InitialCapacity];

        private void EnsureCapacity(int requiredCapacity)
        {
			if (_frames.Length <= requiredCapacity)
			{
				Frame[] newFrames = new Frame[Math.Max(_frames.Length, 1) * 2];
				for (int index = 0; index < _frames.Length; index++)
					newFrames[index] = _frames[index];
				_frames = newFrames;
			}
        }
        
        public void Push(Frame frame)
        {
			EnsureCapacity(_count);
			_frames[_count] = frame;
			_count++;
        }
        
        public Frame Pop()
        {
			_count--;
			Frame result = _frames[_count];
			_frames[_count] = null;
			return result;
        }
        
        public Frame CurrentFrame { get { return _frames[_count - 1]; } }
        
        public int RowBase
        {
			get
			{
				int index;
				for (index = _count - 1; index >= 0; index--)
					if (!_frames[index].RowContext)
						break;

				index++;						
				if ((index >= _count) || (index < 0))
					return -1;
				
				return _frames[index].Base;
			}
        }
	}
}
