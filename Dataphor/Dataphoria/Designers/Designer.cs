/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

using Alphora.Dataphor.Frontend.Client;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	/// <remarks> 
	///		Designers are expected to have a constructor with the signature:
	///		cctor(Dataphoria ADataphoria, string ADesignerID) 
	///	</remarks>
	public interface IDesigner : IDisposable
	{
		void Open(DesignBuffer ABuffer);
		void New();
		void Close();
		
		void Show();

		/// <summary> Activates the designer. </summary>
		void Select();
		
		event EventHandler Disposed;
		
		string DesignerID { get; }

		IDesignService Service { get; }

		/// <summary> Attempt closure only after prompting the users to save changes and such. </summary>
		/// <returns> True if the designer was able to close. </returns>
		bool CloseSafely();
	}

	public interface ILiveDesigner : IDesigner
	{
		void Open(IHost AHost);
	}

	public struct DesignerInfo
	{
		public string ID;
		public string ClassName;
	}
}
