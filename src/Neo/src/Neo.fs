namespace Neo

open System
open Neo4j.Driver
open System.Threading.Tasks
open FsToolkit.ErrorHandling

/// Helpers for constructing `NeoValue`s
module Neo =
    open FSharp.Control.Tasks

    /// Create a `QueryContext` from a query string
    let contextFromQuery queryString =
        { QueryContext.empty with Query = Some queryString }

    /// Provide Neo4j query parameters
    let parameters (queryParams: QueryParam list) ctx : QueryContext =
        { ctx with Params = ctx.Params @ queryParams }

    let mapQuery (mapper: string -> string) (ctx: QueryContext) =
        { ctx with Query = ctx.Query |> Option.map mapper }

    /// Run `queryFunc` with the Neo4j query representation, returning the context.
    /// This is useful for logging inside a pipeline.
    let tapQuery (queryFun: string -> unit) (ctx: QueryContext) =
        match ctx.Query with
        | Some query ->
            let builtQuery =
                match ctx.Params with
                |[] -> Query query
                | _ -> Query (query, Helpers.toParametersDict ctx.Params)
            do queryFun (string builtQuery)
        | None -> raise EmptyQueryException
        ctx

    //
    // Transactions
    //

    let private inTransactionInternal (transactionConfig: (TransactionConfigBuilder -> _) option) (transactionFun: IAsyncTransaction -> Task<'a>) (session: IAsyncSession) = task {
        let! transaction =
            match transactionConfig with
            | Some cfg -> session.BeginTransactionAsync (Action<_> cfg)
            | None     -> session.BeginTransactionAsync ()

        let! body = Task.catch (transactionFun transaction)
        match body with
        | Choice1Of2 x ->
            do! transaction.CommitAsync ()
            return x
        | Choice2Of2 err ->
            do! transaction.RollbackAsync ()
            return! raise err
    }

    /// Begin a new transaction in this session using server default transaction configurations.
    /// The transaction is automatically committed at the end, unless any exceptions thrown, then it will be rolled back.
    ///
    /// A session can have at most one transaction running at a time, if you want to run multiple concurrent transactions,
    /// you should use multiple concurrent sessions.
    let autoTransaction (session: IAsyncSession) transactionFun =
        inTransactionInternal None transactionFun session

    /// Asynchronously begin a new transaction in this session using server default transaction configurations.
    ///
    /// A session can have at most one transaction running at a time, if you want to run multiple concurrent transactions,
    /// you should use multiple concurrent sessions.
    let inTransactionWithConfig (session: IAsyncSession) config transactionFun =
        inTransactionInternal (Some config) transactionFun session

    let executeInReadTransaction (session: IAsyncSession) transactionFun =
        session.ReadTransactionAsync (Func<IAsyncTransaction, Task<'a>> transactionFun)

    let executeInReadTransactionWithConfig (session: IAsyncSession) transactionConfig transactionFun =
        session.ReadTransactionAsync (Func<IAsyncTransaction, Task<'a>> transactionFun, transactionConfig)

    let executeInWriteTransaction (session: IAsyncSession) transactionFun =
        session.WriteTransactionAsync (Func<IAsyncTransaction, Task<'a>> transactionFun)

    let executeInWriteTransactionWithConfig (session: IAsyncSession) transactionConfig transactionFun =
        session.WriteTransactionAsync (Func<IAsyncTransaction, Task<'a>> transactionFun, transactionConfig)

    (*
        Returners
    *)

    /// Returns a single result
    let returnSingle (transformer: QueryReturns -> 't) (cursor: IResultCursor) = task {
        let! result = Task.catch (cursor.SingleAsync (Func<_, _>(QueryReturns.ofRecord >> transformer)))
        match result with
        | Choice1Of2 result ->
            return Ok result
        | Choice2Of2 err ->
            return Error err
    }

    /// Returns a single result and the summary
    let returnSingleWithSummary (transformer: QueryReturns -> 't) (cursor: IResultCursor) = task {
        let! result = Task.catch (cursor.SingleAsync (Func<_, _>(QueryReturns.ofRecord >> transformer)))
        match result with
        | Choice1Of2 result ->
            let! summary = cursor.ConsumeAsync ()
            return Ok (result, summary)
        | Choice2Of2 err ->
            return Error err
    }

    let (|EmptyResultException|_|) (ex: exn) =
        match ex with
        | :? InvalidOperationException as e ->
            if e.Message = "The result is empty." then
                Some e
            else
                None

        | _ -> None

    let returnSingleOrNone (transformer: QueryReturns -> 't option) (cursor: IResultCursor) = task {
        let! result = Task.catch (cursor.SingleAsync (Func<_, _>(QueryReturns.ofRecord >> transformer)))
        match result with
        | Choice1Of2 result ->
            return Ok result
        | Choice2Of2 err ->
            match err with
            | EmptyResultException _ ->
                return Ok None
            | e ->
                return Error e
    }

    /// Similar to `returnSingle` but returns `None` if nothing is returned by the
    /// transformer function (does not return the summary in this case)
    ///
    /// This also handles an `InvalidOperationException` you may get when the
    /// result is empty and returns None for this case too.
    let returnSingleOrNoneWithSummary (transformer: QueryReturns -> 't option) (cursor: IResultCursor) = task {
        let! result = Task.catch (cursor.SingleAsync (Func<_, _>(QueryReturns.ofRecord >> transformer)))
        match result with
        | Choice1Of2 result ->
            match result with
            | Some result ->
                let! summary = cursor.ConsumeAsync ()
                return Ok <| Some (result, summary)
            | None ->
                return Ok None
        | Choice2Of2 err ->
            match err with
            | EmptyResultException _ ->
                return Ok None
            | e ->
                return Error e
    }

    /// Returns multiple results
    let returnMultiple transformer (cursor: IResultCursor) = task {
        let func = Func<_, _>(QueryReturns.ofRecord >> transformer)
        let! results = Task.catch (cursor.ToListAsync func)
        match results with
        | Choice1Of2 results ->
            return Ok (Seq.toList results)
        | Choice2Of2 err ->
            match err with
            | EmptyResultException _ ->
                return Ok []
            | :? AggregateException as e ->
                return Error e.InnerException
            | e ->
                return Error e
    }

    /// Returns multiple results with the summary
    let returnMultipleWithSummary transformer (cursor: IResultCursor) = task {
        let func = Func<_, _>(QueryReturns.ofRecord >> transformer)
        let! results = Task.catch (cursor.ToListAsync func)
        match results with
        | Choice1Of2 results ->
            let! summary = cursor.ConsumeAsync ()
            return Ok (Seq.toList results, summary) // TODO: Remove Seq.toList
        | Choice2Of2 err ->
            match err with
            | EmptyResultException _ ->
                let! summary = cursor.ConsumeAsync ()
                return Ok ([], summary)
            | :? AggregateException as e ->
                return Error e.InnerException
            | e ->
                return Error e
    }

    /// Returns multiple results where the `transformer` returns `Some`
    let chooseMultiple transformer (cursor: IResultCursor) = task {
        let! results = Task.catch (cursor.ToListAsync (Func<_, _>(QueryReturns.ofRecord >> transformer)))
        match results with
        | Choice1Of2 results ->
            return Ok (results |> Seq.choose id |> Seq.toList)
        | Choice2Of2 err ->
            match err with
            | EmptyResultException _ ->
                return Ok []
            | e ->
                return Error e
    }

    /// Returns multiple results where the `transformer` returns `Some`
    let chooseMultipleWithSummary transformer (cursor: IResultCursor) = task {
        let! results = Task.catch (cursor.ToListAsync (Func<_, _>(QueryReturns.ofRecord >> transformer)))
        match results with
        | Choice1Of2 results ->
            let! summary = cursor.ConsumeAsync ()
            return Ok (results |> Seq.choose id |> Seq.toList, summary)
        | Choice2Of2 err ->
            return Error err
    }

    let returnSummary (cursor: IResultCursor) = task {
        let! summary = cursor.ConsumeAsync ()
        return summary
    }

    /// Run the query with a query runner, ie an IAsyncSession or IAsyncTransaction
    let executeQuery (queryRunner: #IAsyncQueryRunner) (cursorFun: IResultCursor -> Task<'a>) ctx = task {
        let! cursor =
            match ctx.Query with
            | Some query ->
                match ctx.Params with
                | [] -> queryRunner.RunAsync query
                | _  -> queryRunner.RunAsync (query, Helpers.toParametersDict ctx.Params)
            | None -> raise EmptyQueryException
        return! cursorFun cursor
    }