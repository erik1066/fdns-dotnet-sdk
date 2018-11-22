# FDNS .NET Core SDK Documentation

You will need to have the following software installed to use this SDK:

- [.NET Core SDK 2.1](https://www.microsoft.com/net/download)
- [Visual Studio Code](https://code.visualstudio.com/)
- [C# Extension for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)

You can include the FDNS .NET Core SDK into a .NET Core 2.x project by adding a package reference in your `.csproj` file:

```xml
<ItemGroup>
    <PackageReference Include="Foundation.Sdk" Version="0.0.3" />
</ItemGroup>
```

Once you've added that line to the `csproj` file, Visual Studio Code will prompt you to restore packages. Select **Restore** when prompted. You may alternatively run `dotnet restore` from the command line.

## Quick-start guides:

* [How to run the Foundation Services for local development](guide00-starting-fdns-microservices.md)
* [Interacting with the FDNS Object microservice](guide01-using-fdns-object-microservice.md)
* [Interacting with the FDNS Storage microservice](guide02-using-fdns-storage-microservice.md)
* [Interacting with the FDNS Indexing microservice](guide03-using-fdns-indexing-microservice.md)