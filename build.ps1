$ErrorActionPreference = "Stop"

dotnet tool restore
dotnet build

AddToPath .\ReviewPendingChanges\bin\Debug
