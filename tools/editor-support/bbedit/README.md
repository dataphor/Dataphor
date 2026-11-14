# NAME

BBEdit\_LM\_D4.plist - BBEdit Codeless Language Module for Dataphor D4

# DESCRIPTION

The XML/plain-text file ```BBEdit_LM_D4.plist``` is a Codeless Language
Module for the Mac OS text editor BBEdit
[https://www.barebones.com/products/bbedit/](
https://www.barebones.com/products/bbedit/).

To use it, simply copy that file into this directory (which you might have
to create):

    ~/Library/Application Support/BBEdit/Language Modules

... and BBEdit will load it when it is next launched.

The CLM empowers BBEdit to syntax color D4 source code, and to scan it for
major entity (eg, routine and type) declarations so you can easily jump
around to those in your D4 source code files.

The CLM will automatically be applied to any text files with the filename
extension ".d4" but you can apply it to any file.

This CLM should be considered beta quality and may not handle some corner
case syntax.

See also [https://www.barebones.com/support/develop/clm.html](
https://www.barebones.com/support/develop/clm.html) for a CLM reference,
should you want to improve on this CLM or make other ones.

See also [https://jclingo.gitbooks.io/dataphor/content/DevelopersGuide/D4LexicalElements.html](
https://jclingo.gitbooks.io/dataphor/content/DevelopersGuide/D4LexicalElements.html)
which documents the D4 syntax.

See also [https://github.com/DBCG/Dataphor/blob/master/Dataphor/Dataphoria/TextEditor/Modes/D4-Mode.xshd](
https://github.com/DBCG/Dataphor/blob/master/Dataphor/Dataphoria/TextEditor/Modes/D4-Mode.xshd)
which is the ```ICSharpCode.TextEditor``` syntax definition XML file that
is used internally by Dataphoria.

# AUTHORS

Darren Duncan - darren@databaseconsultinggroup.com

# LICENSE AND COPYRIGHT

Copyright © 2017-2017, Alphora, All Rights Reserved.

[http://www.alphora.com](http://www.alphora.com)
