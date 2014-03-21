function OnLoad(AForm, ABody, position)
{
	var maxWidth = 300;
	for (var i = 0; i < AForm.children.length; i++)
	{
		if (AForm.children[i].className != "errorlist")
		{
			var width = AForm.children[i].offsetWidth;
			maxWidth = width > maxWidth ? width : maxWidth;
		}
	};

	AForm.setAttribute('style', "width:" + maxWidth + "px; margin:0 auto;");

	ABody.scrollTop = position;
}

function Submit(AUri, AEvent)
{
	document.forms[0].action = AUri;
	if (AEvent != null)
		AEvent.cancelBubble = true;
	document.forms[0].submit();
}

function NotNull(AInput, AHasValueID, ANotNullClass)
{
	AInput.onchange = new Function("");
	AInput.onkeydown = new Function("");
	AInput.className = ANotNullClass;
	if (AHasValueID != null)
		document.getElementById(AHasValueID).value='true'
}

/* Ratio stretched images */

function ConstrainSizeWithRatio(AImage, AWidth, AHeight)
{
	var LWidthRatio = AImage.width / AWidth;
	var LHeightRatio = AImage.height / AHeight;
	var LRatio = (LWidthRatio > LHeightRatio ? LWidthRatio : LHeightRatio);
	if (LRatio > 1)
	{
		AImage.width = AImage.width / LRatio;
//		AImage.height = AImage.height / LRatio;		Already adjusted by browser
	}
}

/* SearchBy */

function ShowDropDown(AID, AParent)
{
	var LElement = document.getElementById(AID);
	LElement.style.left = FindPosX(AParent) + "px";
	LElement.style.top = (FindPosY(AParent) + AParent.offsetHeight) + "px";
	LElement.style.display = '';	//show
}

/* Menus */
	
document.MenusActive = false;

function SetCurrent(AItem, AMenu)
{
	if (AItem.Current != AMenu)
	{
		if (AItem.Current != null)
		{
			SetCurrent(AItem.Current.children[0], null);
			AItem.Current.style.display = 'none';
			AItem.Current = null;
		}
		if (AMenu != null)
		{
			AItem.Current = AMenu;
			AMenu.style.display = '';
		}
	}
}

function FindPosX(AObject)
{
	var LCurLeft = 0;
	if (AObject.offsetParent)
	{
		while (AObject.offsetParent)
		{
			LCurLeft += AObject.offsetLeft
			AObject = AObject.offsetParent;
		}
	}
	else if (AObject.x)
		LCurLeft += AObject.x;
	return LCurLeft;
}

function FindPosY(AObject)
{
	var LCurTop = 0;
	if (AObject.offsetParent)
	{
		while (AObject.offsetParent)
		{
			LCurTop += AObject.offsetTop
			AObject = AObject.offsetParent;
		}
	}
	else if (AObject.y)
		LCurTop += AObject.y;
	return LCurTop;
}

function GetParentTable(AItem)
{
	while (AItem.tagName != "TABLE")
		AItem = AItem.parentNode;
	return AItem;
}

function ShowSubMenu(AItem)
{
	var LSub = AItem.getAttribute('submenu');
	var LMenu = GetParentTable(AItem);
	if (LSub != null)
	{
		LSub = document.getElementById(LSub);
		SetCurrent(LMenu, LSub);
		if (AItem.tagName == "TD")
		{
			LSub.style.left = FindPosX(AItem) + "px";
			LSub.style.top = (FindPosY(AItem) + AItem.offsetHeight) + "px";
		}
		else
		{
			LSub.style.left = (FindPosX(AItem) + AItem.offsetWidth) + "px";
			LSub.style.top = FindPosY(AItem) + "px";
		}
	}
	else
		SetCurrent(LMenu, null);
	if (!document.MenusActive)
	{
		document.onclick = new Function("SetCurrent(document.getElementById('MainMenu'), null); document.MenusActive=false;");
		document.MenusActive = true;
	}
}

function MenuItemOver(AItem)
{
	if (document.MenusActive)
		ShowSubMenu(AItem);
	AItem.className = "highlightedmenuitem";
	if (AItem.tagName == "TR")
		AItem.children[1].className = "highlightedmenuitem";
}

function MenuItemOut(AItem)
{
	AItem.className = ""
	if (AItem.tagName == "TR")
		AItem.children[1].className = "";
}

function MenuItemClick(AItem, AEvent)
{
	ShowSubMenu(AItem);
	AEvent.cancelBubble = true;
}

/* Errors */

