/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Collections;
using System.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Xml.Linq;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Service
{
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
	public class CrossDomainService : ICrossDomainService
	{
		public Message GetPolicyFile()
		{
			XDocument LDocument;
			using (FileStream LStream = File.Open("clientaccesspolicy.xml", FileMode.Open, FileAccess.Read))
				LDocument = XDocument.Load(new StreamReader(LStream));
				
			// Replace a "dynamicresources" element with a set of resource elements for each instance
			var LDynamic = LDocument.Root.Element("cross-domain-access").Element("policy").Element("grant-to").Element("dynamicresources");
			if (LDynamic != null)
			{
				var LNewElements = new List<object>();
				foreach (var LName in InstanceManager.Instances.Keys)
					LNewElements.Add(new XElement("resource", new XAttribute("path", "/" + LName), new XAttribute("include-subpaths", "true")));
				LDynamic.ReplaceWith(LNewElements);
			}
			
			Message LMessage = Message.CreateMessage(MessageVersion.None, "", LDocument.CreateReader());
			using (MessageBuffer LBuffer = LMessage.CreateBufferedCopy(1000))
			{
				return LBuffer.CreateMessage();
			}
		}
	}
}
