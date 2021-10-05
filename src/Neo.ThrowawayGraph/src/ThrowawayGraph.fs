module Neo.ThrowawayGraph

open System
open Neo4j.Driver
open FsToolkit.ErrorHandling

type ThrowawayGraph internal (driver: IDriver, ?dbPrefix: string) =
    let generateSuffix () = Guid.NewGuid().ToString("n").Substring(0, 10).ToLowerInvariant()
    let mutable dbName = dbPrefix |> Option.defaultValue $"throwaway{generateSuffix()}"

    static member FromDriver (driver: IDriver, ?dbPrefix) =
        let throwaway = new ThrowawayGraph (driver, ?dbPrefix = dbPrefix)
        throwaway.CreateDatabase ()
        throwaway

    member this.Name = dbName
    member private this.Name with set (x: string) = this.Name <- x

    member this.Driver = driver

    member private this.CreateDatabase () =
        use session = driver.AsyncSession ()
        "CREATE DATABASE $dbName"
        |> Neo.contextFromQuery
        |> Neo.parameters [ "dbName", NeoValue.String dbName ]
        |> Neo.executeQuery session Neo.returnSummary
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> ignore

    member this.DropGraph () =
        use session = driver.AsyncSession ()
        "DROP DATABASE $dbName IF EXISTS"
        |> Neo.contextFromQuery
        |> Neo.parameters [ "dbName", NeoValue.String dbName ]
        |> Neo.executeQuery session Neo.returnSummary
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> ignore

    interface IDisposable with
        member this.Dispose () =
            this.DropGraph ()