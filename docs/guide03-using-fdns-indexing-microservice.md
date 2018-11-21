# How to use the SDK to interact with the FDNS Indexing microservice

The [FDNS Indexing microservice](https://github.com/CDCGov/fdns-ms-indexing) is an abstraction layer over Elasticsearch. It exposes several HTTP API endpoints that map to Elasticsearch APIs.

You will need to have the following software installed to follow the instructions in this document:

- [Visual Studio Code](https://code.visualstudio.com/)
- [C# Extension for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
- [.NET Core SDK 2.1](https://www.microsoft.com/net/download)

> This guide assumes you've followed the [FDNS Object guick-start guide](guide01-using-fdns-object-microservice.md) and are running the FDNS Indexing microservice at `http://localhost:8084/api/1.0` and the FDNS Object microservice at `http://localhost:8083/api/1.0`. If you haven't done these things, please do so now before continuing!

Let's assume we have a requirement to index `Customer` objects that are stored in our MongoDB database. We may be asked to implement this kind of indexing to improve lookup performance when running our software at scale, as Elasticsearch has performance benefits over MongoDB in certain scenarios. Note that MongoDB is what's running underneath the [FDNS Object microservice](https://github.com/CDCGov/fdns-ms-object), which is what we used to implement our database CRUD operations in [the first getting started guide](guide01-using-fdns-object-microservice.md).

Open `Startup.cs` and add the following lines:

```cs
services.AddHttpClient($"{applicationName}-{Common.INDEXING_SERVICE_NAME}", client =>
{
    client.BaseAddress = new Uri($"{indexingServiceUrl}/");
    client.DefaultRequestHeaders.Add("X-Correlation-App", applicationName);
});

services.AddSingleton<IIndexingService>(provider => new HttpIndexingService(
  provider.GetService<IHttpClientFactory>(),
  provider.GetService<ILogger<HttpIndexingService>>(),
  applicationName));
```

Modify your controller to accept an `IIndexingService`:

```cs
[Route("api/1.0")]
[ApiController]
public class CustomerController : ControllerBase
{
    ...
    private readonly IIndexingService _indexingService;

    public CustomerController(..., IIndexingService indexingService)
    {
      ...
        _indexingService = indexingService;
    }
    ...
```

Before we can index anything, though, we need to do two things to the FDNS Indexing service:

1. Create an indexing configuration
1. Register the configuration

Open the FDNS Indexing swagger page at http://localhost:8084/swagger-ui.html (or wherever it's hosted) and open the `/api/1.0/config/{config}` POST route, titled "Create or update rules for the specified configuration". Type "bookstore" for the `config` input, use the following Json for the payload, and then press **Execute**.

```json
{
  "mongo": {
    "database": "bookstore",
    "collection": "customer"
  },
  "elastic": {
    "index": "bookstore",
    "type": "customer"
  },
  "mapping": {
    "$unset": [
      "_id"
    ],
    "$set": {
      "name": {
        "fields": [
          "$.firstName",
          "$.lastName"
        ],
        "separator": ""
      }
    }
  },
  "filters": {
    "everything": {
      "regex": null,
      "queryType": "multi_match",
      "clause": "filter",
      "fields": [
        "name"
      ]
    }
  }
}
```

We then need to register this config with the Elasticsearch service before we can use it. Open the `/api/1.0/index/{config}` PUT route, titled "Creates a new index." Type "bookstore" for the config name and press **Execute**.

Notice in the Json config that we're combining the `firstName` and `lastName` fields into a single `name` field. The new `name` field will not exist in MongoDB and is only available via the object that is indexed in Elasticsearch. (This is just a sample of some of the cool things that the FDNS Indexing service can do.)

In the [FDNS Object microservice quick-start guide](guide01-using-fdns-object-microservice.md), you added a `Post` method to insert `Customer` objects into your database. We'll modify this method to show how you can leverage the Indexing service. In this case, we're simply making a second HTTP call to index the object that was just inserted.

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
    ServiceResult<string> indexResult = await _indexingService.IndexAsync("bookstore", id);
    return HandleObjectResult<Customer>(result, id);
}
```

Use the Insert route you just updated to insert the following `Customer` with an `id` value of `6`:

```json
{
  "id": "6",
  "firstName": "Rebecca",
  "lastName": "Jensen",
  "age": 56,
  "streetAddress": "string",
  "dateOfBirth": "2018-09-30T16:11:43.290Z"
}
```

Let's add a new `Get` API that maps to `/api/1.0/index`:

```cs
[Produces("application/json")]
[HttpGet("index/{id}")]
public async Task<ActionResult<string>> GetIndexed([FromRoute] string id)
{
    ServiceResult<string> result = await _indexingService.GetAsync("bookstore", id);
    return Content(result.Response);
}
```

Observe that this API will retrieve the `Customer` from the indexing service and not the object service. Let's try it out: Visit your Swagger page, open the `api/1.0/index/{id}` route, enter an `id` value of `6`, and press **Execute**. You should see something like the following:

```json
{"_index":"bookstore","_type":"customer","_id":"6","_version":1,"found":true,"_source":{"firstName":"Rebecca","lastName":"Jensen","streetAddress":"string","name":"RebeccaJensen","dateOfBirth":"2018-09-30T16:11:43.29Z","id":"6","age":56}}
```

Inside the `_source` property is the same `Customer` Json that you inserted, but now with an additional field: `"name": "RebeccaJensen"`. There's also a wrapper around the Json payload that includes the name of the index, the type of object that was indexed, and the version of the object.

## Google-like search

You can also execute a Google-like search against the FDNS Indexing service. Google-like searches are far more user-friendly than the [MongoDB find syntax](https://docs.mongodb.com/manual/reference/method/db.collection.find/) that is required for the FDNS Object microservice. You may find that FDNS Indexing's searches are better in scenarios where you want to give end users a powerful search capability, for example in a web app dashboard.

We can see this in action via the following C# code:

```cs
var searchResult = await _indexingService.SearchAsync("bookstore", "JohnSmith OR JaneDoe", true, 0, 10, string.Empty);
return Content(searchResult);
```

The `"JohnSmith OR JaneDoe"` is the search syntax in this case. It's simple, straightforward, and probably what at least some people have used before on Google. You can very easily take a user's typed search query submitted from a web app and pass it through as an argument to the `SearchAsync` method - with no translation needed.

