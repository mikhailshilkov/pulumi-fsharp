module Cosmos

open System
open Microsoft.Azure.Documents.Client

open Pulumi

open Components

let private createCollection (account: Azure.Cosmosdb.Account) databaseName collectionName = io {
    let! endpoint = account.Endpoint
    let! key = account.PrimaryMasterKey
    let database = Microsoft.Azure.Documents.Database(Id = databaseName)
    let collection = Microsoft.Azure.Documents.DocumentCollection(Id = collectionName)
    let flow = async {
        use client = new DocumentClient(Uri endpoint, key)
        let! result = client.CreateDatabaseIfNotExistsAsync(database) |> Async.AwaitTask
        do! client.CreateDocumentCollectionIfNotExistsAsync(result.Resource.SelfLink, collection) |> Async.AwaitTask |> Async.Ignore
        return ()
    }
    Async.RunSynchronously flow
    return databaseName, collectionName
}

let private makeCosmosdb (args: ComponentArgs) =
    let account = 
        Azure.Cosmosdb.Account(
            args.Name, 
            Azure.Cosmosdb.AccountArgs(
                ResourceGroupName = args.ResourceGroup.Name,                
                Location = args.ResourceGroup.Location,
                OfferType = IO "Standard",
                GeoLocations = IO [| IO(Azure.Cosmosdb.AccountArgsGeoLocation(Location = args.ResourceGroup.Location, FailoverPriority = IO 0)) |],
                ConsistencyPolicy = 
                    IO(Azure.Cosmosdb.AccountArgsConsistencyPolicy(ConsistencyLevel = IO "Session"))))
    let (database, collection) =
        match args.CollectionName with
        | Some collectionName -> 
            let databaseName = args.DatabaseName |> Option.defaultValue "defaultdb"
            let result = createCollection account databaseName collectionName                    
            result.Select(fun (a, _) -> a), result.Select(fun (_, b) -> b)
        | None -> IO "", IO ""
    { Account = account; Database = database; Collection = collection }

type CosmosDbBuilder internal () =
    member __.Yield(_) = Common.zero

    member __.Run(state: ComponentArgs) = makeCosmosdb state

    [<CustomOperation("name")>]
    member __.Name(state, name) = { state with Name = name }

    [<CustomOperation("resourceGroup")>]
    member __.ResourceGroup(state, rg) = { state with ResourceGroup = rg }

    [<CustomOperation("database")>]
    member __.Database(state, value) = { state with DatabaseName = Some value }

    [<CustomOperation("collection")>]
    member __.Collection(state, value) = { state with CollectionName = Some value }

let cosmosdb = CosmosDbBuilder()
