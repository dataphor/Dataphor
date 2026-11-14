using System;
using System.Windows.Media.Imaging;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public static class WPFInteropUtility
	{
		[System.Runtime.InteropServices.DllImport("gdi32")]
		private static extern int DeleteObject(IntPtr o);

		/// <summary> Convert from a WinForms bitmap to a WPF ImageSource. </summary>
		/// <remarks> Credits: http://stackoverflow.com/questions/1118496/using-image-control-in-wpf-to-display-system-drawing-bitmap/1118557#1118557 </remarks>
		public static BitmapSource ImageToBitmapSource(System.Drawing.Image source)
		{
			var localSource = source as System.Drawing.Bitmap;
			if (localSource != null)
			{
				IntPtr handle = localSource.GetHbitmap();
				try
				{
					return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap
					(
						handle,
						IntPtr.Zero,
						System.Windows.Int32Rect.Empty,
						System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()
					);
				}
				finally
				{
					DeleteObject(handle);
				}
			}
			else
				return null;
		}
	}
}
