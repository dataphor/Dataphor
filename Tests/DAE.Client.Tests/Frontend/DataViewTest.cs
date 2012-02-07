using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Client.Tests.Frontend
{
	using Alphora.Dataphor.DAE.Client.Controls;
	using System.Windows.Forms;

	[TestFixture]
	public class DataViewTest : OutOfProcessTestFixture
	{
		[Test]
		public void TestDataView()
		{
			DataSession.Execute("create table TestForDataView { ID : Integer, Name : String, key { ID } };");
			DataSession.Execute("insert row { 1 ID, 'Joe' Name } into TestForDataView;");

			DataView LDataView = DataSession.OpenDataView("TestForDataView rename Main browse by { Main.ID } capabilities { navigable, backwardsnavigable, bookmarkable, searchable, updateable } isolation browse");
			try
			{
				LDataView.UseApplicationTransactions = false;

				if (!LDataView.IsEmpty())
				{
					LDataView.First();
					LDataView.Edit();
					LDataView["Name"].AsString = "John";
					LDataView.Refresh();
				}
				
				LDataView.Insert();
				LDataView["ID"].AsInt32 = 2;
				LDataView["Name"].AsString = "Jacob";
				LDataView.Post();
				
				if (LDataView["ID"].AsInt32 != 2)
					throw new Exception("Data View Insert Failed");
					
				//DataSession.Execute("delete TestForDataView where ID = 2");
				//LDataView.Refresh();
				
				LDataView.Delete();
				
				if (LDataView.IsEmpty())
					throw new Exception("Data View Delete Failed");
			}
			finally
			{
				LDataView.Dispose();
			}
		}
		
		[Test]
		public void TestAttachedDataView()
		{
			DataSession.Execute("create table TestForAttachedDataView { ID : Integer, Name : String, key { ID } };");
			DataSession.Execute("insert row { 1 ID, 'Joe' Name } into TestForAttachedDataView;");
			
			DataView LDataView = DataSession.OpenDataView("TestForAttachedDataView");
			try
			{
				Form LForm = new Form();
				try
				{
					DataSource LSource = new DataSource();
					LSource.DataSet = LDataView;
					DBGrid LGrid = new DBGrid();
					LGrid.Source = LSource;
					LGrid.Dock = DockStyle.Fill;
					LForm.Controls.Add(LGrid);
					LForm.Show();
					
					Application.DoEvents();

					if (!LDataView.IsEmpty())
					{
						LDataView.First();
						LDataView.Edit();
						LDataView["Name"].AsString = "John";
						LDataView.Refresh();
					}
					
					Application.DoEvents();

					LDataView.Insert();
					LDataView["ID"].AsInt32 = 2;
					LDataView["Name"].AsString = "Jacob";
					LDataView.Post();
					
					Application.DoEvents();

					//LDataView.Delete();
					DataSession.Execute("delete TestForAttachedDataView where ID = 2");
					LDataView.Refresh();
					
					Application.DoEvents();

					if (LDataView["ID"].AsInt32 != 1)
						throw new Exception("Data View Refresh After Delete Failed");
					
					if (LDataView.IsEmpty())
						throw new Exception("Data View Delete Failed");
				}
				finally
				{
					LForm.Dispose();
				}
			}
			finally
			{
				LDataView.Dispose();
			}
		}

		[Test]
		public void TestDeleteAtEOF()
		{
			DataSession.Execute("create table TestForDeleteAtEOF { ID : Integer, Name : String, key { ID } };");
			DataSession.Execute("insert row { 1 ID, 'Joe' Name } into TestForDeleteAtEOF;");
			DataSession.Execute("insert row { 2 ID, 'John' Name } into TestForDeleteAtEOF;");

			DataView LDataView = DataSession.OpenDataView("TestForDeleteAtEOF rename Main browse by { Main.ID } capabilities { navigable, backwardsnavigable, bookmarkable, searchable, updateable } isolation browse");
			try
			{
				LDataView.UseApplicationTransactions = false;
				
				LDataView.Next();
				
				LDataView.Delete();
				
				if (LDataView.IsEmpty())
					throw new Exception("Data View Delete Failed");
			}
			finally
			{
				LDataView.Dispose();
			}
		}
		
		[Test]
		public void TestCancelOfEmpty()
		{
			DataSession.Execute("create table TestForCancelOfEmpty { ID : Integer, Name : String, key { ID } };");

			DataView LDataView = DataSession.OpenDataView("TestForCancelOfEmpty rename Main browse by { Main.ID } capabilities { navigable, backwardsnavigable, bookmarkable, searchable, updateable } isolation browse");
			try
			{
				LDataView.UseApplicationTransactions = false; 				

				LDataView.Insert();
				LDataView["ID"].AsInt32 = 2;
				LDataView["Name"].AsString = "Jacob";
				LDataView.Cancel();						
			}
			finally
			{
				LDataView.Dispose();
			}
		}
	}
}
