using System;
using System.Windows;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class FrameInterface : Interface, IFrameInterface
	{
		public FrameInterface(IFrame AFrame) : base()
		{
			FFrame = AFrame;
		}

		// Frame

		private IFrame FFrame;
		
		public IFrame Frame
		{
			get { return FFrame; }
		}

		/// <remarks> Frame does nothing with the ForceAcceptReject. Vestigial of IInterface ancestry. </remarks>
		public bool ForceAcceptReject { get; set; }
		
		// Default Action

		public override void PerformDefaultAction()
		{
			if (OnDefault != null)
				OnDefault.Execute(this, new EventParams());
			else
				((Interface)FindParent(typeof(IInterface))).PerformDefaultAction();
		}

		// Element
		
		protected override FrameworkElement CreateFrameworkElement()
		{
			return new ContentControl();
		}

		// Node

		public override INode Parent
		{
			get { return FFrame; }
		}
	}
}
