<%@ Import namespace="Alphora.Dataphor.Frontend.Client.Web" %>
<%@ Import namespace="Alphora.Dataphor.Frontend.Client" %>
<%@ Page language="c#" Codebehind="Connect.aspx.cs" AutoEventWireup="false" Inherits="Alphora.Dataphor.Frontend.Client.Web.Application.Connect" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN" >
<HTML>
	<HEAD>
		<title>Dataphor Server Login</title>
		<meta http-equiv="Content-Type" content="text/html; charset=windows-1252">
		<meta content="Microsoft Visual Studio 7.0" name="GENERATOR">
		<meta content="C#" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<link href="style.css" type="text/css" rel="stylesheet">
		<script language="JavaScript" src="Default.js"></script>
	</HEAD>
	<body MS_POSITIONING="FlowLayout">
		<form id="Connect" method="post" runat="server">
			<H3>Connect To Server</H3>
			<TABLE id="Table2" cellSpacing="1" cellPadding="1" width="307" border="0">
				<TR valign="top">
					<TD style="WIDTH: 244px">
						<table class="grid" border="0" cellspacing="0" cellpadding="0">
							<tr>
								<td>
									<TABLE id="Table3" class="innergrid" border="0" cellspacing="0" cellpadding="0">
										<TR class="gridheaderrow">
											<TD class="gridheadercell" width="240">Server Aliases</TD>
										</TR>
										<%
								WriteAliases();
							%>
									</TABLE>
								</td>
							</tr>
						</table>
					</TD>
					<TD>
						<P align="center"><asp:button id="_addButton" runat="server" Text="Add..." Width="100%" accessKey="A"></asp:button><BR>
							<asp:button id="_editButton" runat="server" Text="Edit..." Width="100%" accessKey="E"></asp:button><BR>
							<asp:button id="_deleteButton" accessKey="D" runat="server" Width="100%" Text="Delete"></asp:button>
							<BR>
						</P>
					</TD>
				</TR>
				<TR>
					<TD style="WIDTH: 244px" colSpan="2">
						<P><asp:label id="Label1" runat="server">User ID</asp:label><BR>
							<asp:textbox id="UserIDTextBox" runat="server" Wrap="False" Columns="25" Width="100%"></asp:textbox><BR>
							<asp:label id="Label2" runat="server">Password</asp:label><BR>
							<asp:textbox id="PasswordTextBox" runat="server" Wrap="False" Columns="18" TextMode="Password"
								Width="100%"></asp:textbox><BR>
							<asp:button id="_login" runat="server" Text="Login" accessKey="L"></asp:button></P>
					</TD>
				</TR>
			</TABLE>
		</form>
	</body>
</HTML>
