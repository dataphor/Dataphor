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

using Alphora.Dataphor.DAE.Contracts;
using System.Collections;
using System.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace Alphora.Dataphor.DAE.Service
{
	public class CrossDomainService : ICrossDomainService
	{
		public Message GetPolicyFile()
		{
			using (FileStream LStream = File.Open("clientaccesspolicy.xml", FileMode.Open))
			{
				using (XmlReader LReader = XmlReader.Create(LStream))
				{
					Message LMessage = Message.CreateMessage(MessageVersion.None, "", LReader);
					
					using (MessageBuffer LBuffer = LMessage.CreateBufferedCopy(1000))
					{
						return LBuffer.CreateMessage();
					}
				}
			}
		}
	}
}
