<%@ Page language="c#" Codebehind="Applications.aspx.cs" AutoEventWireup="false" Inherits="Alphora.Dataphor.Frontend.Client.Web.Application.Applications" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN" >
<HTML>
	<HEAD>
		<title>Applications</title>
		<meta name="GENERATOR" Content="Microsoft Visual Studio 7.0">
		<meta name="CODE_LANGUAGE" Content="C#">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<LINK href="style.css" type="text/css" rel="stylesheet">
		<script language="JavaScript" src="Default.js"></script>
	</HEAD>
	<body MS_POSITIONING="FlowLayout">
		<form id="Applications" method="post" runat="server">
			<H3>Select Application</H3>
			<table class="grid" cellpadding="0" celspacing="0">
				<tr>
					<td>
						<table class="innergrid" cellpadding="0" cellspacing="0">
							<tr class="gridheaderrow">
								<td class="gridheadercell" width="240">Application Description</td>
							</tr>
							<%
						WriteApplications();
					%>
						</table>
					</td>
				</tr>
			</table>
		</form>
	</body>
</HTML>
