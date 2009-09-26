/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Drawing;

namespace Alphora.Dataphor.DAE.Client
{
	[ToolboxItem(false)]
	public class FieldDataLink : DataLink
	{
		public DataField DataField
		{
			get
			{
				if ((DataSet != null) && DataSet.Active && !DataSet.IsEmpty() && (FColumnName != String.Empty) && (FColumnName != null))
					return DataSet.Fields[FColumnName];
				else
					return null;
			}
		}

		private string FColumnName = String.Empty;
		public string ColumnName
		{
			get { return FColumnName; }
			set
			{
				if (FColumnName != value)
				{
					if (Active && (value != String.Empty) && (DataSet.Fields[value] == null))
						throw new ClientException(ClientException.Codes.ColumnNotFound, value);
					FColumnName = value;
					FieldChanged(null);
				}
			}
		}

		/// <summary> Indicates whether the DataView for this link can be modified. </summary>
		/// <remarks>
		/// Use ReadOnly to determine whether the DataView for this link can be modified.
		/// Setting ReadOnly to True causes any subsequent calls to the Edit method to fail,
		/// and changes the controls readonly state if there is one.
		/// Setting ReadOnly to False enables changes to the data in the DataView.
		/// </remarks>
		public bool ReadOnly
		{
			get { return FFlags[ReadOnlyMask]; }
			set
			{
				if (FFlags[ReadOnlyMask] != value)
				{
					FFlags[ReadOnlyMask] = value;
					UpdateReadOnly();
				}
			}
		}

		public bool InUpdate { get { return FFlags[InUpdateMask]; } }

		public event EventHandler OnUpdateReadOnly;
		protected virtual void UpdateReadOnly()
		{
			if (OnUpdateReadOnly != null)
				OnUpdateReadOnly(this, EventArgs.Empty);
		}

		public event DataLinkFieldHandler OnFieldChanged;
		protected virtual void FieldChanged(DataField AField)
		{
			if (!Modified && !InUpdate && (OnFieldChanged != null))
				OnFieldChanged(this, DataSet, AField);
		}

		protected override void DataChanged()
		{
			base.DataChanged();
			FieldChanged(null);
		}

		protected internal override void RowChanged(DataField AField)
		{
			base.RowChanged(AField);
			if ((AField == null) || (AField == DataField))
				FieldChanged(AField);
		}

		protected internal override void StateChanged()
		{
			base.StateChanged();
			UpdateReadOnly();
			DataChanged();
		}

		private void BeginUpdate()
		{
			FFlags[InUpdateMask] = true;
		}

		private void EndUpdate()
		{
			FFlags[InUpdateMask] = false;
		}

		public override void SaveRequested()
		{
			BeginUpdate();
			try
			{
				if (Modified)
					base.SaveRequested();
				Modified = false;
			}
			finally
			{
				EndUpdate();
			}
		}

		public bool CanModify()
		{
			return
				!ReadOnly &&
				(DataSet != null) &&
				((DataSet.State == DataSetState.Insert) || (DataSet.State == DataSetState.Edit));
		}

		/// <summary> True when a control contains unsaved information. </summary>
		public bool Modified
		{
			get { return FFlags[ModifiedMask]; }
			set { FFlags[ModifiedMask] = value; }
		}
		
		/// <summary> Called by the control before modification. </summary>
		/// <returns> True if the control is allowed to begin modification. </returns>
		public bool Edit()
		{
			if (!ReadOnly && (DataSet != null) && Active)
			{
				BeginUpdate();
				try
				{
					Modified = true;
					DataSet.Edit();
					return true;
				}
				finally
				{
					EndUpdate();
				}
			}
			else
				return false;
		}

		/// <summary> Called by the control to cancel modifications and re-load field value. </summary>
		public void Reset()
		{
			Modified = false;
			FieldChanged(null);
		}

	}

}