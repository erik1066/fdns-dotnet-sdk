# FDNS .NET Core SDK Documentation

You will need to have the following software installed to use this SDK:

- [.NET Core SDK 2.x](https://www.microsoft.com/net/download)
- [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio](https://visualstudio.microsoft.com/)
- [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) (only if using Visual Studio Code)

There are many tutorials that show how to create a basic .NET Core 2.x web application. We will avoid repeating those tutorials here. Once you have created a .NET Core 2.x application, you must include a package reference to the FDNS .NET Core SDK. To do this in in **Visual Studio Code**, open the application's `.csproj` file and add a package reference as shown below:

```xml
<ItemGroup>
    <PackageReference Include="Foundation.Sdk" Version="0.0.4" />
</ItemGroup>
```

You may be prompted to restore packages after saving the `.csproj` file. You may alternatively run `dotnet restore` from the command line. 

See [Package references in project files](https://docs.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files) for more information.

To add a package reference in **Visual Studio**, see [Quickstart: Install and use a package in Visual Studio](https://docs.microsoft.com/en-us/nuget/quickstart/install-and-use-a-package-in-visual-studio).



## SDK Quick-start Guides:

* [How to run the Foundation Services for local development](guide00-starting-fdns-microservices.md)
* [Interacting with the FDNS Object microservice](guide01-using-fdns-object-microservice.md)
* [Interacting with the FDNS Storage microservice](guide02-using-fdns-storage-microservice.md)
* [Interacting with the FDNS Indexing microservice](guide03-using-fdns-indexing-microservice.md)