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
		public const int InitialCapacity = 0;
		public const int DefaultMaxStackDepth = 32767;
		
		public Stack() : this(DefaultMaxStackDepth, StackWindowList.DefaultMaxCallDepth) { }
		public Stack(int maxStackDepth, int maxCallDepth) : base()
		{
			_maxStackDepth = maxStackDepth;
			_windows = new StackWindowList(maxCallDepth);
		}
		
		protected int _count;
		public int Count { get { return (AllowExtraWindowAccess ? _count : (_count - Base)); } }
		public int FrameCount { get { return _count - _windows.CurrentStackWindow.FrameBase; } }
		
		private int _maxStackDepth;
		public int MaxStackDepth
		{
			get { return _maxStackDepth; }
			set
			{
				if (value < _count)
					throw new BaseException(BaseException.Codes.StackDepthExceedsNewSetting, _count, value);

				_maxStackDepth = value;
			}
		}
		
		public int MaxCallDepth
		{
			get { return _windows.MaxCallDepth; }
			set { _windows.MaxCallDepth = value; }
		}
		
		public int CallDepth { get { return _windows.Count; } }
		
		protected T[] _stack = new T[InitialCapacity];
		protected StackWindowList _windows;
		protected int Base { get { return _windows.CurrentStackWindow.Base; } }

        private void EnsureCapacity(int requiredCapacity)
        {
			if (_stack.Length <= requiredCapacity)
			{
				T[] FNewStack = new T[Math.Max(_stack.Length, 1) * 2];
				for (int index = 0; index < _stack.Length; index++)
					FNewStack[index] = _stack[index];
				_stack = FNewStack;
			}
        }
        
		public void Push(T item)
		{
			if (_count >= MaxStackDepth)
				throw new BaseException(BaseException.Codes.StackOverflow, MaxStackDepth);
			EnsureCapacity(_count);
			_stack[_count] = item;
			_count++;
		}
		
		public T Pop()
		{
			#if DEBUG
			if (_count <= Base)
				throw new BaseException(BaseException.Codes.StackEmpty);
			#endif

			_count--;
			T result = _stack[_count];
			_stack[_count] = default(T);
			return result;
		}
		
		private int _extraWindowAccess = 0;
		
		public bool AllowExtraWindowAccess
		{
			get { return _extraWindowAccess > 0; }
			set { if (value) _extraWindowAccess++; else _extraWindowAccess--; }
		}
		
		public bool InRowContext { get { return _windows.CurrentStackWindow.FrameRowBase >= 0; } }
		
		public T Peek(int offset)
		{
			#if DEBUG 
			int index = _count - 1 - offset;
			if ((index >= _count) || (!AllowExtraWindowAccess && (index < Base)))
				throw new BaseException(BaseException.Codes.InvalidStackIndex, offset.ToString());
			return _stack[index];
			#else
			return _stack[_count - 1 - offset]; // same code as the indexer, duplicated for performance
			#endif
		}
		
		public void Poke(int offset, T item)
		{
			#if DEBUG 
			int index = _count - 1 - offset;
			if ((index >= _count) || (!AllowExtraWindowAccess && (index < Base)))
				throw new BaseException(BaseException.Codes.InvalidStackIndex, offset.ToString());
			_stack[index] = item;
			#else
			_stack[_count - 1 - offset] = item; // same code as the indexer, duplicated for performance
			#endif
		}
		
		public T this[int index] 
		{ 
			get 
			{ 
				#if DEBUG 
				int localIndex = _count - 1 - index;
				if ((localIndex >= _count) || (!AllowExtraWindowAccess && (localIndex < Base)))
					throw new BaseException(BaseException.Codes.InvalidStackIndex, index.ToString());
				return _stack[localIndex];
				#else
				return _stack[_count - 1 - index]; 
				#endif
			} 
			set
			{
				#if DEBUG 
				int localIndex = _count - 1 - index;
				if ((localIndex >= _count) || (!AllowExtraWindowAccess && (localIndex < Base)))
					throw new BaseException(BaseException.Codes.InvalidStackIndex, index.ToString());
				_stack[localIndex] = value;
				#else
				_stack[_count - 1 - index] = value; 
				#endif
			}
		} // same code as peek and poke, duplicated for performance
		
		public virtual void PushWindow(int count)
		{
			_windows.Push(new StackWindow(_count - count));
		}
		
		public void PopWindow()
		{
			int baseValue = _windows.Pop().Base;
			for (int index = baseValue; index < _count; index++)
				_stack[index] = default(T);
			_count = baseValue;
		}

		public void PushFrame()
		{
			_windows.CurrentStackWindow.PushFrame(_count);
		}
		
		public void PushFrame(bool rowContext)
		{
			_windows.CurrentStackWindow.PushFrame(_count, rowContext);
		}
		
		public void PopFrame()
		{
			int baseValue = _windows.CurrentStackWindow.PopFrame();
			for (int index = baseValue; index < _count; index++)
				_stack[index] = default(T);
			_count = baseValue;
		}
		
		public StackWindow CurrentStackWindow
		{
			get
			{
				return _windows.CurrentStackWindow;
			}
		}
		
		public List<StackWindow> GetCallStack()
		{
			return _windows.GetCallStack();
		}
		
		public object[] GetStack(int windowIndex)
		{
			int baseValue = _windows[windowIndex].Base;
			int ceiling = windowIndex == 0 ? _count : _windows[windowIndex - 1].Base;
			
			object[] result = new object[ceiling - baseValue];
			for (int index = baseValue; index < ceiling; index++)
				result[result.Length - 1 - (index - baseValue)] = _stack[index];
			return result;
		}
	}
}
