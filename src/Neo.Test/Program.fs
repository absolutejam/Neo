module Neo.Test.Program

open Expecto

[<EntryPoint>]
let main args =
    runTestsInAssemblyWithCLIArgs [] args
