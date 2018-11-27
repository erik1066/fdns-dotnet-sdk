# How to use the SDK to interact with the FDNS Object microservice

The [FDNS Object microservice](https://github.com/CDCGov/fdns-ms-object) is an abstraction layer over a NoSQL database. It exposes several HTTP API endpoints that map to CRUD operations. 

The FDNS .NET Core SDK offers two ways to interact with the Object service: With strongly-typed objects (implemented using C# generics) and with strings. The former is intended for scenarios where object schemas are well-known at runtime and where the developer wishes to enforce such schemas. Consider a `Book` or a `Customer` object.

The latter is intended for scenarios where schemas are unknown at runtime and/or when more flexibility is needed. Consider an app where users design surveys and have others respond to those surveys over the web (e.g. SurveyMonkey). The user-defined survey determines the Json schema of the survey responses. Thus, it is at runtime - not compile-time - that the schema is made known. Strongly-typed objects cannot be used in this case. The .NET Core SDK therefore allows interactions with the Object Service with strings that represent Json objects.

You will need to have the following software installed to follow the instructions in this document:

- [.NET Core SDK 2.x](https://www.microsoft.com/net/download)
- [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio](https://visualstudio.microsoft.com/)
- [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) (only if using Visual Studio Code)

> This guide assumes you're already running the FDNS Object microservice at `http://localhost:8083/api/1.0`. If not, you will receive errors when trying to use the code below.


## Using the SDK with strongly-typed objects

In this example, we'll assume there is an object of type `Customer` that we wish to conduct CRUD operations on:

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

Open `Startup.cs` in your ASP.NET Core 2 microservice and add the following lines to `ConfigureServices`:

```cs
services.AddHttpClient($"myAppName-{Common.OBJECT_SERVICE_NAME}", client =>
{
    client.BaseAddress = new Uri($"http://localhost:8083/api/1.0/bookstore/customer/");
    client.DefaultRequestHeaders.Add("X-Correlation-App", "myAppName");
})

services.AddSingleton<IObjectRepository<Customer>>(provider => new HttpTypedObjectRepository<Customer>(
    provider.GetService<IHttpClientFactory>(),
    provider.GetService<ILogger<HttpTypedObjectRepository<Customer>>>(),
    "myAppName"));
```

Also in `Startup.cs`, modify the `services.AddMvc()` method call to add the `TextPlainInputFormatter`. Doing so is required for implementing the SDK's Find API.

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

Still in `Startup.cs`, add the following `using` directive:

```cs
using Foundation.Sdk.Data;
```

In your controller, add the SDK's `IObjectRepository` interface as a dependency that is injected via the constructor:

```cs
[Route("api/1.0")]
[ApiController]
public class CustomerController : ControllerBase
{
    private readonly IObjectRepository<Customer> _customerRepository;

    public CustomerController(IObjectRepository<Customer> repository)
    {
        _customerRepository = repository;
    }
    ...
}
```

Add the following helper method to the bottom of the controller:

```cs
private ActionResult<T> HandleObjectResult<T>(ServiceResult<T> result, string id = "")
{
    switch (result.Code)
    {
        case HttpStatusCode.OK:
            return Ok(result.Response);
        case HttpStatusCode.Created:
            return CreatedAtAction(nameof(Get), new { id = id }, result.Response);
        default:
            return StatusCode((int)result.Code, result.Message);
    }
}
```

Operations are fairly straightforward. To get an object by its id value:

```cs
[Produces("application/json")]
[HttpGet("{id}")]
public async Task<ActionResult<Customer>> Get([FromRoute] string id)
{
    ServiceResult<Customer> result = await _customerRepository.GetAsync(id);
    return HandleObjectResult<Customer>(result);
}
```

To insert an object with an explicit id:

```cs
[Produces("application/json")]
[HttpPost("{id}")]
public async Task<ActionResult<Customer>> Post([FromRoute] string id, [FromBody] Customer payload)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    ServiceResult<Customer> result = await _customerRepository.InsertAsync(id, payload);
    return HandleObjectResult<Customer>(result, id);
}
```

To conduct a wholesale object replacement:

```cs
[Produces("application/json")]
[HttpPut("{id}")]
public async Task<ActionResult<Customer>> Put([FromRoute] string id, [FromBody] Customer payload)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    ServiceResult<Customer> result = await _customerRepository.ReplaceAsync(id, payload);
    return HandleObjectResult<Customer>(result);
}
```

To delete an object:

```cs
[HttpDelete("{id}")]
public async Task<bool> Delete([FromRoute] string id)
{
    ServiceResult<bool> result = await _customerRepository.DeleteAsync(id);
    return HandleObjectResult<bool>(result);
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

You can also get a Json array of distinct values for any given field in your collection. The aforementioned MongoDB find syntax is an optional parameter to this method if you wish to limit the scope of the operation. For example:

```cs
var distinctResult = await _customerRepository.GetDistinctAsync("id", findCriteria);
```



## Using the SDK with raw strings as Json

There are plentiful scenarios where the developer may not know or want to know the schema of objects at compile-time. The FDNS .NET Core SDK will work in these scenarios. Simply use C# strings that contain the Json representation of the object. For example, in `Startup.cs`, the type of the `IObjectRepository` can be `string`:

```cs
services.AddSingleton<IObjectRepository<string>>(provider => new HttpObjectRepository<string>(
    provider.GetService<IHttpClientFactory>(),
    provider.GetService<ILogger<HttpObjectRepository<string>>>(),
    "myAppName"));
```

A method to insert a new Book into the database, implemented as an HTTP POST with a 201 return code, might look like the following:

```cs
[Produces("application/vnd.mycompany.myapp.book+json; version=1.0")]
[Consumes("application/vnd.mycompany.myapp.book+json; version=1.0")]
[HttpPost("{id}")]
public async Task<ActionResult<string>> Post([FromRoute] string id, [FromBody] string payload)
{
    ServiceResult<string> result = await _bookRepository.InsertAsync(id, payload);
    return HandleObjectResult<string>(result, id);
}
```

> Note the absence of a `if (!ModelState.IsValid)` call. Because the input type is a raw string, ASP.NET Core's model validator middleware can't be used.

ASP.NET Core generally expects APIs to use strongly-typed objects. Since we're not using strongly-typed objects, we need to create two middleware classes and add them to `Startup.cs`. Otherwise, ASP.NET Core 2.1 is going to incorrectly handle the string input and output values for the POST API. We need to first add a Raw Json input input formatter class:

```cs
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public class JsonRawInputFormatter : TextInputFormatter
    {
        public JsonRawInputFormatter()
        {
            SupportedMediaTypes.Add("application/vnd.mycompany.myapp.book+json; version=1.0");
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);
        }
        protected override bool CanReadType(Type type)
        {
            return type == typeof(string);
        }
        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            string data = null;
            using (var streamReader = context.ReaderFactory(context.HttpContext.Request.Body, encoding))
            {
                data = await streamReader.ReadToEndAsync();
            }
            return InputFormatterResult.Success(data);
        }
    }
#pragma warning restore 1591
}
```

And then we need to another class for formatting the output:

```cs
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public class JsonRawOutputFormatter : TextOutputFormatter
    {
        public JsonRawOutputFormatter()
        {
            SupportedMediaTypes.Add("application/vnd.mycompany.myapp.book+json; version=1.0");
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var response = context.HttpContext.Response;

            var buffer = new StringBuilder();
            buffer.Append(context.Object.ToString());
            return response.WriteAsync(buffer.ToString());
        }

        protected override bool CanWriteType(Type type)
        {
            return type == typeof(string);
        }
    }
#pragma warning restore 1591
}
```

> Observe the vendor-specific MIME type `"application/vnd.mycompany.myapp.book+json; version=1.0"` that matches across the custom input formatter, custom output formatter, and the `Produces` and `Consumes` annotations on the API. 

The last step is to modify the `Startup.cs` file to add the formatters:

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

