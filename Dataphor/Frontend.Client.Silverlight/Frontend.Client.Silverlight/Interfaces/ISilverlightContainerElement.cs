using System;
using System.Windows;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public interface ISilverlightContainerElement
	{
		/// <remarks> This method is invoked on the main thread. </remarks>
		void InsertChild(int AIndex, FrameworkElement AChild);
		/// <remarks> This method is invoked on the main thread. </remarks>
		void RemoveChild(FrameworkElement AChild);
	}
}
