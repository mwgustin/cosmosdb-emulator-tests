
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Newtonsoft.Json;

namespace EmulatorTests;

public class UnitTest1 : IAsyncLifetime
{
    Microsoft.Azure.Cosmos.Container container = null!; // Initialize this in the InitializeAsync method

    public async Task DisposeAsync()
    {
        await ClearData();
    }

    public async Task InitializeAsync()
    {
        var builder = new CosmosClientBuilder("AccountEndpoint=http://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
        builder.WithConnectionModeGateway();
        var client = builder.Build();

        var dbResp = await client.CreateDatabaseIfNotExistsAsync("TestDatabase");
        var containerResp = await dbResp.Database.CreateContainerIfNotExistsAsync("TestContainer", "/PartitionKey");
        container = containerResp.Container;


        await ClearData();
        await SeedData();
    }

    [Fact]
    public async Task ReadById()
    {
        var item = await container.ReadItemAsync<TestItem>("1", new PartitionKey("pk1"));
        Assert.NotNull(item);
        Assert.Equal("Test Item 1 pk1", item.Resource.Name);

    }

    [Fact]
    public async Task QueryById_with_SharedId_SeparatePK()
    {
        var query = "SELECT * FROM c WHERE c.id = '1'";
        var iterator = container.GetItemQueryIterator<TestItem>(query, requestOptions: new QueryRequestOptions()
        {
            PartitionKey = new PartitionKey("pk1")
        });
        var response = await iterator.ReadNextAsync();
        
        Assert.Single(response); // FAILS.  Returns both items with id 1.
        Assert.Equal("Test Item 1 pk1", response.First().Name);
    }

    [Fact]
    public async Task QueryById_with_SeparateId_SeparatePK()
    {
        var query = "SELECT * FROM c WHERE c.id = '2'";
        var iterator = container.GetItemQueryIterator<TestItem>(query, requestOptions: new QueryRequestOptions()
        {
            PartitionKey = new PartitionKey("pk3")
        });
        var response = await iterator.ReadNextAsync();

        Assert.Single(response);
        Assert.Equal("Test Item 2 pk3", response.First().Name);
    }

    private async Task SeedData()
    {
        await container.UpsertItemAsync(new TestItem
        {
            Id = "1",
            Name = "Test Item 1 pk1",
            PartitionKey = "pk1"
        });
        await container.UpsertItemAsync(new TestItem
        {
            Id = "1",
            Name = "Test Item 1 pk2",
            PartitionKey = "pk2"
        });

        await container.UpsertItemAsync(new TestItem
        {
            Id = "2",
            Name = "Test Item 2 pk3",
            PartitionKey = "pk3"
        });
    }

    private async Task ClearData()
    {
        var iterator = container.GetItemQueryIterator<TestItem>("SELECT * FROM c");
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                await container.DeleteItemAsync<TestItem>(item.Id, new PartitionKey(item.PartitionKey));
            }
        }
    }
}


public record TestItem
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public required string Id { get; init; } =  Guid.NewGuid().ToString();
    public required string Name { get; init; }
    public required string PartitionKey { get; init; }
}