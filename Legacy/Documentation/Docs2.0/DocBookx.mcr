<?xml version="1.0"?>
<!DOCTYPE MACROS SYSTEM "macros.dtd">

<MACROS> 

<MACRO name="SVG_OnShouldCreate" key="" hide="true" lang="JScript"><![CDATA[
  var ipog = Application.ActiveInPlaceControl;
  if (ipog != null) {
    // Only create for FileRef's with .svg extensions otherwise default to
    // built-in XMetaL behavior...
    ipog.ShouldCreate = false;
    var domnode = ipog.Node;
    if (domnode != null) {
      var attrnode = domnode.attributes.getNamedItem("fileref");
      if (attrnode != null && attrnode.value != null) 
      {
        if (attrnode.value.lastIndexOf(".svg") != -1)
        {
          ipog.ShouldCreate = true; // Has .svg extension, instruct to create control!
        }
      }
    }
  }
]]></MACRO> 

<MACRO name="SVG_OnSynchronize" hide="true" lang="JScript"><![CDATA[
	var aipc = Application.ActiveInPlaceControl;
	var domnode = aipc.Node;

    var attrnode = domnode.attributes.getNamedItem("width");
    if (attrnode != null) {
      aipc.Width = attrnode.value; // Set width in pixels from Graphic Width attr
    }

    attrnode = domnode.attributes.getNamedItem("depth");
    if (attrnode != null) {
      aipc.Height = attrnode.value; // Set height in pixels from Graphic Depth attr
    }

	attrnode = domnode.attributes.getNamedItem("fileref");
	if (attrnode == null)
		aipc.Control.SRC = "";
	else
		aipc.Control.SRC = aipc.Document.LocalPath + "\\" + attrnode.value;
]]></MACRO> 

<MACRO name="On_Before_Document_Save" hide="true" lang="JScript"><![CDATA[
if (ActiveDocument.ViewType != sqViewNormal && ActiveDocument.ViewType != sqViewTagsOn) {
  Application.Alert("Unable to set the last modified date.\nSave in Tags On or Normal view to update this element.");
}
else if (!ActiveDocument.IsXML) {
  Application.Alert("Unable to set the last modified date because document is not XML.");
}
else {
  InsertLastModifiedDate();
}
]]></MACRO> 

<MACRO name="On_Before_Document_SaveAs" hide="true" lang="JScript"><![CDATA[
if (ActiveDocument.ViewType != sqViewNormal && ActiveDocument.ViewType != sqViewTagsOn) {
  Application.Alert("Must save in Tags On or Normal view to generate Last Modified Date.");
}
else if (!ActiveDocument.IsXML) {
  Application.Alert("Cannot generate Last Modified Date because document is not XML.");
}
else {
  InsertLastModifiedDate();
}
]]></MACRO> 

<MACRO name="Insert Abstract" key="" lang="JScript" id="1303" tooltip="Insert Abstract" desc="Insert Abstract"><![CDATA[


function doInsertAbstract() {
  // See if abstract already present
  var abstracts = ActiveDocument.getElementsByTagName("abstract");
  if (abstracts.length > 0) {
    Application.Alert("element already has an abstract.");
    Selection.SelectNodeContents(abstracts(0));

  // Find insert location for Abstract, then insert
  } else {
    var rng = ActiveDocument.Range;
    rng.MoveToDocumentStart();
    if (rng.FindInsertLocation("abstract")) {
       rng.InsertWithTemplate("abstract");
       rng.Select();
    }
    else {
       Application.Alert("Cannot find location to insert Abstract.");
    }
    rng = null;
  }
}
if (CanRunMacros())
  doInsertAbstract();

]]></MACRO> 

<MACRO name="Insert Appendix" key="" lang="VBScript" id="1228" tooltip="Insert Appendix" desc="Insert an appendix to the article, book, or part"><![CDATA[

Function doInsertAppendix

  Dim rng
  Set rng = ActiveDocument.Range
  ' Test If can insert Appendix at current position
  If not rng.CanInsert("appendix") Then
    
    ' If In an Appendix, insert a new before the current one    
    If not rng.IsParentElement("appendix") Then
    
      ' Otherwise insert an Appendix at the End
      rng.MoveToDocumentEnd
    End If
    
    If not rng.FindInsertLocation("appendix", False) Then
      Application.Alert("Could not find insert location for Appendix")
      Set rng = Nothing
      Exit Function
    End If
    
  End If

  rng.InsertWithTemplate "appendix"
  rng.Select
  Set rng = Nothing
  
End Function

If CanRunMacrosVB Then
    doInsertAppendix
  
End If

]]></MACRO> 

<MACRO name="Insert Author" key="" lang="VBScript" id="1315" tooltip="Insert Author" desc="Insert author's information"><![CDATA[

Function doInsertAuthor

  Dim rng
  Set rng = ActiveDocument.Range
  ' Test If can insert Author at current position
  If not rng.CanInsert("author") Then
    
    ' If In an Author, insert a new before the current one    
    If not rng.IsParentElement("author") Then
    
      ' Otherwise insert an Author at the End
      rng.MoveToDocumentEnd
    End If
    
    If not rng.FindInsertLocation("author", False) Then
      Application.Alert("Could not find insert location for Author")
      Set rng = Nothing
      Exit Function
    End If
    
  End If

  rng.InsertWithTemplate "author"
  Set rng = Nothing
  
End Function

If CanRunMacrosVB Then
  doInsertAuthor
End If

]]></MACRO> 

<MACRO name="Insert BiblioItem" key="" lang="VBScript" id="1704" tooltip="Insert BiblioItem" desc="Insert a bibliography item"><![CDATA[

' SoftQuad Script Language VBScript:

Sub doInsertBiblioItem
  dim Bibliographies
  Dim rng
  Set rng = ActiveDocument.Range

  If Selection.CanInsert("biblioitem") Then
    Selection.InsertWithTemplate "biblioitem"
    
  Else
    If Selection.IsParentElement("bibliography") Then
      If Selection.FindInsertLocation("biblioitem") Then
        Selection.InsertWithTemplate("biblioitem")
      Else
        rng.MoveToDocumentEnd
        rng.MoveToElement "bibliography", False
        If rng.FindInsertLocation("title") Then
          rng.InsertWithTemplate("title")
          If rng.FindInsertLocation("biblioitem") Then
            rng.InsertWithTemplate("biblioitem")
            rng.Select
          Else
            Application.Alert("Could not find insert location for BiblioItem")
            Set rng = Nothing
            Exit Sub
          End If
        Else
          Application.Alert("Could not find insert location for Bibliography Title")
          Set rng = Nothing
          Exit Sub
        End If
      End If
      
    Else
      Set Bibliographies = ActiveDocument.getElementsByTagName("bibliography")
  
      rng.MoveToDocumentEnd

      If Bibliographies.length = 0 Then
      
        If rng.FindInsertLocation("bibliography", false) Then
          rng.InsertWithTemplate "bibliography"
        Else
          Application.Alert("Could not find insert location for Bibliography")
          Set rng = Nothing
          Exit Sub
        End If
        
        If rng.MoveToElement("biblioitem") Then
          rng.Select
        Else
          If rng.FindInsertLocation("biblioitem",false) Then
            rng.InsertWithTemplate("biblioitem")
            rng.Select
          Else
            Application.Alert("Could not find insert location for BiblioItem")
            Set rng = Nothing
            Exit Sub
          End If
        End If

      Else
        If rng.FindInsertLocation("biblioitem",false) Then
          rng.InsertWithTemplate("biblioitem")
          rng.Select
        Else
          rng.MoveToDocumentEnd
          If rng.FindInsertLocation("title", false) Then
            rng.InsertWithTemplate("title")
            If rng.FindInsertLocation("biblioitem") Then
              rng.InsertWithTemplate("biblioitem")
              rng.Select
            Else
              Application.Alert("Could not find insert location for BiblioItem")
            End If
          Else
            Application.Alert("Could not find insert location for Bibliography Title")
          End If
        End If
      End If
    End If
  End If
  Set rng = Nothing
End Sub

If CanRunMacrosVB Then
  doInsertBiblioItem
End If

]]></MACRO> 

<MACRO name="Insert Citation" key="" lang="VBScript" id="1319" tooltip="Insert Citation" desc="Insert citation to a bibliography item">
<![CDATA[
Sub doInsertCitation
  On Error Resume Next 
  dim obj
  set obj = CreateObject("journalist.Citation")
  if Err.Number <> 0 Then 
    Application.Alert ("The Citation DLL isn't installed. " + Chr(13) _
      + "Please register Samples\VC++\ReleaseMinDependency\journalist.dll.") 
  Else 
    If Selection.CanInsert("citation") Then
      obj.NewCitation
    Else
      Application.Alert("Cannot insert Citation at this point in document.")
    End If
  End If
  set obj = nothing
End Sub

If CanRunMacrosVB Then
  doInsertCitation
End If
]]>
</MACRO> 

<MACRO name="Insert Copyright" key="" lang="VBScript" id="22008" tooltip="Insert Copyright" desc="Insert copyright information"><![CDATA[

Sub doInsertCopyright
  Dim Copyrights
  Set Copyrights = ActiveDocument.getElementsByTagName("copyright")

  If Copyrights.length = 0 Then
    Dim rng
    Set rng = ActiveDocument.Range
    rng.MoveToDocumentStart
    If rng.FindInsertLocation("copyright") Then
      rng.InsertWithTemplate "copyright"
      rng.Select
    Else
      Application.Alert("Could not find insert location for Copyright")
    End If
    Set rng = Nothing
  Else
    Selection.SelectNodeContents(Copyrights.item(0))
  End If
End Sub

If CanRunMacrosVB Then
  doInsertCopyright
End If

]]></MACRO> 

<MACRO name="Toggle Emphasis" key="" lang="JScript" id="20409" tooltip="Insert or Remove Emphasis" desc="Insert, Surround, or Remove Emphasis (Italic)"><![CDATA[
function doToggleEmphasis() {
// If emphasis already present, remove tags.
// If not:  If insertion pt, insert emphasis template
//          If selection, surround selection with emphasis tags.
  var rng = ActiveDocument.Range;
  if (rng.IsParentElement("emphasis")) {
    if (rng.ContainerName != "emphasis") {
      rng.SelectElement();
    }
    rng.RemoveContainerTags();
  }
  else {
    if (rng.IsInsertionPoint) {
      if (rng.CanInsert("emphasis")) {
        rng.InsertWithTemplate("emphasis");
        rng.SelectContainerContents();
        rng.Select();
      }
      else {
        Application.Alert("Cannot insert Emphasis element here.");
      }
    }
    else {
      if (rng.CanSurround("emphasis")) {
        rng.Surround ("emphasis");
      }
      else {
        Application.Alert("Cannot change Selection to Emphasis element.");
      }
    }
  }
  rng = null;
}

if (CanRunMacros()) {
  doToggleEmphasis();
}
]]></MACRO> 

<MACRO name="Toggle Strong" key="" lang="JScript" id="20403" tooltip="Insert or Remove Strong" desc="Insert, Surround, or Remove Strong (Bold)"><![CDATA[
function doToggleStrong() {
// If Strong already present, remove tags.
// If not:  If insertion pt, insert Strong template
//          If selection, surround selection with emphasis tags.
  var rng = ActiveDocument.Range;
  if (rng.IsParentElement("strong")) {
    if (rng.ContainerName != "strong") {
      rng.SelectElement();
    }
    rng.RemoveContainerTags();
  }
  else {
    if (rng.IsInsertionPoint) {
      if (rng.CanInsert("strong")) {
        rng.InsertWithTemplate("strong");
        rng.SelectContainerContents();
        rng.Select();
      }
      else {
        Application.Alert("Cannot insert Strong element here.");
      }
    }
    else {
      if (rng.CanSurround("strong")) {
        rng.Surround ("strong");
      }
      else {
        Application.Alert("Cannot change Selection to Strong element.");
      }
    }
  }
  rng = null;
}

if (CanRunMacros()) {
  doToggleStrong();
}
]]> </MACRO> 

<MACRO name="Toggle Underscore" key="" lang="JScript" id="20416" tooltip="Insert or Remove Underscore" desc="Insert, Surround, or Remove Underscore"><![CDATA[
function doToggleUnderscore() {
// If Underscore already present, remove tags.
// If not:  If insertion pt, insert Underscore template
//          If selection, surround selection with emphasis tags.
  var rng = ActiveDocument.Range;
  if (rng.IsParentElement("underscore")) {
    if (rng.ContainerName != "underscore") {
      rng.SelectElement();
    }
    rng.RemoveContainerTags();
  }
  else {
    if (rng.IsInsertionPoint) {
      if (rng.CanInsert("underscore")) {
        rng.InsertWithTemplate("underscore");
        rng.SelectContainerContents();
        rng.Select();
      }
      else {
        Application.Alert("Cannot insert Underscore element here.");
      }
    }
    else {
      if (rng.CanSurround("underscore")) {
        rng.Surround ("underscore");
      }
      else {
        Application.Alert("Cannot change Selection to Underscore element.");
      }
    }
  }
  rng = null;
}

if (CanRunMacros()) {
  doToggleUnderscore();
}
]]> </MACRO> 

<MACRO name="Insert Figure" key="" lang="JScript" id="1116" tooltip="Insert Figure" desc="Insert Figure"><![CDATA[

function doFigureInsert() {
  var rng2 = ActiveDocument.Range;
  rng2.InsertWithTemplate("figure");
  rng2.MoveToElement("graphic");
  rng2.SelectContainerContents();
  rng2.Select();
  if (!ChooseImage()) {
    rng2.MoveToElement("figure", false);
    rng2.SelectElement();
    rng2.Delete();
    rng2 = null;
    return false;
  }
  rng2.MoveToElement("title", false);
  rng2.SelectContainerContents();
  rng2.Select();
  rng2 = null;
  return true;
}
  

function doInsertFigure() {
  var rng = ActiveDocument.Range;
  
  // Make Insertion point then try to insert figure here
  rng.Collapse(sqCollapseStart);

  // Try to insert the figure
  if (rng.CanInsert("figure")) {
    rng.Select();
    doFigureInsert();
    rng = null;
    return;
  }

  // If can't insert figure, split the container and see if we can then
  var node = rng.ContainerNode;
  if (node) {
    var elName = node.nodeName;
    if (elName == "para" || elName == "literallayout" || elName == "programlisting") {
      Selection.SplitContainer();
      rng = ActiveDocument.Range;
      var rngSave = rng.Duplicate;
      rng.SelectBeforeContainer();
      if (rng.CanInsert("figure")) {
        rng.Select();
        if (doFigureInsert()) {
          rng = null;
          rngSave = null;
          return;
        }
        else {
          rngSave.Select();
          Selection.JoinElementToPreceding();
          rng = null;
          rngSave = null;
          return;
        }
      }

      // Join selection back together
      else {
        Selection.JoinElementToPreceding();
      }
      rngSave = null;
    }
  }
  
  // If not, try to find a place to insert the Figure
  
  if (rng.FindInsertLocation("figure")) {
    rng.Select();
    doFigureInsert();
    rng = null;
    return;
  }

  // Try looking backwards
  if (rng.FindInsertLocation("figure", false)) {
    rng.Select();
    doFigureInsert();
    rng = null;
    return;
  }  

  Application.Alert("Could not find insert location for Figure.");
  rng = null;
}
if (CanRunMacros()) { 
  doInsertFigure();
}

]]></MACRO> 

