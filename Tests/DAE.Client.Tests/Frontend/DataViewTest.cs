using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Client.Tests.Frontend
{
	[TestFixture]
	public class DataViewTest : OutOfProcessTestFixture
	{
		[Test]
		public void TestDataView()
		{
			DataSession.Execute("create table TestForDataView { ID : Integer, Name : String, key { ID } };");
			DataSession.Execute("insert row { 1 ID, 'Joe' Name } into TestForDataView;");

			DataView LDataView = DataSession.OpenDataView("TestForDataView");
			try
			{
				//LDataView.UseApplicationTransactions = false;
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
				
				LDataView.Delete();
				
				if (LDataView.IsEmpty())
					throw new Exception("Data View Delete Failed");
			}
			finally
			{
				LDataView.Dispose();
			}
		}
	}
}
