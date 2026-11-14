using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public class Panel : System.Windows.Forms.Panel
	{
		protected override Point ScrollToControl(Control activeControl)
		{  			
			//Hack: the commented implementation is preferred, but does not correctly detect the state of the mouse key
			//the following is a next-best implementation
			//if ((Control.MouseButtons & MouseButtons.Left) != 0) 
			if (_leftMouseDown)	
			{
				_leftMouseDown = false;
				return DisplayRectangle.Location;
			}
			else 
				return base.ScrollToControl(activeControl);			
		}	  
				
		//Hack: sets a flag for ScrollToControl to indicate the left mouse button has been pushed
		public const short WM_LBUTTONDOWN = 0x0201;	 	
		public const short WM_PARENTNOTIFY = 0x210;
		private bool _leftMouseDown; 
		protected override void WndProc(ref Message m)
		{
			if ((m.Msg == WM_PARENTNOTIFY) && (m.WParam.ToInt32() == WM_LBUTTONDOWN)) 		
				_leftMouseDown = true;
			base.WndProc(ref m);
		}  		
	}
}