function ShowErrorDetail(AItem)
{
	AItem.previousSibling.style.display = ''; 
	AItem.style.display = 'none';
}

function HideError(AItem)
{
	AItem.parentNode.removeChild(AItem);
}

/* Calendar */

function ShowCalendar(str_target, str_datetime) 
{
	var arr_months = ["January", "February", "March", "April", "May", "June",
		"July", "August", "September", "October", "November", "December"];
	var week_days = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"];
	var n_weekstart = 0; // day week starts from (normally 0 or 1)

	var dt_datetime = (str_datetime == null || str_datetime =="" ?  new Date() : str2dt(str_datetime));
	var dt_prev_month = new Date(dt_datetime);
	dt_prev_month.setMonth(dt_datetime.getMonth()-1);
	var dt_next_month = new Date(dt_datetime);
	dt_next_month.setMonth(dt_datetime.getMonth()+1);
	var dt_firstday = new Date(dt_datetime);
	dt_firstday.setDate(1);
	dt_firstday.setDate(1-(7+dt_firstday.getDay()-n_weekstart)%7);
	var dt_lastday = new Date(dt_next_month);
	dt_lastday.setDate(0);
	
	var str_buffer = new String (
		"<html>\n"+
		"<head>\n"+
		"	<title>Calendar</title>\n"+
		"</head>\n"+
		"<body bgcolor=\"White\">\n"+
		"<table class=\"clsOTable\" cellspacing=\"0\" border=\"0\" width=\"100%\">\n"+
		"<tr><td bgcolor=\"#4682B4\">\n"+
		"<table cellspacing=\"1\" cellpadding=\"3\" border=\"0\" width=\"100%\">\n"+
		"<tr>\n	<td bgcolor=\"#4682B4\"><a href=\"javascript:window.opener.ShowCalendar('"+
		str_target+"', '"+ dt2dtstr(dt_prev_month)+"'+document.cal.time.value);\">"+
		"<img src=\"images\\prev.gif\" width=\"16\" height=\"16\" border=\"0\""+
		" alt=\"previous month\"></a></td>\n"+
		"	<td bgcolor=\"#4682B4\" colspan=\"5\">"+
		"<font color=\"white\" face=\"tahoma, verdana\" size=\"2\">"
		+arr_months[dt_datetime.getMonth()]+" "+dt_datetime.getFullYear()+"</font></td>\n"+
		"	<td bgcolor=\"#4682B4\" align=\"right\"><a href=\"javascript:window.opener.ShowCalendar('"
		+str_target+"', '"+dt2dtstr(dt_next_month)+"'+document.cal.time.value);\">"+
		"<img src=\"images\\next.gif\" width=\"16\" height=\"16\" border=\"0\""+
		" alt=\"next month\"></a></td>\n</tr>\n"
	);

	var dt_current_day = new Date(dt_firstday);
	// print weekdays titles
	str_buffer += "<tr>\n";
	for (var n=0; n<7; n++)
		str_buffer += "	<td bgcolor=\"#87CEFA\">"+
		"<font color=\"white\" face=\"tahoma, verdana\" size=\"2\">"+
		week_days[(n_weekstart+n)%7]+"</font></td>\n";
	// print calendar table
	str_buffer += "</tr>\n";
	while (dt_current_day.getMonth() == dt_datetime.getMonth() ||
		dt_current_day.getMonth() == dt_firstday.getMonth()) 
	{
		// print row header
		str_buffer += "<tr>\n";
		for (var n_current_wday=0; n_current_wday<7; n_current_wday++) 
		{
			if (dt_current_day.getDate() == dt_datetime.getDate() &&
				dt_current_day.getMonth() == dt_datetime.getMonth())
				// print current date
				str_buffer += "	<td bgcolor=\"#FFB6C1\" align=\"right\">";
			else if (dt_current_day.getDay() == 0 || dt_current_day.getDay() == 6)
				// weekend days
				str_buffer += "	<td bgcolor=\"#DBEAF5\" align=\"right\">";
			else
				// print working days of current month
				str_buffer += "	<td bgcolor=\"white\" align=\"right\">";

			if (dt_current_day.getMonth() == dt_datetime.getMonth())
				// print days of current month
				str_buffer += "<a href=\"javascript:window.opener."+str_target+
				".value='"+dt2dtstr(dt_current_day)+"';  if (window.opener."+str_target+".onchange != null) window.opener."+str_target+".onchange();  window.close();\">"+
				"<font color=\"black\" face=\"tahoma, verdana\" size=\"2\">";
			else 
				// print days of other months
				str_buffer += "<a href=\"javascript:window.opener."+str_target+
				".value='"+dt2dtstr(dt_current_day)+"';  if (window.opener."+str_target+".onchange != null) window.opener."+str_target+".onchange();  window.close();\">"+
				"<font color=\"gray\" face=\"tahoma, verdana\" size=\"2\">";
			str_buffer += dt_current_day.getDate()+"</font></a></td>\n";
			dt_current_day.setDate(dt_current_day.getDate()+1);
		}
		// print row footer
		str_buffer += "</tr>\n";
	}
	// print calendar footer
	str_buffer +=
		"<form name=\"cal\">\n<tr><td colspan=\"7\" bgcolor=\"#87CEFA\">"+
		"<font color=\"White\" face=\"tahoma, verdana\" size=\"2\">"+
		"Time: <input type=\"text\" name=\"time\" value=\""+dt2tmstr(dt_datetime)+
		"\" size=\"11\" maxlength=\"11\"></font></td></tr>\n</form>\n" +
		"</table>\n" +
		"</tr>\n</td>\n</table>\n" +
		"</body>\n" +
		"</html>\n";

	var vWinCal = window.open("", "Calendar", 
		"width=200,height=250,status=no,resizable=yes,top=200,left=200");
	vWinCal.opener = self;
	var calc_doc = vWinCal.document;
	calc_doc.write (str_buffer);
	calc_doc.close();
}

