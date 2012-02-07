using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public interface ISilverlightFormInterface : IFormInterface, ISilverlightElement
	{
		/// <summary> Shows the form. </summary>
		/// <param name="AOnCloseForm"> Callback when the form closes. </param>
		void Show(FormInterfaceHandler AOnCloseForm);
		
		DispatchedReadOnlyCollection<Exception> BindErrors { get; }
		
		FormControl Form { get; }

		event EventHandler Accepting;
	}
}
