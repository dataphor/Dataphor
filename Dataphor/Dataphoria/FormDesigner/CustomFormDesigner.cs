/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Xml;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using Serializer=Alphora.Dataphor.Frontend.Client.Serializer;
using Alphora.Dataphor.Dataphoria.FormDesigner.DesignerTree;
using System.Xml.Linq;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
    public class CustomFormDesigner : FormDesigner
    {
        private Container FComponents;
        private Ancestors FAncestors;

        public CustomFormDesigner() // dummy constructor for MDI menu merging?
        {
            InitializeComponent();
        }

        public CustomFormDesigner(IDataphoria ADataphoria, string ADesignerID) : base(ADataphoria, ADesignerID)
        {
            InitializeComponent();
        }

        [Browsable(false)]
        public Ancestors Ancestors
        {
            get { return FAncestors; }
        }

        protected override void Dispose(bool ADisposing)
        {
            if (ADisposing)
            {
                if (FComponents != null)
                {
                    FComponents.Dispose();
                }
            }
            base.Dispose(ADisposing);
        }

        public override void New()
        {
            var LAncestors = new Ancestors();
            var LDocumentExpression = new DocumentExpression();
            LDocumentExpression.Type = DocumentType.Document;
            LDocumentExpression.DocumentArgs.OperatorName = ".Frontend.Form";
            LAncestors.Add(DocumentExpressionForm.Execute(LDocumentExpression));
            New(LAncestors);
        }

        /// <summary> Initializes the designer from a list of ancestor document expressions. </summary>
        /// <param name="AAncestors"> List of ancestor document expressions. Must be non-empty. </param>
        public void New(Ancestors AAncestors)
        {
            XDocument LAncestor = MergeAncestors(AAncestors);
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
            var LDocument = new DilxDocument();
            LDocument.Read(LBuffer.LoadData());
            XDocument LMergedAncestors = MergeAncestors(LDocument.Ancestors); // do this first in case of errors
            SetDesignHost(AHost, false);
            Service.SetBuffer(LBuffer);
            Service.SetModified(false);
            FAncestors = LDocument.Ancestors;
            UpdateReadOnly(LMergedAncestors);
        }

        protected override void RequestLoad(DesignService AService, DesignBuffer ABuffer)
        {
            var LDocument = new DilxDocument();
            LDocument.Read(ABuffer.LoadData());

            Ancestors LAncestors;
            IHost LHost;
            XDocument LAncestor = null;
            if (LDocument.Ancestors.Count >= 1)
            {
                LAncestors = LDocument.Ancestors;
                LAncestor = MergeAncestors(LAncestors);
                var LMerge = XDocument.Load(new StringReader(LDocument.Content));
                var LCurrent = new XDocument();
                LCurrent.Add(new XElement(LAncestor.Root));
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

        private XDocument MergeAncestors(Ancestors AAncestors)
        {
            XDocument LDocument = null;
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

        private XDocument LoadDocument(string ADocument)
        {
            using (Scalar LResultData = FrontendSession.Pipe.RequestDocument(ADocument))
            {
                try
                {
                    return XDocument.Load(new StringReader(LResultData.AsString));
                }
                catch (Exception LException)
                {
                    throw new DataphoriaException(DataphoriaException.Codes.InvalidXMLDocument, LException,
                                                  LResultData.ToString());
                }
            }
        }

        private static string XDocumentToString(XDocument ADocument)
        {
            var LWriter = new StringWriter();
            ADocument.Save(LWriter);
            return LWriter.ToString();
        }

        protected override void RequestSave(DesignService AService, DesignBuffer ABuffer)
        {
            var LDocument = new DilxDocument();
            if (FAncestors != null)
                LDocument.Ancestors = FAncestors;

            var LContent = new XDocument();
            Serializer LSerializer = FrontendSession.CreateSerializer();
            LSerializer.Serialize(LContent, DesignHost.Children[0]);
            Dataphoria.Warnings.AppendErrors(this, LSerializer.Errors, true);

            if (FAncestors != null)
                LContent = Inheritance.Diff(MergeAncestors(LDocument.Ancestors), LContent);
            LDocument.Content = LContent.Root.ToString(); // only the document node

            ABuffer.SaveData(LDocument.Write());
            UpdateHostsDocument(ABuffer);
        }

        private void UpdateReadOnly(XDocument AAncestor)
        {
            var LRootNode = (DesignerNode)FNodesTree.Nodes[0];
            if (AAncestor != null)
            {
                var LNodesByName = new Hashtable(StringComparer.OrdinalIgnoreCase);
                BuildNodesByName(LNodesByName, LRootNode);
                if (AAncestor.Root != null)
                    foreach (XElement LNode in AAncestor.Root.Elements()) // (skips root interface)
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

        private static void UpdateNodeReadOnly(XElement ANode, Hashtable ANodesByName)
        {
            XAttribute LName = ANode.Attribute(Persistence.CXmlBOPName);
            if (LName != null)
            {
                var LDesignerNode = (DesignerNode)ANodesByName[LName.Value];
                if (LDesignerNode != null)
                    LDesignerNode.SetReadOnly(true, false);
            }
            foreach (XElement LNode in ANode.Elements())
                UpdateNodeReadOnly(LNode, ANodesByName);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.FComponents = new System.ComponentModel.Container();
            this.Size = new System.Drawing.Size(300, 300);
            this.Text = "CustomFormDesigner";
        }

        #endregion
    }
}