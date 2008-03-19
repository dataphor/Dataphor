/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Data.OleDb;

using OraWrap = Alphora.Dataphor.DAE.Connection.Oracle.Oracle;
using OraNet = System.Data.OracleClient;
using OraHome = Oracle.DataAccess.Client;
using OraNetWrap = Alphora.Dataphor.DAE.Connection.Oracle;
using OraDD = DDTek.Oracle;
using MSSQL = System.Data.SqlClient;
using ODBC = Microsoft.Data.Odbc;
using ADODB;

using Alphora.Dataphor;
using DAE = Alphora.Dataphor.DAE;

namespace ControlsSample
{
	/// <summary>
	/// Summary description for ControlsSampleForm.
	/// </summary>
	public class ControlsSampleForm : System.Windows.Forms.Form
	{
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.Button button5;
		private System.Windows.Forms.Button button4;

		public ControlsSampleForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}
		
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.button5 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// button5
			// 
			this.button5.Location = new System.Drawing.Point(264, 216);
			this.button5.Name = "button5";
			this.button5.Size = new System.Drawing.Size(92, 25);
			this.button5.TabIndex = 9;
			this.button5.Text = "button5";
			this.button5.Click += new System.EventHandler(this.button5_Click);
			// 
			// button4
			// 
			this.button4.Location = new System.Drawing.Point(260, 183);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(84, 25);
			this.button4.TabIndex = 8;
			this.button4.Text = "button4";
			this.button4.Click += new System.EventHandler(this.button4_Click);
			// 
			// ControlsSampleForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(728, 406);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.button5,
																		  this.button4});
			this.Name = "ControlsSampleForm";
			this.Text = "ControlsSampleForm";
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new ControlsSampleForm());
		}
