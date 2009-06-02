Notes on the content of the bat folder:
bat files with a numeric prefix (XX-) are order dependent. The number in the batch file name represents the order in which to execute the batch file.

Before starting the batch file steps:
	build Dataphor in release mode to generate the distributed code docs (Dataphor, Libraries)
	run NDoc to generate the codedoc files required to build docs (DAE, DAEClient, DilRef)
	run Dataphoria (current build) and run Documentation/DocLibraries (documents the catalog)

The batch file steps are:
	01-CopyHumanDocs20.bat (requires: get latest from NG3)
	02-CopyPictures20.bat (requires: get latest from NG3)
	02a-GenParserBNF.bat (requires: get latest Dataphor from NG3)
	03-GenCodeDocs.bat (requires: getlatest, compile dataphor in release mode, run ndoc)
	04-CopyCodeDocs.bat (requres: step 3)
	05-CopyDocLibraryDocs.bat (requires: Dataphoria run of DocLibrary with DocLibraries Script)
	06-DataphorSetPDF.bat (requres: steps 1 through 5)
	07-DataphorSetHTMLHelp.bat (requires: steps 1 through 5)
	08-ToWebsite.bat (requires: step 7)
	09-CopyPDF.bat (requires: step 6)
	10-CopyChm.bat (requires: step 7)

The following steps are for print oriented output:
	11-DataphorSetOLinkDB.bat (requires: 1 through 6)
	12-DevGuidePDF.bat (requires: steps 1 through 6)
	13-UserGuidePDF.bat (requires: steps 1 through 6)
	14-DataphorReferencePDF.bat (requires: steps 1 through 6)[not printed, so may be omitted]


Other bat files of interest
	AddIDS used to automate assigning IDs to new human generated docs