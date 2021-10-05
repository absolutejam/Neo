module Neo.QueryBuilder

open Neo
open Neo4j.Driver

type QueryBuilder =
    {
        Clauses: (string * string) list
        Params: QueryParam seq
    }
    static member empty =
        {
            Clauses = []
            Params  = Seq.empty
        }

    static member reduce (queryBuilderFuns: (QueryBuilder -> QueryBuilder) seq) =
        Seq.reduce (>>) queryBuilderFuns

    /// Build a `QueryContext` from a `QueryBuilder`.
    static member buildContext (qb: QueryBuilder) : QueryContext =
        {
            // TODO: better formatting
            Query  = Some (qb.Clauses |> List.map (fun (k, v) -> $"{k} {v}") |> String.concat "\n")
            Params = List.ofSeq qb.Params
        }

    /// Build a `Clause` for the `QueryBuilder`, with optional `parameters`
    static member Clause (name: string) (clause: string) (parameters: QueryParam seq) =
        fun (qb: QueryBuilder) ->
            { qb with Clauses = qb.Clauses @ [name, clause]
                      Params  = Seq.append qb.Params parameters }

    /// Take a `QueryClause option` and add it to the `QueryBuilder` if it is not `None`
    static member DynamicClause (queryBuilderFun: QueryClause option) =
        fun (qb: QueryBuilder) ->
            match queryBuilderFun with
            | Some fn -> fn qb
            | None -> qb

    /// Not a Neo4j clause, but provides a mechanism of adding parameters within the query
    static member Parameters (parameters: QueryParam seq) =
        fun (qb: QueryBuilder) ->
            { qb with Params = Seq.append qb.Params parameters }

    static member CREATE (clause: string) (parameters: QueryParam seq) =
        QueryBuilder.Clause "CREATE" clause parameters

    static member MATCH (clause: string) (parameters: QueryParam seq) =
        QueryBuilder.Clause "MATCH" clause parameters

    static member MERGE (clause: string) (parameters: QueryParam seq) =
        QueryBuilder.Clause "MERGE" clause parameters

    static member FOREACH (clause: string) (parameters: QueryParam seq) =
        QueryBuilder.Clause "FOREACH" clause parameters

    static member OPTIONAL_MATCH (clause: string) (parameters: QueryParam seq) =
        QueryBuilder.Clause "OPTIONAL MATCH" clause parameters

    static member WHERE (clause: string) (parameters: QueryParam seq) =
        QueryBuilder.Clause "WHERE" clause parameters

    static member SET (clause: string) (parameters: QueryParam seq) =
        QueryBuilder.Clause "SET" clause parameters

    static member ON_CREATE_SET (clause: string) (parameters: QueryParam seq) =
        QueryBuilder.Clause "ON CREATE SET" clause parameters

    static member ORDER_BY (clause: string) (parameters: QueryParam seq) =
        QueryBuilder.Clause "ORDER BY" clause parameters

    static member SKIP (clause: string) (parameters: QueryParam seq) =
        QueryBuilder.Clause "SKIP" clause parameters

    static member LIMIT (clause: string) (parameters: QueryParam seq) =
        QueryBuilder.Clause "LIMIT" clause parameters

    static member RETURN (clause: string) (parameters: QueryParam seq) =
        QueryBuilder.Clause "RETURN" clause parameters

and QueryClause = QueryBuilder -> QueryBuilder

module Neo =
    /// Builds a query that was built using `QueryBuilder`
    let contextFromBuilders (queryBuilderFuns: (QueryBuilder -> QueryBuilder) seq) =
        QueryBuilder.empty
        |> QueryBuilder.reduce queryBuilderFuns
        |> QueryBuilder.buildContext

    let buildQuery ctx =
        match ctx.Query with
        | Some query ->
            match ctx.Params with
            | [] -> Query query
            | _  -> Query (query, Helpers.toParametersDict ctx.Params)
        | None -> raise EmptyQueryException
