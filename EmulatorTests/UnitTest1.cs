using Microsoft.Azure.Cosmos;

namespace EmulatorTests;

public class UnitTest1 : IAsyncLifetime
{
    //succeeds.  This is the expected behavior when reading by id with a partition key.
    [Fact]
    public async Task ReadById()
    {
        var item = await fixture.Container.ReadItemAsync<TestItem>("1", new PartitionKey("pk1"));
        Assert.NotNull(item);
        Assert.Equal("Test Item 1 pk1", item.Resource.Name);

    }

    //fails. This should be equivalent to the above because we specify the partition Key, but it returns both items with id 1.
    // succeeds with real DB.
    [Fact]
    public async Task QueryById_with_SharedId_SeparatePK()
    {
        var query = "SELECT * FROM c WHERE c.id = '1'";
        var iterator = fixture.Container.GetItemQueryIterator<TestItem>(query, requestOptions: new QueryRequestOptions()
        {
            PartitionKey = new PartitionKey("pk1")
        });
        var response = await iterator.ReadNextAsync();

        Assert.Single(response); // FAILS.  Returns both items with id 1, should only return the one with pk1.
        Assert.Equal("Test Item 1 pk1", response.First().Name);
    }

    //same as above, but succeeds because it's the only item with id 2
    [Fact]
    public async Task QueryById_with_SeparateId_SeparatePK()
    {
        var query = "SELECT * FROM c WHERE c.id = '2'";
        var iterator = fixture.Container.GetItemQueryIterator<TestItem>(query, requestOptions: new QueryRequestOptions()
        {
            PartitionKey = new PartitionKey("pk3")
        });
        var response = await iterator.ReadNextAsync();

        Assert.Single(response);
        Assert.Equal("Test Item 2 pk3", response.First().Name);
    }


    // fails. Similar to prior tests, but demonstrates that it's unrelated to the id field and applies to any property.
    // succeeds with real DB. 
    [Fact]
    public async Task QueryByName_SeparateId_SeparatePK()
    {
        var query = "select * from c where c.Name = 'TestItem'";
        var iterator = fixture.Container.GetItemQueryIterator<TestItem>(query, requestOptions: new QueryRequestOptions()
        {
            PartitionKey = new PartitionKey("pk4")
        });
        var response = await iterator.ReadNextAsync();

        Assert.Single(response); //FAILS. returns both items with Name 'TestItem', should only return the one with pk4.
        Assert.Equal("TestItem", response.First().Name);
        Assert.Equal("pk4", response.First().PartitionKey);
    }


    //fails.  This should return the count of the items in the fixture.Container across partitions (3), but it throws a serialization exception.
    // succeeds with real DB.
    [Fact]
    public async Task CountTest()
    {
        var query = "SELECT value COUNT(1) FROM c";
        var iterator = fixture.Container.GetItemQueryIterator<int>(query);
        var response = await iterator.ReadNextAsync();

        Assert.Single(response);
        Assert.Equal(3, response.First());
    }

    private TestFixture fixture;

    public Task InitializeAsync()
    {
        fixture = new TestFixture();
        return fixture.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        return fixture.DisposeAsync();
    }
}

