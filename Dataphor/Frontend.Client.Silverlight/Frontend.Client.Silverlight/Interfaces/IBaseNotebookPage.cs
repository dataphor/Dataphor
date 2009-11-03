using System;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public interface ISilverlightBaseNotebookPage : IBaseNotebookPage
	{
		void Selected();
		void Unselected();
		ContentControl ContentControl { get; }
	}
}