<MACRO name="Insert Graphic" key="" lang="JScript" id="1115" tooltip="Insert Graphic" desc="Insert Graphic element"><![CDATA[
function doGraphicInsert() {
  var rng2 = ActiveDocument.Range;
  rng2.InsertWithTemplate("graphic");
  rng2.Select();
  if (!ChooseImage()) {
    rng2.SelectElement();
    rng2.Delete();
    rng2 = null;
    return false;
  }
  rng2 = null;
  return true;
}

function doInsertGraphic() {
  var rng = ActiveDocument.Range;
  
  // Make Insertion point then try to insert Graphic here
  rng.Collapse(sqCollapseStart);

  // Try to insert the Graphic
  if (rng.CanInsert("graphic")) {
    rng.Select();
    doGraphicInsert();
    rng = null;
    return;
  }

  // If can't insert Graphic, split the container and see if we can then
  var node = rng.ContainerNode;
  if (node) {
    var elName = node.nodeName;
    if (elName == "para" || elName == "literallayout" || elName == "programlisting") {
      Selection.SplitContainer();
      rng = ActiveDocument.Range;
      var rngSave = rng.Duplicate;
      rng.SelectBeforeContainer();
      if (rng.CanInsert("graphic")) {
        rng.Select();
        if (doGraphicInsert()) {
          rng = null;
          return;
        }
        else {
          rngSave.Select();
          Selection.JoinElementToPreceding();
          rng = null;
          return;
        }
      }
  
      // Join selection back together
      else {
        Selection.JoinElementToPreceding();
      }
      rngSave = null;
    }
  }
  
  // If not, try to find a place to insert the Figure
  
  if (rng.FindInsertLocation("graphic")) {
    rng.Select();
    doGraphicInsert();
    rng = null;
    return;
  }

  // Try looking backwards
  if (rng.FindInsertLocation("graphic", false)) {
    rng.Select();
    doGraphicInsert();
    rng = null;
    return;
  }  

  Application.Alert("Could not find insert location for Graphic.");
  rng = null;
}
if (CanRunMacros()) { 
  doInsertGraphic();
}

]]></MACRO> 

<MACRO name="Insert InlineGraphic" key="" lang="JScript" id="1117" tooltip="Insert InlineGraphic" desc="Insert an inline graphic"><![CDATA[

function doInsertInlineGraphic() {
  var rng = ActiveDocument.Range;
  
  // Make Insertion point then try to insert InlineGraphic here
  rng.Collapse(sqCollapseStart);

  // Try to insert the graphic
  if (rng.CanInsert("inlinegraphic")) {
    rng.InsertWithTemplate("inlinegraphic");
    rng.Select();
    if (!ChooseImage()) {
      rng.SelectElement();
      rng.Delete();
    }
  }

  else {
    Application.Alert("Cannot insert InlineGraphic here.");
  }
  rng = null;
}
if (CanRunMacros()) {
  doInsertInlineGraphic();
}

]]></MACRO> 

<MACRO name="Replace Graphic" key="" lang="JScript" id="1265" tooltip="Replace InlineGraphic or Graphic" desc="Choose a new graphic image"><![CDATA[

function doReplaceGraphic() {
  var rng = ActiveDocument.Range;
  
  // If in a graphic -- let user pick new graphic
  if (rng.ContainerName == "graphic" || rng.ContainerName == "inlinegraphic") {
    ChooseImage();
  }
  else {
    Application.Alert("Select a Graphic or inlineGraphic to replace.");
  }
  rng = null;
}
if (CanRunMacros()) {
  doReplaceGraphic();
}

]]></MACRO> 

<MACRO name="Insert Link" key="" lang="VBScript" id="1328" tooltip="Insert Link" desc="Insert Link element referencing another element in document"><![CDATA[

Sub doInsertLink
  On Error Resume Next
  dim LinkDlg
  set LinkDlg = CreateObject("linkdemo.InsertLinkDlg")
  if Err.Number <> 0 Then  
    Application.Alert ("Can't create Link form.  Inserting <Link> element.")
    Selection.InsertWithTemplate "link"  
  Else  
    LinkDlg.InsertLinkDlg
  End If
  set LinkDlg = nothing
End Sub

If CanRunMacrosVB Then
  doInsertLink
End If

]]></MACRO> 

<MACRO name="Insert LiteralLayout" key="" lang="VBScript" id="1205" tooltip="Insert Literal Layout" desc="Insert LiteralLayout element in which spaces and line breaks are preserved">
<![CDATA[
Sub doInsertLiteralLayout
  Dim rng
  Set rng = ActiveDocument.Range
  
  If rng.IsInsertionPoint Then
    If rng.FindInsertLocation("literallayout") OR rng.FindInsertLocation("literallayout", false) Then
      rng.InsertWithTemplate "literallayout"
      rng.Select
    Else
      Application.Alert("Could not find insert location for LiteralLayout")
    End If
  Else
    If rng.CanSurround("literallayout") Then
      rng.Surround "literallayout"
      rng.Select
    Else
      Application.Alert("Cannot change selection to LiteralLayout")
    End If
  End If
  
  Set rng = Nothing
End Sub

If CanRunMacrosVB Then
  doInsertLiteralLayout
End If
]]>
</MACRO> 

<MACRO name="Insert Note" key="" lang="VBScript" id="1227" tooltip="Insert Note" desc="Insert note to the reader"><![CDATA[

Sub doInsertNote
  Dim rng
  Set rng = ActiveDocument.Range
  If rng.IsInsertionPoint Then
    If rng.FindInsertLocation("note") OR rng.FindInsertLocation("note", false) Then
      rng.InsertWithTemplate "note"
      rng.Select
    Else
      Application.Alert("Could not find insert location for Note")
    End If
  Else
    If rng.CanSurround("note") Then
      rng.Surround "note"
      rng.Select
    Else
      Application.Alert("Cannot change selection to Note")
    End If
  End If
  Set rng = Nothing
End Sub

If CanRunMacrosVB Then
  doInsertNote
End If

]]></MACRO> 

<MACRO name="Insert Section" key="Ctrl+Alt+N" lang="VBScript" id="1744" tooltip="Insert New Section" desc="Insert the same level section where allowed after current point"><![CDATA[

Sub doInsertNewSection
  On Error Resume Next
  Dim rng
  Set rng = ActiveDocument.Range
  
  rng.Collapse
  If rng.IsParentElement("sect4") Then
    ' Just because we're in a Sect4 doesn't mean it's our container.
    ' Move up the hierarchy until the Sect4 is our parent
    While rng.ContainerNode.nodeName <> "sect4"
      rng.SelectElement
    Wend

    ' Move the selection to after the current Sect4
    rng.SelectAfterNode(rng.ContainerNode)

    ' Insert a new Sect4
    rng.InsertWithTemplate("sect4")
  Else
    If rng.IsParentElement("sect3") Then
      While rng.ContainerNode.nodeName <> "sect3"
        rng.SelectElement
      Wend

      rng.SelectAfterNode(rng.ContainerNode)

      rng.InsertWithTemplate("sect3")
    Else
      If rng.IsParentElement("sect2") Then
        While rng.ContainerNode.nodeName <> "sect2"
          rng.SelectElement
        Wend

        rng.SelectAfterNode(rng.ContainerNode)

        rng.InsertWithTemplate("sect2")
      Else
        If rng.IsParentElement("sect1") Then
          While rng.ContainerNode.nodeName <> "sect1"
            rng.SelectElement
          Wend

          rng.SelectAfterNode(rng.ContainerNode)

          rng.InsertWithTemplate("sect1")
        Else
          If rng.IsParentElement("bibliography") Then
            Application.Alert("You cannot insert sections inside a Bibliography.")
          Else
            Application.Alert("You are not currently inside a section.  Try inserting a subsection instead.")
          End If
        End If    
      End If
    End If
  End If
  rng.Select
  Set rng = Nothing
End Sub

If CanRunMacrosVB Then
  doInsertNewSection
End If

]]></MACRO> 

<MACRO name="Insert ProgramListing" key="" lang="VBScript" id="1248" tooltip="Insert Program Listing" desc="Insert ProgramListing element in which spaces and line breaks are preserved">
<![CDATA[
Sub doInsertProgramListing
  Dim rng
  Set rng = ActiveDocument.Range
  
  If rng.IsInsertionPoint Then
    If rng.FindInsertLocation("programlisting") OR rng.FindInsertLocation("programlisting", false) Then
      rng.InsertWithTemplate "programlisting"
      rng.Select
    Else
      Application.Alert("Could not find insert location for ProgramListing")
    End If
  Else
    If rng.CanSurround("programlisting") Then
      rng.Surround "programlisting"
      rng.Select
    Else
      Application.Alert("Cannot change selection to ProgramListing")
    End If
  End If
  
  Set rng = Nothing
End Sub

If CanRunMacrosVB Then
  doInsertProgramListing
End If
]]>
</MACRO> 

<MACRO name="Insert Subsection" key="Ctrl+Alt+S" lang="VBScript" id="1748" tooltip="Insert Subsection" desc="Insert the next-lower level section where allowed after current point"><![CDATA[

Sub doInsertSubSection
  dim Rng
  set Rng = ActiveDocument.Range
  dim UserRng
  set UserRng = ActiveDocument.Range
  dim RngNode
  On Error Resume Next 
  Rng.Collapse
  If Rng.IsParentElement("sect4") Then
    ' Sect4 is the lowest level section in the DTD
    Application.Alert("You cannot enter any more levels of subsection.")
  Else
    If Rng.IsParentElement("sect3") Then
    ' Just because we're in a Sect3 doesn't mean it's our container.
    ' Move up the hierarchy until the Sect3 is our parent.
    ' First, set a DOM Node match to the range for navigating.

      set RngNode = Rng.ContainerNode      
      While RngNode.nodeName <> "sect3"
          set RngNode = RngNode.parentNode
      Wend

    ' Set the range to the contents of the Sect3 element, and collapse 
    ' it to the beginning of that element.
  
      set Rng = SelectNodeContents(RngNode)
      Rng.Collapse(sqCollapseEnd)

    ' Just because we're at the level where we can insert a Sect4 doesn't
    ' mean that we're at a point where we can.  Move the selection point
    ' past any intervening elements until we can insert a Sect4

      set RngNode = RngNode.firstChild
      While not Rng.CanInsert("sect4")
        Rng.SelectAfterNode(RngNode)
        set RngNode = RngNode.nextSibling
      Wend

    ' Now we can insert the Sect4
      Rng.InsertWithTemplate("sect4")
      Selection = Rng.Select
    Else
      If Rng.IsParentElement("sect2") Then
        set RngNode = Rng.ContainerNode
        While RngNode.nodeName <> "sect2"
          set RngNode = RngNode.parentNode
        Wend
  
        set Rng = SelectNodeContents(RngNode)
        Rng.Collapse(sqCollapseEnd)

        set RngNode = RngNode.firstChild
        While not Rng.CanInsert("sect3")
          Rng.SelectAfterNode(RngNode)
          set RngNode = RngNode.nextSibling
        Wend
  
        Rng.InsertWithTemplate("sect3")
        Selection = Rng.Select
      Else
        If Rng.IsParentElement("sect1") Then
          set RngNode = Rng.ContainerNode
          While RngNode.nodeName <> "sect1"
            set RngNode = RngNode.parentNode
          Wend
    
          set Rng = SelectNodeContents(RngNode)
          Rng.Collapse(sqCollapseEnd)

          set RngNode = RngNode.firstChild
          While not Rng.CanInsert("sect2")
            Rng.SelectAfterNode(RngNode)
            set RngNode = RngNode.nextSibling
          Wend

          Rng.InsertWithTemplate("sect2")
          Selection = Rng.Select
        Else
          If Rng.IsParentElement("appendix") Then
            set RngNode = Rng.ContainerNode
            While RngNode.nodeName <> "appendix"
              set RngNode = RngNode.parentNode
            Wend
      
            set Rng = SelectNodeContents(RngNode)
            Rng.Collapse(sqCollapseEnd)

            set RngNode = RngNode.firstChild
            While (not Rng.CanInsert("sect1")) and (Rng.IsParentElement("article") or Rng.IsParentElement("chapter"))
              Rng.SelectAfterNode(RngNode)
              set RngNode = RngNode.nextSibling
            Wend
      
            Rng.InsertWithTemplate("sect1")
            Selection = Rng.Select
          Else
            If Rng.IsParentElement("bibliography") Then
              Application.Alert("Cannot insert sections in a Bibliography.")
              Selection = UserRng.Select
            Else
              If (Rng.IsParentElement("article") or Rng.IsParentElement("chapter")) Then
                set RngNode = Rng.ContainerNode
                While (RngNode.nodeName <> "article" or RngNode.nodeName <> "chapter")
                  set RngNode = RngNode.parentNode
                Wend
      
                set Rng = SelectNodeContents(RngNode)
                Rng.Collapse(sqCollapseEnd)

                set RngNode = RngNode.firstChild
                While (not Rng.CanInsert("sect1")) and (Rng.IsParentElement("article") or Rng.IsParentElement("chapter"))
                  Rng.SelectAfterNode(RngNode)
                  set RngNode = RngNode.nextSibling
                Wend

                If (Rng.IsParentElement("article") or Rng.IsParentElement("chapter")) Then
' need If statement in case we walked outside the document Element
                  Rng.InsertWithTemplate("sect1")
                  Selection = Rng.Select
                Else
                  Application.Alert("Cannot insert subsection here.")
                  Selection = UserRng.Select
                End If
              End If
            End If
          End If
        End If    
      End If
    End If
  End If
  set Rng = Nothing
  set UserRng = Nothing
  set RngNode = Nothing
End Sub

If CanRunMacrosVB Then
  doInsertSubSection
End If

]]></MACRO> 

