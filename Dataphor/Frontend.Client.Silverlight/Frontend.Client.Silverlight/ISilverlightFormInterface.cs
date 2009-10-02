using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public interface ISilverlightFormInterface : IFormInterface
	{
		/// <summary> Shows the form non-modally. </summary>
		/// <param name="AOnCloseForm"> Callback when the form closes. </param>
		void Show(FormInterfaceHandler AOnCloseForm, ContentControl AContainer);
		Control Control { get; set; }
		bool SupressCloseButton { get; set; }
	}
}
