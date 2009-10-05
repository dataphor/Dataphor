/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Channels;

namespace Alphora.Dataphor.DAE.Contracts
{
	[ServiceContract(Name = "ICrossDomainService", Namespace = "http://dataphor.org/dataphor/3.0/")]
	public interface ICrossDomainService
	{
		[OperationContract]
		[WebGet(UriTemplate = "clientaccesspolicy.xml")]
		Message GetPolicyFile();
	}
}
