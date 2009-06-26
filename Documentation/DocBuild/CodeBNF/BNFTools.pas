{
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
}
unit BNFTools;
interface

uses SysUtils, Classes;
// todo: need a progress procedure?

type
  TBNFExtractor = class(TObject)
  private
    FSource: TStrings;
    FBNF: TStrings;
    FHeader: TStrings;
    FOutput: TStrings;
    FTitle: string;
    function GetBadPunctuations: TStrings;
    function GetBNF: TStrings;
    function GetDocbook: TStrings;
    function GetHTML: TStrings;
    function GetMissedProductions: TStrings;
    function GetSource: TStrings;
    procedure SetBNF(const Value: TStrings);
    procedure SetSource(const Value: TStrings);
    function GetHeader: TStrings;
    procedure SetHeader(const Value: TStrings);
    // FOnProgress:
  protected
    procedure ExtractBNFComments;
    procedure CollectStatements(ALeftList, ARightList: TStringList);
    procedure CollectRuleNames(aNameList: TStrings);
    procedure SimplifyRightSide(AStrings: TStringList);
    function NextStatement(aBNF: string; var aOffset: integer): string;
    function LeftOf(aStatement: string): string;
    function RightOf(aStatement: string): string;
    procedure ReportPunctSyntax(aStatement: string);
    procedure GenHtmlXRef(aStrings: TStrings);
    procedure GenDocbookXRef(aStrings: TStrings; aPrefix: string);
    procedure GenHeader(aStrings: TStrings);
  public
    constructor Create;
    destructor Destroy; override;
    property SourceLines: TStrings read GetSource write SetSource;
    property BNF: TStrings read GetBNF write SetBNF;
    property Header: TStrings read GetHeader write SetHeader;
    property HTML: TStrings read GetHTML;
    property Docbook: TStrings read GetDocbook;
    property MissedProductions: TStrings read GetMissedProductions;
    property BadPunctuations: TStrings read GetBadPunctuations;
    property Title: string read FTitle write FTitle;
    //property OnProgress:
  end;

{
function LoadCSSource(const aFilename: string; aBNF: TStrings): boolean; // reuturn success/fail
function ValidateBNF(aBNF: TStrings): integer; // return count
function ValidatePunctuation(aBNF: TStrings): integer; // return count
function BNFToHTML(aBNF, aHTML: TStrings): boolean;
function BNFToDocbook(aBNF, aHTML: TStrings): boolean;
}
implementation

{ TBNFExtractor }

procedure TBNFExtractor.CollectRuleNames(aNameList: TStrings);
var
  LStatement: string;
  LStatementOffset: integer;
  LSub: integer;
begin
  aNameList.Clear;
  LStatementOffset := 1;
  //Gauge1.MaxValue := length(FBnf.text);
  //Gauge1.Progress := 0;
  while (LStatementOffset < Length(FBNF.Text)) do
  begin
    //Gauge1.Progress := LStatementOffset;
    LStatement := NextStatement(FBNF.Text,LStatementOffset);
    if aNameList.IndexOf(LStatement) <> -1 then
      //MessageDlg('Duplicate production named ' + LStatement,mtWarning,[mbok],0);
      ;
    aNameList.Add(LeftOf(LStatement));
  end;
  for LStatementOffset := 0 to pred(aNameList.Count) do
  begin
    LStatement := aNameList[LStatementOffset];

    // strip < >
    LStatement := trim(copy(LStatement,2,length(LStatement) - 2));

    // strip non empty list qualifier
    if pos('ne ',LStatement) = 1 then
      LStatement := Copy(LStatement,4,MaxInt);

    // strip commalist off end of reference
    LSub := pos(' commalist',LStatement);
    if LSub > 0 then
      LStatement := copy(LStatement,1,LSub - 1);

    // strip semicolonlist off end of reference
    LSub := pos(' semicolonlist',LStatement);
    if LSub > 0 then
      LStatement := copy(LStatement,1,LSub - 1);

    // strip list off end of reference
    LSub := pos(' list',LStatement);
    if LSub > 0 then
      LStatement := copy(LStatement,1,LSub - 1);

    aNameList[LStatementOffset] := LStatement;
  end;
