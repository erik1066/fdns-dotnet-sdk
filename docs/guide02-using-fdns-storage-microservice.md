# How to use the SDK to interact with the FDNS Storage microservice

The [FDNS Storage microservice](https://github.com/CDCGov/fdns-ms-storage) is an abstraction layer over Amazon S3. It exposes several HTTP API endpoints that map to S3 operations.

You will need to have the following software installed to follow the instructions in this document:

- [Visual Studio Code](https://code.visualstudio.com/)
- [C# Extension for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
- [.NET Core SDK 2.1](https://www.microsoft.com/net/download)

> This guide assumes you're already running the FDNS Storage microservice at `http://localhost:8082/api/1.0`. If not, you will receive errors when trying to use the code below.

Let's assume we have a requirement to import a CSV file of `Customer` objects into our database. As part of the import, we're asked to save the raw CSV file to a location where it can later on be retreived if needed.

In this example, we will assume that:
1. The CSV file will be placed into the FDNS Storage microservice
1. The `Customer` objects parsed from each row in the CSV file will be saved to the FDNS Object microservice as Json documents.

First, add the `Customer` object to your code:

```cs
public sealed class Customer
{
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
    public string StreetAddress { get; set; }
    public DateTime DateOfBirth { get; set; }
}
```

Open `Startup.cs` and add the following lines:

```cs
services.AddHttpClient($"{applicationName}-{Common.STORAGE_SERVICE_NAME}", client =>
{
    client.BaseAddress = new Uri($"{storageServiceUrl}/");
    client.DefaultRequestHeaders.Add("X-Correlation-App", applicationName);
});

services.AddSingleton<IStorageRepository>(provider => new HttpStorageRepository(
    provider.GetService<IHttpClientFactory>(),
    provider.GetService<ILogger<HttpStorageRepository>>(),
    applicationName,
    "bookstore-customer"));
```

> The "bookstore-customer" string is the name of the S3 Drawer that this HttpStorageRepository will be using for all of its operations. If you want to use more than one Drawer, you should create additional services.

Also in `Startup.cs`, modify the `services.AddMvc()` method call to add the `TextPlainInputFormatter`. Doing so is required for importing the CSV file as a `text/plain` document. You may wish to use your own input formatter to accept a proper `text/csv` document, though that's beyond the scope of this guide.

```cs
services.AddMvc(options =>
{
    options.InputFormatters.Add(new TextPlainInputFormatter());
})
.AddJsonOptions(options =>
{
    options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
})
.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
```

Modify your controller to accept an `IStorageRepository`:

```cs
[Route("api/1.0")]
[ApiController]
public class CustomerController : ControllerBase
{
    ...
    private readonly IStorageRepository _storageRepository;

    public CustomerController(..., IStorageRepository storageRepository)
    {
        ...
        _storageRepository = storageRepository;
    }
    ...
```

Let's write an API to accept the CSV import. The first step is create the method stub:

```cs
[Produces("application/json")]
[Consumes("text/plain")]
[HttpPost("import")]
public async Task<ActionResult<ImportResult>> BulkImport([FromBody] string payload)
{
    return Ok();
}
```

We need to assign a unique ID to each import operation and associate that ID with the CSV file. Let's use .NET's Guid generator:

```cs
var importId = System.Guid.NewGuid().ToString();
```

The API accepts `text/plain` and we receive a `string` as the payload, but we need to convert this to a byte array for pushing to the FDNS Storage service. We can convert from `string` to `byte[]` like such:

```cs
byte[] data = System.Text.Encoding.ASCII.GetBytes(payload);
```

We next need to try and create the "Drawer" into which these CSV files will be created. We'll run the API call for creating the drawer, and if it fails with a "Drawer already exists" response, we'll simply ignore it. This way the app will work in your debug environment. (In a real-world production app you would likely handle this differently.)

```cs
var createDrawerResult = await _storageRepository.CreateDrawerAsync();
```

Now we can stuff the CSV file into the Drawer. Every item in an S3 Drawer is called a Node, so we now have to create the node, which we can do with the following syntax:

```cs
var storageResult = await _storageRepository.CreateNodeAsync(importId, $"csv-import-{importId}", data);
```

Observe that `importId` is our Guid, `data` is the byte array we created earlier, and `$"csv-import-{importId}"` is the file name required for sending an HTTP multipart request.

We then need to return something from our API call, so let's handle that:

```cs
if (storageResult == null)
{
    return StatusCode(400, "Unknown error");
}
if (storageResult.IsSuccess == false)
{
    return StatusCode((int)storageResult.Code, storageResult.Message);
}
```

The complete method looks like such:

```cs
[Produces("application/json")]
[Consumes("text/plain")]
[HttpPost("import")]
public async Task<ActionResult<string>> BulkImport([FromBody] string payload)
{
    var importId = System.Guid.NewGuid().ToString();
    byte[] data = System.Text.Encoding.ASCII.GetBytes(payload);

    var createDrawerResult = await _storageRepository.CreateDrawerAsync();
    var storageResult = await _storageRepository.CreateNodeAsync(importId, $"csv-import-{importId}", data);

    if (storageResult == null)
    {
        return StatusCode(400, "Unknown error");
    }
    if (storageResult.IsSuccess == false)
    {
        return StatusCode((int)storageResult.Code, storageResult.Message);
    }

    // Now, parse the CSV file and store each record to the Object service...
    return Ok("Success!");
}
```

> For brevity's sake we've stubbed out parsing the CSV file and storing each item into the FDNS Object microservice.

For debugging purposes, it can be helpful to delete all the nodes in a Drawer and even the Drawer itself. _S3 does not allow you to delete a Drawer that has nodes in it_, however, so the operation isn't a one-liner. We first have to get all the nodes in the drawer, delete each one individually, and only after that can we delete the drawer:

```cs
[Produces("application/json")]
[HttpPost("reset-storage")]
public async Task<ActionResult> DeleteAllNodes()
{
    var getDrawerResult = await _storageRepository.GetDrawerAsync();

    if (getDrawerResult.IsSuccess)
    {
        var listAllNodesResult = await _storageRepository.GetAllNodesAsync();

        foreach (var node in listAllNodesResult.Response)
        {
            var id = node.Id.ToString();
            var deleteNodeResult = await _storageRepository.DeleteNodeAsync(id);
        }

        var deleteDrawerResult = await _storageRepository.DeleteDrawerAsync();
    }

    return Ok();
}
```



