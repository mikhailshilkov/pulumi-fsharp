module FunctionApp

open System
open System.Collections.Generic
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Blob

open Pulumi
open Pulumi.Azure.Storage
open Pulumi.Azure.Appservice

open Components

let private getUrl connectionString containerName blobName =
    let client = CloudStorageAccount.Parse connectionString
    let blobClient = client.CreateCloudBlobClient ()
    let container = blobClient.GetContainerReference containerName
    let blob = container.GetBlobReference blobName

    let sasConstraints = SharedAccessBlobPolicy()
    sasConstraints.SharedAccessExpiryTime <- Nullable(DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero))
    sasConstraints.Permissions <- SharedAccessBlobPermissions.Read

    let sasContainerToken = blob.GetSharedAccessSignature(sasConstraints)
    blob.Uri.AbsoluteUri + sasContainerToken

let private makeFunctionApp (args: ComponentArgs) =
    let container = 
        Container(
            sprintf "%s-c" args.Name, 
            ContainerArgs(
                ResourceGroupName = args.ResourceGroup.Name,
                StorageAccountName = args.StorageAccount.Name,
                ContainerAccessType = IO "private"))
    let appServicePlan = 
        Plan(
            sprintf "%s-asp" args.Name, 
            PlanArgs(
                ResourceGroupName = args.ResourceGroup.Name,                
                Location = args.ResourceGroup.Location,
                Kind = IO "FunctionApp",
                Sku = IO(PlanArgsSku(Tier = IO "Dynamic", Size = IO "Y1"))
            ))       

    let blob = 
        Blob(
            sprintf "%s-blob" args.Name,
            BlobArgs(
                ResourceGroupName = args.ResourceGroup.Name,
                StorageAccountName = args.StorageAccount.Name,
                StorageContainerName = container.Name,
                Type = IO "block",
                Source = IO args.PackagePath
            ))            

    let versionString = match args.Version with | V1 -> "~1" | V2 -> "~2"
    let runtimeString = match args.Runtime with | DotNet -> "dotnet" | NodeJs -> "node"

    let settings = io {
        let! connectionString = args.StorageAccount.PrimaryConnectionString
        let! containerName = container.Name
        let! blobName = blob.Name
        let codeBlobUrl = getUrl connectionString containerName blobName

        let! cosmosConnections = args.CosmosDb |> Option.map (fun c -> c.Account.ConnectionStrings) |> Option.defaultValue (IO [||])
        let! _ = args.CosmosDb |> Option.map (fun c -> c.Collection) |> Option.defaultValue (IO "")

        let settings = seq {
            yield "WEBSITE_RUN_FROM_ZIP", codeBlobUrl
            yield "FUNCTIONS_EXTENSION_VERSION", versionString
            yield "FUNCTIONS_WORKER_RUNTIME", runtimeString
            if Array.isEmpty cosmosConnections |> not then yield ("CosmosDBConnection", cosmosConnections.[0])
        }
        return new Dictionary<string, string>(dict settings)
    }

    FunctionApp(
        sprintf "%s-app" args.Name,
        FunctionAppArgs(
            ResourceGroupName = args.ResourceGroup.Name,                
            Location = args.ResourceGroup.Location,
            AppServicePlanId = appServicePlan.Id,
            StorageConnectionString = args.StorageAccount.PrimaryConnectionString,        
            Version = IO versionString,
            AppSettings = settings
        ))

type FunctionAppBuilder internal () =
    member __.Yield(_) = Common.zero

    member __.Run(state: ComponentArgs) : FunctionApp = makeFunctionApp state

    [<CustomOperation("name")>]
    member __.Name(state, name) = { state with Name = name }

    [<CustomOperation("resourceGroup")>]
    member __.ResourceGroup(state, rg) = { state with ResourceGroup = rg }

    [<CustomOperation("storageAccount")>]
    member __.StorageAccount(state, sa) = { state with StorageAccount = sa }

    [<CustomOperation("version")>]
    member __.Version(state, value) = { state with Version = value }

    [<CustomOperation("runtime")>]
    member __.Runtime(state, value) = { state with Runtime = value }

    [<CustomOperation("package")>]
    member __.PackagePath(state, value) = { state with PackagePath = value }

    [<CustomOperation("cosmosdb")>]
    member __.CosmosDb(state, value) = { state with CosmosDb = Some value }

let functionApp = FunctionAppBuilder()