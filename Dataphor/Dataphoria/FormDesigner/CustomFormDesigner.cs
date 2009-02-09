/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using System.IO;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Dataphoria;
using Alphora.Dataphor.Dataphoria.Designers;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
	public class CustomFormDesigner : FormDesigner
	{
		private System.ComponentModel.Container components = null;

		public CustomFormDesigner() : base()	// dummy constructor for SyncFusion's MDI menu merging
		{
			InitializeComponent();
		}

		public CustomFormDesigner(IDataphoria ADataphoria, string ADesignerID) : base(ADataphoria, ADesignerID)
		{
			InitializeComponent();
		}

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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.Size = new System.Drawing.Size(300,300);
			this.Text = "CustomFormDesigner";
		}
		#endregion

		private Ancestors FAncestors;
		[Browsable(false)]
		public Ancestors Ancestors
		{
			get { return FAncestors; }
		}

		public override void New()
		{
			Ancestors LAncestors = new Ancestors();
			Frontend.Client.Windows.DocumentExpression LDocumentExpression = new Frontend.Client.Windows.DocumentExpression();
			LDocumentExpression.Type = Frontend.Client.Windows.DocumentType.Document;
			LDocumentExpression.DocumentArgs.OperatorName = ".Frontend.Form";
			LAncestors.Add(DocumentExpressionForm.Execute(LDocumentExpression));
			New(LAncestors);
		}

		/// <summary> Initializes the designer from a list of ancestor document expressions. </summary>
		/// <param name="AAncestors"> List of ancestor document expressions. Must be non-empty. </param>
		public void New(Ancestors AAncestors)
		{
			XmlDocument LAncestor = MergeAncestors(AAncestors);
			InternalNew(HostFromDocumentData(LAncestor, String.Empty), true);
			UpdateReadOnly(LAncestor);
			FAncestors = AAncestors;
		}

		public void CheckHostsDocument(IHost AHost)
		{
			if ((AHost.Document == null) || (AHost.Document == String.Empty))
				throw new DataphoriaException(DataphoriaException.Codes.CannotCustomizeDocument);
		}

		public new void New(IHost AHost)
		{
			CheckHostsDocument(AHost);
			InternalNew(AHost, false);
			FAncestors = new Ancestors();
			FAncestors.Add(AHost.Document);
			UpdateReadOnly(MergeAncestors(FAncestors));
		}

		public override void Open(IHost AHost)
		{
			CheckHostsDocument(AHost);
			DocumentDesignBuffer LBuffer = BufferFromHost(AHost);
			Service.ValidateBuffer(LBuffer);
			DilxDocument LDocument = new DilxDocument();
			LDocument.Read(LBuffer.LoadData());
			XmlDocument LMergedAncestors = MergeAncestors(LDocument.Ancestors);	// do this first in case of errors
			SetDesignHost(AHost, false);
			Service.SetBuffer(LBuffer);
			Service.SetModified(false);
			FAncestors = LDocument.Ancestors;
			UpdateReadOnly(LMergedAncestors);
		}

		protected override void RequestLoad(DesignService AService, DesignBuffer ABuffer)
		{
			DilxDocument LDocument = new DilxDocument();
			LDocument.Read(ABuffer.LoadData());

			Ancestors LAncestors;
			IHost LHost;
			XmlDocument LAncestor = null;
			if (LDocument.Ancestors.Count >= 1)
			{
				LAncestors = LDocument.Ancestors;
				LAncestor = MergeAncestors(LAncestors);
				XmlDocument LMerge = new XmlDocument();
				LMerge.LoadXml(LDocument.Content);
				XmlDocument LCurrent = (XmlDocument)LAncestor.CloneNode(true);
				Inheritance.Merge(LCurrent, LMerge);
				LHost = HostFromDocumentData(LCurrent, GetDocumentExpression(ABuffer));
			}
			else
			{
				LHost = HostFromDocumentData(LDocument.Content, GetDocumentExpression(ABuffer));
				LAncestors = null;
			}
			SetDesignHost(LHost, true);
			UpdateReadOnly(LAncestor);
			FAncestors = LAncestors;
		}

		private XmlDocument MergeAncestors(Ancestors AAncestors)
		{
			XmlDocument LDocument = null;
			// Process any ancestors
			foreach (string LAncestor in AAncestors)
			{
				if (LDocument == null)
					LDocument = LoadDocument(LAncestor);
				else
					Inheritance.Merge(LDocument, LoadDocument(LAncestor));
			}
			return LDocument;
		}

		private XmlDocument LoadDocument(string ADocument)
		{
			XmlDocument LResult = new XmlDocument();
			using (DAE.Runtime.Data.DataValue LResultData = FrontendSession.Pipe.RequestDocument(ADocument))
			{
				try
				{
					LResult.LoadXml(LResultData.AsString);
				}
				catch (Exception LException)
				{
					throw new DataphoriaException(DataphoriaException.Codes.InvalidXMLDocument, LException, LResultData.ToString());
				}
			}
			return LResult;
		}

		private static string XmlDocumentToString(XmlDocument ADocument)
		{
			StringWriter LWriter = new StringWriter();
			ADocument.Save(LWriter);
			return LWriter.ToString();
		}

		protected override void RequestSave(DesignService AService, DesignBuffer ABuffer)
		{
			DilxDocument LDocument = new DilxDocument();
			if (FAncestors != null)
				LDocument.Ancestors = FAncestors;

			XmlDocument LContent = new XmlDocument();
			Frontend.Client.Serializer LSerializer = FrontendSession.CreateSerializer();
			LSerializer.Serialize(LContent, DesignHost.Children[0]);
			Dataphoria.Warnings.AppendErrors(this, LSerializer.Errors, true);
			
			if (FAncestors != null)
				LContent = Inheritance.Diff(MergeAncestors(LDocument.Ancestors), LContent);
			StringWriter LWriter = new StringWriter();
			XmlTextWriter LXmlWriter = new XmlTextWriter(LWriter);
			LXmlWriter.Formatting = Formatting.Indented;
			LXmlWriter.Indentation = 3;
			LContent.WriteContentTo(LXmlWriter);
			LDocument.Content = LWriter.ToString();	// only the document node

			ABuffer.SaveData(LDocument.Write());
			UpdateHostsDocument(ABuffer);
		}

		private void UpdateReadOnly(XmlDocument AAncestor)
		{
			DesignerNode LRootNode = (DesignerNode)FNodesTree.Nodes[0];
			if (AAncestor != null)
			{
				Hashtable LNodesByName = new Hashtable(StringComparer.OrdinalIgnoreCase);
				BuildNodesByName(LNodesByName, LRootNode);
				foreach (XmlNode LNode in AAncestor.DocumentElement.ChildNodes) // (skips root interface)
					UpdateNodeReadOnly(LNode, LNodesByName);
			}
		}

		private static void BuildNodesByName(Hashtable ANodesByName, DesignerNode ANode)
		{
			if (ANode.Node.Name != String.Empty)
				ANodesByName.Add(ANode.Node.Name, ANode);
			foreach (DesignerNode LNode in ANode.Nodes)
				BuildNodesByName(ANodesByName, LNode);
		}

		private static void UpdateNodeReadOnly(XmlNode ANode, Hashtable ANodesByName) 
		{
			XmlAttribute LName = ANode.Attributes[BOP.Persistence.CXmlBOPName];
			if (LName != null) 
			{
				DesignerNode LDesignerNode = (DesignerNode)ANodesByName[LName.Value];
				if (LDesignerNode != null)
					LDesignerNode.SetReadOnly(true, false);
			}
			foreach (XmlNode LNode in ANode.ChildNodes)
				UpdateNodeReadOnly(LNode, ANodesByName);
		}
	}
}