<MACRO name="Insert ULink" key="" lang="VBScript" id="1110" tooltip="Insert ULink" desc="Insert an external reference"><![CDATA[

Sub doInsertULink
  dim Rng
  set Rng = ActiveDocument.Range
  If Rng.IsInsertionPoint Then
    If Rng.CanInsert("ulink") Then
      Rng.InsertElement "ulink"
    Else
      Application.Alert("Cannot insert ULink element here.")
      Set Rng = Nothing
      Exit Sub
    End If
  Else
    If Rng.CanSurround("ulink") Then
      Rng.Surround "ulink"
    Else
      Application.Alert("Cannot change selection to ULink element.")
      Set Rng = Nothing
      Exit Sub
    End If
  End If

  Dim Dlg
  ' This line creates and displays the dialog
  Set Dlg = FormFuncs.CreateFormDlg(Application.Path + "/Forms/ULink.xft")
  Dim desc
  Set desc = Dlg.URLDesc
  Rng.SelectcontainerContents
  desc.Text = Rng.Text
  Dlg.URLLink.Text = Rng.ContainerNode.getAttribute("url")
  ' make the dialog modal
  If Dlg.DoModal = 1 Then
    Rng.Text = desc.Text
    Rng.ContainerNode.setAttribute "url", Dlg.URLLink.Text
  Else
    Rng.RemoveContainerTags
  End If

  Rng.Select

  Set Dlg = Nothing
  Set Rng = Nothing
End Sub

If CanRunMacrosVB Then
  doInsertULink
End If

]]></MACRO> 

<MACRO name="On_Document_Open_Complete" lang="JScript" hide="true" desc="initialize the macros"><![CDATA[

  Application.Run("Init_JScript_Macros");
  Application.Run("Init_VBScript_Macros");
  
  var viewType = ActiveDocument.ViewType;
  if ((viewType == sqViewNormal || viewType == sqViewTagsOn) && (ActiveDocument.IsXML)) {
    var LastModList = ActiveDocument.getElementsByTagName("date");
    var Rng = ActiveDocument.Range;
    if (LastModList.length > 0) {
      Rng.SelectNodeContents(LastModList.item(0));
      Rng.ReadOnlyContainer = true;
    }
    Rng = null;
  }
  
]]></MACRO> 

<MACRO name="Init_VBScript_Macros" lang="VBScript" desc="initialize VBScript macros" hide="true"><![CDATA[

Function CanRunMacrosVB
  If (not ActiveDocument.ViewType = sqViewNormal) AND (not ActiveDocument.ViewType = sqViewTagsOn) Then
    Application.Alert("Change to Tags On or Normal view to run macros.")
    CanRunMacrosVB = False
    Exit Function
  End If
  
  If not ActiveDocument.IsXML Then
    Application.Alert("Cannot run macros because document is not XML.")
    CanRunMacrosVB = False
    Exit Function
  End If

  CanRunMacrosVB = True
End Function
  
]]></MACRO> 

<MACRO name="Init_JScript_Macros" lang="JScript" desc="initialize JScript macros" hide="true"><![CDATA[

function CanRunMacros() {
  if (ActiveDocument.ViewType != sqViewNormal && ActiveDocument.ViewType != sqViewTagsOn) {
    Application.Alert("Change to Tags On or Normal view to run macros.");
    return false;
  }

  if (!ActiveDocument.IsXML) {
    Application.Alert("Cannot run macros because document is not XML.");
    return false;
  }
  return true;
}

function ChooseImage()
{
  var rng = ActiveDocument.Range;
  if (rng.ContainerName == "graphic" || rng.ContainerName == "inlinegraphic") {
    try {
      var obj = new ActiveXObject("SQExtras.FileDlg");
    }
    catch(exception) {
      var result = reportRuntimeError("Choose Image Error:", exception);
      Application.Alert(result + "\nPlease register SQExtras.dll");
      return false;
    }
    if (obj.DisplayImageFileDlg(true, "Choose Image", "Image Files (*.gif,*.jpg,*.png,*.tiff,*.tif,*.bmp)|*.gif;*.jpg;*.png;*.tiff;*.tif;*.bmp|All Files (*.*)|*.*||",  Application.Path + "\\Samples\\Cameras\\images\\clipart")) {
      var src = obj.FullPathName;
      var url = Application.PathToURL(src, ActiveDocument.Path + "\\");
      rng.ContainerAttribute("fileref") = url;
      obj = null;
      rng = null;
      return true;
    }
    else {
      rng = null;
      obj = null;
      return false;
    }
  }
  else {
    Application.Alert("Graphic not selected");
    rng = null;
    return false;
  }
  rng = null;
}

function StartNewSubsection(sectName)
{
// This function can only be called if it is known that Para is a parent element

  var paraName = "para";
  var titleName = "title";
  
  var rng = ActiveDocument.Range;
  var strBody = "";
  var strTitle = "";
  
  // Use the current Para for the Title of the new section
  var node = Selection.ContainerNode;
  while (node.nodeName != paraName) {
    node = node.parentNode;
  }
  
  rng.SelectNodeContents(node);
  strTitle = rng.Text;
  rng.SelectElement();
  rng.Delete();
  
  // Copy the rest to a string
  rng.Select();
  var rng3 = rng.Duplicate;
  var ret = rng3.MoveToElement(sectName);  // look for following subsections
  var rng2 = rng.Duplicate;
  rng2.SelectContainerContents();
  rng2.Collapse(sqCollapseEnd);  // Make sure subsection found is in current section
  if (ret && rng2.IsGreaterThan(rng3)) {
    rng3.SelectBeforeContainer();
    rng = rng3.Duplicate;
  } 
  else { // no subsections, so move to end of container
    rng = rng2.Duplicate;
  }
  Selection.ExtendTo(rng);
  strBody = Selection.Text;
  Selection.Delete();
  
  // Put in the new section
  rng.InsertElement(sectName);
  rng.InsertElement(titleName);
  rng.TypeText(strTitle);
  rng.SelectAfterContainer();
  if (strBody != "") rng.TypeText(strBody);
  rng.MoveToElement(titleName, false);
  rng.Select();
  rng = null;
  rng3 = null;
  rng2 = null;
}

// InsertLastModifiedDate - inserts date just before document is saved
function InsertLastModifiedDate() {  
  var localtime = new Date();
  var LastModString = localtime.toLocaleString();

  var LastModList = ActiveDocument.getElementsByTagName("date");

  var Rng = ActiveDocument.Range;
  if (LastModList.length > 0) {
    var i = 0;
    var k = LastModList.Count;
  	while (i < k && LastModList.item(i).Attributes.getNamedItem("role") != "LastMod")
  	{
  		inc(i);
  	}
    Rng.SelectNodeContents(LastModList.item(0));
    Rng.ReadOnlyContainer = false;
    Rng.PasteString(LastModString);
    Rng.ReadOnlyContainer = true;
  }
  else {
    Rng.MoveToDocumentEnd();

    if (Rng.FindInsertLocation("date", false)) {
      Rng.InsertElement("date");
      Rng.ContainerAttribute("role") = "LastMod";
      Rng.TypeText(LastModString);
      Rng.ReadOnlyContainer = true;
    }
    else {
      Application.Alert("Could not find insert location for LastModDate");
    }
  }
  Rng = null;
}

// fix PubDate
  fixISODates();

]]></MACRO> 

<MACRO name="On_Update_UI" lang="JScript" hide="true" id="144"><![CDATA[
function refreshStyles() {
}
// This causes too much flickering since On_Update_UI is called so frequently.
//if (ActiveDocument.IsXML &&
//    (ActiveDocument.ViewType == sqViewNormal || ActiveDocument.ViewType == sqViewTagsOn)) {
//  refreshStyles();
//}

// Check if the view is Tags On and if so, adjust the selection out of the 
// top-level
if (Selection.IsInsertionPoint && ActiveDocument.ViewType == sqViewTagsOn) {
   if (Selection.ContainerNode == null) {
      Selection.MoveRight();
   }
   if (Selection.ContainerNode == null) {
      Selection.MoveLeft();
   }
}

// Disable most macros if in Plain Text view or if the document is not XML
if (!ActiveDocument.IsXML ||
    (ActiveDocument.ViewType != sqViewNormal && ActiveDocument.ViewType != sqViewTagsOn)) {
  Application.DisableMacro("Insert Abstract");
  Application.DisableMacro("Insert Appendix");
  Application.DisableMacro("Insert Author");
  Application.DisableMacro("Insert BiblioItem");
  Application.DisableMacro("Insert Citation");
  Application.DisableMacro("Insert Copyright");
  Application.DisableMacro("Toggle Emphasis");
  Application.DisableMacro("Toggle Strong");
  Application.DisableMacro("Toggle TT");
  Application.DisableMacro("Toggle Underscore");
  Application.DisableMacro("Insert Figure");
  Application.DisableMacro("Insert Graphic");
  Application.DisableMacro("Replace Graphic");
  Application.DisableMacro("Insert InlineGraphic");
  Application.DisableMacro("Insert Link");
  Application.DisableMacro("Insert LiteralLayout");
  Application.DisableMacro("Insert Note");
  Application.DisableMacro("Insert New Section");
  Application.DisableMacro("Insert ProgramListing");
  Application.DisableMacro("Insert PubDate");
  Application.DisableMacro("Insert Subsection");
  Application.DisableMacro("Insert ULink");
  Application.DisableMacro("Import Table");
  Application.DisableMacro("Update Table");
  Application.DisableMacro("Import SeeAlso");
  Application.DisableMacro("Update SeeAlso");
  Application.DisableMacro("View HTML");
  Application.DisableMacro("View PDF");
  Application.DisableMacro("Setup PDF");
  Application.DisableMacro("Open Word Document");
  Application.DisableMacro("Convert to Subsection");
  Application.DisableMacro("Convert to Section");
  Application.DisableMacro("Convert to Paragraph");
  Application.DisableMacro("Convert to Article Title");
  Application.DisableMacro("Convert to Chapter Title");
  Application.DisableMacro("Join Paragraphs");
  Application.DisableMacro("Promote Section");
  Application.DisableMacro("Demote Section");
  Application.DisableMacro("Toggle Rules Checking");
  Application.DisableMacro("List All Comments");
  Application.DisableMacro("Clean Up Empty");
}

if (ActiveDocument.ViewType != sqViewNormal && ActiveDocument.ViewType != sqViewTagsOn) {
  Application.DisableMacro("Use 1.css for the Structure View");
  Application.DisableMacro("Use 2.css for the Structure View");
  Application.DisableMacro("Use 3.css for the Structure View");
  Application.DisableMacro("Use 4.css for the Structure View");
  Application.DisableMacro("Use 5.css for the Structure View");
  Application.DisableMacro("Use the default (generated) Structure View");
}

// Disable some macros if the view is Normal or Tags On
if (ActiveDocument.ViewType == sqViewNormal || ActiveDocument.ViewType == sqViewTagsOn) {

// Structural elements
  if (  Selection.IsParentElement("bibliography") ||
        Selection.IsParentElement("abstract") ||
        Selection.IsParentElement("pubdate") ||
        Selection.IsParentElement("copyright") ||
        Selection.IsParentElement("title")) {
    Application.DisableMacro("Insert New Section"); }
    
  if (!Selection.IsParentElement("sect1")) {
    Application.DisableMacro("Insert New Section"); }
 
  if (  Selection.IsParentElement("sect4") ||
        Selection.IsParentElement("bibliography") ) {
    Application.DisableMacro("Insert Subsection"); }

// Text-level elements
  if (!Selection.CanInsert("citation")) {
    Application.DisableMacro("Insert Citation"); }
  if (!Selection.CanInsert("inlinegraphic")) {
    Application.DisableMacro("Insert InlineGraphic"); }
  if (!Selection.CanInsert("link")) {
    Application.DisableMacro("Insert Link"); }
  
  if (Selection.IsInsertionPoint){
      if (!Selection.CanInsert("ulink")) {
        Application.DisableMacro("Insert ULink"); }
  }

  if (!Selection.IsInsertionPoint) {
    if (!Selection.CanSurround("literallayout"))
      Application.DisableMacro("Insert LiteralLayout");
    if (!Selection.CanSurround("note"))
      Application.DisableMacro("Insert Note");
    if (!Selection.CanSurround("programlisting"))
      Application.DisableMacro("Insert ProgramListing");
    if (!Selection.CanSurround("ulink"))
     Application.DisableMacro("Insert ULink");
  }


  // Emphasis elements
  if (Selection.IsInsertionPoint) {
    if (!Selection.CanInsert("emphasis")&& Selection.ContainerName != "emphasis")
        Application.DisableMacro("Toggle Emphasis");
  }
  else {
    if (!Selection.CanSurround("emphasis")&& Selection.ContainerName != "emphasis")
        Application.DisableMacro("Toggle Emphasis");
  }
  
  if (Selection.IsInsertionPoint) {
    if (!Selection.CanInsert("strong")&& Selection.ContainerName != "strong")
      Application.DisableMacro("Toggle Strong");
  }
  else {
    if (!Selection.CanSurround("strong")&& Selection.ContainerName != "strong")
        Application.DisableMacro("Toggle Strong");
  }
  
  if (Selection.IsInsertionPoint) {
    if (!Selection.CanInsert("TT")&& Selection.ContainerName != "TT")
        Application.DisableMacro("Toggle TT");
  }
  else {
    if (!Selection.CanSurround("TT")&& Selection.ContainerName != "TT")
        Application.DisableMacro("Toggle TT");
  }
  
  if (Selection.IsInsertionPoint) {
    if (!Selection.CanInsert("underscore")&& Selection.ContainerName != "underscore")
        Application.DisableMacro("Toggle Underscore");
  }
  else {
    if (!Selection.CanSurround("underscore")&& Selection.ContainerName != "underscore")
        Application.DisableMacro("Toggle Underscore");
  }
  
  // Word Import Macros
  if (!Selection.IsParentElement("para")) {
    Application.DisableMacro("Convert to Subsection");
    Application.DisableMacro("Convert to Section");
    if (!Selection.IsParentElement("title")) {
      Application.DisableMacro("Convert to Article Title");
    }
  }
  else {
    if (Selection.IsParentElement("sect4")) {
      Application.DisableMacro("Convert to Subsection");
    }
  }
  if (!Selection.IsParentElement("title")) {
    Application.DisableMacro("Convert to Paragraph");
    Application.DisableMacro("Promote Section");
    Application.DisableMacro("Demote Section");
  }
  else {  
    if (!Selection.IsParentElement("sect1") ){
      Application.DisableMacro("Convert to Paragraph");
    }
    if (!Selection.IsParentElement("sect2") ){
      Application.DisableMacro("Promote Section");
    }
    if (!Selection.IsParentElement("sect1") || Selection.IsParentElement("sect4")){
      Application.DisableMacro("Demote Section");
    }
  }
  if (Selection.IsInsertionPoint) {
    Application.DisableMacro("Join Paragraphs");
  }
  
  // Structure view macros
  if (!ActiveDocument.StructureViewVisible) {
    Application.DisableMacro("Use 1.css for the Structure View");
    Application.DisableMacro("Use 2.css for the Structure View");
    Application.DisableMacro("Use 3.css for the Structure View");
    Application.DisableMacro("Use 4.css for the Structure View");
    Application.DisableMacro("Use 5.css for the Structure View");
    Application.DisableMacro("Use the default (generated) Structure View");
  }


  // Style Element box customization for Para
  customizeStyleElementForPara(); // function is located in On_Macro_File_Load macro

}
]]></MACRO> 

