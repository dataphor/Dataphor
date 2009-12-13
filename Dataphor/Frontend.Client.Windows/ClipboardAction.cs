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
		public const string CDefaultClipboardFormatName = "DataphorData";
		
		private string FClipboardFormatName = CDefaultClipboardFormatName;
		[Description("The identifier to use when storing or retrieving information from the clipboard.")]
		[DefaultValue(CDefaultClipboardFormatName)]
		public string ClipboardFormatName
		{
			get { return FClipboardFormatName; }
			set { FClipboardFormatName = value; }
		}
		
		public override bool IsValidChild(Type AChildType)
		{
			return typeof(DataArgument).IsAssignableFrom(AChildType) || base.IsValidChild(AChildType);
		}

		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			// Collect the params
			DataParams LParams = DataArgument.CollectArguments(this);
			
			// Serialize the params and place them on the clipboard
			if (LParams != null)
				Clipboard.SetData
				(
					ClipboardFormatName, 
					DataParamSerializer.DataParamsToSerializableParamData
					(
						HostNode.Session.DataSession.UtilityProcess, 
						LParams
					)
				);
		}
	}
	
	public class PasteAction : Action
	{
		private string FClipboardFormatName = CopyAction.CDefaultClipboardFormatName;
		[Description("The identifier to use when storing or retrieving information from the clipboard.")]
		[DefaultValue(CopyAction.CDefaultClipboardFormatName)]
		public string ClipboardFormatName
		{
			get { return FClipboardFormatName; }
			set 
			{ 
				if (FClipboardFormatName != value)
				{
					FClipboardFormatName = value; 
					EnabledChanged();
				}
			}
		}

		public override bool IsValidChild(Type AChildType)
		{
			return typeof(DataArgument).IsAssignableFrom(AChildType) || base.IsValidChild(AChildType);
		}

		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			// Fetch the data params from the clipboard and deserialize them
			DataParams LParams =
				DataParamSerializer.SerializableParamDataToDataParams
				(
					HostNode.Session.DataSession.UtilityProcess,
					(SerializableParamData)Clipboard.GetData(ClipboardFormatName)
				);
			
			// Change the params to be out
			foreach (DataParam LParam in LParams)
				LParam.Modifier = Modifier.Out;
			
			// Apply the params	
			DataArgument.ApplyArguments(this, LParams);
		}

		public override bool GetEnabled()
		{
			return base.GetEnabled() && Clipboard.ContainsData(ClipboardFormatName);
		}

		private vbAccelerator.Components.Clipboard.ClipboardChangeNotifier FNotifier;

		protected override void Activate()
		{
			base.Activate();
			FNotifier = new vbAccelerator.Components.Clipboard.ClipboardChangeNotifier();
			FNotifier.ClipboardChanged += new EventHandler(ClipboardChanged);
		}

		private void ClipboardChanged(object sender, EventArgs e)
		{
			EnabledChanged();
		}

		protected override void Deactivate()
		{
			try
			{
				FNotifier.Dispose();
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
		public static DataParams SerializableParamDataToDataParams(Alphora.Dataphor.DAE.IServerProcess AProcess, SerializableParamData AParams)
		{
			if ((AParams.Params != null) && (AParams.Params.Length > 0))
			{
				DataParams LParams = new DataParams();
				Schema.RowType LRowType = new Schema.RowType();
				for (int LIndex = 0; LIndex < AParams.Params.Length; LIndex++)
					LRowType.Columns.Add(new Schema.Column(AParams.Params[LIndex].Name, (Schema.ScalarType)AProcess.Session.Server.Catalog[AParams.Params[LIndex].TypeName]));

				Data.Row LRow = new Data.Row(AProcess.ValueManager, LRowType);
				try
				{
					LRow.ValuesOwned = false;
					LRow.AsPhysical = AParams.Data.Data;

					for (int LIndex = 0; LIndex < AParams.Params.Length; LIndex++)
						if (LRow.HasValue(LIndex))
							LParams.Add(new DataParam(LRow.DataType.Columns[LIndex].Name, LRow.DataType.Columns[LIndex].DataType, SerializableModifierToModifier(AParams.Params[LIndex].Modifier), Data.DataValue.CopyValue(AProcess.ValueManager, LRow[LIndex])));
						else
							LParams.Add(new DataParam(LRow.DataType.Columns[LIndex].Name, LRow.DataType.Columns[LIndex].DataType, SerializableModifierToModifier(AParams.Params[LIndex].Modifier), null));

					return LParams;
				}
				finally
				{
					LRow.Dispose();
				}
			}
			else
				return null;
		}

		public static SerializableParamData DataParamsToSerializableParamData(Alphora.Dataphor.DAE.IServerProcess AProcess, DataParams AParams)
		{
			int LParamCount = AParams != null ? AParams.Count : 0;
			if (LParamCount > 0)
			{
				Schema.RowType LRowType = new Schema.RowType();
				if (AParams != null)
					foreach (DataParam LParam in AParams)
						LRowType.Columns.Add(new Schema.Column(LParam.Name, LParam.DataType));
				using (Data.Row LRow = new Data.Row(AProcess.ValueManager, LRowType))
				{
					LRow.ValuesOwned = false;
					SerializableParamData LParams = new SerializableParamData();
					LParams.Params = new SerializableParam[LParamCount];
					for (int LIndex = 0; LIndex < LParamCount; LIndex++)
					{
						LParams.Params[LIndex].Name = AParams[LIndex].Name;
						LParams.Params[LIndex].TypeName = AParams[LIndex].DataType.Name;
						LParams.Params[LIndex].Modifier = ModifierToSerializableModifier(AParams[LIndex].Modifier);
						if (AParams[LIndex].Value != null)
							LRow[LIndex] = AParams[LIndex].Value;
					}
					// TODO: Not able to adapt this without adding a common StreamManager public property
					//EnsureOverflowReleased(AProcess, LRow);
					LParams.Data.Data = LRow.AsPhysical;
					return LParams;
				}
			}
			else	// optimization
			{
				return new SerializableParamData();
			}
		}

		public static Modifier SerializableModifierToModifier(SerializableParamModifier ASerializableModifier)
		{
			switch (ASerializableModifier)
			{
				case SerializableParamModifier.In: return Modifier.In;
				case SerializableParamModifier.Out: return Modifier.Out;
				case SerializableParamModifier.Var: return Modifier.Var;
				case SerializableParamModifier.Const: return Modifier.Const;
				default: throw new ArgumentOutOfRangeException("ASerializableModifier");
			}
		}

		public static SerializableParamModifier ModifierToSerializableModifier(Modifier AModifier)
		{
			switch (AModifier)
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
