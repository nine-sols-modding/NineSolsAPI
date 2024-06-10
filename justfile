build:
    dotnet build --configuration Release NineSolsAPI

publish: build
    dotnet nuget push --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json NineSolsAPI/bin/Release/*.nupkg
