using System;

namespace System.ComponentModel
{
	/// <summary> Reimplementation of Component for Silverlight compatability. </summary>
	public class Component : IComponent, IDisposable
	{
		public IContainer Container 
		{ 
			get
			{
				return (Site != null ? Site.Container : null);
			} 
		}

		protected bool DesignMode 
		{ 
			get
			{
				return (Site != null ? Site.DesignMode : false);
			}
		}

		public virtual ISite Site { get; set; }

		public event EventHandler Disposed;
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		protected virtual void Dispose(bool ADisposing)
		{
			if (ADisposing)
			{
				lock (this)
				{
					if ((Site != null) && (Site.Container != null))
					{
						Site.Container.Remove(this);
					}
					if (Disposed != null)
						Disposed(this, EventArgs.Empty);
				}
			}
		}
		
		protected virtual object GetService(Type AService)
		{
			return (Site != null ? Site.GetService(AService) : false);
		}
	}
	
	public class EditorAttribute : Attribute
	{
		public EditorAttribute(string AClassName, string AUITypeEditor) { }
	}
	
	public class BrowsableAttribute : Attribute
	{
		public BrowsableAttribute(bool AValue) { }
	}
}
