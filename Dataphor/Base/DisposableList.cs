using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor
{
	public class DisposableList<T> : ValidatingBaseList<T>, IDisposable, IDisposableNotify where T : IDisposable
	{
		public DisposableList() : base() 
		{ 
			FItemsOwned = true;
		}
		
		public DisposableList(int ACapacity) : base(ACapacity) 
		{ 
			FItemsOwned = true;
		}

		public DisposableList(bool AItemsOwned) : base()
		{
			FItemsOwned = AItemsOwned;
		}
		
		public DisposableList(bool AItemsOwned, int ACapacity) : base(ACapacity)
		{
			FItemsOwned = AItemsOwned;
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

		protected bool FItemsOwned;
		/// <summary>Determines whether or not the list "owns" the items it contains.<summary>
		/// <remarks>
		///		ItemsOwned controls whether or not the List "owns" the contained items.  
		///		"Owns" means that if the item supports the IDisposable interface, the will be
		///		disposed when the list is disposed or when an item is removed.  
		///	</remarks>
		public bool ItemsOwned
		{
			get { return FItemsOwned; }
			set { FItemsOwned = value; }
		}

		protected bool FDisposed;
		public bool IsDisposed { get { return FDisposed; } }

		protected bool FDisowning;
		
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

		protected virtual void Dispose(bool ADisposing)
		{
			FDisposed = true;
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);

			Exception LException = null;
			while (Count > 0)
				try
				{
					RemoveAt(0);
				}
				catch (Exception E)
				{
					LException = E;
				}
				
			if (LException != null)
				throw LException;
		}

		/// <summary> <c>ItemDispose</c> is called by contained items when they are disposed. </summary>
		/// <remarks>
		///		This method simply removes the item from the list.  <c>ItemDispose</c> is 
		///		only called if the item is not disposed by this list.
		///	</remarks>
		protected virtual void ItemDispose(object ASender, EventArgs AArgs)
		{
			Disown((T)ASender);
		}
		
		///	<remarks> Hooks the Disposed event of the item if the item implements IDisposableNotify. </remarks>
		protected override void Adding(T AValue, int AIndex)
		{
			if (AValue is IDisposableNotify)
				((IDisposableNotify)AValue).Disposed += new EventHandler(ItemDispose);
		}

		/// <remarks> If the item is owned, it is disposed. </remarks>
		protected override void Removing(T AValue, int AIndex)
		{
			if (AValue is IDisposableNotify)
        	    ((IDisposableNotify)AValue).Disposed -= new EventHandler(ItemDispose);

        	if (FItemsOwned && !FDisowning)
		        AValue.Dispose();
		}

		/// <summary> Removes the specified object without disposing it. </summary>
		public virtual T Disown(T AValue)
		{
			FDisowning = true;
			try
			{
				Remove(AValue);
				return AValue;
			}
			finally
			{
				FDisowning = false;
			}
		}
		
		/// <summary> Removes the specified object index without disposing it. </summary>
		public virtual T DisownAt(int AIndex)
		{
			FDisowning = true;
			try
			{
				return RemoveAt(AIndex);
			}
			finally
			{
				FDisowning = false;
			}
		}

		public override void Move(int AOldIndex, int ANewIndex)
		{
			FDisowning = true;
			try
			{
				base.Move(AOldIndex, ANewIndex);
			}
			finally
			{
				FDisowning = false;
			}
		}
	}
}
