# FDNS .NET Core SDK Documentation

You will need to have the following software installed to use this SDK:

- [.NET Core SDK 2.x](https://www.microsoft.com/net/download)
- [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio](https://visualstudio.microsoft.com/)
- [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) (only if using Visual Studio Code)

There are many tutorials that show how to create a basic .NET Core 2.x web application. We will avoid repeating those tutorials here for the sake of brevity. Once you have created a .NET Core 2.x application, you must include the [FDNS .NET Core SDK NuGet package](https://www.nuget.org/packages/Foundation.Sdk). To do this in in **Visual Studio Code**, open the application's `.csproj` file and add a package reference as shown below:

```xml
<ItemGroup>
    <PackageReference Include="Foundation.Sdk" Version="0.0.12" />
</ItemGroup>
```

You may be prompted to restore packages after saving the `.csproj` file. You may alternatively run `dotnet restore` from the command line. 

See [Package references in project files](https://docs.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files) for more information.

To add a NuGet package in **Visual Studio**, see [Quickstart: Install and use a package in Visual Studio](https://docs.microsoft.com/en-us/nuget/quickstart/install-and-use-a-package-in-visual-studio).

## SDK quick-start guides:

* [How to run the Foundation Services for local development](guide00-starting-fdns-microservices.md)
* [Interacting with the FDNS Object microservice](guide01-using-fdns-object-microservice.md)
* [Interacting with the FDNS Storage microservice](guide02-using-fdns-storage-microservice.md)
* [Interacting with the FDNS Indexing microservice](guide03-using-fdns-indexing-microservice.md)

## Analyzing code for quality and vulnerabilities

Developers can easily run [SonarQube](https://www.sonarqube.org/) on `fdns-dotnet-sdk` for code quality and security vulnerability analysis:

1. Open a terminal window
1. `cd` to the `fdns-dotnet-sdk` folder
1. `make sonar-up`
1. `make sonar-run`
1. Wait until the analysis is finished
1. Open a web browser and point to http://localhost:9000/dashboard?id=fdns-dotnet-sdk
1. `make sonar-down`


