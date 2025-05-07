Very crude tool to create the file ScriptSyntax.xml for opensimulator.
It will read the information from the Opensimulator sources. Does require dotnet 8.0
You need to provide the path to those sources
  GenScriptSyntax  PathToSource
or in linux
  dotnet GenScriptSyntax.dll PathToSource

example:
  GenScriptSyntax D:\opensimSource

