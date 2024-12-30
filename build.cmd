@ECHO OFF

dotnet tool restore
dotnet build -- %*

AddToPath .\src\ReviewPendingChanges\bin\Debug
