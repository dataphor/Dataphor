/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client
{
	public class SetPropertyAction : Action, ISetPropertyAction, INodeReference, ISourceReference
	{
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Node = null;
			Source = null;
		}

		// Node

		private INode FNode;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("The target node that will be set.")]
		public INode Node
		{
			get { return FNode; }
			set
			{
				if (FNode != null)
					FNode.Disposed -= new EventHandler(NodeDisposed);
				FNode = value;
				if (FNode != null)
					FNode.Disposed += new EventHandler(NodeDisposed);
			}
		}

		private void NodeDisposed(object ASender, EventArgs AArgs)
		{
			Node = null;
		}

		// MemberName

		private string FMemberName = String.Empty;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.MemberNameConverter,Alphora.Dataphor.Frontend.Client")]
		[DefaultValue("")]
		[Description("The target member to set.")]
		public string MemberName
		{
			get { return FMemberName; }
			set { FMemberName = value; }
		}

		// Value

		private string FValue = String.Empty;
		[DefaultValue("")]
		[Description("The value to set the target member to.")]
		public string Value
		{
			get { return FValue; }
			set { FValue = value; }
		}

		// Source

		private ISource FSource;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("The source where the value will be retrieved from.")]
		public ISource Source
		{
			get { return FSource; }
			set
			{
				if (FSource != value)
				{
					if (FSource != null) 
					{
						FSource.Disposed -= new EventHandler(SourceDisposed);
					}
					FSource = value;
					if (FSource != null) 
					{
						FSource.Disposed += new EventHandler(SourceDisposed);
					}
				}
			}
		}

		protected virtual void SourceDisposed(object ASender, EventArgs AArgs)
		{
			Source = null;
		}

		// ColumnName

		private string FColumnName = String.Empty;
		[DefaultValue("")]
		[TypeConverter("Alphora.Dataphor.Frontend.Client.ColumnNameConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("The name of the column in the data source this element is associated with.")]
		public string ColumnName
		{
			get { return FColumnName; }
			set
			{
				if (FColumnName != value)
				{
					FColumnName = value;
				}
			}
		}

		// Action

		public override bool GetEnabled()
		{
			return base.GetEnabled() && (FNode != null) && (FMemberName != String.Empty) 
				&& 
				(
					(FSource == null)
						|| ((FSource.DataView != null) && FSource.DataView.Active && !FSource.DataView.IsEmpty())
				);
		}

		/// <summary> Sets the target member to value. </summary>
		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			string LValue;
			if (FSource == null)
				LValue = FValue;
			else
				LValue = FSource.DataView.Fields[FColumnName].AsString;
			ReflectionUtility.SetInstanceMember(FNode, FMemberName, LValue);
		}
	}
}
