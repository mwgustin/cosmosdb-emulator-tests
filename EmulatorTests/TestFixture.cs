using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

namespace EmulatorTests
{
public class TestFixture : IAsyncLifetime
{
    // Simple test setup and teardown. 
    public Container Container { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = new CosmosClientBuilder("AccountEndpoint=http://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
        builder.WithConnectionModeGateway();
        var client = builder.Build();

        var dbResp = await client.CreateDatabaseIfNotExistsAsync("TestDatabase");
        var containerResp = await dbResp.Database.CreateContainerIfNotExistsAsync("TestContainer", "/PartitionKey");
        Container = containerResp.Container;


        await ClearData();
        await SeedData();
    }

    public async Task DisposeAsync()
    {
        await ClearData();
    }
    

    private async Task SeedData()
    {
        await Container.UpsertItemAsync(new TestItem
        {
            Id = "1",
            Name = "Test Item 1 pk1",
            PartitionKey = "pk1"
        });
        await Container.UpsertItemAsync(new TestItem
        {
            Id = "1",
            Name = "Test Item 1 pk2",
            PartitionKey = "pk2"
        });

        await Container.UpsertItemAsync(new TestItem
        {
            Id = "2",
            Name = "Test Item 2 pk3",
            PartitionKey = "pk3"
        });
    }

    private async Task ClearData()
    {
        var iterator = Container.GetItemQueryIterator<TestItem>("SELECT * FROM c");
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                await Container.DeleteItemAsync<TestItem>(item.Id, new PartitionKey(item.PartitionKey));
            }
        }
    }
}
}