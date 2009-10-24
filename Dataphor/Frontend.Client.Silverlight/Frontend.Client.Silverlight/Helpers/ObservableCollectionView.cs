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

		public bool Contains(object AItem)
		{
			return AItem is T && base.Contains((T)AItem);
		}

		private System.Globalization.CultureInfo FCulture;
		
		public System.Globalization.CultureInfo Culture
		{
			get { return FCulture; }
			set
			{
				if (FCulture != value)
				{
					FCulture = value;
					OnPropertyChanged(new PropertyChangedEventArgs("Culture"));
				}
			}
		}

		private object FCurrentItem;
		
		public object CurrentItem
		{
			get { return FCurrentItem; }
		}

		private int FCurrentPosition;
		
		public int CurrentPosition
		{
			get { return FCurrentPosition; }
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
		
		private bool IsInView(int AIndex)
		{
			return AIndex >= 0 && AIndex < Count;
		}

		public bool IsEmpty
		{
			get { return Count == 0; }
		}

		public bool MoveCurrentTo(object AItem)
		{
			if (AItem is T && AItem != null)
			{
				if (Object.Equals(AItem, CurrentItem))
					return true;
				return MoveCurrentToPosition(IndexOf((T)AItem));
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

		public bool MoveCurrentToPosition(int APosition)
		{
			if ((APosition < -1) || (APosition > Count)) 
				throw new ArgumentOutOfRangeException("APosition");
			
			if ((APosition != CurrentPosition || !IsCurrentInSync) && OnCurrentChanging()) 
			{ 
				var LOldIsCurrentAfterLast = IsCurrentAfterLast; 
				var LOldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

				if (APosition < 0)
				{
					FCurrentItem = null;
					FCurrentPosition = -1;
				}
				else if (APosition >= Count)
				{
					FCurrentItem = null;
					FCurrentPosition = Count;
				}
				else
				{
					FCurrentItem = base[APosition];
					FCurrentPosition = APosition;
				}

				OnCurrentChanged(); 
				if (IsCurrentAfterLast != LOldIsCurrentAfterLast)
					OnPropertyChanged(new PropertyChangedEventArgs("IsCurrentAfterLast"));
				if (IsCurrentBeforeFirst != LOldIsCurrentBeforeFirst) 
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
			var LArgs = new CurrentChangingEventArgs();
			if (CurrentChanging != null)
				CurrentChanging(this, LArgs);
			return !LArgs.Cancel;
		}

		protected override void ClearItems()
		{
			MoveCurrentToPosition(-1);
			base.ClearItems();
		}

		protected override void RemoveItem(int AIndex)
		{
			base.RemoveItem(AIndex);
			if (AIndex == FCurrentPosition)
				if (AIndex > 0)
					MoveCurrentToPosition(AIndex - 1);
				else
					MoveCurrentToPosition(AIndex);
		}
		
		private class DeferTarget : IDisposable
		{
			internal DeferTarget(ObservableCollectionView<T> ASource)
			{
				FSource = ASource;
				FSource.FDeferCount++;
			}

			public void Dispose()
			{
				FSource.FDeferCount--;
				if (FSource.FDeferCount == 0 && FSource.FDeferredRefresh)
				{
					FSource.FDeferredRefresh = false;
					FSource.InternalRefresh();
				}
			}
			
			private ObservableCollectionView<T> FSource;
		}
		
		private bool FDeferredRefresh;
		private int FDeferCount;
		
		public IDisposable DeferRefresh()
		{
			return new DeferTarget(this);
		}

		public void Refresh()
		{
			if (FDeferCount > 0)
				FDeferredRefresh = true;
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
