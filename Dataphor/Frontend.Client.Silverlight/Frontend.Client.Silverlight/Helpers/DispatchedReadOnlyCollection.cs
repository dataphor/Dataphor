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
		public DispatchedReadOnlyCollection(ObservableCollection<T> list) : base(list)
		{
			list.CollectionChanged += HandleCollectionChanged;
			((INotifyPropertyChanged)list).PropertyChanged += HandlePropertyChanged;
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			OnCollectionChanged(args);
		}

		protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
		{
			if (CollectionChanged != null)
				Silverlight.Session.DispatcherInvoke(new NotifyCollectionChangedEventHandler(CollectionChanged), this, args);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void HandlePropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			OnPropertyChanged(args);
		}

		protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
		{
			if (PropertyChanged != null)
				Silverlight.Session.DispatcherInvoke(new PropertyChangedEventHandler(PropertyChanged), this, args);
		}
	}

}
