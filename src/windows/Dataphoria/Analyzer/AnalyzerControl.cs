/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;
using System.Drawing;
using System.Xml.Linq;

using Alphora.Dataphor.Dataphoria.Visual;
using System.IO;

namespace Alphora.Dataphor.Dataphoria.Analyzer
{
	public class AnalyzerControl : DesignerControl
	{
		public AnalyzerControl()
		{
			_mainSurface = new PlanTree();
			Push(_mainSurface, Strings.Analyzer_MainSurfaceTitle);
		}

		private PlanTree _mainSurface;
		public PlanTree MainSurface { get { return _mainSurface; } }

		private XDocument _document;
		private XElement _root;

		public void Load(string plan)
		{
			XDocument document = XDocument.Load(new StringReader(plan));
			Clear();
			Set(document.Root);
			_document = document;
		}

		public string Save()
		{
			if (_document != null)
			{
				System.IO.StringWriter writer = new System.IO.StringWriter();
				_document.Save(writer);
				return writer.ToString();
			}
			else
				return String.Empty;
		}

		public virtual void Clear()
		{
			PopAllButTop();
			_root = null;
			_document = null;
			_mainSurface.Clear();
		}

		private void Set(XElement root)
		{
			_root = root;
			_mainSurface.Set(root);
		}
	}
}
