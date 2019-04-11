open System
open Pulumi

open Components
open Common
open Cosmos
open FunctionApp

module Program =

    let app () =
        
        let rg = resourceGroup {
            name "urlshort-rg"
            region WestUS
        }

        let zipStorage = storageAccount {
            name "urlshortsa"
            resourceGroup rg
        }

        let cosmos = cosmosdb {
            name "urlshort-cosmos"
            resourceGroup rg
            database "mydb"
            collection "myitems"
        }
        
        functionApp {
            name "fsharpfa"
            package "..\\Functions\\bin\\Debug\\netcoreapp2.1\\Functions.zip"
            version V2
            runtime DotNet
            resourceGroup rg
            storageAccount zipStorage
            cosmosdb cosmos
        }

    [<EntryPoint>]
    let Main (_: string[]) =
        Runtime.Run(Action (app >> ignore))
        0