build:
    rm NineSolsAPI/bin/Release/*.nupkg
    dotnet publish

publish: build
    dotnet nuget push --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json NineSolsAPI/bin/Release/*.nupkg