<MACRO name="Import Table" lang="JScript" id="1378" tooltip="Import Database Table" desc="Import a table from a database"><![CDATA[
// SoftQuad Script Language JScript:
function RepairXMetaLInstallPath(paramFile) {
	// Open the param.txt
	var iomode = 1;  // ForReading
	var createmode = false; // a new file is NOT created if the specified filename doesn't exist.
	var formatmode = -1;  // Unicode
	if (Application.UnicodeSupported == false) {
		formatmode = 0;  // ASCII
	}

	try {
		var fso = new ActiveXObject("Scripting.FileSystemObject");
		var f = fso.OpenTextFile(paramFile, iomode, createmode, formatmode );
	}
	catch(exception) {
		result = reportRuntimeError("Import Table Error:", exception);
		Application.Alert(result + "\nFailed to invoke Scripting.FileSystemObject\nYou need to get Windows Scripting Host from the Microsoft site.");
		fso = null;
		rng = null;
		return false;
	}

	// Read the whole file
	var str;
	str = f.ReadAll();
	f.Close();

	// Insert the xmetal install path if necessary.

	if (!str) {
	       Application.Alert("Initialization file for Database Import Wizard is empty.");
		return true; // file empty? Carry on anyway.
	}
	
	var  found = str.search(/XMETAL_INSTALL_PATH/g);
	if (found == -1) {
		return true;  // Not there, don't need to do anything.
	}

	var path = Application.Path;
	str = str.replace(/XMETAL_INSTALL_PATH/g, path);
	
	var iomode = 2;  // ForWriting
	var createmode = true; // a new file is created if the specified filename doesn't exist.
	f = fso.OpenTextFile(paramFile, iomode, createmode, formatmode);
	f.Write(str);

	// Close the text file
	f.Close();
	return true;

}

function doImportTable() {
// Local variables
  var paramFile = Application.Path + "\\Samples\\Cameras\\param.txt";
  var tableFile = Application.Path + "\\Samples\\Cameras\\DBImport.htm";
  
//Fix XmetaL Install Path in param.txt
  if (!RepairXMetaLInstallPath(paramFile)) return;

// Find a place to insert the table
  var rng = ActiveDocument.Range;
  var found = false;
  
  // look forwards
  found = rng.FindInsertLocation("table");
  if (!found)
    // look backwards
    found = rng.FindInsertLocation("table", false);
    
  if (found) {
  
    var result = "";
    // Generate a new, unique, parameter file name
    var newParamFile=Application.UniqueFileName(Application.Path + "\\Samples\\Cameras\\","dbi",".txt");

    // Copy the old parameter file into the new one
    // (so that the wizard won't come up blank)
    if (paramFile != null) {
      try {
        var fso = new ActiveXObject("Scripting.FileSystemObject");
        fso.CopyFile(paramFile,newParamFile);
      }
      catch(exception) {
        result = reportRuntimeError("Import Table Error:", exception);
        Application.Alert(result + "\nFailed to invoke Scripting.FileSystemObject\nYou need to get Windows Scripting Host from the Microsoft site.");
        fso = null;
        rng = null;
        return;
      }
      fso = null;
    }

    // Run the wizard
    // Show the dialog
    try {
      var obj = new ActiveXObject("SoftQuad.DBImport");
      var ret = obj.NewDBImport(newParamFile, tableFile);
    }
    catch(exception) {
      result = reportRuntimeError("Import Table Error:", exception);
      Application.Alert(result + "\nPlease register DBImport.dll");
      rng = null;
      obj = null;
      return;
    }
  
    // If user chose OK ...
    if (ret) {

      // Read the resulting table into the document
      var str = Application.FileToString(tableFile);
      rng.TypeText(str);
    
      // The table id is the paramFile, minus the full path
      var splitPath=newParamFile.split('\\');
      var spLength=splitPath.length;
      var tableId=splitPath[spLength-1];
  
      rng.MoveToElement("table", false); // move backwards to table element
      if (rng.ContainerName == "table") {
        rng.ContainerAttribute("border") = "1";  // looks better
        rng.ContainerAttribute("id") = tableId; // for later updates
      }
        
      // Scroll to the location of the inserted table
      rng.Select();
      ActiveDocument.ScrollToSelection();
      Selection.MoveLeft();  // avoid big cursor next to table

      // Copy the new parameter file into the original one
      // (so that the next time the wizard is run, our new parameter file will dictate the initial state)
      if (paramFile!=null) {
        try {
          var fso = new ActiveXObject("Scripting.FileSystemObject");
          fso.CopyFile(newParamFile,paramFile);
        }
        catch(exception) {
          result = reportRuntimeError("Import Table Error:", exception);
          Application.Alert(result + "\nFailed to invoke Scripting.FileSystemObject\nYou need to get Windows Scripting Host from the Microsoft site.");
          fso = null;
          rng = null;
          return;
        }
        fso = null;
      }

    }
  }
  else {
    Application.Alert("Can't find insert location for TABLE.");
  } 
  rng = null;
  obj = null;
}
if (CanRunMacros()) {
  doImportTable();
}
]]></MACRO> 

<MACRO name="Update Table" lang="JScript" tooltip="Update Imported Table" desc="Update table imported from database" id="1377"><![CDATA[
// SoftQuad Script Language JScript:
function doUpdateTable() {
  // Local variables
  var rng = ActiveDocument.Range;
  var tableFile = Application.Path + "\\Samples\\Cameras\\DBImport.htm";
  
  // Check that we are inside a table
  var node = rng.ContainerNode;
  while (node && node.nodeName != "TABLE") {
    node = node.parentNode;
  }
  
  if (node) {
    // Check we are in the right kind of table
    var tableId = rng.ElementAttribute("id", "table");

    var paramFile = Application.Path + "\\Samples\\Cameras\\" + tableId;
    try {
      var fso = new ActiveXObject("Scripting.FileSystemObject");
    }
    catch(exception) {
      result = reportRuntimeError("Update Table Error:", exception);
      Application.Alert(result + "\nFailed to invoke Scripting.FileSystemObject\nYou need to get Windows Scripting Host from the Microsoft site.");
      rng = null;
      return;
    }
    if (fso.FileExists(paramFile)) {
    
      // Insert the new table
      try {
        var obj = new ActiveXObject("SoftQuad.DBImport");
        obj.UpdateDBImport(paramFile, tableFile);
      }
      catch(exception) {
        var result = reportRuntimeError("Update Table Error:", exception);
        Application.Alert(result + "\nPlease register DBImport.dll");
        rng = null;
        obj = null;
        return;
      }

      // Delete the old table
      rng.SelectNodeContents(node);
      rng.SelectElement();
      rng.Delete();

      // Read the resulting table into the document
      var str = Application.FileToString(tableFile);
      rng.TypeText(str);

      // Set the border and id attributes of the table
      rng.MoveToElement("table", false); // move backwards to table element
      if (rng.ContainerName == "table") {
        rng.ContainerAttribute("border") = "1";  // looks better
        rng.ContainerAttribute("id") = tableId; // for later updates
      }

      // Scroll to the location of the inserted table
      rng.MoveLeft();
      rng.Select();
      ActiveDocument.ScrollToSelection();
    }
    else {
      Application.Alert("Parameter file "+paramFile+" does not exist.\nCannot update this table.");
    }
  }
  else {
    Application.Alert("You are not currently inside a table that can be updated.");
  }
  rng = null;
  obj = null;
}

if (CanRunMacros()) {
  doUpdateTable();
}
]]></MACRO> 

<MACRO name="Revert To Saved" lang="JScript" id="1367" desc="Opens last saved version of the current document"><![CDATA[
if (!ActiveDocument.Saved) {
  if (ActiveDocument.FullName != "") {
    retVal = Application.Confirm("If you continue you will lose changes to this document.\nDo you want to revert to the last-saved version?");
    if (retVal) {
      ActiveDocument.Reload();
    }
  } else {
    Application.Alert("Unable to revert to saved. This document has not been saved.")
  }
}
]]></MACRO> 

<MACRO name="Toggle Rules Checking" lang="JScript" id="1919" desc="Turn Rules Checking On/Off">
<![CDATA[
if (ActiveDocument.RulesChecking) {
  var response = Application.MessageBox("Running macros without rules checking may have unpredictable results.\nDo you want to proceed?", 32+1, "Toggle Rules Checking");
  if (response == 1)  
    ActiveDocument.RulesChecking = false;
}
else {
  ActiveDocument.RulesChecking = true;
  if (!ActiveDocument.RulesChecking) {
    Application.Alert("Could not turn Rules Checking on due to validation errors.");
    ActiveDocument.Validate();
  }
}
]]></MACRO> 

<MACRO name="On_Document_Close" hide="true" lang="JScript"><![CDATA[

  // Get the document property for PreviewTempFile.
  var ndlProperties = ActiveDocument.CustomDocumentProperties;
  var ndProperty    = ndlProperties.item( "PreviewTempFile" );

  // If the PreviewTempFile exists for this document, delete it.
  if ( ndProperty != null ) {
    try {
      var objFileSystem = new ActiveXObject( "Scripting.FileSystemObject" );
      objFileSystem.DeleteFile( ndProperty.value );
    }
    catch(exception) {
      var result = reportRuntimeError("Document Close Error:", exception);
      Application.Alert(result + "\nFailed to invoke Scripting.FileSystemObject\nYou need to get Windows Scripting Host from the Microsoft site.");
    }
    objFileSystem = null;
  }
]]></MACRO> 

<MACRO name="Save As HTML" key="" lang="JScript" id="1308" tooltip="Save As HTML" desc="Save document as an HTML file"><![CDATA[

// Save HTML.  Check if setup HTML is needed.
// Following functions are in multipleOutput.mcr
//   createFileSystemObject(), getXSLNameForHTML(), XMLToHTMLSetup() and previewHTML()
function saveAsHTMLHelper() {
  var fso = createFileSystemObject();
  if (!fso)
    return;
  var xslPath = getXSLNameForHTML();
  if (!fso.FileExists(xslPath))
    XMLToHTMLSetup();
  saveAsHTML();
}

saveAsHTMLHelper();

]]></MACRO> 

<MACRO name="Open Word Document" key="" lang="VBScript" id="1306" tooltip="Import From Word" desc="Create new document from an MS Word document"><![CDATA[
If (myWordDir = "") Then
	myWordDir = Application.Path & "\Samples\Cameras\Word\"
End If

Set fileopen = CreateObject( "xmExchange.FileOpen")

fileopen.initialDir = myWordDir
existingViewType = ActiveDocument.ViewType 

If fileopen.chooseWordDocument() Then

	Set rulesobject = CreateObject( "xmExchange.SelectOpenRules" ) 
	rulesobject.xmDir = Application.Path & "\"
	
	rulesobject.initialDir = Application.Path & "\Rules\"
	Set rulesobject.xmApplication = Application
	
	openOK = rulesobject.openWordDocument(fileopen.file)

	If openOK Then
		myWordDir = rulesobject.wordDocument.Path
		
		rulesobject.ruleSetFile = Application.Path & "\document\journalist_openrules.xml"
		rulesSetOK = rulesobject.initOpenRuleSet()
		
		If Not rulesSetOK Then
			rulesobject.wordApplication.Quit
			Set rulesobject = Nothing
		Else
			// todo update this to docbook: set | book | part | chapter
			s = "<?xml version='1.0' encoding='utf-8'?>"
			s = s & "<!DOCTYPE article SYSTEM 'journalist.dtd' []>" & vbCrlf
			s = s & "<article></article>"
			
			Set xmDoc = Application.Documents.OpenString( s, sqViewTagsOn, "XMetaL document" ) 
			
			If (xmDoc Is Nothing) Then
				MsgBox("Unable to open XML document: " + s)
				rulesobject.wordDocument.Close 0
				rulesobject.wordApplication.Quit
				Set rulesobject = Nothing
			Else
				prev = Application.DisplayAlerts
				Application.DisplayAlerts = 0 ' turn off error messages
				xmDoc.SaveAs
				Application.DisplayAlerts = prev ' reset
				
				If len(xmDoc.Path) > 0 Then
					' document was saved
					xmDoc.CustomDocumentProperties.Add "XMGraphicNumber", "0"
				End If
				
				Set progress = CreateObject( "xmExchange.Progress" )
				
				Set progress.xmApplication = Application				
				Set progress.wordApplication = rulesobject.wordApplication
			    Set progress.rules = rulesobject.rules
			    Set progress.wordDocument = rulesobject.wordDocument
			    Set progress.xmdocument = xmDoc
				
				' get My Documents location as default for graphics
				Set sh = CreateObject( "Wscript.Shell" ) 
				Dim graphicFolder
				graphicFolder = sh.SpecialFolders( "MyDocuments" ) 
			    
			    progress.entitydirectory = graphicFolder & "\"
			    			
				processOK = progress.processWordDocument
							
				' close word doc, do not save changes
				rulesobject.wordDocument.Close 0
				' close word appn
				rulesobject.wordApplication.Quit
				xmDoc.ViewType = existingViewType
			End If
		End If
	End If
End If

]]></MACRO> 

<MACRO name="Convert to Subsection" key="Ctrl+Alt+B" lang="JScript" id="1723" tooltip="Convert to Subsection" desc="Change current Para into the Title of a subsection"><![CDATA[
// Convert current paragraph and  everything below it into a new section.
function doConvertToSubsection() {
  if (Selection.IsParentElement("para")) {
    if (Selection.IsParentElement("sect4")) Application.Alert("No more levels of subsections available");
    else if (Selection.IsParentElement("sect3")) StartNewSubsection("sect4");
    else if (Selection.IsParentElement("sect2")) StartNewSubsection("sect3");
    else if (Selection.IsParentElement("sect1")) StartNewSubsection("sect2");
    else StartNewSubsection("sect1");
  }
  else
    Application.Alert("Place insertion point in the paragraph that will become the title of the subsection");
}

if (CanRunMacros()) {
  doConvertToSubsection();
}
]]></MACRO> 