end;


procedure TBNFExtractor.CollectStatements(ALeftList,
  ARightList: TStringList);
var
  LLeftSide: TStringList;
  LRightSide: TStringList;
  LStatement: string;
  LStatementOffset: integer;
begin
  LLeftSide := TStringList.Create;
  LRightSide := TStringList.Create;
  try
    LStatementOffset := 1;
    //Gauge1.MaxValue := length(FBnf.text);
    //Gauge1.Progress := 0;
    while (LStatementOffset < Length(FBNF.Text)) do
    begin
      //Gauge1.Progress := LStatementOffset;
      LStatement := NextStatement(FBNF.Text,LStatementOffset);
      LLeftSide.Add(LeftOf(LStatement));
      LRightSide.Add(RightOf(LStatement));
    end;
    SimplifyRightSide(LRightSide);
    // FOutput.Assign(LLeftSide); // debug
    // FOutput.Assign(LRightSide); // debug
    FOutput.Clear;
    for LStatementOffset := 0 to pred(LRightSide.Count) do
      if LLeftSide.IndexOf(LRightSide[LStatementOffset]) = -1 then
        FOutput.Add(LRightSide[LStatementOffset]);
    if FOutput.Count = 0 then
      FOutput.Add('---- No missing defintions ----')
    else
      FOutput.Insert(0, '---- Missing defintions ----');
  finally
    LLeftSide.Free;
    LRightSide.Free;
  end;
end;


constructor TBNFExtractor.Create;
begin
  // todo: setups?
end;

destructor TBNFExtractor.Destroy;
begin
  if assigned(FSource) then
    FSource.Free;
  if assigned(FBNF) then
    FBNF.Free;
  if assigned(FHeader) then
    FHeader.Free;
  if assigned(FOutput) then
    FOutput.Free;
  inherited;
end;

procedure TBNFExtractor.ExtractBNFComments;
var
  i: integer;
begin
  i := 0;
  BNF.Clear;
  Header.Clear;
  //Gauge1.MaxValue := pred(FSource.Count);
  while i < FSource.Count do
  begin
    //Gauge1.Progress := i;
    if pos('/*',FSource[i]) > 0 then
    begin
      inc(i);
      if AnsiCompareText('BNF:',trim(FSource[i])) = 0 then
      begin
        inc(i);
        while (i < FSource.Count) and (pos('*/',FSource[i]) = 0) do
        begin
          // prettify the results with consistant tabbing (remove 2 tabs)
          if pos('::=',FSource[i]) > 0 then
            FBNF.Add(copy(FSource[i],3,MaxInt))
          else
            if (trim(FSource[i]) <> '') then
              FBNF.Add(Copy(FSource[i],3,MaxInt))
            else
              FBNF.Add('');
          inc(i);
        end;
        FBNF.Add('');
      end
      else
      begin
        if AnsiCompareText('HEADER:',trim(FSource[i])) = 0 then
        begin
          inc(i);
          while (i < FSource.Count) and (pos('*/',FSource[i]) = 0) do
          begin
            Header.Add(Copy(FSource[i],3,MaxInt));
            inc(i);
          end;
        end
        else
          while (i < FSource.Count) and (pos('*/',FSource[i]) = 0) do
            inc(i);
      end;
    end;
    inc(i);
  end;
end;

procedure TBNFExtractor.GenDocbookXRef(aStrings: TStrings;
  aPrefix: string);