// datetime parsing and formatting routimes. modify them if you wish other datetime format
function str2dt (str_datetime) 
{
	var LHour = 0;
	str_datetime = str_datetime.toUpperCase();
	var re_date = /^(\d+)\/(\d+)\/(\d+)\s+(\d+)\:(\d+)\:(\d+)\s+(\w+)$/;
	if (!re_date.exec(str_datetime))
		return alert("Invalid Datetime format: "+ str_datetime);
	if (RegExp.$7 = "PM")
		LHour = parseInt(RegExp.$4, 10) + 12;
	return (new Date (RegExp.$3, RegExp.$1-1, RegExp.$2, LHour, RegExp.$5, RegExp.$6));
}

function dt2dtstr (dt_datetime) 
{
	var LDate;
	
	LDate = dt_datetime.getDate();
	if (dt_datetime.getDate() < 10)
		LDate = "0" + LDate;
		
	return (new String (
			(dt_datetime.getMonth()+1)+"/"+LDate+"/"+dt_datetime.getFullYear()+" "));
}

function dt2tmstr (dt_datetime) 
{
	var LAMPM = new String();
	var LHours, LDate, LDate;
	
	if (dt_datetime.getHours() > 12)
	{
		LHours = dt_datetime.getHours() - 12;
		LAMPM = "PM";
	}
	else
	{
		LHours = dt_datetime.getHours();
		LAMPM = "AM";
	}
	
	LDate = dt_datetime.getSeconds();
	if (dt_datetime.getSeconds() < 10)
		LDate = "0" + LDate;
		
	LDate = dt_datetime.getSeconds();
	if (dt_datetime.getSeconds() < 10)
		LDate = "0" + LDate;
	
	return (new String (
			LHours+":"+LDate+":"+LDate + " " + LAMPM));
}

// The following enter key processing logic was taken from Darrell Norton's blog: http://dotnetjunkies.com/WebLog/darrell.norton/archive/2004/03/03/8374.aspx

function TrapKeyDown(btn, event)
{
	if (document.all)
	{
		if (event.keyCode == 13)
		{
			event.returnValue=false;
			event.cancel = true;
			btn.click();
		}
	}
	else if (document.getElementById)
	{
		if (event.which == 13)
		{
			event.returnValue=false;
			event.cancel = true;
			btn.click();
		}
	}
	else if(document.layers)
	{
		if (event.which == 13)
		{
			event.returnValue=false;
			event.cancel = true;
			btn.click();
		}
	}
}

// http://weblogs.asp.net/bstahlhood/
// Form level enter key processing taken from ASP.NET js code
var __defaultFired = false;
function FireDefaultButton(event, target) 
{    
	if (!__defaultFired && event.keyCode == 13 && !(event.srcElement && (event.srcElement.tagName.toLowerCase() == "textarea"))) 
	{        
		var defaultButton = document.getElementById(target);        
		if (defaultButton && typeof(defaultButton.click) != "undefined") 
		{            
			__defaultFired = true;            
			defaultButton.click();            
			event.cancelBubble = true;            
			if (event.stopPropagation) 
				event.stopPropagation();            
			return false;        
		}    
	}    
		return true;
}
