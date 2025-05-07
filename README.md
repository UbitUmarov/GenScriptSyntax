# GenScriptSyntax

A crude tool to create the file ScriptSyntax.xml for opensimulator.

Does require dotnet 8.0

It will read the information from the Opensimulator sources.

so you need to provide the path to those sources, and and optionaly provide a folder for the output

#Building

to build do 
dotnet build --configuration Debug GenSriptSyntax.sln

#Running

change to bin folder and do

  GenScriptSyntax PathToSource [outputPath]
  
or on linux

   dotnet GenScriptSyntax.dll PathToSource [outputPath]
   
if you do not provide a output path, ScriptSyntax.xml will be written on same bin folder
