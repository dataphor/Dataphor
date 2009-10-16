using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <summary> Implements a read only observable that invokes the change notifications on the main thread. </summary>
	public class DispatchedReadOnlyCollection<T> : ReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		public DispatchedReadOnlyCollection(ObservableCollection<T> AList) : base(AList)
		{
			AList.CollectionChanged += HandleCollectionChanged;
			((INotifyPropertyChanged)AList).PropertyChanged += HandlePropertyChanged;
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private void HandleCollectionChanged(object ASender, NotifyCollectionChangedEventArgs AArgs)
		{
			OnCollectionChanged(AArgs);
		}

		protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs AArgs)
		{
			if (CollectionChanged != null)
				Silverlight.Session.DispatcherInvoke(new NotifyCollectionChangedEventHandler(CollectionChanged), this, AArgs);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void HandlePropertyChanged(object ASender, PropertyChangedEventArgs AArgs)
		{
			OnPropertyChanged(AArgs);
		}

		protected virtual void OnPropertyChanged(PropertyChangedEventArgs AArgs)
		{
			if (PropertyChanged != null)
				Silverlight.Session.DispatcherInvoke(new PropertyChangedEventHandler(PropertyChanged), this, AArgs);
		}
	}

}
