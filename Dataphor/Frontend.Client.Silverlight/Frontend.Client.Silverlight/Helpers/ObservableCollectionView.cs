using System;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <summary> Observable collection which also implements ICollectionView. </summary>
	/// <remarks> Props to http://weblogs.asp.net/manishdalal/archive/2008/12/30/silverlight-datagrid-custom-sorting.aspx </remarks>
	public class ObservableCollectionView<T> : ObservableCollection<T>, ICollectionView
	{
		public bool CanFilter
		{
			get { return false; }
		}

		public bool CanGroup
		{
			get { return false; }
		}

		public bool CanSort
		{
			get { return false; }
		}

		public bool Contains(object item)
		{
			return item is T && base.Contains((T)item);
		}

		private System.Globalization.CultureInfo _culture;
		
		public System.Globalization.CultureInfo Culture
		{
			get { return _culture; }
			set
			{
				if (_culture != value)
				{
					_culture = value;
					OnPropertyChanged(new PropertyChangedEventArgs("Culture"));
				}
			}
		}

		private object _currentItem;
		
		public object CurrentItem
		{
			get { return _currentItem; }
		}

		private int _currentPosition;
		
		public int CurrentPosition
		{
			get { return _currentPosition; }
		}

		public Predicate<object> Filter
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public ObservableCollection<GroupDescription> GroupDescriptions
		{
			get { throw new NotImplementedException(); }
		}

		public ReadOnlyObservableCollection<object> Groups
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsCurrentAfterLast
		{
			get 
			{ 
				if (!IsEmpty)
					return CurrentPosition >= Count;
				else
					return true; 
			}
		}

		public bool IsCurrentBeforeFirst
		{
			get
			{
				if (!IsEmpty)
					return CurrentPosition < 0;
				else
					return true; 
			}
		}
		
		protected bool IsCurrentInSync
		{
			get
			{
				if (IsInView(CurrentPosition))
					return Object.Equals(base[CurrentPosition], CurrentItem);
				else
					return CurrentItem == null;
			}
		}
		
		private bool IsInView(int index)
		{
			return index >= 0 && index < Count;
		}

		public bool IsEmpty
		{
			get { return Count == 0; }
		}

		public bool MoveCurrentTo(object item)
		{
			if (item is T && item != null)
			{
				if (Object.Equals(item, CurrentItem))
					return true;
				return MoveCurrentToPosition(IndexOf((T)item));
			}
			else
				return false;
		}

		public bool MoveCurrentToFirst()
		{
			return MoveCurrentToPosition(0);
		}

		public bool MoveCurrentToLast()
		{
			return MoveCurrentToPosition(Count - 1);
		}

		public bool MoveCurrentToNext()
		{
			return CurrentPosition < Count && MoveCurrentToPosition(CurrentPosition + 1);
		}

		public bool MoveCurrentToPrevious()
		{
			return CurrentPosition >= 0 && MoveCurrentToPosition(CurrentPosition - 1);
		}

		public bool MoveCurrentToPosition(int position)
		{
			if ((position < -1) || (position > Count)) 
				throw new ArgumentOutOfRangeException("APosition");
			
			if ((position != CurrentPosition || !IsCurrentInSync) && OnCurrentChanging()) 
			{ 
				var oldIsCurrentAfterLast = IsCurrentAfterLast; 
				var oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

				if (position < 0)
				{
					_currentItem = null;
					_currentPosition = -1;
				}
				else if (position >= Count)
				{
					_currentItem = null;
					_currentPosition = Count;
				}
				else
				{
					_currentItem = base[position];
					_currentPosition = position;
				}

				OnCurrentChanged(); 
				if (IsCurrentAfterLast != oldIsCurrentAfterLast)
					OnPropertyChanged(new PropertyChangedEventArgs("IsCurrentAfterLast"));
				if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst) 
					OnPropertyChanged(new PropertyChangedEventArgs("IsCurrentBeforeFirst"));
				OnPropertyChanged(new PropertyChangedEventArgs("CurrentPosition"));
				OnPropertyChanged(new PropertyChangedEventArgs("CurrentItem"));

				return IsInView(CurrentPosition);
			}
			else
				return false;
		}

		public event EventHandler CurrentChanged;

		protected virtual void OnCurrentChanged()
		{
			if (CurrentChanged != null)
				CurrentChanged(this, EventArgs.Empty);
		}

		public event CurrentChangingEventHandler CurrentChanging;

		protected virtual bool OnCurrentChanging()
		{
			var args = new CurrentChangingEventArgs();
			if (CurrentChanging != null)
				CurrentChanging(this, args);
			return !args.Cancel;
		}

		protected override void ClearItems()
		{
			MoveCurrentToPosition(-1);
			base.ClearItems();
		}

		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);
			if (index == _currentPosition)
				if (index > 0)
					MoveCurrentToPosition(index - 1);
				else
					MoveCurrentToPosition(index);
		}
		
		private class DeferTarget : IDisposable
		{
			internal DeferTarget(ObservableCollectionView<T> source)
			{
				_source = source;
				_source._deferCount++;
			}

			public void Dispose()
			{
				_source._deferCount--;
				if (_source._deferCount == 0 && _source._deferredRefresh)
				{
					_source._deferredRefresh = false;
					_source.InternalRefresh();
				}
			}
			
			private ObservableCollectionView<T> _source;
		}
		
		private bool _deferredRefresh;
		private int _deferCount;
		
		public IDisposable DeferRefresh()
		{
			return new DeferTarget(this);
		}

		public void Refresh()
		{
			if (_deferCount > 0)
				_deferredRefresh = true;
			else
				InternalRefresh();
		}

		protected virtual void InternalRefresh()
		{
			// pure virtual
		}

		public SortDescriptionCollection SortDescriptions
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumerable SourceCollection
		{
			get { throw new NotImplementedException(); }
		}
	}
}