<MACRO name="Convert to Section" key="Ctrl+Alt+C" lang="JScript" id="1722" tooltip="Convert to Section" desc="Change current Para into the Title of a new Section at same level as current Section"><![CDATA[
// Convert current paragraph and  everything below it into a new section at the same
// level as the section currently in.
function doConvertToSection() {
  if (Selection.IsParentElement("para")) {
      if (Selection.IsParentElement("sect1") || 
          Selection.IsParentElement("sect2") ||
          Selection.IsParentElement("sect3") ||
          Selection.IsParentElement("sect4")) {
        var paraName = "para";
        var titleName = "title";
      
        var rng = ActiveDocument.Range;
        var strBody = "";
        var strTitle = "";
      
        // Use the current Para for the Title of the new section
        var node = rng.ContainerNode;
        while (node.nodeName != paraName) {
          node = node.parentNode;
        }
      
        rng.SelectNodeContents(node);
        strTitle = rng.Text;
        rng.SelectElement();
        rng.Delete();
      
        // Copy the rest to a string
        var rng2 = rng.Duplicate;
        node = rng.ContainerNode;
        if (node.lastChild) {
          rng.SelectAfterNode(node.lastChild);
          rng2.ExtendTo(rng);
        }
        strBody = rng2.Text;
        rng2.Delete();
      
        // Put in the new section
        node = rng.ContainerNode;
        rng.SelectAfterContainer();
        rng.InsertElement(node.nodeName);
        rng.InsertElement(titleName);
        rng.TypeText(strTitle);
        rng.SelectAfterContainer();
        if (strBody != "") rng.TypeText(strBody);
        rng.MoveToElement(titleName, false);
        rng.Select();
        rng = null;
        rng2 = null;
      }
      else if (Selection.IsParentElement("para")) {
        StartNewSubsection("sect1");
      }
  }
}
if (CanRunMacros()) {
  doConvertToSection();
}
]]></MACRO> 

<MACRO name="Convert to Paragraph" key="" lang="JScript" id="20341" tooltip="Convert Subsection to Paragraph" desc="Put cursor in the Title of the section to convert to paragraphs"><![CDATA[
// Convert current Section title to a paragraph.
function doConvertToParagraph() {
  var node = Selection.ContainerNode;
  if ((Selection.IsParentElement("sect1") ||
      Selection.IsParentElement("sect2") ||
      Selection.IsParentElement("sect3") ||
      Selection.IsParentElement("sect4")) &&
      node.nodeName == "title") {
    var paraName = "para";
    var titleName = "title";
  
    var rng = ActiveDocument.Range;
    var strBody = "";
    var strTitle = "";
  
    // Save title
    rng.SelectNodeContents(node);
    strTitle = rng.Text;

    // Save the Section body
    rng.SelectAfterContainer();
    var rng2 = rng.Duplicate;
    node = rng.ContainerNode;
    var sectName = node.nodeName;
    rng.SelectAfterNode(node.lastChild);
    rng2.ExtendTo(rng);
    strBody = rng2.Text;

    // Find out if the section we're changing to a paragraph contains any subsections
    var containsSections = 0;
    var nodeCheck = node.firstChild;
    while (nodeCheck) {
      if (sectName == "sect1" && nodeCheck.nodeName == "sect2") containsSections = 1;
      else if (sectName == "sect2" && nodeCheck.nodeName == "sect3") containsSections = 1;
      else if (sectName == "sect3" && nodeCheck.nodeName == "sect4") containsSections = 1;
      nodeCheck = nodeCheck.nextSibling;
    }

    // Find out if the section we're changing has any sibling sections before it
    var prevSectnode = node.previousSibling;
    while (prevSectnode.nodeType != 1) // 1 == DOMElement
      prevSectnode = prevSectnode.previousSibling;
    // Save where to delete the whole section
    var rngDelete = rng.Duplicate;

    // Changing to a paragraph may be an invalid thing to do
    var changeValid = 1;
    if (prevSectnode.nodeName == sectName) {
      // Find out if the previous section contains any subsections
      var prevContainsSections = 0;
      nodeCheck = prevSectnode.firstChild;
      while (nodeCheck) {
        if ((nodeCheck.nodeName == "sect2") || (nodeCheck.nodeName == "sect3") || (nodeCheck.nodeName == "sect4")) prevContainsSections = 1;
        nodeCheck = nodeCheck.nextSibling;
      }

      // If there are subsections in the previous sibling section AND the section being
      // changed has subsections then the subsections would have to be demoted or the
      // section would have to be split into its paragraphs and sections -- way too
      // complicated -- let's not do it
      if (prevContainsSections == 1 && containsSections == 1) {
        changeValid = 0;
        Application.Alert("This section contains subsections -- can't change to paragraphs.");
      }
      else {
        // If we don't contain sections then find the last para or title and go after it
        node = prevSectnode.lastChild;
        rng.SelectAfterNode(node);
        rng.FindInsertLocation(paraName, false);
      }
    }
    // There are just paragraphs before this -- promote all the subsections
    else {
      strBody = strBody.replace(/Sect2>/g, "sect1>");
      strBody = strBody.replace(/Sect3>/g, "sect2>");
      strBody = strBody.replace(/Sect4>/g, "sect3>");
      
      // Fix the replaceable text
      strBody = strBody.replace(/xm-replace_text Section 2 Title/g, "xm-replace_text Section 1 Title");
      strBody = strBody.replace(/xm-replace_text Section 3 Title/g, "xm-replace_text Section 2 Title");
      strBody = strBody.replace(/xm-replace_text Section 4 Title/g, "xm-replace_text Section 3 Title");
    }

    // Select the section that we're planning to change


    if (changeValid == 1) {
      // Delete the whole section
      rngDelete.SelectElement();
      rngDelete.Delete();
 
      // Now insert the section as a Para 
      rng.InsertElement(paraName);
      rng.Select();
      strTitle = strTitle.replace(/xm-replace_text Section \d Title/g, "xm-replace_text Paragraph");
      rng.TypeText(strTitle);
      rng.SelectAfterContainer();
      if (strBody != "") rng.TypeText(strBody);
    }
    // Not valid change -- put insertion point in the title of the section
    else {
      Application.Alert("Change not valid");
      node = rngDelete.ContainerNode.firstChild;
      rng.SelectNodeContents(node);
      rng.Collapse(sqCollapseStart);
      rng.Select();
    }
    rng = null;
    rng2 = null;
  }
  else {
    Application.Alert("Put insertion pointer on the title of the subsection you want to convert");
  }
}

if (CanRunMacros()) {
  doConvertToParagraph();
}
 ]]></MACRO> 

<MACRO name="Convert to Article Title" key="" lang="JScript" id="1249" tooltip="Convert to Article Title" desc="Copy current Para to the Article Title"><![CDATA[
// Convert current selection or current paragraph to the Article title.  Overwrite title if there is one.
function doConvertToTitle() {
  if (Selection.IsParentElement("para") || Selection.IsParentElement("title")) {
    var rng = ActiveDocument.Range;
    if (rng.IsInsertionPoint) {
      rng.SelectContainerContents();
    }
    var title = rng.Text;            // save the text of the paragraph
    rng.MoveToDocumentStart();
    if (!rng.MoveToElement("title")) { // move to the Title
      rng.MoveToDocumentStart();        // insert Title if it's not there
      rng.MoveToElement("article");
      rng.InsertElement("title");
    }
    rng.SelectContainerContents();   // select Title element
    rng.PasteString(title);          // Paste in the saved text.
    rng = null;
  }
}

if (CanRunMacros()) {
  doConvertToTitle();
}
]]></MACRO> 

<MACRO name="Convert to Chapter Title" key="" lang="JScript" id="1249" tooltip="Convert to Chapter Title" desc="Copy current Para to the Chapter Title"><![CDATA[
// Convert current selection or current paragraph to the Chapter title.  Overwrite title if there is one.
// assumes chapter is the "top" element
function doConvertToTitle() {
  if (Selection.IsParentElement("para") || Selection.IsParentElement("title")) {
    var rng = ActiveDocument.Range;
    if (rng.IsInsertionPoint) {
      rng.SelectContainerContents();
    }
    var title = rng.Text;            // save the text of the paragraph
    rng.MoveToDocumentStart();
    if (!rng.MoveToElement("title")) { // move to the Title
      rng.MoveToDocumentStart();        // insert Title if it's not there
      rng.MoveToElement("chapter");
      rng.InsertElement("title");
    }
    rng.SelectContainerContents();   // select Title element
    rng.PasteString(title);          // Paste in the saved text.
    rng = null;
  }
}

if (CanRunMacros()) {
  doConvertToTitle();
}
]]></MACRO> 

<MACRO name="Join Paragraphs" key="" lang="JScript" id="1018" tooltip="Join Paragraphs" desc="Join selected paragraphs together into one paragraph"><![CDATA[
// Joins all selected paragraphs into one paragraph
function doJoinParagraphs() {
  var rng = ActiveDocument.Range;
  if (rng.IsInsertionPoint) {
    Application.Alert("Select the paragraphs to join");
  }
  else {
    var rng2 = rng.Duplicate;
    rng.Collapse(sqCollapseStart);  // the beginning of the selection
    rng.MoveToElement("para");  // Go to first paragraph in the selection
    var nd = rng.ContainerNode; // determine the element containing the Para
    var parent = nd.parentNode;

    rng2.Collapse(sqCollapseEnd);  // the end of the selection
    rng2.MoveToElement("para", false);  // Go to last paragraph in the selection
    var nd2 = rng2.ContainerNode; // determine the element containing the Para
    var parent2 = nd2.parentNode;

    // check that the elements moved to are "Para"s
    if (rng.ContainerName == "para" && rng2.ContainerName == "para") {
      rng.SelectContainerContents();
      rng2.SelectContainerContents();
      if (!rng2.IsGreaterThan(rng)) {
        Application.Alert("Select the paragraphs to convert to one paragraph");
      }
      else {
        if (parent == parent2) { // join paragraphs only if contained in same element
          var rng2Node = rng2.ContainerNode;
          var rng2PrevSib = rng2Node.previousSibling;
          while (rng2PrevSib && (rng2PrevSib.nodeType != 1)) // 1 == DOMElement
            rng2PrevSib = rng2PrevSib.previousSibling;
          while (rng2PrevSib && rng2.IsGreaterThan(rng) // Start from the end and join paragraphs one by one
                 && (rng2PrevSib.nodeName == "para")) { // Stop if an element other than Para is encountered
            rng2.JoinElementToPreceding();
            rng2.SelectContainerContents();
            rng2Node = rng2.ContainerNode;
            rng2PrevSib = rng2Node.previousSibling;
            while (rng2PrevSib && (rng2PrevSib.nodeType != 1)) // 1 == DOMElement
              rng2PrevSib = rng2PrevSib.previousSibling;
          }
          rng2.Select();    // Select the resultant paragraph.
        }
        else {
          Application.Alert("Cannot join paragraphs since they are in separate elements");
        }
      }
    }
    else {
      Application.Alert("Select the paragraphs to convert to one paragraph");
    }
    rng2 = null;
  }
  rng = null;
}

if (CanRunMacros()) {
  doJoinParagraphs();
}
]]></MACRO> 

<MACRO name="Promote Section" key="Ctrl+Alt+P" lang="JScript" id="20111" tooltip="Promote Section" desc="Convert current section to next-higher level section"><![CDATA[
// Convert current section into a section 1 smaller. ie. Sect2 to Sect1
function doPromoteSection() {
  // Selection must be in a title.
  var containNode = Selection.ContainerNode;
    if (containNode && containNode.nodeName == "title") {
    // Not valid for Sect
    if (Selection.IsParentElement("sect2") || Selection.IsParentElement("sect3") || Selection.IsParentElement("sect4")) {

      var rng = ActiveDocument.Range;
      var strTitle = "";
      var strBody = "";
      var strRest = "";
 
      // Copy the title of the section
      var node = rng.ContainerNode;
      rng.SelectNodeContents(node);
      strTitle = rng.Text;
    
      // Copy the rest the section to a string
      rng.SelectAfterContainer();
      var rng2 = rng.Duplicate;
      node = rng.ContainerNode;
      rng.SelectAfterNode(node.lastChild);
      rng2.ExtendTo(rng);
      strBody = rng2.Text;

      // Delete the section
      rng.SelectElement();
      rng.Delete();

      // Fix the subsections of the section we are promoting
      strBody = strBody.replace(/Sect2>/g, "sect1>");
      strBody = strBody.replace(/Sect3>/g, "sect2>");
      strBody = strBody.replace(/Sect4>/g, "sect3>");

      // Fix the replaceable text
      strBody = strBody.replace(/xm-replace_text Section 2 Title/g, "xm-replace_text Section 1 Title");
      strBody = strBody.replace(/xm-replace_text Section 3 Title/g, "xm-replace_text Section 2 Title");
      strBody = strBody.replace(/xm-replace_text Section 4 Title/g, "xm-replace_text Section 3 Title");
      strTitle = strTitle.replace(/xm-replace_text Section 2 Title/g, "xm-replace_text Section 1 Title");
      strTitle = strTitle.replace(/xm-replace_text Section 3 Title/g, "xm-replace_text Section 2 Title");
      strTitle = strTitle.replace(/xm-replace_text Section 4 Title/g, "xm-replace_text Section 3 Title");
      
      // Save the rest of the parent section
      rng2 = rng.Duplicate;
      node = rng.ContainerNode;
      rng.SelectAfterNode(node.lastChild);
      rng2.ExtendTo(rng);
      strRest = rng2.Text;
      rng2.Delete();

      // Put in the new section
      node = rng.ContainerNode;
      rng.SelectAfterContainer();
      rng.InsertElement(node.nodeName);

      // Insert the title and leave the selection there.
      rng.InsertElement("title");
      rng.TypeText(strTitle);
      rng.Select();

      // Insert the rest of the section that is being promoted
      rng.SelectAfterContainer();
      if (strBody != "") rng.TypeText(strBody);

      // Insert the last part as child to our new section
      node = rng.ContainerNode;
      rng.SelectAfterNode(node.lastChild);
      if (strRest != "") rng.TypeText(strRest);

      rng = null;
      rng2 = null;
    }
    else Application.Alert("This section is already a top level section");
  }
  else Application.Alert("Put your insertion cursor inside the title of the section you want to promote");
}
if (CanRunMacros()) {
  doPromoteSection();
}
]]></MACRO> 

