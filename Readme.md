Testing issues with the Cosmos Emulator.  Start the emulator with `docker compose up -d` and then run the tests with `dotnet test EmulatorTests/`.


Primary issues:
- When doing a query with a parameter, but limiting the query to the scope of a Partition Key.
- Value count() not supported.

