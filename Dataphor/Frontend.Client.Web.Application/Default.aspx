<%@ Page language="c#" Codebehind="Default.aspx.cs" AutoEventWireup="false" Inherits="Alphora.Dataphor.Frontend.Client.Web.Application.Default" %>
<html>
	<head>
		<title><% WebSession.GetTitle(); %></title>
		<link rel="stylesheet" type="text/css" href="style.css">
		<script language=JavaScript src=Default.js></script>
	</head>
	<body leftmargin=3 topmargin=3 id=MainBody <% WriteBodyAttributes(); %> >
		<form id="Default" method="post" enctype="multipart/form-data">
			<input type=hidden name=ScrollPosition id=ScrollPosition>
			<% WebSession.Render(Context); %>
		</form>
	</body>
</html>