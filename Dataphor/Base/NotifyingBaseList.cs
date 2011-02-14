using System.ComponentModel;

namespace Alphora.Dataphor
{
	/// <summary> A generic list that publishes a change event. </summary>
	public class NotifyingBaseList<T> : ValidatingBaseList<T>
	{
		public NotifyingBaseList() : base() { }
		public NotifyingBaseList(int capacity) : base(capacity) { }

		public event NotifyingListChangeEventHandler<T> Changed;
		
		protected override void Adding(T value, int index) 
		{
			base.Adding(value, index);
			if (Changed != null)
				Changed(this, true, value, index);
		}

		protected override void Removing(T value, int index) 
		{ 
			base.Removing(value, index);
			if (Changed != null)
				Changed(this, false, value, index);
		}
	}
	
	public delegate void NotifyingListChangeEventHandler<T>(NotifyingBaseList<T> ASender, bool AIsAdded, T AItem, int AIndex);
}
