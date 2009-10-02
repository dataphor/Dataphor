/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

namespace Alphora.Dataphor.Frontend.Client
{
	public sealed class LookupUtility
	{
		public static string[] GetColumnNames(string AFirstKeyNames, string ASecondKeyNames)
		{
			if (AFirstKeyNames != String.Empty)
			{
				string[] LFirstKeyNames = AFirstKeyNames.Split(DAE.Client.DataView.CColumnNameDelimiters);
				string[] LSecondKeyNames = ASecondKeyNames.Split(DAE.Client.DataView.CColumnNameDelimiters);
				string[] LKeyNames = new string[LFirstKeyNames.Length + LSecondKeyNames.Length];
				for (int LIndex = 0; LIndex < LFirstKeyNames.Length; LIndex++)
					LKeyNames[LIndex] = LFirstKeyNames[LIndex];
					
				for (int LIndex = 0; LIndex < LSecondKeyNames.Length; LIndex++)
					LKeyNames[LIndex + LFirstKeyNames.Length] = LSecondKeyNames[LIndex];
				
				return LKeyNames;
			}
			else
				return ASecondKeyNames.Split(DAE.Client.DataView.CColumnNameDelimiters);
		}
		
		public static void DoLookup(ILookup ALookupNode, FormInterfaceHandler AOnFormAccept, FormInterfaceHandler AOnFormReject, System.Collections.IDictionary AState)
		{
			bool LIsReadOnly = (ALookupNode is ILookupElement) && ((ILookupElement)ALookupNode).ReadOnly;
			if (!LIsReadOnly && (ALookupNode.Document != String.Empty) && (ALookupNode.Source != null) && (ALookupNode.Source.DataView != null))
			{
				ALookupNode.Source.DataView.Edit();
				ALookupNode.Source.DataView.RequestSave();
				IFormInterface LForm = ALookupNode.HostNode.Session.LoadForm(ALookupNode, ALookupNode.Document, new FormInterfaceHandler(new LookupContext(ALookupNode).PreLookup));
				try
				{
					// Append the specified state
					if (AState != null)
					{
						foreach (KeyValuePair<string, object> LEntry in AState)
							LForm.UserState.Add(LEntry.Key, LEntry.Value);
					}

					ALookupNode.LookupFormInitialize(LForm);
					
					string[] LColumnNames = GetColumnNames(ALookupNode.MasterKeyNames, ALookupNode.GetColumnNames());
					string[] LLookupColumnNames = GetColumnNames(ALookupNode.DetailKeyNames, ALookupNode.GetLookupColumnNames());

					LForm.CheckMainSource();
					
					LookupUtility.FindNearestRow
					(
						LForm.MainSource.DataSource,
						LLookupColumnNames,
						ALookupNode.Source.DataSource,
						LColumnNames
					);

					LForm.Show
					(
						(IFormInterface)ALookupNode.FindParent(typeof(IFormInterface)),
						AOnFormAccept, 
						AOnFormReject, 
						FormMode.Query
					);
				}
				catch
				{
					LForm.HostNode.Dispose();
					throw;
				}
			}
		}
		
		/// <summary> Locates the nearest matching row in one DataSource given another DataSource. </summary>
		/// <param name="ATarget"> The DataSource to target for the search. </param>
		/// <param name="ATargetColumnNames"> The list of columns to search by. </param>
		/// <param name="ASource"> A DataSource to pull search values from. </param>
		/// <param name="ASourceColumnNames">
		///		Column names corresponding ATargetColumnNames, which map to fields 
		///		within ASource.
		///	</param>
		public static void FindNearestRow(DAE.Client.DataSource ATarget, string[] ATargetColumnNames, DAE.Client.DataSource ASource, string[] ASourceColumnNames)
		{
			//Build the row type
			DAE.Schema.RowType LRowType = new DAE.Schema.RowType();
			string LTrimmedName;
			foreach (string LColumnName in ATargetColumnNames)
			{
				LTrimmedName = LColumnName.Trim();
				LRowType.Columns.Add(new DAE.Schema.Column(LTrimmedName, ATarget.DataSet[LTrimmedName].DataType));
			}

			//Fill in the row values
			bool LFind = true;
			using (DAE.Runtime.Data.Row LRow = new DAE.Runtime.Data.Row(ATarget.DataSet.Process.ValueManager, LRowType))
			{
				for (int i = 0; i < ATargetColumnNames.Length; i++)
					if (!ASource.DataSet[ASourceColumnNames[i].Trim()].HasValue())
					{
						LFind = false;
						break;
					}
					else
						LRow[i] = ASource.DataSet[ASourceColumnNames[i].Trim()].Value;

				DAE.Client.TableDataSet LTargetDataSet = ATarget.DataSet as DAE.Client.TableDataSet;
				if (LFind && (LTargetDataSet != null))
				{
					string LSaveOrder = String.Empty;
					
					// If the view order does not match the row to find
					bool LOrderMatches = true;
					for (int LIndex = 0; LIndex < LRow.DataType.Columns.Count; LIndex++)
						if ((LIndex >= LTargetDataSet.Order.Columns.Count) || !DAE.Schema.Object.NamesEqual(LTargetDataSet.Order.Columns[LIndex].Column.Name, LRow.DataType.Columns[LIndex].Name))
						{
							LOrderMatches = false;
							break;
						}

					if (!LOrderMatches)
					{
						LSaveOrder = LTargetDataSet.OrderString;
						DAE.Schema.Order LNewOrder = new DAE.Schema.Order();
						foreach (DAE.Schema.Column LColumn in LRow.DataType.Columns)
							LNewOrder.Columns.Add(new DAE.Schema.OrderColumn(ATarget.DataSet.TableVar.Columns[LColumn.Name], true));
						LTargetDataSet.Order = LNewOrder;
					}
					try
					{
						LTargetDataSet.FindNearest(LRow);
					}
					finally
					{
						if (LSaveOrder != String.Empty)
							LTargetDataSet.OrderString = LSaveOrder;
					}
				}
			}
		}
	}

	public class LookupContext
	{
		public LookupContext(ILookup ALookup)
		{
			FLookup = ALookup;
		}

		private ILookup FLookup;

		public bool HasMasterSource()
		{
			return ((FLookup.MasterKeyNames != String.Empty) && (FLookup.DetailKeyNames != String.Empty) && (FLookup.Source != null));
		}
		
		public void PreLookup(IFormInterface AForm)
		{
			if (HasMasterSource())
			{
				AForm.CheckMainSource();
				AForm.MainSource.MasterKeyNames = FLookup.MasterKeyNames;
				AForm.MainSource.DetailKeyNames = FLookup.DetailKeyNames;
				AForm.MainSource.Master = FLookup.Source;
			}
		}

	}
}