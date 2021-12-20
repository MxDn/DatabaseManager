dotnet new tool-manifest
dotnet tool install fake-cli --tool-path .fake
dotnet new -i "fake-template::*"
dotnet new fake --force