<MACRO name="Demote Section" key="Ctrl+Alt+D" lang="JScript" id="20110" tooltip="Demote Section" desc="Convert current section to next-lower level section"><![CDATA[
// Convert current section into a section 1 bigger.  eg. Sect1 to Sect2
function doDemoteSection() {
  // Selection must be in a title.
    var containNode = Selection.ContainerNode;
    if (containNode && containNode.nodeName == "title") { 
    // If Sect4 or contains Sect4, can't do it!
    var rng = ActiveDocument.Range;
    rng.SelectElement();
    var node = rng.ContainerNode;  // the section node
    var elemlist = node.getElementsByTagName("sect4");
    // Ask explicitly just to rule out anything crazy
    var sectName = node.nodeName;
    if ((sectName == "sect1" || sectName == "sect2" || sectName == "sect3")
         && (elemlist.length == 0)) {
      // Also can't do it if there is no section at same level as this one before this one.
      rng.SelectBeforeNode(node);       // just before the section to be demoted
      var rngSave = rng.Duplicate;      // save this spot
      node = rng.ContainerNode;         // The parent section
      rng.SelectNodeContents(node);
      rng.Collapse(sqCollapseStart);    // The beginning of the contents of the parent sect
      rng.MoveToElement(sectName);      // Find the first section at the same level
      rng.SelectBeforeContainer();      // Just before the first section found
      if (rngSave.IsGreaterThan(rng)) {  // There is a section at same level before it

        var strTitle = "";
        var strBody = "";
        var strRest = "";
 
        rngSave.MoveToElement(sectName);  // Find the section to be demoted again
        rngSave.SelectElement();
        strBody = rngSave.Text;
        rngSave.Delete();
        
        // Fix the tags of the section we are demoting
        strBody = strBody.replace(/Sect3>/g, "sect4>");
        strBody = strBody.replace(/Sect2>/g, "sect3>");
        strBody = strBody.replace(/Sect1>/g, "sect2>");
        
        // Fix the replaceable text
        strBody = strBody.replace(/<\?xm-replace_text Section 3 Title\?>/g, "<\?xm-replace_text Section 4 Title\?>");
        strBody = strBody.replace(/<\?xm-replace_text Section 2 Title\?>/g, "<\?xm-replace_text Section 3 Title\?>");
        strBody = strBody.replace(/<\?xm-replace_text Section 1 Title\?>/g, "<\?xm-replace_text Section 2 Title\?>");

        rngSave.MoveToElement(sectName, false);  // Find sibling before it
        rngSave.SelectContainerContents();
        rngSave.Collapse(sqCollapseEnd);         // inside the end of the sibling
        rng = rngSave.Duplicate;
        if (strBody != "") rngSave.TypeText(strBody);
        rng.MoveToElement("title");
        rng.Select();

      }
      else Application.Alert("There has to be a section of the same level before this one");
    }
    else Application.Alert("This section is (or contains) a bottom level section");
    rng = null;
  }
  else Application.Alert("Put your insertion cursor inside the title of the section you want to demote");
}
if (CanRunMacros()) {
  doDemoteSection();
}
]]></MACRO> 

<MACRO name="On_Macro_File_Load" hide="false" lang="JScript"><![CDATA[

  var sqDefaultCursor = 0;
  var sqViewNormal = 0;
  var sqViewTagsOn = 1;
  var sqViewPlainText = 2;
  var sqCollapseEnd = 0;
  var sqCollapseStart = 1;
  var sqCursorHand = 4;
  var sqCursorArrow = 1;

var monthArray = new Array("January", "February", "March",
  "April", "May", "June", "July", "August", "September",
  "October", "November", "December");
var dateElems = new Array("PubDate");
var afDocumentComplete = true;

function num02(num)
{
  if (num < 10)
    return "0" + num;
  else
    return num + "";
}

function convertFromISODate(date)
{
  // date will have form YYYYMMDD. E.g. "19990214"
  // Return value will be Month DD, YYYY. E.g. "February 14, 1999"
  var year = date.substring(0, 4);
  var month = date.substring(4, 6) - 1;
  var day = date.substring(6, 8);
  if (month < 0 || year == "0000") {
    // date wasn't initialized
    // Use current time of day.
    var tod = new Date();
    year = tod.getYear();
    month = tod.getMonth();
    day = tod.getDate();
  }
  return monthArray[month] + " " + day + ", " + year;
}

function convertToDateArray(date)
{
  // Date will have form "February 14, 1999" and will
  // be returned as the array of numbers (1999, 2, 14).
  var dateArray = new Array(1, 1, 1);
  var r = date.match(/^(\S+)\s+([^,]+),\s(.*)$/);
  var i;

  // Convert the month to a number
  for (i=0; i<12; i++) {
    if (RegExp.$1 == monthArray[i]) {
      dateArray[1] = i+1;
      break;
    }
  }

  // Convert the day and year to numbers
  dateArray[2] = RegExp.$2 - 0; //day
  dateArray[0] = RegExp.$3 - 0; // year}

  return dateArray;
}

function fixISODate(/* Range */ r) {
	r.SelectContainerContents();
	var date = r.Text;
	if (!date.match(/[0-9]{8}/)) {
	  // Not a valid date. Set to today's date.
	  var tod = new Date();
	  date = tod.getYear() + num02(tod.getMonth()+1) +
		  num02(tod.getDate());
	  r.ReadOnlyContainer = false;
	  r.Text = date;
	  r.ReadOnlyContainer = true;
	}
	ActiveDocument.SetRenderedContent(r.ContainerNode, convertFromISODate(date));
}

function fixISODates()
{
  var r = ActiveDocument.Range;
  var i;
  for (i = 0; i < dateElems.length; i++) {
    var elemName = dateElems[i];
    r.MoveToDocumentStart();
    while (r.MoveToElement(elemName, true)) {
	  fixISODate(r);
    }
  }
  r = null;
  ActiveDocument.ClearAllChangedStates();
}

function hasChildPIs(children)
{
  var i = 0;
  var child = children.item(i);
  while (child) {
    if (child.nodeType == 7) { // PROCESSING_INSTRUCTION
      return i;
    }
    ++i;
    child = children.item(i);
  }
  return -1;
}

function getDTDName()
{
   var macroFN = ActiveDocument.MacroFile;
   var slash = macroFN.lastIndexOf("\\");
   var dot = macroFN.lastIndexOf(".");
   var dtdName = macroFN.substring(slash+1, dot);
   return dtdName;
}

function getStructureViewStylesFileName()
{
  var dtdName = getDTDName();

  // find the 2 possible paths for the SV styles file
  // (svsfn = structure view styles file name)
  var svsfn;
  var svsfnWithDoc = ActiveDocument.Path       + "\\" + dtdName + "_structure.css";
  var svsfnInSQDir = Application.Path + "\\display\\" + dtdName + "_structure.css";

  // figure out which one XMetaL is using
  try {
    var fso = new ActiveXObject("Scripting.FileSystemObject");
  }
  catch(exception) {
    result = reportRuntimeError("Structure View Styles Error:", exception);
    Application.Alert(result + "\nFailed to invoke Scripting.FileSystemObject\nYou need to get Windows Scripting Host from the Microsoft site.");
    return svsfnInSQDir;
  }
  if (fso.FileExists(svsfnWithDoc)) {
    svsfn = svsfnWithDoc;
  } else {
    svsfn = svsfnInSQDir;
  }
  return svsfn;
}

function switchSVStyles(fileName)
{
  try {
    var fso = new ActiveXObject("Scripting.FileSystemObject");
  }
  catch(exception) {
    var result = reportRuntimeError("Structure View Styles Error:", exception);
    Application.Alert(result + "\nFailed to invoke Scripting.FileSystemObject\nYou need to get Windows Scripting Host from the Microsoft site.");
    return;
  }
  try {
    var svsfn = getStructureViewStylesFileName();
    if (fileName) {
      // copy the new one over top of the current one
      var tail = svsfn.lastIndexOf("_structure.css");
      var newSVSFN = svsfn.substring(0, tail+1) + fileName;
      if (fso.FileExists(newSVSFN)) {
         var newCSS = fso.GetFile(newSVSFN);
         newCSS.Copy(svsfn);
      } else {
         Application.Alert(newSVSFN + " does not exist.");
      }
    } else {
      // delete the current one to force XMetaL to generate the default one
      var cssFile = fso.GetFile(svsfn);
      cssFile.Delete();
    }
    ActiveDocument.RefreshCssStyle();
  }
  catch (exception) {
    var result = reportRuntimeError("Structure View Styles Error:", exception);
    Application.Alert(result + "\nUse menu item View/Structure View to display Structure View or\nCheck that all journalist css files in \\Display are not read-only.");
  }
}

// Parse error formatting function
function reportParseError(error)
{
  var r = "XML Error loading '" + error.url + "'.\n" + error.reason;
  if (error.line > 0)
    r += "at line " + error.line + ", character " + error.linepos +"\n" + error.srcText;
  return r;
}

// Run-time error formatting function
function reportRuntimeError(preface, exception)
{
  return preface + " " + exception.description;
}


var UserName;

// Global function for Revision Control
function InitUserData(){
  var environ = new ActiveXObject("WScript.Network");
  UserName = environ.username;
}
  
InitUserData(); 

//
// Demonstrate the new customization possibilities for the Style Element box.
// Give the user who has just hit Para to get a new block at the end of n-level
// section, an opportunity to change it to different level heading (i.e a Title
// in a SectN).
//
function customizeStyleElementForPara() {
  var rng = ActiveDocument.Range;

  if (rng.ContainerName != "para") // element is not a <Para>
    return;

  var para = rng.ContainerNode;

  // Check whether <Para> contains only one child element of text (DOMText) or 
  // processing instruction (DOMProcessingInstruction)
  if (has_Only_One_Text_Or_PI_Child(para) != true)
    return;

  // Check whether the parent of <Para> is a <Sect N>
  if (isParentSect(para) != true)
    return;
  var sect = para.parentNode; // Current <Sect>

  // The smallest <Heading N> number in Style Element
  var smallestHeadingNumber = null;

  // The largest <Heading M> number.  If the parent <Sect K> that contains current <Para>
  // has K < 4, M = K + 1; otherwise, M = 4.
  var largestHeadingNumber = sect.nodeName.substr(sect.nodeName.length-1,1);
  if (largestHeadingNumber < 4) // K in <Sect K> is from 1 to 4 only.
    largestHeadingNumber++;

  // Get the smallest <Heading N> number.  The criterion is that the new <Heading N> inserted for 
  // current <Para> stays at the same position (in normal view) except its element tag changed.
  if (hasNonTextSibling(para) != true)
  {
    // Check whether <Para> is the last element of all kinds (except DOMText) in its <Sect>.
    // If it is, the smallest <Heading N> number = N, where <Sect N> is the first ancestor of 
    // current <Para> that has sibling of another <Sect N> for N > 1, or N = 1 regardless
    // <Sect 1> has another <Sect 1> sibling or not.

    smallestHeadingNumber = getAncestorSectNumber(para);
  }
  else if (hasParaSibling(para) != true)
  {
    // Check whether <Para> is the last <Para> element in its <Sect>
    // If it is, there will be only one Heading in Style Element with Heading number
    // equal to largestHeadingNumber initialized above.

    smallestHeadingNumber = largestHeadingNumber;
  }

  // Insert Heading N where N is from firstHeadingNumber to lastHeadingNumber
  if (smallestHeadingNumber)
    insHeadingInStyleElement(smallestHeadingNumber, largestHeadingNumber);
}


//
// Insert <Heading N> in Style Element where N is from firstHeadingNumber
// to lastHeadingNumber
//
function insHeadingInStyleElement(firstHeadingNumber, lastHeadingNumber) {
  var se = Application.StyleElements;
  for(var i = firstHeadingNumber; i <= lastHeadingNumber; i++)
  {  
    se.Insert(-1, "Heading "+i);
    se = Application.StyleElements;
  }
}


//
// Check whether element contains only one child element of text (DOMText) or 
// processing instruction (DOMProcessingInstruction)
//
function has_Only_One_Text_Or_PI_Child(element)
{
  if (!element)
    return null;

  var children = element.childNodes;

  if (!children)
    return false;

  if (children.length != 1)
    return false;

  var childNodeType = element.firstChild.nodeType;
  if (childNodeType != 3 && childNodeType != 7) // DOMText = 3, DOMProcessingInstruction = 7
    return false;

  return true;
}


//
// Check whether element's parent is a <Sect N>
//
function isParentSect(element)
{
  if (!element)
    return null;

  var parent = element.parentNode;
  if (!parent)
    return false;

  return isSectElement(parent);
}


//
// Check whether argument element is a <Sect> sibling
//
function isSectElement(element) {
  if (!element)
    return null;

  if (element.nodeName.substr(0, 4) != "sect")
    return false;

  return true;
}


//
// Check whether argument element has a sibling except DOMText; in other word,
// check if the argument element is the last element (except DOMText nodes).
//
function hasNonTextSibling(element)
{
  if (!element)
    return null;

  var element = element.nextSibling;
  while (element)
  {
    if (element.nodeType != 3) // Current element is not a DOMText node (DOMText = 3)
      return true;
    element = element.nextSibling;
  }
  return false; //
}

//
// Check if the argument Para has a sibling of another <Para> element
// in its <Sect N>, where N is an integer from 1 to 4.
//
function hasParaSibling(element)
{
  if (!element)
    return null;

  var element = element.nextSibling;
  while (element)
  {
    if (element.nodeName == "para")
      return true;
    element = element.nextSibling;
  }
  return false; //
}

//
// Get the smallest <Heading N> number when <Para> is the last element of all kinds in its <Sect>.
// <Sect N> is the first ancestor of current <Para> that has sibling of another <Sect N> for N > 1.
//  N = 1 regardless <Sect 1> has another <Sect 1> sibling or not.
//
function getAncestorSectNumber(para) {
  if (!para)
    return null;

  var ancestor = para.parentNode; // Current <Sect>
  if ((isSectElement(ancestor) != true))
    return null;

  // ancestorSectNumber is the return value.  It is initialized with N, where <Sect N> 
  // contains current <Para>.
  var ancestorSectNumber = ancestor.nodeName.substr(ancestor.nodeName.length-1,1);

  while ((isSectElement(ancestor) == true) && (hasSectSibling(ancestor) != true))
  { // Keep visiting parent of current <Sect N> until no more <Sect N-1> parent or <Sect N> has a sibling.
    ancestorSectNumber--;
    ancestor = ancestor.parentNode;
  }
  
  if (ancestorSectNumber <= 0) // In case ancestorSectNumber is decremented too much.
    ancestorSectNumber = 1;

  return ancestorSectNumber;
}


//
// Check whether argument element has a <Sect> sibling
//
function hasSectSibling(element)
{
  if (!element)
    return null;

  var sibling = element.nextSibling;
  while (sibling)
  {
    if (sibling.nodeName.substr(0, 4) == "sect")
      return true;
    sibling = sibling.nextSibling;
  }

  return false;
}

]]></MACRO> 