var
  i: integer;
  LString: string;
  LAnchor: string;
  LOffset: integer;
  LBegin: integer;
  LSub: integer;
  LNames: TStrings;

  function NextTerm(aString: string): string;
  var
    LEnd: integer;
  begin
    // locate beginning
    LBegin := LOffset;
    while (LBegin < length(aString)) and ('&lt;' <> copy(aString,LBegin,4)) do
      inc(LBegin);

    if not (LBegin < length(aString))  then
    begin
      Result := '';
      LOffset := length(AString);
      exit;
    end;

    // locate the ending
    LEnd := LBegin + 4;
    if (not (aString[LEnd] in ['A'..'Z','a'..'z'])) or ('&lt;' <> copy(aString,LBegin,4)) then
    begin
      Result := '';
      exit;
    end;

    While (LEnd <= length(aString)) and ('&gt;' <> copy(aString,LEnd,4)) do
      inc(LEnd);

    LOffset := LEnd + 4;
    Result := copy(aString, LBegin, LEnd - LBegin);
  end;

begin
  //Gauge1.MaxValue := pred(FBNF.Count);
  LNames := TStringList.Create;
  try
    CollectRuleNames(LNames);
    for i := 0 to pred(FBNF.Count) do
    begin
      //Gauge1.Progress := i;
      LString := StringReplace(FBNF[i],'&','&amp;',[rfReplaceAll]);
      LString := StringReplace(LString,'<','&lt;',[rfReplaceAll]);
      LString := StringReplace(LString,'>','&gt;',[rfReplaceAll]);
      if trim(LString) <> '' then
      begin
        if pos ('::=',LString) > 0 then
        begin
          // process anchor end points
          LAnchor := LeftOf(LString);
          LAnchor := copy(LAnchor,5,length(LAnchor) - 8);
          LAnchor := StringReplace(LAnchor,' ','',[rfReplaceAll]);
          LAnchor := format('<anchor id="%s%s"/>',[APrefix,LAnchor]);
          System.Insert(LAnchor,LString,pos('::=',LString) - 1);
          LAnchor := copy(LString,pos('::=',LString) + 4,MaxInt);
          if LAnchor <> '' then
          begin
            LString := LString + '<!-- big oops -->';
          end;
        end
        else
        begin
          // process anchor jump points
          LOffset := 1;
          LAnchor := NextTerm(LString);
          while LAnchor <> '' do
          begin
            LAnchor := copy(LAnchor,5,length(LAnchor) - 4);

            // strip non empty list qualifier
            if pos('ne ',LAnchor) = 1 then
              LAnchor := Copy(LAnchor,4,MaxInt);

            // strip commalist off end of reference
            LSub := pos(' commalist',LAnchor);
            if LSub > 0 then
              LAnchor := copy(LAnchor,1,LSub - 1);

            // strip semicolonlist off end of reference
            LSub := pos(' semicolonlist',LAnchor);
            if LSub > 0 then
              LAnchor := copy(LAnchor,1,LSub - 1);

            // strip list off end of reference
            LSub := pos(' list',LAnchor);
            if LSub > 0 then
              LAnchor := copy(LAnchor,1,LSub - 1);

            if LNames.IndexOf(LAnchor) > -1 then
            begin
              LAnchor := StringReplace(LAnchor,' ','',[rfReplaceAll]);
              LAnchor := format('<link linkend="%s%s">',[aPrefix,LAnchor]);
              System.Insert('</link>',LString,LOffset);
              System.Insert(LAnchor,LString,LBegin);
              inc(LOffset,length(LAnchor));// + 4);
            end;
            LAnchor := NextTerm(LString);
          end;
        end;
      end;
      aStrings.Add(LString);
    end;
  finally
    LNames.Free;
  end;
end;


procedure TBNFExtractor.GenHeader(aStrings: TStrings);
var
  i: integer;
  LString: string;
begin
  for i := 0 to pred(Fheader.Count) do
  begin
    LString := StringReplace(FHeader[i],'&','&amp;',[rfReplaceAll]);
    LString := StringReplace(LString,'<','&lt;',[rfReplaceAll]);
    LString := StringReplace(LString,'>','&gt;',[rfReplaceAll]);
    aSTrings.Add(LString);
  end;
