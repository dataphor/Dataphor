using System;
using System.Collections.Generic;
using System.Windows;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Runtime;
using Data = Alphora.Dataphor.DAE.Runtime.Data;
using Schema = Alphora.Dataphor.DAE.Schema;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public class CopyAction : Action, ICopyAction
	{
		// Do not locallize
		public const string DefaultClipboardFormatName = "DataphorData";
		
		private string _clipboardFormatName = DefaultClipboardFormatName;
		[Description("The identifier to use when storing or retrieving information from the clipboard.")]
		[DefaultValue(DefaultClipboardFormatName)]
		public string ClipboardFormatName
		{
			get { return _clipboardFormatName; }
			set { _clipboardFormatName = value; }
		}
		
		public override bool IsValidChild(Type childType)
		{
			return typeof(DataArgument).IsAssignableFrom(childType) || base.IsValidChild(childType);
		}

		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			// Collect the params
			DataParams localParamsValue = BaseArgument.CollectArguments(this);
			
			// Serialize the params and place them on the clipboard
			if (localParamsValue != null)
				Clipboard.SetData
				(
					ClipboardFormatName, 
					DataParamSerializer.DataParamsToSerializableParamData
					(
						HostNode.Session.DataSession.UtilityProcess, 
						localParamsValue
					)
				);
		}
	}
	
	public class PasteAction : Action
	{
		private string _clipboardFormatName = CopyAction.DefaultClipboardFormatName;
		[Description("The identifier to use when storing or retrieving information from the clipboard.")]
		[DefaultValue(CopyAction.DefaultClipboardFormatName)]
		public string ClipboardFormatName
		{
			get { return _clipboardFormatName; }
			set 
			{ 
				if (_clipboardFormatName != value)
				{
					_clipboardFormatName = value; 
					EnabledChanged();
				}
			}
		}

		public override bool IsValidChild(Type childType)
		{
			return typeof(DataArgument).IsAssignableFrom(childType) || base.IsValidChild(childType);
		}

		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			// Fetch the data params from the clipboard and deserialize them
			DataParams localParamsValue =
				DataParamSerializer.SerializableParamDataToDataParams
				(
					HostNode.Session.DataSession.UtilityProcess,
					(SerializableParamData)Clipboard.GetData(ClipboardFormatName)
				);
			
			// Change the params to be out
			foreach (DataParam param in localParamsValue)
				param.Modifier = Modifier.Out;
			
			// Apply the params	
			BaseArgument.ApplyArguments(this, localParamsValue);
		}

		public override bool GetEnabled()
		{
			return base.GetEnabled() && Clipboard.ContainsData(ClipboardFormatName);
		}

		private vbAccelerator.Components.Clipboard.ClipboardChangeNotifier _notifier;

		protected override void Activate()
		{
			base.Activate();
			_notifier = new vbAccelerator.Components.Clipboard.ClipboardChangeNotifier();
			_notifier.ClipboardChanged += new EventHandler(ClipboardChanged);
		}

		private void ClipboardChanged(object sender, EventArgs e)
		{
			EnabledChanged();
		}

		protected override void Deactivate()
		{
			try
			{
				_notifier.Dispose();
			}
			finally
			{
				base.Deactivate();
			}
		}
	}
		

	/// <summary> Converts DataParams to and from a format that can be serialized using .NET Serialization. </summary>
	/// <remarks> This is an adaptation of the logic in the local and remote servers to serialize and deserialize DataParams.  This incarnation, 
	/// however, uses Serializable rather than WCF DataContracts. </remarks>
	public static class DataParamSerializer
	{
		public static DataParams SerializableParamDataToDataParams(Alphora.Dataphor.DAE.IServerProcess process, SerializableParamData paramsValue)
		{
			if ((paramsValue.Params != null) && (paramsValue.Params.Length > 0))
			{
				DataParams localParamsValue = new DataParams();
				Schema.RowType rowType = new Schema.RowType();
				for (int index = 0; index < paramsValue.Params.Length; index++)
					rowType.Columns.Add(new Schema.Column(paramsValue.Params[index].Name, (Schema.ScalarType)process.Session.Server.Catalog[paramsValue.Params[index].TypeName]));

				Data.Row row = new Data.Row(process.ValueManager, rowType);
				try
				{
					row.ValuesOwned = false;
					row.AsPhysical = paramsValue.Data.Data;

					for (int index = 0; index < paramsValue.Params.Length; index++)
						if (row.HasValue(index))
							localParamsValue.Add(new DataParam(row.DataType.Columns[index].Name, row.DataType.Columns[index].DataType, SerializableModifierToModifier(paramsValue.Params[index].Modifier), Data.DataValue.CopyValue(process.ValueManager, row[index])));
						else
							localParamsValue.Add(new DataParam(row.DataType.Columns[index].Name, row.DataType.Columns[index].DataType, SerializableModifierToModifier(paramsValue.Params[index].Modifier), null));

					return localParamsValue;
				}
				finally
				{
					row.Dispose();
				}
			}
			else
				return null;
		}

		public static SerializableParamData DataParamsToSerializableParamData(Alphora.Dataphor.DAE.IServerProcess process, DataParams paramsValue)
		{
			int paramCount = paramsValue != null ? paramsValue.Count : 0;
			if (paramCount > 0)
			{
				Schema.RowType rowType = new Schema.RowType();
				if (paramsValue != null)
					foreach (DataParam param in paramsValue)
						rowType.Columns.Add(new Schema.Column(param.Name, param.DataType));
				using (Data.Row row = new Data.Row(process.ValueManager, rowType))
				{
					row.ValuesOwned = false;
					SerializableParamData localParamsValue = new SerializableParamData();
					localParamsValue.Params = new SerializableParam[paramCount];
					for (int index = 0; index < paramCount; index++)
					{
						localParamsValue.Params[index].Name = paramsValue[index].Name;
						localParamsValue.Params[index].TypeName = paramsValue[index].DataType.Name;
						localParamsValue.Params[index].Modifier = ModifierToSerializableModifier(paramsValue[index].Modifier);
						if (paramsValue[index].Value != null)
							row[index] = paramsValue[index].Value;
					}
					// TODO: Not able to adapt this without adding a common StreamManager public property
					//EnsureOverflowReleased(AProcess, LRow);
					localParamsValue.Data.Data = row.AsPhysical;
					return localParamsValue;
				}
			}
			else	// optimization
			{
				return new SerializableParamData();
			}
		}

		public static Modifier SerializableModifierToModifier(SerializableParamModifier serializableModifier)
		{
			switch (serializableModifier)
			{
				case SerializableParamModifier.In: return Modifier.In;
				case SerializableParamModifier.Out: return Modifier.Out;
				case SerializableParamModifier.Var: return Modifier.Var;
				case SerializableParamModifier.Const: return Modifier.Const;
				default: throw new ArgumentOutOfRangeException("ASerializableModifier");
			}
		}

		public static SerializableParamModifier ModifierToSerializableModifier(Modifier modifier)
		{
			switch (modifier)
			{
				case Modifier.In: return SerializableParamModifier.In;
				case Modifier.Out: return SerializableParamModifier.Out;
				case Modifier.Var: return SerializableParamModifier.Var;
				case Modifier.Const: return SerializableParamModifier.Const;
				default: throw new ArgumentOutOfRangeException("AModifier");
			}
		}
	}

	public enum SerializableParamModifier : byte { In, Var, Out, Const }

	[Serializable]
	public struct SerializableParam
	{
		public string Name;

		public string TypeName;

		public SerializableParamModifier Modifier;
	}

	/// <nodoc/>
	[Serializable]
	public struct SerializableParamData
	{
		public SerializableParam[] Params;

		public SerializableRowBody Data;
	}

	[Serializable]
	public struct SerializableRowBody
	{
		public byte[] Data;
	}
}
