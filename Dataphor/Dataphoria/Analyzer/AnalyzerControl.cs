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

using Alphora.Dataphor.Dataphoria.Visual;

namespace Alphora.Dataphor.Dataphoria.Analyzer
{
	public class AnalyzerControl : DesignerControl
	{
		public AnalyzerControl()
		{
			FMainSurface = new PlanTree();
			Push(FMainSurface, Strings.Get("Analyzer.MainSurfaceTitle"));
		}

		private PlanTree FMainSurface;
		public PlanTree MainSurface { get { return FMainSurface; } }

		private XmlDocument FDocument;
		private XmlElement FRoot;

		public void Load(string APlan)
		{
			XmlDocument LDocument = new XmlDocument();
			LDocument.LoadXml(APlan);
			Clear();
			Set(LDocument.DocumentElement);
			FDocument = LDocument;
		}

		public string Save()
		{
			if (FDocument != null)
			{
				System.IO.StringWriter LWriter = new System.IO.StringWriter();
				FDocument.Save(LWriter);
				return LWriter.ToString();
			}
			else
				return String.Empty;
		}

		public virtual void Clear()
		{
			PopAllButTop();
			FRoot = null;
			FDocument = null;
			FMainSurface.Clear();
		}

		private void Set(XmlElement ARoot)
		{
			FRoot = ARoot;
			FMainSurface.Set(ARoot);
		}
	}
}
