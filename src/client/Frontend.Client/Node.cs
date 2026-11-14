/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Collections;
using Alphora.Dataphor;
using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client
{
	[PublishDefaultList("Children")]
	[PublishName("Name")]
	public abstract class Node : Disposable, INode
	{
		/// <summary> Constructs a new node an initializes it's child list. </summary>
		public Node() : base()
		{
			_children = new ChildCollection(this);
		}

		/// <summary> Dispose will unhook all of it's children. and call dispose on each one. </summary>
		protected override void Dispose(bool disposing)
		{
			try
			{
				base.Dispose(disposing);
			}
			finally
			{
				if (_children != null)
				{
					_children.Dispose();  //Must happen after owner is cleared to deactivate tree
					_children = null;
				}
			}
		}

		#region UserData

		private object _userData;
		/// <summary> User-defined scratch-pad for use by the application </summary>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		[DefaultValue(null)]
		public object UserData 
		{ 
			get { return _userData; } 
			set { _userData = value; } 
		}

		#endregion

		#region Parent/Child and Ownership

		// Owner

		/// <remarks> This is non private so that ChildCollection.Adding can do it's work without recursing.  (<see cref="ChildCollection.Adding"/>)</remarks>
		protected internal Node _owner;
		/// <summary> The owner of a node is responsible for it's lifetime. </summary>
		/// <remarks> 
		///		The owner is usually the node's parent, but not necessarily.  For example, the ownership heirarchy 
		///		is used to determine a frame interface's host, whereas the parent is typically another control. 
		///	</remarks>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public INode Owner
		{
			get { return _owner; }
			set
			{
				if (_owner != value)
				{
					if (_owner != null)
						_owner.Children.Disown(this);
					if (value != null)
						value.Children.Add(this);
				}
			}
		}

		// Parent

		/// <summary> The parent of a node is the node that utilizes the node as a child. </summary>
		/// <remarks> This is not necessarily the owner of the node. </remarks>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual INode Parent
		{
			get { return _owner; }
		}
		
		// Children

		private ChildCollection _children;
		/// <summary> The set of nodes that this node owns (but is not necessarily a parent to). </summary>
		[Publish(PublishMethod.List)]
		[Browsable(false)]
		public IChildCollection Children
		{
			get { return _children; }
		}

		/// <summary> Allows for descendant behavior when child nodes are added. </summary>
		/// <remarks> Should not be used for validation (see <see cref="IsValidChild"/>). </remarks>
		protected internal virtual void AddChild(INode child) {}
		
		/// <summary> Allows for descendant behavior when child nodes are removed. </summary>
		protected internal virtual void RemoveChild(INode child) {}

		/// <summary> Indicates that a child node has been removed or added. </summary>
		protected internal virtual void ChildrenChanged() {}

		/// <summary> Determines if a particular node is valid as a child of this one. </summary>
		/// <remarks> 
		///		Allows a descendant node to screen it's potential children.
		///		Should throw an exception if a submitted child should not be added as a child.
		///		Should not assume the item will be added (See <see cref="AddChild"/>).
		///		The default implementation throws an exception, disallowing the child object.
		/// </remarks>
		public virtual bool IsValidChild(INode child)
		{
			if (child == null)
				return false;
			else
				return IsValidChild(child.GetType());
		}

		/// <summary> Determines if a particular class of node is valid as a child of this one. </summary>
		public virtual bool IsValidChild(Type childType)
		{
			return false;
		}

		/// <summary> Determines if a particular node is valid as an owner of this node. </summary>
		public virtual bool IsValidOwner(INode owner)
		{
			if (owner == null)
				return false;
			else
				return IsValidOwner(owner.GetType());
		}

		/// <summary> Determines if a particular class of node is valid as an owner of this node. </summary>
		public virtual bool IsValidOwner(Type ownerType)
		{
			return true;
		}

		/// <summary> Throws and exception to indicate that a particular node is not valid as a child of this node. </summary>
		/// <remarks> This method allows for descendents to provide more discriptive errors when invalid children are added. </remarks>
		protected internal virtual void InvalidChildError(INode child)
		{
			throw new ClientException(ClientException.Codes.InvalidChild, child.Name, child.GetType().ToString(), GetType().ToString());
		}

		/// <summary> Finds the first Owner which implements the specified interface. </summary>
		/// <returns> The located node, or null if non found. </returns>
		public virtual INode FindParent(Type type)
		{
			if (Parent != null)
			{
				if (type.IsAssignableFrom(Parent.GetType()))
					return Parent;
				else
					return Parent.FindParent(type);
			}
			else
				return null;
		}

		/// <summary> Returns the root owner node that manages the lifetime of this node. </summary>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual IHost HostNode
		{
			get
			{
				INode current = this;
				while (current.Owner != null)
					current = current.Owner;
				return current as IHost;
			}
		}

		#endregion

		#region Name and Name Searches

		public INode GetNode(string name, INode excluding)
		{
			if ((Name != String.Empty) && String.Equals(name, Name, StringComparison.OrdinalIgnoreCase) && !Object.ReferenceEquals(excluding, this))
				return this;
			else
			{
				INode result;
				foreach (INode child in Children)
				{
					result = child.GetNode(name, excluding);
					if (result != null)
						return result;
				}
			}
			return null;
		}
		
		public INode GetNode(string name)
		{
			if ((Name != String.Empty) && String.Equals(name, Name, StringComparison.OrdinalIgnoreCase))
				return this;
			else
			{
				INode result;
				foreach (INode child in Children)
				{
					result = child.GetNode(name);
					if (result != null)
						return result;
				}
			}
			return null;
		}
		
		public INode FindNode(string name)
		{
			INode result = GetNode(name);
			if (result == null)
				throw new ClientException(ClientException.Codes.NodeNotFound, name);
			return result;
		}
		
		/// <summary> Validates a node's name change. </summary>
		public event NameChangeHandler OnValidateName;

		private string _name = String.Empty;
		[DefaultValue("")]
		[Browsable(false)]
		public string Name
		{
			get { return _name; }
			set
			{
				if (_name != value)
				{
					if(value.IndexOf(' ') != -1)
						throw new ClientException(ClientException.Codes.InvalidNodeName, value);

					// TODO : Enforce node name uniqueness within owner
					if (OnValidateName != null)
						OnValidateName(this, _name, value);
					_name = value;
				}
			}
		}

		/// <summary> Returns the name of the node. </summary>
		/// <returns> The name of the node. </returns>
		public override string ToString() 
		{
			return Name;
		}
		
		#endregion

		#region Activation/Deactivation

		private bool _transitional;
		/// <summary> True when the node is transitioning to/from active state. </summary>
		/// <remarks> While transitioning, the Active property will return false. </remarks>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public bool Transitional
		{
			get { return _transitional; }
		}

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual bool Active
		{
			get
			{
				return !_transitional && (Owner != null) && Owner.Active;
			}
		}

		/// <summary> Ensures that the node is in an active state.  Throws otherwise. </summary>
		protected void CheckInactive()
		{
			if (Active)
				throw new ClientException(ClientException.Codes.NodeActive);
		}

		/// <summary> Ensures that the node is in an inactive state.  Throws otherwise. </summary>
		protected void CheckActive()
		{
			if (!Active)
				throw new ClientException(ClientException.Codes.NodeInactive);
		}

		/// <summary> Used internally to bring the node to an active state. </summary>
		/// <remarks>
		///		During activation, the active property will be false.  After
		///		activation, and once the active property is true, 
		///		<see cref="AfterActivate"/> is called.
		/// </remarks>
		protected virtual void Activate()
		{
			for (int index = 0; index < _children.Count; index++)
			{
				try
				{
					((Node)Children[index]).ActivateAll();
				}
				catch
				{
					// Note: BeforeDeactivate will not be called in a failure situation (such would not be appropriate as the node is not in an active state)
					for (int undoIndex = index - 1; undoIndex >= 0; undoIndex--)
						((Node)Children[undoIndex]).Deactivate();
					throw;
				}
			}
		}

		/// <summary> Called for each node after activation. </summary>
		/// <remarks> Optimistically attempts to activate each child.  The last error (if any) will be thrown. </remarks>
		protected internal virtual void AfterActivate()
		{
			ErrorList errors = null;
			foreach (Node child in Children)
			{
				try
				{
					child.AfterActivate();
				}
				catch (Exception exception)
				{
					if (errors == null)
						errors = new ErrorList();
					errors.Add(exception);
				}
			}
			if (errors != null)
				HostNode.Session.ReportErrors(this.HostNode, errors);
		}

		/// <summary> Used internally to bring the node to an inactive state. </summary>
		/// <remarks>
		///		During deactivation, the active property will be false.  Before
		///		deactivation, and before the active property returns false,
		///		<see cref="BeforeDeactivate"/> is called.  This method 
		///		optimistically deactivates each child node.  The last error (if
		///		any) will be thrown.
		///	</remarks>
		protected virtual void Deactivate()
		{
			if (Children != null)
			{
				ErrorList errors = null;
				foreach (Node child in Children)
				{
					try
					{
						child.Deactivate();
					}
					catch (Exception exception)
					{
						if (errors == null)
							errors = new ErrorList();
						errors.Add(exception);
					}
				}
				if (errors != null)
					HostNode.Session.ReportErrors(HostNode, errors);
			}
		}

		/// <summary> Called for each node before deactivation. </summary>
		protected internal virtual void BeforeDeactivate()
		{
			if (Children != null)
			{
				ErrorList errors = null;
				foreach (Node child in Children)
				{
					try
					{
						child.BeforeDeactivate();
					}
					catch (Exception exception)
					{
						if (errors == null)
							errors = new ErrorList();
						errors.Add(exception);
					}
				}
				if (errors != null)
					HostNode.Session.ReportErrors(HostNode, errors);
			}
		}

		internal void DeactivateAll()
		{
			_transitional = true;
			try
			{
				Deactivate();
			}
			finally
			{
				_transitional = false;
			}
		}

		internal void ActivateAll()
		{
			_transitional = true;
			try
			{
				Activate();
			}
			finally
			{
				_transitional = false;
			}
		}
		
		#endregion

		#region Broadcast/Handle events

		/// <remarks> The default behavior is to handle the event then propigate to any children. </remarks>
		public virtual void BroadcastEvent(NodeEvent eventValue)
		{
			if (!eventValue.IsHandled)
			{
				HandleEvent(eventValue);
				if (!eventValue.IsHandled)
				{
					eventValue.Handle(this);
					if (!eventValue.IsHandled)
						foreach (Node child in Children)
						{
							child.BroadcastEvent(eventValue);
							if (eventValue.IsHandled)
								break;
						}
				}
			}
		}

		/// <remarks> Override to handle an event. </remarks>
		public virtual void HandleEvent(NodeEvent eventValue) {}
		
		#endregion

		#region IComponent

		private ISite _site;
		/// <summary> Designer support. </summary>
		[Browsable(false)]
		[Publish(PublishMethod.None)]
		public ISite Site
		{
			get { return _site; }
			set { _site = value; }
		}

		#endregion
	}

	/// <summary> Used to maintain a node(s) list of children. </summary>
	public class ChildCollection : DisposableList<Node>, IChildCollection
	{
		/// <summary> Initializes a Child Collection. </summary>
		/// <param name="node"> The node that contains this children collection. </param>
		protected internal ChildCollection(Node node) : base(true)
		{
			_node = node;
		}

		/// <summary> The owner node that has this children collection. </summary>
		protected Node _node;

		/// <summary> Called to add a node to the children. </summary>
		/// <param name="value"> The node to be added. </param>
		/// <param name="index"> The index of the node. </param>
		/// <remarks> Also links the node to it's owner. </remarks>
		protected override void Adding(Node value, int index)
		{
			value.Owner = null;
			if (value.Active)
				throw new ClientException(ClientException.Codes.CannotAddActiveChild);

			base.Adding(value, index);

			_node.AddChild(value);
			try
			{
				value._owner = _node;
				try
				{
					if (_node.Active)
					{
						IUpdateHandler handler = (IUpdateHandler)_node.FindParent(typeof(IUpdateHandler));
						if (handler != null)
							handler.BeginUpdate();
						else
							handler = null;
						try
						{
							value.ActivateAll();
							try
							{
								value.AfterActivate();
								_node.ChildrenChanged();
							}
							catch
							{
								value.DeactivateAll();
								throw;
							}
						}
						finally
						{
							if (handler != null)
								handler.EndUpdate(true);
						}
					}
				}
				catch
				{
					value._owner = null;
					throw;
				}
			}
			catch
			{
				_node.RemoveChild(value);
				throw;
			}
		}
		
		/// <summary> Called to remove a child node. </summary>
		/// <param name="value"> The node to be removed. </param>
		/// <param name="index"> The nodes index. </param>
		/// <remarks> Unlinks a node from it's owner. </remarks>
		protected override void Removed(Node value, int index)
		{
			try
			{
				try
				{
					if (_node.Active)
					{
						IUpdateHandler handler = (IUpdateHandler)_node.FindParent(typeof(IUpdateHandler));
						if (handler != null)
							handler.BeginUpdate();
						else
							handler = null;
						try
						{
							try
							{
								value.BeforeDeactivate();
							}
							finally
							{
								value.DeactivateAll();
							}
						}
						finally
						{
							if (handler != null)
								handler.EndUpdate(false);
						}
					}
				}
				finally
				{
					value._owner = null;
				}
			}
			finally
			{
				_node.RemoveChild(value);
				_node.ChildrenChanged();
				base.Removed(value, index);
			}
		}

		/// <summary> Validates the child nodes. </summary>
		/// <param name="value"> The Node to be validated. </param>
		/// <remarks>  Calls owner node's IsValidChild on the new node. </remarks>
		protected override void Validate(Node value)
		{
			base.Validate(value);
			if (!_node.IsValidChild(value))
				_node.InvalidChildError(value);
		}

		/// <summary> Index accessor for the children nodes. </summary>
		public new INode this[int index]
		{
			get { return base[index]; }
			set { base[index] = (Node)value; }
		}

		public void Disown(INode item)
		{
			base.Disown((Node)item);
		}

		public new INode DisownAt(int index)
		{
			return base.DisownAt(index);
		}
	}
}
