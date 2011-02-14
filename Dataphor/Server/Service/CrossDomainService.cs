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
			XDocument document;
			using (FileStream stream = File.Open("clientaccesspolicy.xml", FileMode.Open, FileAccess.Read))
				document = XDocument.Load(new StreamReader(stream));
				
			// Replace a "dynamicresources" element with a set of resource elements for each instance
			var dynamic = document.Root.Element("cross-domain-access").Element("policy").Element("grant-to").Element("dynamicresources");
			if (dynamic != null)
			{
				var newElements = new List<object>();
				foreach (var name in InstanceManager.Instances.Keys)
					newElements.Add(new XElement("resource", new XAttribute("path", "/" + name), new XAttribute("include-subpaths", "true")));
				dynamic.ReplaceWith(newElements);
			}
			
			Message message = Message.CreateMessage(MessageVersion.None, "", document.CreateReader());
			using (MessageBuffer buffer = message.CreateBufferedCopy(1000))
			{
				return buffer.CreateMessage();
			}
		}
	}
}
