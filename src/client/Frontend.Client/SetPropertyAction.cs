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
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Node = null;
			Source = null;
		}

		// Node

		private INode _node;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("The target node that will be set.")]
		public INode Node
		{
			get { return _node; }
			set
			{
				if (_node != null)
					_node.Disposed -= new EventHandler(NodeDisposed);
				_node = value;
				if (_node != null)
					_node.Disposed += new EventHandler(NodeDisposed);
			}
		}

		private void NodeDisposed(object sender, EventArgs args)
		{
			Node = null;
		}

		// MemberName

		private string _memberName = String.Empty;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.MemberNameConverter,Alphora.Dataphor.Frontend.Client")]
		[DefaultValue("")]
		[Description("The target member to set.")]
		public string MemberName
		{
			get { return _memberName; }
			set { _memberName = value; }
		}

		// Value

		private string _value = String.Empty;
		[DefaultValue("")]
		[Description("The value to set the target member to.")]
		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		// Source

		private ISource _source;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("The source where the value will be retrieved from.")]
		public ISource Source
		{
			get { return _source; }
			set
			{
				if (_source != value)
				{
					if (_source != null) 
					{
						_source.Disposed -= new EventHandler(SourceDisposed);
					}
					_source = value;
					if (_source != null) 
					{
						_source.Disposed += new EventHandler(SourceDisposed);
					}
				}
			}
		}

		protected virtual void SourceDisposed(object sender, EventArgs args)
		{
			Source = null;
		}

		// ColumnName

		private string _columnName = String.Empty;
		[DefaultValue("")]
		[TypeConverter("Alphora.Dataphor.Frontend.Client.ColumnNameConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("The name of the column in the data source this element is associated with.")]
		public string ColumnName
		{
			get { return _columnName; }
			set
			{
				if (_columnName != value)
				{
					_columnName = value;
				}
			}
		}

		// Action

		public override bool GetEnabled()
		{
			return base.GetEnabled() && (_node != null) && (_memberName != String.Empty) 
				&& 
				(
					(_source == null)
						|| ((_source.DataView != null) && _source.DataView.Active && !_source.DataView.IsEmpty())
				);
		}

		/// <summary> Sets the target member to value. </summary>
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			string value;
			if (_source == null)
				value = _value;
			else
				value = _source.DataView.Fields[_columnName].AsString;
			ReflectionUtility.SetInstanceMember(_node, _memberName, value);
		}
	}
}
