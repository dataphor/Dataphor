using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor
{
	public class DisposableList<T> : ValidatingBaseList<T>, IDisposable, IDisposableNotify where T : IDisposable
	{
		public DisposableList() : base() 
		{ 
			_itemsOwned = true;
		}
		
		public DisposableList(int capacity) : base(capacity) 
		{ 
			_itemsOwned = true;
		}

		public DisposableList(bool itemsOwned) : base()
		{
			_itemsOwned = itemsOwned;
		}
		
		public DisposableList(bool itemsOwned, int capacity) : base(capacity)
		{
			_itemsOwned = itemsOwned;
		}
		
		#if USEFINALIZER
		~DisposableList()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose();
			#endif
		}
		#endif

		protected bool _itemsOwned;
		/// <summary>Determines whether or not the list "owns" the items it contains.<summary>
		/// <remarks>
		///		ItemsOwned controls whether or not the List "owns" the contained items.  
		///		"Owns" means that if the item supports the IDisposable interface, the will be
		///		disposed when the list is disposed or when an item is removed.  
		///	</remarks>
		public bool ItemsOwned
		{
			get { return _itemsOwned; }
			set { _itemsOwned = value; }
		}

		protected bool _disposed;
		public bool IsDisposed { get { return _disposed; } }

		protected bool _disowning;
		
		/// <summary> IDisposable implementation </summary>
		public event EventHandler Disposed;

		/// <summary> IDisposable implementation </summary>
		public void Dispose()
		{
			#if USEFINALIZER
			System.GC.SuppressFinalize(this);
			#endif
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			_disposed = true;
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);

			Exception exception = null;
			while (Count > 0)
				try
				{
					RemoveAt(0);
				}
				catch (Exception E)
				{
					exception = E;
				}
				
			if (exception != null)
				throw exception;
		}

		/// <summary> <c>ItemDispose</c> is called by contained items when they are disposed. </summary>
		/// <remarks>
		///		This method simply removes the item from the list.  <c>ItemDispose</c> is 
		///		only called if the item is not disposed by this list.
		///	</remarks>
		protected virtual void ItemDispose(object sender, EventArgs args)
		{
			Disown((T)sender);
		}
		
		///	<remarks> Hooks the Disposed event of the item if the item implements IDisposableNotify. </remarks>
		protected override void Adding(T value, int index)
		{
			if (value is IDisposableNotify)
				((IDisposableNotify)value).Disposed += new EventHandler(ItemDispose);
		}

		/// <remarks> If the item is owned, it is disposed. </remarks>
		protected override void Removing(T value, int index)
		{
			if (value is IDisposableNotify)
        	    ((IDisposableNotify)value).Disposed -= new EventHandler(ItemDispose);

        	if (_itemsOwned && !_disowning)
		        value.Dispose();
		}

		/// <summary> Removes the specified object without disposing it. </summary>
		public virtual T Disown(T value)
		{
			_disowning = true;
			try
			{
				Remove(value);
				return value;
			}
			finally
			{
				_disowning = false;
			}
		}
		
		/// <summary> Removes the specified object index without disposing it. </summary>
		public virtual T DisownAt(int index)
		{
			_disowning = true;
			try
			{
				return RemoveAt(index);
			}
			finally
			{
				_disowning = false;
			}
		}

		public override void Move(int oldIndex, int newIndex)
		{
			_disowning = true;
			try
			{
				base.Move(oldIndex, newIndex);
			}
			finally
			{
				_disowning = false;
			}
		}
	}
}