/*		
		private void TestADODB()
		{
			ADODB.Connection LConnection = new ADODB.Connection();
			LConnection.Open("provider=MSDASQL;data source=Oracle;initial catalog=;user id=system;password=manager", String.Empty, String.Empty, -1);

			//LConnection.Open("Provider=MSDASQL;Data Source=Linter 5.9;User ID=SYSTEM;Password=MANAGER", String.Empty, String.Empty, -1);
			//LConnection.Open("Provider=SQLOLEDB;Data Source=SARLACC\\SARLACC;Initial Catalog=Test;User ID=sa;Integrated Security=SSPI", String.Empty, String.Empty, -1);
			//LConnection.Open("Provider=MSDASQL;Data Source=DB2TEST;User ID=db2admin;Password=db2admin", String.Empty, String.Empty, -1);
			//LConnection.Open("Provider=MSDASQL;Data Source=OracleTest;User ID=SYSTEM;Password=manager", String.Empty, String.Empty, -1);
			//LConnection.CursorLocation = ADODB.CursorLocationEnum.adUseServer;
			//ADODB.Command LCommand = new ADODB.Command();
			//LCommand.ActiveConnection = LConnection;
			//LCommand.Properties["Preserve on Commit"].Value = true;
			//LCommand.Properties["Preserve on Abort"].Value = true;
			//LCommand.CommandText = @"select * from Shipping_Customer";
			
//			LCommand.CommandText = 
//				@"
//					select 
//						case when not(exists((select 
//							*
//							from (select 
//								ID ID, Name Name
//								from TestTable (fastfirstrow)
//								where (ID = ?)) as T1))) then 1 else 0 end dummy1
//						from (select 
//							0 dummy1) as dummy1;				
//				";
				
			//LCommand.Parameters.Append(LCommand.CreateParameter("@P1", ADODB.DataTypeEnum.adInteger, ADODB.ParameterDirectionEnum.adParamInput, 0, 1));
			//LCommand.Parameters.Append(LCommand.CreateParameter("@P2", ADODB.DataTypeEnum.adInteger, ADODB.ParameterDirectionEnum.adParamInput, 0, 1));
				
			//LCommand.Prepared = true;
			//object LRecordsAffected;
			//object LParameters = System.Reflection.Missing.Value;
			//LCommand.Execute(out LRecordsAffected, ref LParameters, -1);
			//LConnection.BeginTrans();
			//ADODB.Recordset LRecordset = new ADODB.Recordset();
			//ADODB.Recordset LRecordset = LCommand.Execute(out LRecordsAffected, ref LParameters, -1);
			//for (int LIndex = 0; LIndex < 20; LIndex++)
			//	LRecordset.MoveNext();
			//LRecordset.Source = LCommand;
			//LRecordset.Properties["Preserve on Commit"].Value = true;
			//LRecordset.Properties["Preserve on Abort"].Value = true;
			//LRecordset.Open(Missing.Value, Missing.Value, ADODB.CursorTypeEnum.adOpenDynamic, ADODB.LockTypeEnum.adLockReadOnly, -1);
			//MessageBox.Show(String.Format("Preserve on Commit: {0}", LRecordset.Properties["Preserve on Commit"].Value.ToString()));
			//LConnection.BeginTrans();
			//LConnection.CommitTrans();
			//object AValue = LRecordset.Fields[0].Value;
			//LRecordset.Close();
			
			//ADODB.Recordset LRecordset = LConnection.Execute("select * from Person", out LRecordsAffected, -1);
//			ADODB.Recordset LRecordset =
//				LConnection.Execute
//				(
//					@"
//						select 
//							Main_id Main_id, 
//							Main_LastName Main_LastName, 
//							Main_FirstName Main_FirstName 
//							from 
//							(
//								select 
//										id Main_id, 
//										LastName Main_LastName, 
//										FirstName Main_FirstName 
//									from Names (fastfirstrow)
//							) as T1 
//							where ((not(Main_id is null) and not(Main_FirstName is null)) and not(Main_LastName is null)) 
//							order by Main_LastName, Main_FirstName, Main_ID
//					",
//					"create table SP ( SNUM varchar(5) not null, PNUM varchar(6) not null, QTY integer not null )",
//					//"select * from Names (fastfirstrow) order by id",
//					out LRecordsAffected,
//					-1
//				);
//			while (!LRecordset.EOF)
//				LRecordset.MoveNext();
//			LRecordset.Close();
			LConnection.Close();
		}
		
		private void TestOLEDB()
		{
			OleDbConnection LConnection = new OleDbConnection(@"Provider=SQLOLEDB;Data Source=OBIWAN;Initial Catalog=Customers;User ID=sa");
			try
			{
				LConnection.Open();
				OleDbCommand LCommand1 = LConnection.CreateCommand();
				try
				{
					OleDbCommand LCommand2 = LConnection.CreateCommand();
					try
					{
						LCommand1.CommandText = "select ID, LastName, FirstName from Names (fastfirstrow) order by LastName, FirstName";
						LCommand1.CommandType = CommandType.Text;
						OleDbDataReader LReader1 = LCommand1.ExecuteReader();
						try
						{
							OleDbDataReader LReader2 = LCommand2.ExecuteReader();
							try
							{
							}
							finally
							{
								LReader2.Close();
							}
						}
						finally
						{
							LReader1.Close();
						}
					}
					finally
					{
						LCommand2.Dispose();
					}
				}
				finally
				{
					LCommand1.Dispose();
				}
			}
			finally
			{
				LConnection.Dispose();
			}
		}
		
		private void TestADO(int AVersion)
		{
			using (DAE.Connection.ADO.ADOConnection LConnection = new DAE.Connection.ADO.ADOConnection(@"Provider=SQLOLEDB;Data Source=.;Initial Catalog=Customers;User ID=sa;Password="))
			{
				using (DAE.Connection.SQLCommand LCommand = LConnection.CreateCommand())
				{
					switch (AVersion)
					{
						case 0:
							LCommand.Statement =
								@"
									select 
										Main_id Main_id, Main_Name Main_Name
										from (select 
											id Main_id, Name Main_Name
											from CombinedNames (fastfirstrow)) as T1
										where (((Main_Name = @P1) and (Main_id < @P2)) or ((Main_Name < @P3) and not(Main_id  is null)))
									order by Main_Name desc, Main_id desc;							
								";
								
							LCommand.Parameters.Add(new DAE.Connection.SQLParameter("P1", new DAE.Connection.SQLStringType(80), "Eades, Dianna"));
							LCommand.Parameters.Add(new DAE.Connection.SQLParameter("P2", new DAE.Connection.SQLIntegerType(4), 700634));
							LCommand.Parameters.Add(new DAE.Connection.SQLParameter("P3", new DAE.Connection.SQLStringType(80), "Eades, Dianna"));
						break;
						case 1:
							LCommand.Statement =
								@"
									select 
										Main_id Main_id, Main_Name Main_Name
										from (select 
											id Main_id, Name Main_Name
											from CombinedNames (fastfirstrow)) as T1
										where (((Main_Name = 'Eades, Dianna') and (Main_id < 700634)) or ((Main_Name < 'Eades, Dianna') and not(Main_id  is null)))
									order by Main_Name desc, Main_id desc;							
								";
						break;
						case 2:
							LCommand.Statement =
								@"
									select 
										Main_id Main_id, Main_Name Main_Name
										from (select 
											id Main_id, Name Main_Name
											from CombinedNames (fastfirstrow)) as T1
										where ((Main_Name >= 'R') and (Main_id >= 555091))
									order by Main_Name asc, Main_id asc;
								";
						break;

					}

					using (DAE.Connection.SQLReader LReader = LCommand.Open(DAE.Connection.SQLCursorType.Dynamic, DAE.Connection.SQLIsolationLevel.ReadUncommitted))
					{
						int LCounter = 0;
						while ((LCounter++ < 20) && LReader.Next())
						{
							object LObject;
							for (int LIndex = 0; LIndex < LReader.ColumnCount; LIndex++)
								LObject = LReader[LIndex];
						}
					}
				}
			}
//			SqlConnection LConnection = new SqlConnection(@"Server=obiwan;User ID=sa;Database=Customers;Pooling=false");
//			try
//			{
//				LConnection.Open();
//				SqlTransaction LTransaction = LConnection.BeginTransaction();
//				SqlCommand LCommand = LConnection.CreateCommand();
//				try
//				{
//  //					LCommand.CommandText = "select * from Customer (fastfirstrow) order by name";
//					LCommand.CommandText = @"select ID, LastName, FirstName from Names (fastfirstrow) order by LastName, FirstName";
//					LCommand.CommandType = CommandType.Text;
//					SqlDataReader LReader = LCommand.ExecuteReader();
//					try
//					{
//						SqlConnection LNewConnection = new SqlConnection(@"Server=obiwan;User ID=sa;Database=Customers;Pooling=false");
//						LNewConnection.Open();
//						//SqlTransaction LTransaction = LNewConnection.BeginTransaction();
//						//LCommand.Transaction = LTransaction;
//						//try
//						//{
//							int LCounter = 0;
//							while (LReader.Read())
//							{
//								LCounter++;
//								if (LCounter > 20)
//									break;
//							}
//						//}
//						//finally
//						//{
//						//	LTransaction.Commit();
//						//}
//						//LNewConnection.Dispose();
//					}
//					finally
//					{
//						LCommand.Cancel();
//						LReader.Close();
//					}
//				}
//				finally
//				{
//					LCommand.Dispose();
//				}
//			}
//			finally
//			{
//				LConnection.Dispose();
//			}
		}
		
//		private void TestSerializer()
//		{
//			FServer.SaveCatalog(DAE.Server.Server.GetGlobalCatalogFileName());
//		}
//		
//		private void LoadCatalog()
//		{
//			FServer.ClearCatalog();
//			FServer.LoadCatalog(DAE.Server.Server.GetGlobalCatalogFileName());
//		}
//
		private void TestPrecedence()
		{
			int a, b, c;
			
			a = 20;
			b = 10;
			c = 10;
			
			MessageBox.Show((b - c + a).ToString());
			MessageBox.Show((a + b - c).ToString());
		}
		
		private void LoopPerformance()
		{
			//int LCount = 1000;
			
		}
		
		private void button1_Click(object sender, System.EventArgs e)
		{
			try
			{
				Cursor = Cursors.WaitCursor;
				try
				{
					//mssqlDataSession1.Active = true;
					TestADODB();
					//TestADO(0);
					//TestADO(1);
					//TestADO(2);
//					if (FDataSession.Active)
//					{
//						FDataSession.Close();
//						button1.Text = "Open Session";
//					}
//					else
//					{
//						//FDataSession.ServerUri = textBox1.Text;
//						FDataSession.Open();
//						button1.Text = "Close Session";
//					}
				}
				finally
				{
					Cursor = Cursors.Default;
				}
			}
			catch (Exception E)
			{
				MessageBox.Show(E.Message);
			}
		}

		private void button2_Click(object sender, System.EventArgs e)
		{
			dataView1.Active = true;
			dbGrid1.Source = dataSource1;
//			try
//			{
//				Cursor = Cursors.WaitCursor;
//				try
//				{
//					if (FDataView.Active)
//					{
//						FDataView.Close();
//						button2.Text = "Open View";
//					}
//					else
//					{
//						FDataView.Expression = textBox2.Text;
//						FDataView.Open();
//						button2.Text = "Close View";
//					}
//				}
//				finally
//				{
//					Cursor = Cursors.Default;
//				}
//			}
//			catch (Exception E)
//			{
//				MessageBox.Show(E.Message);
//			}
		}

		private void button3_Click(object sender, System.EventArgs e)
		{
			PubInfoEdit LEditForm = new PubInfoEdit();
			try
			{
				dataView1.Edit();
				try
				{
					LEditForm.dataSource1.View = dataView1;
					if (LEditForm.ShowDialog() == DialogResult.OK)
						dataView1.Post();
				}
				catch
				{
					dataView1.Cancel();
					throw;
				}
			}
			finally
			{
				LEditForm.Dispose();
			}
		}
*/
		private void button4_Click(object sender, System.EventArgs e)
		{
			try
			{

//create connection (ALL)
				//OraHome.OraConnection LTemp = new OraHome.OraConnection();
				//OraNet.OracleConnection LTemp = new OraNet.OracleConnection("Data Source=TEST9.EMPIRE;User ID=system;Password=manager");
				//OraDD.OracleConnection LTemp = new OraDD.OracleConnection("Data Source=TEST9.EMPIRE;User ID=system;Password=manager");
				//OraWrap.OracleConnection LTemp = new OraWrap.OracleConnection("User Id=system;Password=manager;Data Source=TEST9.EMPIRE");
				//OraNetWrap.OracleConnection LTemp = new OraNetWrap.OracleConnection("Data Source=TEST9.EMPIRE;User ID=system;Password=manager");
				//MSSQL.SqlConnection LTemp = new MSSQL.SqlConnection("user id=sa;password=;data source=obiwan;database=Customers");
				ODBC.OdbcConnection LTemp = new ODBC.OdbcConnection("DSN=DB2SAMPL;UID=db2admin;PWD=db2admin");



//set connsection string (those which aren't in constructor)
				//LTemp.ConnectionString ="User Id=system;Password=manager;Data Source=TEST9.EMPIRE";

//open connection (not for wrappers)
				LTemp.Open();



//create transaction (not for wrappers)
				//OraHome.OraTransaction LTransaction = LTemp.BeginTransaction(IsolationLevel.Serializable);
                //OraNet.OracleTransaction LTransaction = LTemp.BeginTransaction(IsolationLevel.Serializable);
				//OraDD.OracleTransaction LTransaction = LTemp.BeginTransaction(IsolationLevel.Serializable);
				//MSSQL.SqlTransaction LTransaction = LTemp.BeginTransaction(IsolationLevel.Serializable);
				//ODBC.OdbcTransaction LTransaction = LTemp.BeginTransaction(IsolationLevel.Serializable);


//create command (ALL) [all wrappers use the same one]
				//OraHome.OraCommand LTempCommand = LTemp.CreateCommand();
				//OraNet.OracleCommand LTempCommand = LTemp.CreateCommand();
				//OraDD.OracleCommand LTempCommand = LTemp.CreateCommand();
				//MSSQL.SqlCommand LTempCommand = LTemp.CreateCommand();
				ODBC.OdbcCommand LTempCommand = LTemp.CreateCommand();
				//DAE.Connection.SQLCommand LTempCommand = LTemp.CreateCommand();//wrappers
                
//set command statement (all)
				//LTempCommand.Statement = "create table blah5556 (ID integer not null)";//wrappers
				LTempCommand.CommandText = "create table Test_whoblah58 (id integer not null)";
				//LTempCommand.CommandText = "select * from Names;";
				
//begin transaction (not for wrappers)				
				//LTempCommand.Transaction = LTransaction;
				
//run (all) [all wrappers use the same one][all non wrappers use the same one]
				LTempCommand.ExecuteNonQuery();
				//LTempCommand.Execute();//Wrapped

			//unique to this session of MSSQL
				/*
				MSSQL.SqlDataReader LReader = LTempCommand.ExecuteReader();
				LReader.Read();
				LTempCommand.Cancel();
				LReader.Close();
				*/


//committ transaction (not for wrappers)
				//LTransaction.Commit();

				MessageBox.Show("Completed");

			}
			catch(Exception LE)
			{
				MessageBox.Show(Alphora.Dataphor.ExceptionUtility.DetailedDescription( LE ) );
				//throw LE;
			}
		}

		private void button5_Click(object sender, System.EventArgs e)
		{
				try
				{

					ADODB.ConnectionClass LTemp = new ADODB.ConnectionClass();

					LTemp.Open("provider=SQLOLEDB;initial catalog=Customers;data source=OBIWAN;user id=sa;password=", String.Empty, String.Empty, -1);

					//create transaction?

					ADODB.Command LTempCommand = new ADODB.Command();
					LTempCommand.ActiveConnection = LTemp;

					//ADODB.CommandClass LTempCommand = LTemp.
					//MSSQL.SqlCommand LTempCommand = LTemp.CreateCommand();

					LTempCommand.CommandText = "select * from Names;";

					LTemp.BeginTrans();

					//unique to this session of MSSQL
					object LRecordsAffected;
					object LParameters = System.Reflection.Missing.Value;
					ADODB.Recordset LRecordSet = LTempCommand.Execute(out LRecordsAffected, ref LParameters, -1);
					LRecordSet.MoveNext();
					//ADODB.MSSQL.SqlDataReader LReader = LTempCommand.ExecuteReader();
					//LReader.Read();
					//LReader.Close();
					LRecordSet.Close();

					LTemp.CommitTrans();
				}
				catch(Exception LE)
				{
					throw LE;
				}
			MessageBox.Show("Completed");
		
		}
	}
}
