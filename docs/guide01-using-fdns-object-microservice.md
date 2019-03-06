# How to use the SDK to interact with the FDNS Object microservice

The [FDNS Object microservice](https://github.com/CDCGov/fdns-ms-object) is a backing service with a simple RESTful API for NoSQL database operations. Supported operations include CRUD, search, data pipelines, aggregation, and bulk imports. MongoDB is the underlying database technology.

You will need to have the following software installed to follow the instructions in this document:

- [.NET Core SDK 2.1 (or newer)](https://www.microsoft.com/net/download)
- [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio](https://visualstudio.microsoft.com/)
- [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) (only if using Visual Studio Code)

> This guide assumes you're already running the FDNS Object microservice at `http://localhost:8083/api/1.0`. If not, you will receive errors when trying to use the code below.


## Using the SDK

This example assumes you are:
1. Building your own ASP.NET Core web API
1. Want to use the FDNS .NET Core SDK to interact with the FDNS Object microservice for database operations

First, add a package reference to the FDNS .NET Core SDK in your `.csproj` file:

```xml
<ItemGroup>
    <PackageReference Include="Foundation.Sdk" Version="0.0.13" />
</ItemGroup>
```

We'll assume there is an object of type `Customer` that we wish to conduct CRUD operations on:

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

Open `Startup.cs` in your code repository and add the following `using` directive at the top of the file:

```cs
using Foundation.Sdk.Services;
```

Add the following lines to `ConfigureServices`:

```cs
services.AddHttpClient($"myAppName-{Common.OBJECT_SERVICE_NAME}", client =>
{
    client.BaseAddress = new Uri($"http://localhost:8083/api/1.0/");
    client.DefaultRequestHeaders.Add("X-Correlation-App", "myAppName");
});

services.AddSingleton<IObjectService>(provider => new HttpObjectService(
    "myAppName",
    provider.GetService<IHttpClientFactory>(),
    provider.GetService<ILogger<HttpObjectService>>()));
```

Also in `Startup.cs`, modify the `services.AddMvc()` method call to add the `TextPlainInputFormatter` and the Json formatters. The `TextPlainInputFormatter` is required for implementing the SDK's Find API, which consumes `text/plain`. Adding the two Json formatters is needed for working with Json data as strings rather than as typed objects.

```cs
services.AddMvc(options =>
{
    options.InputFormatters.Insert(0, new TextPlainInputFormatter());
    options.InputFormatters.Insert(0, new JsonRawInputFormatter());
    options.OutputFormatters.Insert(0, new JsonRawOutputFormatter());
})
.AddJsonOptions(options =>
{
    options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
})
.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
```

In your controller, add the SDK's `IObjectService` interface as a dependency that is injected via the constructor:

```cs
[Route("api/1.0")]
[ApiController]
public class CustomerController : ControllerBase
{
    private readonly IObjectService _customerService;

    public CustomerController(IObjectService customerService)
    {
        _customerService = customerService;
    }
    ...
}
```

Add the following helper method to the bottom of the controller:

```cs
private ActionResult<T> HandleObjectResult<T>(ServiceResult<T> result, string id = "")
{
    switch (result.Status)
    {
        case 200:
            return Ok(result.Value);
        case 201:
            return CreatedAtAction(nameof(Get), new { id = id }, result.Value);
        default:
            return StatusCode(result.Status, result.Details);
    }
}
```

Operations are straightforward. To get an object by its id value:

```cs
[Produces("application/json")]
[HttpGet("{id}")]
public async Task<ActionResult<Customer>> Get([FromRoute] string id)
{
    ServiceResult<string> result = await _customerService.GetAsync("bookstore", "customer", id);
    return HandleObjectResult<Customer>(result);
}
```

> The FDNS .NET Core API always takes `databaseName` and `collectionName` arguments, which are supplied above as "bookstore" and "customer", respectively.

To insert an object with an explicit id:

```cs
[Produces("application/json")]
[Consumes("application/json")]
[HttpPost("{id}")]
public async Task<ActionResult<string>> Post([FromRoute] string id, [FromBody] Customer customer)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings() 
    { 
        NullValueHandling = NullValueHandling.Ignore, 
        ContractResolver = new CamelCasePropertyNamesContractResolver() 
    };

    string payload = JsonConvert.SerializeObject(customer, jsonSerializerSettings);
    ServiceResult<string> result = await _bookRepository.InsertAsync("bookstore", "books", id, payload);
    return HandleObjectResult<string>(result, id);
}
```

To conduct a wholesale object replacement:

```cs
[Produces("application/json")]
[Consumes("application/json")]
[HttpPut("{id}")]
public async Task<ActionResult<Customer>> Put([FromRoute] string id, [FromBody] Customer customer)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings() 
    { 
        NullValueHandling = NullValueHandling.Ignore, 
        ContractResolver = new CamelCasePropertyNamesContractResolver() 
    };

    string payload = JsonConvert.SerializeObject(customer, jsonSerializerSettings);

    ServiceResult<string> result = await _customerRepository.ReplaceAsync("bookstore", "customer", id, payload);
    return HandleObjectResult<Customer>(result);
}
```

Finding an object requires using the [MongoDB find syntax](https://docs.mongodb.com/manual/reference/method/db.collection.find/). An example of this syntax to find all customers with an age greater than 45: `{ age: { $lt: { 45 } }`. The `find` API also accepts from, size, sort, and sort direction criteria for easy pagination.

> ASP.NET Core requires that the find criteria be submitted as `text/plain`. You will need to implement a `TextPlainInputFormatter` to accept `text/plain`, otherwise ASP.NET Core will always view the payload as `null`. See the FDNS .NET Core Example microservice for how to implement `TextPlainInputFormatter`.

```cs
[Consumes("text/plain")]
[Produces("application/json")]
[HttpPost("find")]
public async Task<ActionResult<SearchResults<Customer>>> Find([FromBody] string findCriteria)
{
    ServiceResult<SearchResults<Customer>> result = await _customerRepository.FindAsync(0, 10, "name", findCriteria, false);
    return HandleObjectResult<SearchResults<Customer>>(result);
}
```

