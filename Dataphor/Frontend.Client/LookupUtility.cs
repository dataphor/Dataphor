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
		public static string[] GetColumnNames(string firstKeyNames, string secondKeyNames)
		{
			if (firstKeyNames != String.Empty)
			{
				string[] localFirstKeyNames = firstKeyNames.Split(DAE.Client.DataView.ColumnNameDelimiters);
				string[] localSecondKeyNames = secondKeyNames.Split(DAE.Client.DataView.ColumnNameDelimiters);
				string[] keyNames = new string[localFirstKeyNames.Length + localSecondKeyNames.Length];
				for (int index = 0; index < localFirstKeyNames.Length; index++)
					keyNames[index] = localFirstKeyNames[index];
					
				for (int index = 0; index < localSecondKeyNames.Length; index++)
					keyNames[index + localFirstKeyNames.Length] = localSecondKeyNames[index];
				
				return keyNames;
			}
			else
				return secondKeyNames.Split(DAE.Client.DataView.ColumnNameDelimiters);
		}
		
		public static void DoLookup(ILookup lookupNode, FormInterfaceHandler onFormAccept, FormInterfaceHandler onFormReject, System.Collections.IDictionary state)
		{
			bool isReadOnly = (lookupNode is ILookupElement) && ((ILookupElement)lookupNode).ReadOnly;
			if (!isReadOnly && (lookupNode.Document != String.Empty) && (lookupNode.Source != null) && (lookupNode.Source.DataView != null))
			{
				lookupNode.Source.DataView.Edit();
				lookupNode.Source.DataView.RequestSave();
				IFormInterface form = lookupNode.HostNode.Session.LoadForm(lookupNode, lookupNode.Document, new FormInterfaceHandler(new LookupContext(lookupNode).PreLookup));
				try
				{
					// Append the specified state
					if (state != null)
					{
						foreach (KeyValuePair<string, object> entry in state)
							form.UserState.Add(entry.Key, entry.Value);
					}

					lookupNode.LookupFormInitialize(form);
					
					string[] columnNames = GetColumnNames(lookupNode.MasterKeyNames, lookupNode.GetColumnNames());
					string[] lookupColumnNames = GetColumnNames(lookupNode.DetailKeyNames, lookupNode.GetLookupColumnNames());

					form.CheckMainSource();
					
					LookupUtility.FindNearestRow
					(
						form.MainSource.DataSource,
						lookupColumnNames,
						lookupNode.Source.DataSource,
						columnNames
					);

					form.Show
					(
						(IFormInterface)lookupNode.FindParent(typeof(IFormInterface)),
						onFormAccept, 
						onFormReject, 
						FormMode.Query
					);
				}
				catch
				{
					form.HostNode.Dispose();
					throw;
				}
			}
		}
		
		/// <summary> Locates the nearest matching row in one DataSource given another DataSource. </summary>
		/// <param name="target"> The DataSource to target for the search. </param>
		/// <param name="targetColumnNames"> The list of columns to search by. </param>
		/// <param name="source"> A DataSource to pull search values from. </param>
		/// <param name="sourceColumnNames">
		///		Column names corresponding ATargetColumnNames, which map to fields 
		///		within ASource.
		///	</param>
		public static void FindNearestRow(DAE.Client.DataSource target, string[] targetColumnNames, DAE.Client.DataSource source, string[] sourceColumnNames)
		{
			//Build the row type
			DAE.Schema.RowType rowType = new DAE.Schema.RowType();
			string trimmedName;
			foreach (string columnName in targetColumnNames)
			{
				trimmedName = columnName.Trim();
				rowType.Columns.Add(new DAE.Schema.Column(trimmedName, target.DataSet[trimmedName].DataType));
			}

			//Fill in the row values
			bool find = true;
			using (DAE.Runtime.Data.Row row = new DAE.Runtime.Data.Row(target.DataSet.Process.ValueManager, rowType))
			{
				for (int i = 0; i < targetColumnNames.Length; i++)
					if (!source.DataSet[sourceColumnNames[i].Trim()].HasValue())
					{
						find = false;
						break;
					}
					else
						row[i] = source.DataSet[sourceColumnNames[i].Trim()].Value;

				DAE.Client.TableDataSet targetDataSet = target.DataSet as DAE.Client.TableDataSet;
				if (find && (targetDataSet != null))
				{
					string saveOrder = String.Empty;
					
					// If the view order does not match the row to find
					bool orderMatches = true;
					for (int index = 0; index < row.DataType.Columns.Count; index++)
						if ((index >= targetDataSet.Order.Columns.Count) || !DAE.Schema.Object.NamesEqual(targetDataSet.Order.Columns[index].Column.Name, row.DataType.Columns[index].Name))
						{
							orderMatches = false;
							break;
						}

					if (!orderMatches)
					{
						saveOrder = targetDataSet.OrderString;
						DAE.Schema.Order newOrder = new DAE.Schema.Order();
						foreach (DAE.Schema.Column column in row.DataType.Columns)
							newOrder.Columns.Add(new DAE.Schema.OrderColumn(target.DataSet.TableVar.Columns[column.Name], true));
						targetDataSet.Order = newOrder;
					}
					try
					{
						targetDataSet.FindNearest(row);
					}
					finally
					{
						if (saveOrder != String.Empty)
							targetDataSet.OrderString = saveOrder;
					}
				}
			}
		}
	}

	public class LookupContext
	{
		public LookupContext(ILookup lookup)
		{
			_lookup = lookup;
		}

		private ILookup _lookup;

		public bool HasMasterSource()
		{
			return ((_lookup.MasterKeyNames != String.Empty) && (_lookup.DetailKeyNames != String.Empty) && (_lookup.Source != null));
		}
		
		public void PreLookup(IFormInterface form)
		{
			if (HasMasterSource())
			{
				form.CheckMainSource();
				form.MainSource.MasterKeyNames = _lookup.MasterKeyNames;
				form.MainSource.DetailKeyNames = _lookup.DetailKeyNames;
				form.MainSource.Master = _lookup.Source;
			}
		}

	}
}