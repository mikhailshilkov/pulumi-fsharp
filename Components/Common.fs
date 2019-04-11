module Common

open Pulumi
open Pulumi.Azure.Core
open Pulumi.Azure.Storage
open Components

let private makeResourceGroup (args: ComponentArgs) =
    let location = match args.Region with | WestUS -> "West US" | WestEurope -> "West Europe"
    ResourceGroup(
        args.Name, 
        ResourceGroupArgs (Location = IO location))

let private makeStorageAccount (args: ComponentArgs) =
    Account(
        args.Name, 
        AccountArgs(
            ResourceGroupName = args.ResourceGroup.Name,                
            Location = args.ResourceGroup.Location,
            AccountKind = IO "StorageV2",
            AccountTier = IO "Standard",
            AccountReplicationType = IO "LRS"))


let zero = 
    { Name = ""
      ResourceGroup = null 
      StorageAccount = null
      Version = V2
      Runtime = DotNet
      PackagePath = null 
      Region = WestEurope
      CosmosDb = None
      DatabaseName = None
      CollectionName = None }

type StorageAccountBuilder internal () =
    member __.Yield(_) = zero

    member __.Run(state: ComponentArgs) : Account = makeStorageAccount state

    [<CustomOperation("name")>]
    member __.Name(state, name) = { state with Name = name }

    [<CustomOperation("resourceGroup")>]
    member __.ResourceGroup(state, rg) = { state with ResourceGroup = rg }

let storageAccount = StorageAccountBuilder()

type ResourceGroupBuilder internal () =
    member __.Yield(_) = zero

    member __.Run(state: ComponentArgs) : ResourceGroup = makeResourceGroup state

    [<CustomOperation("name")>]
    member __.Name(state, name) = { state with Name = name }

    [<CustomOperation("region")>]
    member __.Region(state, region) = { state with Region = region }

let resourceGroup = ResourceGroupBuilder()