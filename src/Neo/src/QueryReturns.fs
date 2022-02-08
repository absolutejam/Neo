namespace Neo

open Neo4j.Driver

exception ReturnEntryNotFoundException of entry: string

/// Map of returns from a query
type QueryReturns =
    QueryReturns of Map<string, obj> with

    /// Extract the `Values` from an `IRecord` and convert to a `QueryReturns`
    static member ofRecord (record: IRecord) =
        record.Values
        |> Seq.map (|KeyValue|)
        |> Map.ofSeq
        |> QueryReturns
    static member values (QueryReturns values) = values

    static member find key mapper (QueryReturns map) =
        match map |> Map.tryFind key with
        | None -> raise <| ReturnEntryNotFoundException $"No such return entry with key: %s{key}"
        | Some value -> mapper value

    static member tryFind key mapper (QueryReturns map) =
        map |> Map.tryFind key |> Option.bind (fun node -> if isNull node then None else Some (mapper node))

    static member findList key mapper =
        QueryReturns.find key (unbox<ResizeArray<obj>> >> Seq.map mapper >> List.ofSeq)

    static member tryFindList key mapper =
        QueryReturns.tryFind key (unbox<ResizeArray<obj>> >> Seq.map mapper >> List.ofSeq) >> Option.defaultValue []

    (* Scalars *)
    static member findString key                = QueryReturns.find key unbox<string>
    static member tryFindString key             = QueryReturns.tryFind key unbox<string>
    static member findInt key                   = QueryReturns.find key unbox<int64>
    static member tryFindInt key                = QueryReturns.tryFind key unbox<int64>
    static member findFloat key                 = QueryReturns.find key unbox<float>
    static member tryFindFloat key              = QueryReturns.tryFind key unbox<float>
    (* INode *)
    static member findNode key                  = QueryReturns.find key unbox<INode>
    static member tryFindNode key               = QueryReturns.tryFind key unbox<INode>
    static member findNodeList key              = QueryReturns.findList key unbox<INode>
    static member tryFindNodeList key           = QueryReturns.tryFindList key unbox<INode>
    static member findNodePropertiesList key    = QueryReturns.findList key (unbox<INode> >> EntityProperties.ofEntity)
    static member tryFindNodePropertiesList key = QueryReturns.tryFindList key (unbox<INode> >> EntityProperties.ofEntity)
    (* IRelationship *)
    static member findRelationship key          = QueryReturns.find key unbox<IRelationship>
    static member tryFindRelationship key       = QueryReturns.tryFind key unbox<IRelationship>
    (* IPath *)
    static member findPath key                  = QueryReturns.find key unbox<IPath>
    static member tryFindPath key               = QueryReturns.tryFind key unbox<IPath>
    (* Scalars *)
    member this.string key                      = QueryReturns.findString key this
    member this.stringOrNone key                = QueryReturns.tryFindString key this
    member this.int key                         = QueryReturns.findInt key this
    member this.intOrNone key                   = QueryReturns.tryFindInt key this
    member this.float key                       = QueryReturns.findFloat key this
    member this.floatOrNone key                 = QueryReturns.tryFindFloat key this
    (* INode *)
    /// Attempts to retrieve an INode. If the value does not exist, an exception is raised.
    member this.node key                        = QueryReturns.findNode key this
    /// Attempts to retrieve an INode. If the value does not exist, None is returned.
    member this.nodeOrNone key                  = QueryReturns.tryFindNode key this
    /// Attempts to retrieve EntityProperties. If the value does not exist, an exception is raised.
    member this.nodeProperties key              = QueryReturns.findNode key this |> EntityProperties.ofEntity
    /// Attempts to retrieve EntityProperties. If the value does not exist, None is returned.
    member this.nodePropertiesOrNone key        = QueryReturns.tryFindNode key this |> Option.map EntityProperties.ofEntity
    /// Attempts to retrieve a list of INode. If the value does not exist, an exception is raised.
    member this.nodeList key                    = QueryReturns.findNodeList key this
    /// Attempts to retrieve a list of INode. If the value does not exist, returns an empty list.
    member this.tryNodeList key                 = QueryReturns.tryFindNodeList key this
    /// Attempts to retrieve a list of EntityProperties. If the value does not exist, an exception is raised.
    member this.nodePropertiesList key          = QueryReturns.findNodePropertiesList key this
    /// Attempts to retrieve a list of EntityProperties. If the value does not exist, returns an empty list.
    member this.tryNodePropertiesList key       = QueryReturns.findNodePropertiesList key this
    (* IRelationship *)
    member this.relationship key                = QueryReturns.findRelationship key this
    member this.relationshipOrNone key          = QueryReturns.tryFindRelationship key this
    (* IPath *)
    member this.path key                        = QueryReturns.findPath key this
    member this.pathOrNone key                  = QueryReturns.tryFindPath key this
