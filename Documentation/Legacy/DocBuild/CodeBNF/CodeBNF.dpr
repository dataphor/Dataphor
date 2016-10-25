{
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
}
program CodeBNF;
{$APPTYPE CONSOLE}
uses
  SysUtils,
  Classes,
  BNFTools in 'BNFTools.pas';

var
  i : integer;
  FSourceName : string;
  FBNFName : string;
  FHtmlName : string;
  FDocbookName : string;
  FVerifyFilename : string;
  FTitle : string;
  FVerify : Boolean;
  FSourceLines : TStrings;
  FBNFTool : TBNFExtractor;
  FPause : Boolean;

  procedure DisplayUsage;
  begin
    writeln('CodeBNF usage:');
    writeln(' -s<filename> source input file');
    writeln(' -h<filename> html output file');
    writeln(' -b<filename> text output file');
    writeln(' -d<filename> docbook xml output file');
    writeln(' -t"<title>" title for topic for html and docbook output');
    writeln(' -l<filename> logfile for checks/errors');
    writeln(' -c    perform checks, send to the console');
    writeln(' -p    pause after execute/show help');
    writeln('');
    writeln('Returns:');
    writeln(' 0 = Ok');
    writeln(' 1 = no source file');
    writeln(' 2 = some production errors');
    writeln(' 3 = some punctuation errors');
    writeln('');
    writeln('press <Enter> to continue.');
    if FPause then
      readln;
  end;



begin
  if ParamCount > 0 then
  begin
    FVerify := False;
    FPause := False;
    for i := 1 to ParamCount do
    begin
    { todo: make commandline with the following options
        -s<filename> source input file
        -h<filename> html output file
        -b<filename> text output file
        -d<filename> docbook xml output file
        -t"<title>" title for topic (html docbook)
        -c    perform checks, send to the console
        -l<filename> logfile for checks/errors
        -p pause
    }
      case ParamStr(i)[2] of
        'S','s': FSourceName := copy(ParamStr(i),3,MaxInt);
        'B','b': FBNFName := copy(ParamStr(i),3,MaxInt);
        'H','h': FHtmlName := copy(ParamStr(i),3,MaxInt);
        'D','d': FDocbookName := copy(ParamStr(i),3,MaxInt);
        'T','t': FTitle := copy(ParamStr(i),3,MaxInt);
        'C','c': FVerify := True;
        'L','l': FVerifyFilename := copy(ParamStr(i),3,MaxInt);
        'P','p': FPause := True;
      end;
    end;

    if FSourceName<> '' then
    begin
      writeln('working...');
      FBNFTool := TBNFExtractor.Create;
      FSourceLines := TStringList.Create;
      try
        FSourceLines.LoadFromFile(FSourceName);
        FBNFTool.SourceLines := FSourceLines;

        // title is required!!
        if FTitle <> '' then
          FBNFTool.Title := FTitle
        else
          FBNFTool.Title := ExtractFileName(FSourceName);

        if FBNFName <> '' then
        begin
          writeln('Saving bnf text file...');
          FBNFTool.BNF.SaveToFile(FBNFName);
        end;

        if FHtmlName <> '' then
        begin
          writeln('Saving HTML file...');
          FBNFTool.HTML.SaveToFile(FHtmlName);
        end;

        if FDocbookName <> '' then
        begin
          writeln('Saving docbook XML file...');
          FBNFTool.Docbook.SaveToFile(FDocbookName);
        end;

        if FVerify then
        begin
          writeln('Verifying production completeness and paired punctuations...');
          FSourceLines.Clear;
          FSourceLines.AddStrings(FBNFTool.MissedProductions);
          FSourceLines.Add('');
          FSourceLines.AddStrings(FBNFTool.BadPunctuations);
          if FVerifyFilename <> '' then
            FSourceLines.SaveToFile(FVerifyFilename);

          for i := 0 to pred(FSourceLines.Count) do
            writeln(FSourceLines[i]);

          if FBNFTool.MissedProductions.Count > 1 then
            ExitCode := 2;
          if FBNFTool.BadPunctuations.Count > 1 then
            ExitCode := 3;
        end;
      finally
        FBNFTool.Free;
        FSourceLines.Free;
      end;
      if FPause then
      begin
        writeln;
        writeln('Press <Enter> to continue');
        readln;
      end;
    end
    else
      DisplayUsage;
  end
  else
  begin
    DisplayUsage;
    ExitCode := 1;
  end;
end.