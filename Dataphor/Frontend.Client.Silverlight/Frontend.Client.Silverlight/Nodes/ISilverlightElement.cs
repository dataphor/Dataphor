using System;
using System.Windows;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public interface ISilverlightElement : IElement
	{
		FrameworkElement FrameworkElement { get; }
	}
}