<MACRO name="Insert PubDate" key="Ctrl+Alt+T" lang="JScript" id="1915" tooltip="Insert PubDate" desc="Insert or update Publication Date element to the current date"><![CDATA[
function doInsertPubDate() {

  var PubDateList = ActiveDocument.getElementsByTagName("pubdate");
  var Rng = ActiveDocument.Range;
  Rng.MoveToDocumentStart();
  
  if (PubDateList.length > 0) {
    Rng.MoveToElement("pubdate");
    Rng.Select();
    Application.Alert("PubDate already present - click on it to change the date.");
    
  } else {

    if (Rng.FindInsertLocation("pubdate")) {
      Rng.InsertElement("pubdate");
      fixISODate(Rng);
      Rng.Select();
    } else {
      Application.Alert("Could not find insert location for PubDate");
    }
  }
  Rng = null;
}

if (CanRunMacros()) {
  doInsertPubDate();
}

]]></MACRO> 

<MACRO name="On_Click" hide="false" lang="JScript"><![CDATA[

  function doULinkOnClick(select)
  {
    // ... and set up for calling the brower
    var strURL   = "";
    var WSHShell = null;
    var pathMSIE = null;
 
    // make sure there is a URL attribute and read its value
    if (select.ContainerNode.hasAttribute("url")) {
      strURL = select.ContainerAttribute("url");
    } else {
      Application.Alert("No website address is available.");
      return;
    }
   
    // Now find the browser by reading the registry.
    try {
      WSHShell = new ActiveXObject("WScript.Shell");

      // Try for "Open in New window" regkey...
      try {
        pathMSIE = WSHShell.RegRead("HKCR\\htmlfile\\shell\\opennew\\command\\");

      // Missed, try regular "Open" regkey...
      } catch (e) {
        try {
          pathMSIE = WSHShell.RegRead("HKCR\\htmlfile\\shell\\open\\command\\");
       
        // Else, out of options...bail!
        } catch (e) {
          throw e;
        }
      }
     
      // If RegRead returns the string with a "%1" on the end -- trim it off
      if (pathMSIE.indexOf("%") > 0) {
        pathMSIE = pathMSIE.substr( 0, pathMSIE.indexOf("%") -2) + "\""
      }

      // Run command to open URL...
      WSHShell.Run(pathMSIE + " " + strURL, 1, false);

    } catch (e) {
      Application.Alert(e.description);
    }

    // Cleanup...
    WSHShell = null;
  }
  
  // Special process can be perform when clicking on
  // an element by creating an "On_Click" macro...
  
  // Get current cursor location...
  var rng = ActiveDocument.Range;

  // If cursor clicked on "ULink", then open URL...
  if (rng.ContainerName == "ulink") {
    doULinkOnClick(rng);
  }

]]></MACRO> 

<MACRO name="On_Mouse_Over" hide="true" lang="JScript"><![CDATA[


function isParentElement(ElemName, node) {
    while (node) {
       if (node.nodeName == ElemName)
         return true;
       node = node.parentNode;
    }
    return false;
}

function OnMouseOver()
{
  // initialize in case mouse out was never called
  Application.SetStatusText("");
  Application.SetCursor(sqDefaultCursor);
  
  var curNode = Application.MouseOverNode;
  if (curNode) {
    var nodeName = curNode.NodeName;
    if (nodeName == "pubdate") {
      Application.SetCursor(sqCursorHand);
      var rng = ActiveDocument.Range;
      rng.SelectNodeContents(curNode);
      rng.ContainerStyle = "color:red";
      rng = null;
      return;
    }
    if (nodeName == "ulink") {
      var rng = ActiveDocument.Range;
      rng.SelectNodeContents(curNode);
      rng.ContainerStyle = "color:red";
      var url = curNode.getAttribute("url");
      // Check if the attribute value is non-null
      // and set the status text acordingly
      if (url) {
         Application.SetStatusText(url);
         Application.SetCursor(sqCursorHand);
      }
      rng = null;
      return;
    }
    
  }
}
OnMouseOver();
 
]]></MACRO> 

<MACRO name="On_Document_Activate" hide="true" lang="JScript"><![CDATA[

Application.Run("On_Mouse_Over");
 
]]></MACRO> 

<MACRO name="On_Mouse_Out" hide="true" lang="JScript"><![CDATA[
function OnMouseOut()
{
  // initialize cursor and status text
  Application.SetCursor(sqDefaultCursor);
  Application.SetStatusText("");
  
  var curNode = Application.MouseOverNode;
  if (curNode) {
    var nodeName = curNode.NodeName;
    if (nodeName == "ulink") {
      var rng = ActiveDocument.Range;
      rng.SelectNodeContents(curNode);
      rng.ContainerStyle = "color:blue";
      rng = null;
      return;
    }
    if (nodeName == "pubdate") {
      var rng = ActiveDocument.Range;
      rng.SelectNodeContents(curNode);
      rng.ContainerStyle = "color:black";
      rng = null;
      return;
    }
  }
}
OnMouseOut();
]]></MACRO> 

<MACRO name="On_Document_Deactivate" hide="true" lang="JScript"><![CDATA[

Application.Run("On_Mouse_Out");

]]></MACRO> 

<MACRO name="On_View_Change" lang="JScript"><![CDATA[

// refreshes the Insertion and Deletion element container styles on view change from
// plain text to Normal or Tags on
function refreshStyles() {
  var docProps = ActiveDocument.CustomDocumentProperties;

}
//-------------------------------------------------------------------------------------------------------
//-------------------------------------------------------------------------------------------------------

//*****************************************************************************************************
//*****************************************************************************************************
var docProps = ActiveDocument.CustomDocumentProperties;
var Annot_parent = false;
var start, end, element;

if ((ActiveDocument.ViewType==sqViewNormal ||
     ActiveDocument.ViewType==sqViewTagsOn) &&
     ActiveDocument.PreviousViewType==sqViewPlainText) {
  fixISODates();
  var LastModList = ActiveDocument.getElementsByTagName("date");
  var Rng = ActiveDocument.Range;
  if (LastModList.length > 0) {
    var i = 0;
    var k = LastModList.Count;
  	while (i < k && LastModList.item(i).Attributes.getNamedItem("role") != "LastMod")
  	{
  		inc(i);
  	}
    Rng.SelectNodeContents(LastModList.item(0));
    Rng.ReadOnlyContainer = true;
  }
  Rng = null;

  refreshStyles();

}

    
]]></MACRO> 


<MACRO name="Import SeeAlso" lang="JScript" id="1362" tooltip="Import 'See Also' Table" desc="Import a 'See Also' table from a database"><![CDATA[
// SoftQuad Script Language JScript:
function RepairXMetaLInstallPath(paramFile) {
	// Open the param.txt
	var iomode = 1;  // ForReading
	var createmode = false; // a new file is NOT created if the specified filename doesn't exist.
	var formatmode = -1;  // Unicode
	if (Application.UnicodeSupported == false) {
		formatmode = 0;  // ASCII
	}

	try {
		var fso = new ActiveXObject("Scripting.FileSystemObject");
		var f = fso.OpenTextFile(paramFile, iomode, createmode, formatmode );
	}
	catch(exception) {
		result = reportRuntimeError("Import Table Error:", exception);
		Application.Alert(result + "\nFailed to invoke Scripting.FileSystemObject\nYou need to get Windows Scripting Host from the Microsoft site.");
		fso = null;
		rng = null;
		return false;
	}

	// Read the whole file
	var str;
	str = f.ReadAll();
	f.Close();

	// Insert the xmetal install path if necessary.

	if (!str) {
	       Application.Alert("Initialization file for Database Import Wizard is empty.");
		return true; // file empty? Carry on anyway.
	}
	
	var  found = str.search(/XMETAL_INSTALL_PATH/g);
	if (found == -1) {
		return true;  // Not there, don't need to do anything.
	}

	var path = Application.Path;
	str = str.replace(/XMETAL_INSTALL_PATH/g, path);
	
	var iomode = 2;  // ForWriting
	var createmode = true; // a new file is created if the specified filename doesn't exist.
	f = fso.OpenTextFile(paramFile, iomode, createmode, formatmode);
	f.Write(str);

	// Close the text file
	f.Close();
	return true;

}

function doImportSeeAlso() {
// Local variables
  var paramFile = Application.Path + "\\Samples\\Cameras\\SA_param.txt";
  var tableFile = Application.Path + "\\Samples\\Cameras\\SA_table.htm";
  
//Fix XmetaL Install Path in param.txt
  if (!RepairXMetaLInstallPath(paramFile)) return;

// Find a place to insert the table
  var rng = ActiveDocument.Range;
  rng.MoveToDocumentEnd();
  if (rng.MoveToElement("seealso", false)) {
    Application.Alert("SeeAlso table already exists in document");
    rng.Select();
    rng = null;
    return;
  }
  
  rng.MoveToDocumentEnd();
  if (!rng.FindInsertLocation("seealso", false)) {
    Application.Alert("Could not find insert location for SeeAlso table");
    rng = null;
    return;
  }
    
  var result = "";
  // Generate a new, unique, parameter file name
  var newParamFile=Application.UniqueFileName(Application.Path + "\\Samples\\Cameras\\","SA_",".txt");

  // Copy the old parameter file into the new one
  // (so that the wizard won't come up blank)
  if (paramFile != null) {
    try {
      var fso = new ActiveXObject("Scripting.FileSystemObject");
      fso.CopyFile(paramFile,newParamFile);
    }
    catch(exception) {
      result = reportRuntimeError("Import See Also Table Error:", exception);
      Application.Alert(result + "\nFailed to invoke Scripting.FileSystemObject\nYou need to get Windows Scripting Host from the Microsoft site.");
      fso = null;
      rng = null;
      return;
    }
    fso = null;
  }

  // Run the wizard
  // Show the dialog
  try {
    var obj = new ActiveXObject("SoftQuad.DBImport");
    var ret = obj.NewDBImport(newParamFile, tableFile);
  }
  catch(exception) {
    result = reportRuntimeError("Import SeeAlso Error:", exception);
    Application.Alert(result + "\nPlease register DBImport.dll");
    rng = null;
    obj = null;
    return;
  }

  // If user chose OK ...
  if (ret) {

    // Read the resulting table into the document
    var str = Application.FileToString(tableFile);
    if (!rng.CanPaste(str)) {
      Application.Alert("Table is invalid.\nSee journalist.dtd for correct structure of the SeeAlso table.");
      rng = null;
      return;
    }
    rng.TypeText(str);
    // The table id is the paramFile, minus the full path
    var splitPath=newParamFile.split('\\');
    var spLength=splitPath.length;
    var tableId=splitPath[spLength-1];

    rng.MoveToElement("seealso", false); // move backwards to table element
    if (rng.ContainerName == "seealso") {
      rng.ContainerAttribute("id") = tableId; // for later updates
    }

    // Scroll to the location of the inserted table
    rng.Select();
    ActiveDocument.ScrollToSelection();
    Selection.MoveLeft();  // avoid big cursor next to table

    // Copy the new parameter file into the original one
    // (so that the next time the wizard is run, our new parameter file will dictate the initial state)
    if (paramFile != null) {
      try {
        var fso = new ActiveXObject("Scripting.FileSystemObject");
        fso.CopyFile(newParamFile,paramFile);
      }
      catch(exception) {
        result = reportRuntimeError("Import See Also Table Error:", exception);
        Application.Alert(result + "\nFailed to invoke Scripting.FileSystemObject\nYou need to get Windows Scripting Host from the Microsoft site.");
        fso = null;
        rng = null;
        return;
      }
      fso = null;
    }

  } 
  rng = null;
  obj = null;
}

if (CanRunMacros()) { 
  doImportSeeAlso();
}
]]></MACRO> 

<MACRO name="Update SeeAlso" lang="JScript" tooltip="Update 'See Also' Table" desc="Update 'See Also' table imported from database" id="1363"><![CDATA[
// SoftQuad Script Language JScript:
function doUpdateSeeAlso() {
  // Local variables
  var rng = ActiveDocument.Range;
  var paramFile = Application.Path + "\\Samples\\Cameras\\SA_param.txt";
  var tableFile = Application.Path + "\\Samples\\Cameras\\SA_table.htm";
  
  // Check that we are inside a table
  var node = rng.ContainerNode;
  while (node && node.nodeName != "seealso") {
    node = node.parentNode;
  }
  
  if (node) {
    // Check we are in the right kind of table
    var tableId=rng.ElementAttribute("id", "seealso");

    var paramFile = Application.Path + "\\Samples\\Cameras\\" + tableId;
    try {
      var fso = new ActiveXObject("Scripting.FileSystemObject");
    }
    catch(exception) {
      result = reportRuntimeError("Update SeeAlso Error:", exception);
      Application.Alert(result + "\nFailed to invoke Scripting.FileSystemObject\nYou need to get Windows Scripting Host from the Microsoft site.");
      rng = null;
      return;
    }
    if (fso.FileExists(paramFile)) {

      // Insert the new table
      try {
        var obj = new ActiveXObject("SoftQuad.DBImport");
        obj.UpdateDBImport(paramFile, tableFile);
      }
      catch(exception) {
        result = reportRuntimeError("Update SeeAlso Error:", exception);
        Application.Alert(result + "\nPlease register DBImport.dll");
        obj = null;
        rng = null;
        return;
      }

      // Delete the old table
      rng.SelectNodeContents(node);
      rng.SelectElement();
      rng.Delete();

      // Read the resulting table into the document
      var str = Application.FileToString(tableFile);
      rng.TypeText(str);

      // Set the border and id attributes of the table
      rng.MoveToElement("seealso", false); // move backwards to table element
      if (rng.ContainerName == "seealso") {
        rng.ContainerAttribute("id") = tableId; // for later updates
      }

      // Scroll to the location of the inserted table
      rng.MoveLeft();
      rng.Select();
      ActiveDocument.ScrollToSelection();
      obj = null;
    }
    else {
      Application.Alert("Parameter file "+paramFile+" does not exist.\nCannot update this table.");
    }
  }
  else {
    Application.Alert("You are not currently inside a SeeAlso table.");
  }
  
  rng = null;
}

if (CanRunMacros()) {
  doUpdateSeeAlso();
}
]]></MACRO> 

