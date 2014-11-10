/*
	Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alphora.Dataphor.Dataphoria.Coordination.Common;

namespace Alphora.Dataphor.Dataphoria.Coordination
{
	public class ActiveNodeList : ValidatingBaseList<NodeDescriptor>
	{
		private int _currentIndex = -1;
		public int CurrentIndex { get { return _currentIndex; } }

		public NodeDescriptor CurrentNode { get { return _currentIndex >= 0 ? this[_currentIndex] : null; } }

		public NodeDescriptor MoveNext()
		{
			_currentIndex++;
			if (_currentIndex >= Count)
			{
				_currentIndex = Count == 0 ? -1 : 0;
			}

			return CurrentNode;
		}

		protected override void Adding(NodeDescriptor tempValue, int index)
		{
			base.Adding(tempValue, index);

			if (_currentIndex < 0)
			{
				_currentIndex = index;
			}
		}

		protected override void Removed(NodeDescriptor tempValue, int index)
		{
			base.Removed(tempValue, index);

			if (index == _currentIndex && index >= Count)
			{
				_currentIndex = Count - 1;
			}
		}
	}
}
