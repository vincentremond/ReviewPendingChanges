@ECHO OFF

dotnet tool restore
dotnet build -- %*

add-to-path src\ReviewPendingChanges\bin\Debug
