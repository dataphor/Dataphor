using System.ComponentModel;

namespace Alphora.Dataphor
{
	/// <summary> A generic list that publishes a change event. </summary>
	public class NotifyingBaseList<T> : ValidatingBaseList<T>
	{
		public NotifyingBaseList() : base() { }
		public NotifyingBaseList(int ACapacity) : base(ACapacity) { }

		public event NotifyingListChangeEventHandler<T> Changed;
		
		protected override void Adding(T AValue, int AIndex) 
		{
			base.Adding(AValue, AIndex);
			if (Changed != null)
				Changed(this, true, AValue, AIndex);
		}

		protected override void Removing(T AValue, int AIndex) 
		{ 
			base.Removing(AValue, AIndex);
			if (Changed != null)
				Changed(this, false, AValue, AIndex);
		}
	}
	
	public delegate void NotifyingListChangeEventHandler<T>(NotifyingBaseList<T> ASender, bool AIsAdded, T AItem, int AIndex);
}
