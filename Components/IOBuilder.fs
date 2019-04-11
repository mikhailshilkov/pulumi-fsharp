module Components

open System
open Pulumi
open Pulumi.Azure.Core
open Pulumi.Azure.Appservice
open Pulumi.Azure.Storage

module IOBuilder =
    type IOBuilder () =
        member __.Return (x: 'a)  = IO<'a> x
        member inline __.ReturnFrom (m: IO<_>) = m
        member __.Bind(m : IO<'a>, f : 'a -> IO<'b>) = m.SelectMany(Func<'a, IO<'b>> f)

let io = IOBuilder.IOBuilder()

type Region = WestUS | WestEurope
type FunctionAppVersion = V1 | V2
type FunctionAppRuntime = DotNet | NodeJs

type CosmosWrapper = {
    Account: Azure.Cosmosdb.Account
    Database: IO<string>
    Collection: IO<string>
}

type ComponentArgs = {
    Name: string
    Region: Region
    ResourceGroup: ResourceGroup
    StorageAccount: Account
    Version: FunctionAppVersion
    Runtime: FunctionAppRuntime
    PackagePath: string
    CosmosDb: CosmosWrapper option
    DatabaseName: string option
    CollectionName: string option
}