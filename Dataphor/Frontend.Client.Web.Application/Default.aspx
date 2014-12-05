<%@ Page language="c#" Codebehind="Default.aspx.cs" AutoEventWireup="false" Inherits="Alphora.Dataphor.Frontend.Client.Web.Application.Default" %>
<!DOCTYPE html>
<html>
	<head>
		<title><% WebSession.GetTitle(); %></title>
		<link rel="stylesheet" type="text/css" href="style.css">
	</head>
	<body style="margin-left:3px;margin-top:3px" id="MainBody" <% WriteBodyAttributes(); %> >
		<form id="Default" method="post" enctype="multipart/form-data">
			<% WebSession.Render(Context); %>
		</form>
        <script type="text/javascript" src="jquery-2.1.1.js"></script>
        <script type="text/javascript" src="jquery.blockUI.js"></script>
		<script type="text/javascript" src="Default.js"></script>
	</body>
</html>