end;

procedure TBNFExtractor.GenHtmlXRef(aStrings: TStrings);
var
  i: integer;
  LString: string;
  LAnchor: string;
  LOffset: integer;
  LBegin: integer;
  LSub: integer;
  LNames: TStrings;

  function NextTerm(aString: string): string;
  var
    LEnd: integer;
  begin
    // locate beginning
    LBegin := LOffset;
    while (LBegin < length(aString)) and ('&lt;' <> copy(aString,LBegin,4)) do
      inc(LBegin);

    if not (LBegin < length(aString))  then
    begin
      Result := '';
      LOffset := length(AString);
      exit;
    end;

    // locate the ending
    LEnd := LBegin + 4;
    if (not (aString[LEnd] in ['A'..'Z','a'..'z'])) or ('&lt;' <> copy(aString,LBegin,4)) then
    begin
      Result := '';
      exit;
    end;

    While (LEnd <= length(aString)) and ('&gt;' <> copy(aString,LEnd,4)) do
      inc(LEnd);

    LOffset := LEnd + 4;
    Result := copy(aString, LBegin, LEnd - LBegin);
  end;

begin
  LNames := TStringList.Create;
  try
    CollectRuleNames(LNames);
    //Gauge1.MaxValue := pred(FBNF.Count);
    for i := 0 to pred(FBNF.Count) do
    begin
      //Gauge1.Progress := i;
      LString := StringReplace(FBNF[i],'&','&amp;',[rfReplaceAll]);
      LString := StringReplace(LString,'<','&lt;',[rfReplaceAll]);
      LString := StringReplace(LString,'>','&gt;',[rfReplaceAll]);
      if trim(LString) <> '' then
      begin
        if pos ('::=',LString) > 0 then
        begin
          // process anchor end points
          LAnchor := LeftOf(LString);
          LAnchor := copy(LAnchor,5,length(LAnchor) - 8);
          LAnchor := StringReplace(LAnchor,' ','',[rfReplaceAll]);
          LAnchor := format('<a name="%s"/>',[LAnchor]);
          System.Insert(LAnchor,LString,pos('::=',LString) - 1);
          LAnchor := copy(LString,pos('::=',LString) + 4,MaxInt);
          if LAnchor <> '' then
          begin
            LString := LString + '<!-- big oops -->';
          end;
        end
        else
        begin
          // process anchor jump points
          LOffset := 1;
          LAnchor := NextTerm(LString);
          while LAnchor <> '' do
          begin
            LAnchor := copy(LAnchor,5,length(LAnchor) - 4);

            // strip non empty list qualifier
            if pos('ne ',LAnchor) = 1 then
              LAnchor := Copy(LAnchor,4,MaxInt);

            // strip commalist off end of reference
            LSub := pos(' commalist',LAnchor);
            if LSub > 0 then
              LAnchor := copy(LAnchor,1,LSub - 1);


            // strip semicolonlist off end of reference
            LSub := pos(' semicolonlist',LAnchor);
            if LSub > 0 then
              LAnchor := copy(LAnchor,1,LSub - 1);

            // strip list off end of reference
            LSub := pos(' list',LAnchor);
            if LSub > 0 then
              LAnchor := copy(LAnchor,1,LSub - 1);

            if LNames.IndexOf(LAnchor) > -1 then
            begin
              LAnchor := StringReplace(LAnchor,' ','',[rfReplaceAll]);
              LAnchor := format('<a href="#%s">',[LAnchor]);
              System.Insert('</a>',LString,LOffset);
              System.Insert(LAnchor,LString,LBegin);
              inc(LOffset,length(LAnchor));
            end;
            LAnchor := NextTerm(LString);
          end;
        end;
      end;
      aStrings.Add(LString);
    end;
  finally
    LNames.Free;
  end;
end;

