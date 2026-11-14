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
        private Container _components;
        private Ancestors _ancestors;

        public CustomFormDesigner() // dummy constructor for MDI menu merging?
        {
            InitializeComponent();
        }

        public CustomFormDesigner(IDataphoria dataphoria, string designerID) : base(dataphoria, designerID)
        {
            InitializeComponent();
        }

        [Browsable(false)]
        public Ancestors Ancestors
        {
            get { return _ancestors; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_components != null)
                {
                    _components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        public override void New()
        {
            var ancestors = new Ancestors();
            var documentExpression = new DocumentExpression();
            documentExpression.Type = DocumentType.Document;
            documentExpression.DocumentArgs.OperatorName = ".Frontend.Form";
            ancestors.Add(DocumentExpressionForm.Execute(documentExpression));
            New(ancestors);
        }

        /// <summary> Initializes the designer from a list of ancestor document expressions. </summary>
        /// <param name="ancestors"> List of ancestor document expressions. Must be non-empty. </param>
        public void New(Ancestors ancestors)
        {
            XDocument ancestor = MergeAncestors(ancestors);
            InternalNew(HostFromDocumentData(ancestor, String.Empty), true);
            UpdateReadOnly(ancestor);
            _ancestors = ancestors;
        }

        public void CheckHostsDocument(IHost host)
        {
            if ((host.Document == null) || (host.Document == String.Empty))
                throw new DataphoriaException(DataphoriaException.Codes.CannotCustomizeDocument);
        }

        public new void New(IHost host)
        {
            CheckHostsDocument(host);
            InternalNew(host, false);
            _ancestors = new Ancestors();
            _ancestors.Add(host.Document);
            UpdateReadOnly(MergeAncestors(_ancestors));
        }

        public override void Open(IHost host)
        {
            CheckHostsDocument(host);
            DocumentDesignBuffer buffer = BufferFromHost(host);
            Service.ValidateBuffer(buffer);
            var document = new DilxDocument();
            document.Read(buffer.LoadData());
            XDocument mergedAncestors = MergeAncestors(document.Ancestors); // do this first in case of errors
            SetDesignHost(host, false);
            Service.SetBuffer(buffer);
            Service.SetModified(false);
            _ancestors = document.Ancestors;
            UpdateReadOnly(mergedAncestors);
        }

        protected override void RequestLoad(DesignService service, DesignBuffer buffer)
        {
            var document = new DilxDocument();
            document.Read(buffer.LoadData());

            Ancestors ancestors;
            IHost host;
            XDocument ancestor = null;
            if (document.Ancestors.Count >= 1)
            {
                ancestors = document.Ancestors;
                ancestor = MergeAncestors(ancestors);
                var merge = XDocument.Load(new StringReader(document.Content));
                var current = new XDocument();
                current.Add(new XElement(ancestor.Root));
                Inheritance.Merge(current, merge);
                host = HostFromDocumentData(current, GetDocumentExpression(buffer));
            }
            else
            {
                host = HostFromDocumentData(document.Content, GetDocumentExpression(buffer));
                ancestors = null;
            }
            SetDesignHost(host, true);
            UpdateReadOnly(ancestor);
            _ancestors = ancestors;
        }

        private XDocument MergeAncestors(Ancestors ancestors)
        {
            XDocument document = null;
            // Process any ancestors
            foreach (string ancestor in ancestors)
            {
                if (document == null)
                    document = LoadDocument(ancestor);
                else
                    Inheritance.Merge(document, LoadDocument(ancestor));
            }
            return document;
        }

        private XDocument LoadDocument(string document)
        {
            using (IScalar resultData = FrontendSession.Pipe.RequestDocument(document))
            {
                try
                {
                    return XDocument.Load(new StringReader(resultData.AsString));
                }
                catch (Exception exception)
                {
                    throw new DataphoriaException(DataphoriaException.Codes.InvalidXMLDocument, exception,
                                                  resultData.ToString());
                }
            }
        }

        private static string XDocumentToString(XDocument document)
        {
            var writer = new StringWriter();
            document.Save(writer);
            return writer.ToString();
        }

        protected override void RequestSave(DesignService service, DesignBuffer buffer)
        {
            var document = new DilxDocument();
            if (_ancestors != null)
                document.Ancestors = _ancestors;

            var content = new XDocument();
            Serializer serializer = FrontendSession.CreateSerializer();
            serializer.Serialize(content, DesignHost.Children[0]);
            Dataphoria.Warnings.AppendErrors(this, serializer.Errors, true);

            if (_ancestors != null)
                content = Inheritance.Diff(MergeAncestors(document.Ancestors), content);
            document.Content = content.Root.ToString(); // only the document node

            buffer.SaveData(document.Write());
            UpdateHostsDocument(buffer);
        }

        private void UpdateReadOnly(XDocument ancestor)
        {
            var rootNode = (DesignerNode)FNodesTree.Nodes[0];
            if (ancestor != null)
            {
                var nodesByName = new Hashtable(StringComparer.OrdinalIgnoreCase);
                BuildNodesByName(nodesByName, rootNode);
                if (ancestor.Root != null)
                    foreach (XElement node in ancestor.Root.Elements()) // (skips root interface)
                        UpdateNodeReadOnly(node, nodesByName);
            }
        }

        private static void BuildNodesByName(Hashtable nodesByName, DesignerNode node)
        {
            if (node.Node.Name != String.Empty)
                nodesByName.Add(node.Node.Name, node);
            foreach (DesignerNode localNode in node.Nodes)
                BuildNodesByName(nodesByName, localNode);
        }

        private static void UpdateNodeReadOnly(XElement node, Hashtable nodesByName)
        {
            XAttribute name = node.Attribute(Persistence.XmlBOPName);
            if (name != null)
            {
                var designerNode = (DesignerNode)nodesByName[name.Value];
                if (designerNode != null)
                    designerNode.SetReadOnly(true, false);
            }
            foreach (XElement localNode in node.Elements())
                UpdateNodeReadOnly(localNode, nodesByName);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._components = new System.ComponentModel.Container();
            this.Size = new System.Drawing.Size(300, 300);
            this.Text = "CustomFormDesigner";
        }

        #endregion
    }
}