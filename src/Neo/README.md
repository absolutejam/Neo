# Neo

Check out the examples in the [tests](../Neo.Test/src/Tests.fs) for now

## Transactions

You can handle transactions your self via. the normal `IDisposable` pattern:

```f#
open Neo

task {
    use session = driver.AsyncSession (fun cfg -> cfg.WithDatabase throwaway.Name |> ignore)
    use! transaction = session.BeginTransactionAsync ()
    
    let! result =
        "MATCH (f:Foo { Name: $fooName }) RETURN f"
        |> Neo.contextFromQuery
        |> Neo.parameters [ "fooName", NeoValue.String "test" ]
        |> Neo.executeQuery transaction (
            Neo.returnSingle (fun returns ->
                let fooNode = returns.nodeProperties "f"
                fooNode.string "Name"
            )
        )
        
    do! transaction.CommitAsync ()
    
    return result
}
```

Or you can use a helper:

  - `Neo.autoTransaction` - Automatically commits after the function, or rolls 
    back in the event an exception was thrown.

    ```f#
    Neo.autoTransaction session (fun transaction -> task {
        return!
            "DO THING"
            |> Neo.contextFromQuery
            |> Neo.executeQuery transaction Neo.returnSummary
    })
    ```