function TBNFExtractor.GetBadPunctuations: TStrings;
var
  LStatementOffset: integer;
  LStatement: string;
begin
  if not assigned(FOutput) then
    FOutput := TStringList.Create;
  FOutput.Clear;
  LStatementOffset := 1;
  //Gauge1.MaxValue := length(FBnf.text);
  //Gauge1.Progress := 0;
  while (LStatementOffset < Length(FBNF.Text)) do
  begin
    //Gauge1.Progress := LStatementOffset;
    LStatement := NextStatement(FBNF.Text,LStatementOffset);
    ReportPunctSyntax(LStatement);
  end;
  if FOutput.Count = 0 then
    FOutput.Add('---- No mismatched punctuations ----')
  else
    FOutput.Insert(0, '---- Mismatched punctuations ----');
  Result := FOutput;
end;

function TBNFExtractor.GetBNF: TStrings;
begin
  if not assigned(FBNF) then
    FBNF := TStringlist.Create;
  Result := FBNF;
end;

function TBNFExtractor.GetDocbook: TStrings;
var
  LPrefix: string;
begin
  // todo: write to create a docbook productionset, will require parsing the right side?...
  if not assigned(FOutput) then
    FOutput := TStringList.Create;

  LPrefix := StringReplace(FTitle,' ','',[rfReplaceAll]);

  With FOutput do
  begin
    Clear;
    Add('<?xml version="1.0" encoding="utf-8" ?>');
    Add('<!--');
    Add('  This file was generated from code doc sources, using ExtractBNF.exe.');
    Add('  Do not edit the text of this file, go to the code comments to change any text.');
    Add('-->');
    Add(format('<!-- %s -->',[FTitle]));
    Add('<programlisting >');
    GenHeader(FOutput);
    Add('');
    Add('');
    GenDocbookXRef(FOutput, LPrefix);
    Add('</programlisting>');
  end;
  Result := FOutput;
end;

function TBNFExtractor.GetHeader: TStrings;
begin
  if not assigned(FHeader) then
    FHeader := TStringList.Create;
  Result := FHeader;
end;

function TBNFExtractor.GetHTML: TStrings;
var
  LTitle: string;
begin
  if not assigned(FOutput) then
    FOutput := TStringList.Create;

  with FOutput do
  begin
    Clear;
    Add('<html dir="LTR">');
    Add('  <head>');
    Add('    <META http-equiv="Content-Type" content="text/html; charset=utf-8">');
    Add('    <meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">');
    Add(format('    <title>%s</title>',[FTitle]));
    Add('    <link rel="stylesheet" type="text/css" href="MsdnHelp.css">');
    Add('  </head>');
    Add('  <body>');
    Add('    <div id="banner">');
    Add('      <div id="header">');
    Add(format('						Alphora Dataphor Help Collection - (doc build: %s)',[FormatDateTime('mmm dd, yyyy  hh:mm:ss a/p',now)]));
    Add('					</div>');
    Add(format('      <h1>%s</h1>',[LTitle]));
    Add('    </div>');
    Add('    <div id="content">');
    Add(format('      <p class="i1">This topic presents the %s</p>',[LTitle]));
    Add('      <br>');
    Add('      <h3>Syntax</h3>');
    Add('      <pre class="code">');

    GenHeader(FOutput);
    Add('<br/><br/>');
    // fully xrefed
    GenHtmlXRef(FOutput);

    // just formatted so can show up in HTML// debug
    //for i := 0 to pred(FBNF.Count) do
    //begin
    //  LString := StringReplace(FBNF[i],'&','&amp;',[rfReplaceAll]);
    //  LString := StringReplace(LString,'<','&lt;',[rfReplaceAll]);
    //  LString := StringReplace(LString,'>','&gt;',[rfReplaceAll]);
    //  FOuput.Add(LString);
    //end;

    Add('          </pre>');
    Add('      <p class="i1">');
    Add('      </p>');
    Add('      <div id="footer">');
    Add(format('        <a href="RequiredNotices.html">Copyright Â© %s Softwise, Inc. All rights reserved.</a>',[FormatDateTime('yyyy',now)]));
    Add('      </div>');
    Add('      <object type="application/x-oleobject" classid="clsid:1e2a7bd0-dab9-11d0-b93a-00c04fc99f9e" viewastext="viewastext">');
    Add(format('        <param name="Keyword" value="%s">',[LTitle]));
    Add('      </object>');
    Add('    </div>');
    Add('  </body>');
    Add('</html>');
  end;

  Result := FOutput;
