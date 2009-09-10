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
			FChildren = new ChildCollection(this);
		}

		/// <summary> Dispose will unhook all of it's children. and call dispose on each one. </summary>
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				base.Dispose(ADisposing);
			}
			finally
			{
				if (FChildren != null)
				{
					FChildren.Dispose();  //Must happen after owner is cleared to deactivate tree
					FChildren = null;
				}
			}
		}

		#region UserData

		private object FUserData;
		/// <summary> User-defined scratch-pad for use by the application </summary>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		[DefaultValue(null)]
		public object UserData 
		{ 
			get { return FUserData; } 
			set { FUserData = value; } 
		}

		#endregion

		#region Parent/Child and Ownership

		// Owner

		/// <remarks> This is non private so that ChildCollection.Adding can do it's work without recursing.  (<see cref="ChildCollection.Adding"/>)</remarks>
		protected internal Node FOwner;
		/// <summary> The owner of a node is responsible for it's lifetime. </summary>
		/// <remarks> 
		///		The owner is usually the node's parent, but not necessarily.  For example, the ownership heirarchy 
		///		is used to determine a frame interface's host, whereas the parent is typically another control. 
		///	</remarks>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public INode Owner
		{
			get { return FOwner; }
			set
			{
				if (FOwner != value)
				{
					if (FOwner != null)
						FOwner.Children.Disown(this);
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
			get { return FOwner; }
		}
		
		// Children

		private ChildCollection FChildren;
		/// <summary> The set of nodes that this node owns (but is not necessarily a parent to). </summary>
		[Publish(PublishMethod.List)]
		[Browsable(false)]
		public IChildCollection Children
		{
			get { return FChildren; }
		}

		/// <summary> Allows for descendant behavior when child nodes are added. </summary>
		/// <remarks> Should not be used for validation (see <see cref="IsValidChild"/>). </remarks>
		protected internal virtual void AddChild(INode AChild) {}
		
		/// <summary> Allows for descendant behavior when child nodes are removed. </summary>
		protected internal virtual void RemoveChild(INode AChild) {}

		/// <summary> Indicates that a child node has been removed or added. </summary>
		protected internal virtual void ChildrenChanged() {}

		/// <summary> Determines if a particular node is valid as a child of this one. </summary>
		/// <remarks> 
		///		Allows a descendant node to screen it's potential children.
		///		Should throw an exception if a submitted child should not be added as a child.
		///		Should not assume the item will be added (See <see cref="AddChild"/>).
		///		The default implementation throws an exception, disallowing the child object.
		/// </remarks>
		public virtual bool IsValidChild(INode AChild)
		{
			if (AChild == null)
				return false;
			else
				return IsValidChild(AChild.GetType());
		}

		/// <summary> Determines if a particular class of node is valid as a child of this one. </summary>
		public virtual bool IsValidChild(Type AChildType)
		{
			return false;
		}

		/// <summary> Determines if a particular node is valid as an owner of this node. </summary>
		public virtual bool IsValidOwner(INode AOwner)
		{
			if (AOwner == null)
				return false;
			else
				return IsValidOwner(AOwner.GetType());
		}

		/// <summary> Determines if a particular class of node is valid as an owner of this node. </summary>
		public virtual bool IsValidOwner(Type AOwnerType)
		{
			return true;
		}

		/// <summary> Throws and exception to indicate that a particular node is not valid as a child of this node. </summary>
		/// <remarks> This method allows for descendents to provide more discriptive errors when invalid children are added. </remarks>
		protected internal virtual void InvalidChildError(INode AChild)
		{
			throw new ClientException(ClientException.Codes.InvalidChild, AChild.Name, AChild.GetType().ToString(), GetType().ToString());
		}

		/// <summary> Finds the first Owner which implements the specified interface. </summary>
		/// <returns> The located node, or null if non found. </returns>
		public virtual INode FindParent(Type AType)
		{
			if (Parent != null)
			{
				if (AType.IsAssignableFrom(Parent.GetType()))
					return Parent;
				else
					return Parent.FindParent(AType);
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
				INode LCurrent = this;
				while (LCurrent.Owner != null)
					LCurrent = LCurrent.Owner;
				return LCurrent as IHost;
			}
		}

		#endregion

		#region Name and Name Searches

		public INode GetNode(string AName, INode AExcluding)
		{
			if ((Name != String.Empty) && (String.Compare(AName, Name, true) == 0) && !Object.ReferenceEquals(AExcluding, this))
				return this;
			else
			{
				INode LResult;
				foreach (INode LChild in Children)
				{
					LResult = LChild.GetNode(AName, AExcluding);
					if (LResult != null)
						return LResult;
				}
			}
			return null;
		}
		
		public INode GetNode(string AName)
		{
			if ((Name != String.Empty) && String.Equals(AName, Name, StringComparison.OrdinalIgnoreCase))
				return this;
			else
			{
				INode LResult;
				foreach (INode LChild in Children)
				{
					LResult = LChild.GetNode(AName);
					if (LResult != null)
						return LResult;
				}
			}
			return null;
		}
		
		public INode FindNode(string AName)
		{
			INode LResult = GetNode(AName);
			if (LResult == null)
				throw new ClientException(ClientException.Codes.NodeNotFound, AName);
			return LResult;
		}
		
		/// <summary> Validates a node's name change. </summary>
		public event NameChangeHandler OnValidateName;

		private string FName = String.Empty;
		[DefaultValue("")]
		[Browsable(false)]
		public string Name
		{
			get { return FName; }
			set
			{
				if (FName != value)
				{
					if(value.IndexOf(' ') != -1)
						throw new ClientException(ClientException.Codes.InvalidNodeName, value);

					// TODO : Enforce node name uniqueness within owner
					if (OnValidateName != null)
						OnValidateName(this, FName, value);
					FName = value;
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

		private bool FTransitional;
		/// <summary> True when the node is transitioning to/from active state. </summary>
		/// <remarks> While transitioning, the Active property will return false. </remarks>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public bool Transitional
		{
			get { return FTransitional; }
		}

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual bool Active
		{
			get
			{
				return !FTransitional && (Owner != null) && Owner.Active;
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
			for (int LIndex = 0; LIndex < FChildren.Count; LIndex++)
			{
				try
				{
					((Node)Children[LIndex]).ActivateAll();
				}
				catch
				{
					// Note: BeforeDeactivate will not be called in a failure situation (such would not be appropriate as the node is not in an active state)
					for (int LUndoIndex = LIndex - 1; LUndoIndex >= 0; LUndoIndex--)
						((Node)Children[LUndoIndex]).Deactivate();
					throw;
				}
			}
		}

		/// <summary> Called for each node after activation. </summary>
		/// <remarks> Optimistically attempts to activate each child.  The last error (if any) will be thrown. </remarks>
		protected internal virtual void AfterActivate()
		{
			ErrorList LErrors = null;
			foreach (Node LChild in Children)
			{
				try
				{
					LChild.AfterActivate();
				}
				catch (Exception LException)
				{
					if (LErrors == null)
						LErrors = new ErrorList();
					LErrors.Add(LException);
				}
			}
			if (LErrors != null)
				HostNode.Session.ReportErrors(this.HostNode, LErrors);
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
				ErrorList LErrors = null;
				foreach (Node LChild in Children)
				{
					try
					{
						LChild.Deactivate();
					}
					catch (Exception LException)
					{
						if (LErrors == null)
							LErrors = new ErrorList();
						LErrors.Add(LException);
					}
				}
				if (LErrors != null)
					HostNode.Session.ReportErrors(HostNode, LErrors);
			}
		}

		/// <summary> Called for each node before deactivation. </summary>
		protected internal virtual void BeforeDeactivate()
		{
			if (Children != null)
			{
				ErrorList LErrors = null;
				foreach (Node LChild in Children)
				{
					try
					{
						LChild.BeforeDeactivate();
					}
					catch (Exception LException)
					{
						if (LErrors == null)
							LErrors = new ErrorList();
						LErrors.Add(LException);
					}
				}
				if (LErrors != null)
					HostNode.Session.ReportErrors(HostNode, LErrors);
			}
		}

		internal void DeactivateAll()
		{
			FTransitional = true;
			try
			{
				Deactivate();
			}
			finally
			{
				FTransitional = false;
			}
		}

		internal void ActivateAll()
		{
			FTransitional = true;
			try
			{
				Activate();
			}
			finally
			{
				FTransitional = false;
			}
		}
		
		#endregion

		#region Broadcast/Handle events

		/// <remarks> The default behavior is to handle the event then propigate to any children. </remarks>
		public virtual void BroadcastEvent(NodeEvent AEvent)
		{
			if (!AEvent.IsHandled)
			{
				HandleEvent(AEvent);
				if (!AEvent.IsHandled)
				{
					AEvent.Handle(this);
					if (!AEvent.IsHandled)
						foreach (Node LChild in Children)
						{
							LChild.BroadcastEvent(AEvent);
							if (AEvent.IsHandled)
								break;
						}
				}
			}
		}

		/// <remarks> Override to handle an event. </remarks>
		public virtual void HandleEvent(NodeEvent AEvent) {}
		
		#endregion

		#region IComponent

		private ISite FSite;
		/// <summary> Designer support. </summary>
		[Browsable(false)]
		[Publish(PublishMethod.None)]
		public ISite Site
		{
			get { return FSite; }
			set { FSite = value; }
		}

		#endregion
	}

	/// <summary> Used to maintain a node(s) list of children. </summary>
	public class ChildCollection : DisposableList<Node>, IChildCollection
	{
		/// <summary> Initializes a Child Collection. </summary>
		/// <param name="ANode"> The node that contains this children collection. </param>
		protected internal ChildCollection(Node ANode) : base(true)
		{
			FNode = ANode;
		}

		/// <summary> The owner node that has this children collection. </summary>
		protected Node FNode;

		/// <summary> Called to add a node to the children. </summary>
		/// <param name="AValue"> The node to be added. </param>
		/// <param name="AIndex"> The index of the node. </param>
		/// <remarks> Also links the node to it's owner. </remarks>
		protected override void Adding(Node AValue, int AIndex)
		{
			AValue.Owner = null;
			if (AValue.Active)
				throw new ClientException(ClientException.Codes.CannotAddActiveChild);

			base.Adding(AValue, AIndex);

			FNode.AddChild(AValue);
			try
			{
				AValue.FOwner = FNode;
				try
				{
					if (FNode.Active)
					{
						IUpdateHandler LHandler = (IUpdateHandler)FNode.FindParent(typeof(IUpdateHandler));
						if (LHandler != null)
							LHandler.BeginUpdate();
						else
							LHandler = null;
						try
						{
							AValue.ActivateAll();
							try
							{
								AValue.AfterActivate();
								FNode.ChildrenChanged();
							}
							catch
							{
								AValue.DeactivateAll();
								throw;
							}
						}
						finally
						{
							if (LHandler != null)
								LHandler.EndUpdate(true);
						}
					}
				}
				catch
				{
					AValue.FOwner = null;
					throw;
				}
			}
			catch
			{
				FNode.RemoveChild(AValue);
				throw;
			}
		}
		
		/// <summary> Called to remove a child node. </summary>
		/// <param name="AValue"> The node to be removed. </param>
		/// <param name="AIndex"> The nodes index. </param>
		/// <remarks> Unlinks a node from it's owner. </remarks>
		protected override void Removed(Node AValue, int AIndex)
		{
			try
			{
				try
				{
					if (FNode.Active)
					{
						IUpdateHandler LHandler = (IUpdateHandler)FNode.FindParent(typeof(IUpdateHandler));
						if (LHandler != null)
							LHandler.BeginUpdate();
						else
							LHandler = null;
						try
						{
							try
							{
								AValue.BeforeDeactivate();
							}
							finally
							{
								AValue.DeactivateAll();
							}
						}
						finally
						{
							if (LHandler != null)
								LHandler.EndUpdate(false);
						}
					}
				}
				finally
				{
					AValue.FOwner = null;
				}
			}
			finally
			{
				FNode.RemoveChild(AValue);
				FNode.ChildrenChanged();
				base.Removed(AValue, AIndex);
			}
		}

		/// <summary> Validates the child nodes. </summary>
		/// <param name="AValue"> The Node to be validated. </param>
		/// <remarks>  Calls owner node's IsValidChild on the new node. </remarks>
		protected override void Validate(Node AValue)
		{
			base.Validate(AValue);
			if (!FNode.IsValidChild(AValue))
				FNode.InvalidChildError(AValue);
		}

		/// <summary> Index accessor for the children nodes. </summary>
		public new INode this[int AIndex]
		{
			get { return base[AIndex]; }
			set { base[AIndex] = (Node)value; }
		}

		public void Disown(INode AItem)
		{
			base.Disown((Node)AItem);
		}

		public new INode DisownAt(int AIndex)
		{
			return base.DisownAt(AIndex);
		}
	}
}
