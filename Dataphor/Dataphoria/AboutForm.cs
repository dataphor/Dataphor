/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Reflection;

namespace Alphora.Dataphor.Dataphoria
{
	/// <summary> Dataphoria "about" form. </summary>
	public partial class AboutForm : System.Windows.Forms.Form
	{


		public AboutForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			FTxtVersion.Text = this.GetType().Assembly.GetName(false).Version.ToString();

            
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);            
            DirectoryInfo directory = new DirectoryInfo(path);
            FileInfo[] files = directory.GetFiles("*.dll", SearchOption.TopDirectoryOnly);

            foreach (FileInfo file in files)
            {
                // Load the file into the application domain.
                try
                {
                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(file.FullName);
                    FLvModules.Items.Add(new ListViewItem(new string[] { assemblyName.Name, assemblyName.Version.ToString() }));
                }
                catch (BadImageFormatException badImageFormatException)
                {
                    //HACK: How do I know if a .dll file is (or not) a .NET assembly?                   
                }                                                
            } 
			
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
		

		private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://www.alphora.com");
		}

		private void linkLabel2_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://www.icsharpcode.net");
		}

		private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("news://news.alphora.com");
		}
		
		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}
			else
				return base.ProcessDialogKey(keyData);
		}
	}
}