end;


function TBNFExtractor.GetMissedProductions: TStrings;
var
  LLeftSide: TStringList;
  LRightSide: TStringList;
begin
  if not assigned(FOutput) then
    FOutput := TStringlist.Create;
  LLeftSide := TStringList.Create;
  LRightSide := TStringList.Create;
  try
    //Gauge1.MaxValue := length(FBNF.Text) * 2;
    CollectStatements(LLeftSide, LRightSide);
    //Gauge1.Progress := length(FBNF.Text) * 2;
  finally
    LLeftSide.Free;
    LRightSide.Free;
  end;
  Result := FOutput;
end;

function TBNFExtractor.GetSource: TStrings;
begin
  if not assigned(FSource) then
    FSource := TStringList.Create;
  Result := FSource;
end;

function TBNFExtractor.LeftOf(aStatement: string): string;
var
  i : integer;
begin
  i := pos('::=',aStatement);
  if i > 0 then
    Result := trim(copy(aStatement,1,pred(i)))
  else
    Result := '';
end;

function TBNFExtractor.NextStatement(aBNF: string;
  var aOffset: integer): string;
var
  i: integer;
  LCurrBegin: integer;
  LCurrEnd: integer;
  LNextBegin: integer;
  LNextEnd: integer;
  LLength: integer;
begin
  i := aOffset;
  LCurrBegin := 0;
  LCurrEnd := 0;
  LLength := length(aBNF);
  while i <= LLength do
  begin
    case aBNF[i] of
      ':' :
      begin
        inc(i);
        if aBNF[i] = ':' then
        begin
          inc(i);
          if aBNF[i] = '=' then
          begin
            if (LCurrBegin < LCurrEnd) and (LCurrBegin > 0) then
              break; // we've found the statement, know it's beginning
          end
        end;
      end;
      '<' : LCurrBegin := i;
      '>' : LCurrEnd := i;
    end;
    inc(i);
  end;

  // locate the beginning of the next statement
  LNextBegin := 0;
  LNextEnd := 0;
  while i <= LLength do
  begin
    case aBNF[i] of
      ':' :
      begin
        inc(i);
        if aBNF[i] = ':' then
        begin
          inc(i);
          if aBNF[i] = '=' then
          begin
            if (LNextBegin < LNextEnd) and (LNextBegin > 0) then
            begin
              Result := trim(copy(aBNF,LCurrBegin, LNextBegin - LCurrBegin));
              aOffset := LNextBegin;
              break;
            end;
          end
        end;
      end;
      '<' : LNextBegin := i;
      '>' : LNextEnd := i;
    end;
    inc(i);
  end;
  if i >= LLength then
  begin
    Result := trim(copy(aBNF,LCurrBegin, MaxInt));
    aOffset := i;
  end;
end;


procedure TBNFExtractor.ReportPunctSyntax(aStatement: string);
var
  i: integer;
  LAngle: integer;
  LBracket: integer;
  LBrace: integer;
  LParen: integer;
  LQuote: integer;
  LApostrophe: integer;
