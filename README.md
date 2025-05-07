A crude tool to create the file ScriptSyntax.xml for opensimulator.

Does require dotnet 8.0

It will read the information from the Opensimulator sources.

so you need to provide the path to those sources, and amy optionaly provide a folder for the output

  GenScriptSyntax PathToSource [outputPath]

or in linux

  dotnet GenScriptSyntax.dll PathToSource [outputPath]

example:

  GenScriptSyntax D:\opensimSource

if you do not provide a output path, ScriptSyntax.xml will be written on the folder of the exe/dll files
