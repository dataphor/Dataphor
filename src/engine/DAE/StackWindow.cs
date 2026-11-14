/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE
{
	public class StackWindow : System.Object
	{
		public StackWindow(int baseValue) : base()
		{
			Base = baseValue;
		}
		
		public int Base;
		public int FrameBase { get { return _frames.CurrentFrame.Base; } }
		public int FrameRowBase { get { return _frames.RowBase; } }
		
		private FrameList _frames = new FrameList();

		public void PushFrame(int count)
		{
			_frames.Push(new Frame(count, _frames.Count == 0 ? false : _frames.CurrentFrame.RowContext));
		}
		
		public void PushFrame(int count, bool rowContext)
		{
			_frames.Push(new Frame(count, rowContext));
		}
		
		public int PopFrame()
		{
			return _frames.Pop().Base;
		}
	}
	
	public class StackWindowList
	{
		public const int InitialCapacity = 0;
		public const int DefaultMaxCallDepth = 100;
		
		public StackWindowList() : this(DefaultMaxCallDepth) { }
		public StackWindowList(int maxCallDepth) : base()
		{
			_maxCallDepth = maxCallDepth;
		}
		
		private StackWindow[] _stackWindows = new StackWindow[InitialCapacity];
		
		private int _count;
		public int Count { get { return _count; } }

		private int _maxCallDepth;
		public int MaxCallDepth
		{
			get { return _maxCallDepth; }
			set
			{
				if (_count > value)
					throw new BaseException(BaseException.Codes.CallDepthExceedsNewSetting, _count, value);
					
				_maxCallDepth = value;
			}
		}

        private void EnsureCapacity(int requiredCapacity)
        {
			if (_stackWindows.Length <= requiredCapacity)
			{
				StackWindow[] newStackWindows = new StackWindow[Math.Max(_stackWindows.Length, 1) * 2];
				for (int index = 0; index < _stackWindows.Length; index++)
					newStackWindows[index] = _stackWindows[index];
				_stackWindows = newStackWindows;
			}
        }
        
        public void Push(StackWindow stackWindow)
        {
			if (_count >= _maxCallDepth)
				throw new BaseException(BaseException.Codes.CallStackOverflow, _maxCallDepth);
				
			EnsureCapacity(_count);
			_stackWindows[_count] = stackWindow;
			_count++;
        }
        
        public StackWindow Pop()
        {
			_count--;
			StackWindow result = _stackWindows[_count];
			_stackWindows[_count] = null;
			return result;
        }
        
        public StackWindow CurrentStackWindow { get { return _stackWindows[_count - 1]; } }
        
        // Returns the highest frame row base in the stack, ragardless of windows
        public int FrameRowBase
        {
			get
			{
				int frameRowBase = -1;
				for (int index = 0; index < _count; index++)
				{
					frameRowBase = _stackWindows[index].FrameRowBase;
					if (frameRowBase >= 0)
						break;
				}
				return frameRowBase;					
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
			List<StackWindow> result = new List<StackWindow>();
			for (int index = _count - 1; index >= 0; index--)
				result.Add(_stackWindows[index]);
			return result;
        }
        
        public StackWindow this[int index] { get { return _stackWindows[_count - index - 1]; } }
	}
}