begin
  // go through a statement and report mismatched symbols
  LAngle := 0;
  LBracket := 0;
  LBrace := 0;
  LParen := 0;
  LQuote := 0;
  LApostrophe := 0;
  for i := 1 to length(aStatement) do
  begin
    case aStatement[i] of
      '<' : inc(LAngle);
      '>' : dec(LAngle);
      '(' : inc(Lparen);
      ')' : dec(LParen);
      '{' : inc(LBrace);
      '}' : dec(LBrace);
      '[' : inc(LBracket);
      ']' : dec(LBracket);
      '"' : inc(LQuote);
      '''': inc(LApostrophe);
    end;
  end;

  // issue report:
  if (LAngle > 0) or (LParen > 0) or (LBrace > 0) or (LBracket > 0) or
      (LQuote mod 2 > 0) or (LApostrophe mod 2 > 0) then
  begin
    FOutput.Add(format('%s has mismatched punctuation:',[LeftOf(aStatement)]));
    if LAngle > 0 then
      FOutput.Add('    mismatched < > angle bracket');
    if LParen > 0 then
      FOutput.Add('    mismatched ( ) paranthesis');
    if LBrace > 0 then
      FOutput.Add('    mismatched { } curly brace');
    if LBracket > 0 then
      FOutput.Add('    mismatched [ ] square bracket');
    if LQuote mod 2 > 0 then
      FOutput.Add('    mismatched " double quote');
    if LQuote mod 2 > 0 then
      FOutput.Add('    mismatched '' single quote');
  end;

end;

function TBNFExtractor.RightOf(aStatement: string): string;
var
  i : integer;
begin
  i := pos('::=',aStatement);
  if i > 0 then
    Result := trim(copy(aStatement,i + 4,MaxInt))
  else
    Result := '';
end;

procedure TBNFExtractor.SetBNF(const Value: TStrings);
begin
  BNF.Assign(Value);
end;

procedure TBNFExtractor.SetHeader(const Value: TStrings);
begin
  Header.Assign(Value);
end;

procedure TBNFExtractor.SetSource(const Value: TStrings);
begin
  if not assigned(FSource) then
    FSource := TStringList.Create;
  FSource.Assign(Value);
  ExtractBNFComments;
end;

procedure TBNFExtractor.SimplifyRightSide(AStrings: TStringList);
var
  LStrings: TStringList;
  LString: string;
  LReference: string;
  i : integer;
  LBegin, LEnd, LSub: integer;
begin
  LStrings := TStringList.Create;
  try
    for i := 0 to pred(AStrings.Count) do
    begin
      LString := AStrings[i];
      LBegin := 1;
      while (LBegin < length(LString)) do
      begin
        // find begin of <> element
        while (LBegin < length(LString)) and (LString[LBegin] <> '<') do
          inc(LBegin);
        // find end of <> element
        LEnd := succ(LBegin);
        if LString[LEnd] = '<' then
        begin
          LBegin := succ(LEnd);
          Continue;
        end;
        while (LEnd < length(LString)) and (LString[LEnd] <> '>') do
          inc(LEnd);
        LReference := trim(copy(LString,LBegin + 1,LEnd - LBegin - 1));

        // strip non empty list qualifier
        if pos('ne ',LReference) = 1 then
          LReference := Copy(LReference,4,MaxInt);

        // strip commalist off end of reference
        LSub := pos(' commalist',LReference);
        if LSub > 0 then
          LReference := copy(LReference,1,LSub - 1);

        // strip semicolonlist off end of reference
        LSub := pos(' semicolonlist',LReference);
        if LSub > 0 then
          LReference := copy(LReference,1,LSub - 1);

        // strip list off end of reference
        LSub := pos(' list',LReference);
        if LSub > 0 then
          LReference := copy(LReference,1,LSub - 1);

        if (LReference <> '') and (LReference[1] in ['A'..'Z','a'..'z']) then
        begin
          LReference := format('<%s>',[LReference]);
          if LStrings.IndexOf(LReference) = -1 then
            LStrings.Add(LReference);
        end;
        LBegin := LEnd;
      end;
    end;
    AStrings.Assign(LStrings);
  finally
    LStrings.Free;
  end;
end;
end.
