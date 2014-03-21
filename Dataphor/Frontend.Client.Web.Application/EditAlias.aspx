<%@ Page language="c#" Codebehind="EditAlias.aspx.cs" AutoEventWireup="false" Inherits="Alphora.Dataphor.Frontend.Client.Web.Application.EditAlias" validateRequest=false%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN" >
<HTML>
	<HEAD>
		<title>EditAlias</title>
		<meta name="GENERATOR" Content="Microsoft Visual Studio 7.0">
		<meta name="CODE_LANGUAGE" Content="C#">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<LINK href="style.css" type="text/css" rel="stylesheet">
		<script language="JavaScript" src="Default.js"></script>
	</HEAD>
	<body MS_POSITIONING="FlowLayout">
		<form id="EditAlias" method="post" runat="server" action="EditAlias.aspx?Mode='<%= Response.Write(_mode); %>'">
			<P>
				<asp:Label id="Label1" runat="server">Alias Name</asp:Label>
				<asp:TextBox id="_aliasNameTextBox" runat="server" Width="240px"></asp:TextBox></P>
			<P>
				<asp:RadioButton id="_connectionRadioButton" runat="server" Text="Connect to an Existing Server"
					GroupName="Type"></asp:RadioButton>
				<TABLE id="Table1" cellSpacing="1" cellPadding="1" width="300" border="1">
					<TR>
						<TD>
							<P>
								<asp:Label id="Label2" runat="server">Host</asp:Label>
								<asp:TextBox id="_hostNameTextBox" runat="server"></asp:TextBox><BR>
								<asp:Label id="Label9" runat="server">Instance</asp:Label>
								<asp:TextBox id="_instanceNameTextBox" runat="server"></asp:TextBox><BR>
								<asp:Label id="Label3" runat="server">Override Port Number</asp:Label>
								<asp:TextBox id="_portNumberConnectionTextBox" runat="server"></asp:TextBox></P>
						</TD>
					</TR>
				</TABLE>
			</P>
			<P>
				<asp:RadioButton id="_inProcessRadioButton" runat="server" Text="Start a new Server In-Process" GroupName="Type"></asp:RadioButton>
				<TABLE id="Table1" cellSpacing="1" cellPadding="1" width="300" border="1">
					<TR>
						<TD>
							<asp:Label id="Label4" runat="server">Instance</asp:Label>
							<asp:TextBox id="_inProcessInstanceNameTextBox" runat="server"></asp:TextBox><BR>
							<BR>
						</TD>
					</TR>
				</TABLE>
			</P>
			<P>
				<TABLE id="Table2" cellSpacing="1" cellPadding="1" width="300" border="1">
					<caption>
						Advanced</caption>
					<TR>
						<TD>
							<P>You can optionally store the user ID / password with this alias for 
								auto-connection.&nbsp; This user ID will be overwritten if you log in 
								interactively with a different user ID.</P>
							<P>
								<asp:Label id="Label5" runat="server">User ID</asp:Label>
								<asp:TextBox id="_userID" runat="server" Width="152px"></asp:TextBox></P>
							<P>
								<asp:Label id="Label8" runat="server">Password</asp:Label>
								<asp:TextBox id="_password" runat="server" Width="152px" TextMode="Password"></asp:TextBox></P>
						</TD>
					</TR>
				</TABLE>
			</P>
			<P>
				<asp:Button id="_acceptButton" runat="server" Text="Accept"></asp:Button>
				<asp:Button id="_rejectButton" runat="server" Text="Reject"></asp:Button></P>
		</form>
	</body>
</HTML>