<MACRO name="Use 1.css for the Structure View" lang="JScript" id="1440" desc="Use 1.css for the Structure View"><![CDATA[
if (ActiveDocument.ViewType == sqViewNormal || ActiveDocument.ViewType == sqViewTagsOn) {
  if (ActiveDocument.StructureViewVisible)
    switchSVStyles("1.css");
  else
    Application.Alert("Structure view not showing");
}
else
  Application.Alert("Change to Tags On or Normal view to run macros.");
]]></MACRO> 

<MACRO name="Use 2.css for the Structure View" lang="JScript" id="1441" desc="Use 2.css for the Structure View"><![CDATA[
if (ActiveDocument.ViewType == sqViewNormal || ActiveDocument.ViewType == sqViewTagsOn) {
  if (ActiveDocument.StructureViewVisible)
   switchSVStyles("2.css");
  else
    Application.Alert("Structure view not showing");
}
else
  Application.Alert("Change to Tags On or Normal view to run macros.");
]]></MACRO> 

<MACRO name="Use 3.css for the Structure View" lang="JScript" id="1442" desc="Use 3.css for the Structure View"><![CDATA[
if (ActiveDocument.ViewType == sqViewNormal || ActiveDocument.ViewType == sqViewTagsOn) {
  if (ActiveDocument.StructureViewVisible)
   switchSVStyles("3.css");
  else
    Application.Alert("Structure view not showing");
}
else
  Application.Alert("Change to Tags On or Normal view to run macros.");
]]></MACRO> 

<MACRO name="Use 4.css for the Structure View" lang="JScript" id="1443" desc="Use 4.css for the Structure View"><![CDATA[
if (ActiveDocument.ViewType == sqViewNormal || ActiveDocument.ViewType == sqViewTagsOn) {
  if (ActiveDocument.StructureViewVisible)
   switchSVStyles("4.css");
  else
    Application.Alert("Structure view not showing");
}
else
  Application.Alert("Change to Tags On or Normal view to run macros.");
]]></MACRO> 

<MACRO name="Use 5.css for the Structure View" lang="JScript" id="1444" desc="Use 5.css for the Structure View"><![CDATA[
if (ActiveDocument.ViewType == sqViewNormal || ActiveDocument.ViewType == sqViewTagsOn) {
  if (ActiveDocument.StructureViewVisible)
   switchSVStyles("5.css");
  else
    Application.Alert("Structure view not showing");
}
else
  Application.Alert("Change to Tags On or Normal view to run macros.");
]]></MACRO> 

<MACRO name="Use the default (generated) Structure View" lang="JScript" id="1448" desc="Use the default (generated) Structure View"><![CDATA[
if (ActiveDocument.ViewType == sqViewNormal || ActiveDocument.ViewType == sqViewTagsOn) {
  if (ActiveDocument.StructureViewVisible)
   switchSVStyles();
  else
    Application.Alert("Structure view not showing");
}
else
  Application.Alert("Change to Tags On or Normal view to run macros.");
]]></MACRO> 

<MACRO name="Graphic_OnShouldCreate" key="" hide="true" lang="JScript"><![CDATA[
  // SoftQuad Script Language JSCRIPT:
  var ipog = Application.ActiveInPlaceControl;
  if (ipog != null) {
    // Only create for FileRef's with .html extensions otherwise default to
    // built-in XMetaL behavior...
    ipog.ShouldCreate = false;
    var domnode = ipog.Node;
    if (domnode != null) {
      var attrnode = domnode.attributes.getNamedItem("fileref");
      if (attrnode != null && attrnode.value != null) 
      {
        if ((attrnode.value.lastIndexOf(".html") != -1) || (attrnode.value.lastIndexOf(".svg") != -1))
        {
          ipog.ShouldCreate = true; // Has .html extension, instruct to create control!
        }
      }
    }
  }
]]></MACRO> 

<MACRO name="Graphic_OnInitialize" key="" hide="true" lang="JScript"><![CDATA[
  // SoftQuad Script Language JSCRIPT:
  var ipog = Application.ActiveInPlaceControl;
  if (ipog != null) {
    var domnode = ipog.Node;

    // Set width of control (pixels) from Graphic's "Width" attribute
    var attrnode = domnode.attributes.getNamedItem("width");
    if (attrnode != null) {
      ipog.Width = attrnode.value; // Set width in pixels from Graphic Width attr
    }

    // Set height of control (pixels) from Graphic's "Depth" attribute
    attrnode = domnode.attributes.getNamedItem("depth");
    if (attrnode != null) {
      ipog.Height = attrnode.value; // Set height in pixels from Graphic Depth attr
    }

    // Set IE Control "FileName" property from Graphic's "FileRef" attribute
    // but note that the IE Control needs an absolute filepath...
    attrnode = domnode.attributes.getNamedItem("fileref");
    if (attrnode != null) {
      var str = ipog.Document.LocalPath;
      str = str + "\\";
      str = str + attrnode.value;
      var mp = ipog.Control;
      if (mp != null) {
        mp.Navigate2(str, 2);
      } else {
        Application.Alert("No IE Control object!");
      }
    }
  }
]]></MACRO>

<MACRO name="Save As PDF" key="" lang="JScript" id="2000"><![CDATA[

function saveAsPDFHelper() {
  if (!getFopFolder())
    if (XMLToPDFSetup() != true) // no FOP Folder, need PDF setup
      return;

  usePDF = true; // notice that usePDF is global and defined in multipleOutput.mcr
  saveAsPDF();
}

saveAsPDFHelper();

]]></MACRO> 

<MACRO name="Setup PDF" key="" lang="JScript" id="2001"><![CDATA[
XMLToPDFSetup();
]]></MACRO> 

<MACRO name="View PDF" key="" lang="JScript" id="2003"><![CDATA[

function viewPDFHelper() {
  if (!getFopFolder())
    if (XMLToPDFSetup() != true) // no FOP Folder, need PDF setup
      return;

  previewPDF();
}

viewPDFHelper();

]]></MACRO> 

<MACRO name="View HTML" key="" lang="JScript" id="2002"><![CDATA[

// Preview HTML.  Check if setup HTML is needed.
// Following functions are in multipleOutput.mcr
//   createFileSystemObject(), getXSLNameForHTML(), XMLToHTMLSetup() and previewHTML()
function viewHTMLHelper() {
  var fso = createFileSystemObject();
  if (!fso)
    return;
  var xslPath = getXSLNameForHTML();
  if (!fso.FileExists(xslPath))
    XMLToHTMLSetup();
  previewHTML();
}

viewHTMLHelper();

]]></MACRO> 


<MACRO name="On_Style_Element" lang="JScript"><![CDATA[
// This macro will be called when "Style Element" option is selected and changed.

//
// Insert new <Sect N> with title of current <Para> text content, with corrsponding <Heading N>
// selected in Style Element.  If <Para> content is processing instruction (PI) which is assumed
// a xm-replace_text PI, new <Sect N> title will also be a xm-replace_text PI.
//
function insNewSectForLastPara() {
  var styleElementName = Application.StyleElementName; // Get the name of selected style element

  if (styleElementName.indexOf("Heading") < 0) // Style element selected is not a <Heading N>
    return;

  // the last number in Heading1 or Heading2 or Heading3...
  var headingNumber = styleElementName.substr(styleElementName.length-1, 1);
  var sectName = "sect" + headingNumber;

  var rng = ActiveDocument.Range;
  var para = rng.ContainerNode;

  // Insert <Sect N> and <Title> element
  if(!rng.FindInsertLocation(sectName))
     return;
  rng.InsertElement(sectName);
  rng.InsertElement("title");

  // Type content of <Title>.  If <Para> has text content, copy <Para> content to
  // <Title>.  If <Para> has a xm-replace_text PI (assumed), insert a xm-replace_text PI
  // in <Title>
  var paraContentNode = para.firstChild;
  if (paraContentNode.nodeType == 3) // <Para> has text content.  DOMText = 3
  {
    var paraContentText = paraContentNode.data;
    rng.typeText(paraContentText);
  } 
  else if (paraContentNode.nodeType == 7) // <Para> has xm-replace_text PI.  DOMProcessingInstruction = 7
    rng.InsertReplaceableText("Section Title");

  para.parentNode.removeChild(para);
  rng.Select();
}

if (Application.StyleElementName.indexOf("Heading") >= 0)
  insNewSectForLastPara();
else
  Selection.Style = Application.StyleElementType; // default action

]]></MACRO> 


<MACRO name="Setup HTML" key="" lang="JScript"><![CDATA[
XMLToHTMLSetup();  
]]></MACRO>


<MACRO name="On_Check_Element_SimpleContent" key="" hide="true" lang="JScript"><![CDATA[

var cd = Application.CheckData;
var elem = cd.Element;
var name = elem.tagName;

// Check value.....
if(name == "email") {

	var value = cd.Value;
	if (value != null && value != "") {

		var re = new RegExp("^[^@ ]+@[^@ ]+$");
		if (re.exec(value) == null) {
			var msg = "Invalid email address value:"
			msg += "'";
			msg += value;
			msg += "'";
			
			cd.ValidationMsg = msg;
		}
	}
}	

]]></MACRO>

<MACRO name="On_Check_Attribute_Value" key="" hide='true' lang="JScript"><![CDATA[

var cd = Application.CheckData;
var elem = cd.Element;
var name = elem.tagName;
var attrName = cd.AttributeName;

if((name == "inlinegraphic" || name == "graphic")
    && attrName == "fileref") {

	var value = new String(cd.Value);
    var msg = null; 

	if(value .indexOf(" ")>=0) {
         msg = "URL cannot have whitespace:"		  
    }
	else if (value.indexOf("\\")>= 0) {
          msg = "URL cannot have backslash:"		  
	}

   if(msg) {
	msg += cd.AttributeName;
	msg += "='";
	msg += value;
	msg += "'";
       cd.ValidationMsg = msg;	
   }
}
]]></MACRO>
<MACRO name="Convert To Entity+Reference" key="" lang="JScript" id="1527"><![CDATA[
// Make the selected node an external entity and insert a refence in its place
function doConvertToReference() 
{
  // Selection must be in a title.
  var containNode = Selection.ContainerNode;
  if (containNode && containNode.nodeName == "title") 
  {
  	// todo: use the create entity dialog...
	var rng = ActiveDocument.Range;
	var node = rng.ContainerNode;
	rng.SelectNodeContents(node);
	var strTitle = rng.Text;
    var strBody = "";
	
	// Copy the contents of the chapter/part/...
	node = rng.ContainerNode.parentNode;
	rng.SelectNodeContents(node);
	strBody = rng.Text;

    // prepare a string for the "new document" and open a new document
	var xmlDec = '<?xml version="1.0" ?>';
	var styleSheet = '<?xml-stylesheet type="text/css" href="Display/DocBookx.css" ?>';
	var note = '<!-- you must comment out the doctype declaration for external entities -->';
	//var docType = '<!DOCTYPE chapter PUBLIC "-//OASIS//DTD DocBook XML V4.1.2//EN" "c:/src/docs/docbookmanuals/DocBookx.dtd">';
	var docType = '<!DOCTYPE chapter PUBLIC "-//OASIS//DTD DocBook XML V4.1.2//EN" "DocBookx.dtd">';
	var doc = xmlDec + '\n' + styleSheet + '\n' + note + '\n' + docType + '\n';
	var currDoc = Application.ActiveDocument;
	// todo: get real file name not test.xml
	
	var Dlg = new ActiveXObject("SQExtras.FileDlg");
	var chosen = Dlg.DisplayFileDlg(false, "Save " + node.nodeName + " as", 
			"xml files (*xml)| *.xml||",
			ActiveDocument.LocalPath, "xml",
			strTitle);
	if (chosen)
	{
		var chosenFile = Dlg.FullPathName;
		Documents.OpenString(doc,1,chosenFile,false);
	
		// insert the selected part/chapter/... into the new document
		var rng2 = ActiveDocument.Range;
		rng2.InsertElement(node.nodeName);
		rng2.TypeText(strBody);
		ActiveDocument.Save();

		//currDoc.Activate();
		// Delete the section
		//rng.SelectElement();
		//rng.Delete();
	}
  }
  else
   Application.Alert("Put your insertion cursor inside the title of the part/chapter/section you want to convert to an external entity.");
// get save as file name: DisplayFileDlg pg301
// create new document (OpenString pg180 documents interface) takes care of 2 steps
// copy the selected node to it (may need to use the clipboard)
// save new doc
//
// delete selected node from current document 
// get name for entity
// DeclareExternalEntity in document pg157
// createEntityReference in document pg153 or DOMDocument pg366
}
if (CanRunMacros()){
  doConvertToReference();
}]]></MACRO>
<MACRO name="Set Missing ID" key="" lang="JScript" id="1919"><![CDATA[
// Run through the document setting ID on TOC level tags
function doSetMissingID()
{
	var i;
	var elemList;
	var lElement;
	var lPrefix;
	var lTocTags = "set book part sect1 sect2 sect3 sect4 sect5 simplesect section appendix"
	var re="/\s/"
	var lTitle;
	var lTitleNode;
	var lChildren;
	var lID;
	
	lPrefix = Application.Prompt("Enter the prefix for IDs in this document","",35,35,"Alphora Documentation");
	if (lPrefix == "")
		return;
	elemList = ActiveDocument.getElementsByTagName("*");
	//Application.Alert(elemList.Length);
	for (i = 0; i < elemList.Length; i++)
	{
		lElement = elemList.item(i);
		// process only TOC level tags
		if (lTocTags.indexOf(lElement.nodeName) > -1)
		{
			if (lElement.hasAttribute('id') != true)
			{
				lChildren = lElement.childNodes;
				
				for (j = 0; j < lChildren.length; j++)
				{
				 	lTitleNode = lChildren.item(j);
				 	if (lTitleNode.nodeName == 'title')
				 		break;
				}
				
				if (lTitleNode != null)
				{
					if	(lTitleNode.lastChild != null)
						lTitle = lTitleNode.lastChild.nodeValue;
					else
						lTitle = "";
				}
				else
					lTitle = "";
				lID = Application.Prompt("Proposed ID for " + lElement.nodeName,lPrefix + lTitle.replace("/\s/g",""),40,100,"Alphora Documentation");
				if (lID != "")
					lElement.setAttribute("id",lID);
			}
		}
	}
}
if (CanRunMacros()) {
	doSetMissingID();
}]]></MACRO>
<MACRO name="Insert Inline Code" key="Ctrl+Shift+M" lang="JScript"><![CDATA[

Selection.Surround("phrase")
Selection.ElementAttribute("role", "phrase", 0) = "code"

]]></MACRO>
<MACRO name="Insert Keyword" key="Ctrl+Shift+K" lang="JScript"><![CDATA[

Selection.Surround("emphasis")
Selection.ElementAttribute("role", "emphasis", 0) = "bold"

]]></MACRO>
</MACROS> 
