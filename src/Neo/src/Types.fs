namespace Neo

open System
open System.Collections
open System.Collections.Generic
open Neo4j.Driver
open System.Threading.Tasks

// https://neo4j.com/docs/dotnet-manual/current/cypher-workflow/#dotnet-driver-type-mapping

[<RequireQualifiedAccess>]
type NeoValue =
    | Null

    | String             of string
    | Integer            of int64
    | Float              of float
    | ByteArray          of byte array
    | Boolean            of bool
    | Date               of DateTime
    | DateTime           of DateTime
    | DateTimeOffset     of DateTimeOffset
    | TimeSpan           of TimeSpan
    | Map                of Microsoft.FSharp.Collections.Map<string, obj>

    | StringList         of string seq
    | IntegerList        of int64 seq
    | FloatList          of float seq
    | BooleanList        of bool seq
    | DateList           of DateTime seq
    | DateTimeList       of DateTime seq
    | DateTimeOffsetList of DateTimeOffset seq
    | TimeSpanList       of TimeSpan seq
    | MapList            of Microsoft.FSharp.Collections.Map<string, obj> seq
    | Raw                of obj

    static member toObj = function
        | Null                     -> null
        | String value             -> box value
        | Integer value            -> box value
        | Float value              -> box value
        | ByteArray value          -> box value
        | Boolean value            -> box value
        | Date value               -> box (LocalDate value)
        | DateTime value           -> box (LocalDateTime value)
        | DateTimeOffset value     -> box (ZonedDateTime value)
        | TimeSpan value           -> box (LocalTime value)
        | Map value                -> box (Dictionary value :> IDictionary)

        | StringList value         -> box (Seq.toArray value :> IList<string>)
        | IntegerList value        -> box (Seq.toArray value :> IList<int64>)
        | FloatList value          -> box (Seq.toArray value :> IList<float>)
        | BooleanList value        -> box (Seq.toArray value :> IList<bool>)
        | DateList value           -> box (value |> Seq.map LocalDate     |> Seq.toArray :> IList<LocalDate>)
        | DateTimeList value       -> box (value |> Seq.map LocalDateTime |> Seq.toArray :> IList<LocalDateTime>)
        | DateTimeOffsetList value -> box (value |> Seq.map ZonedDateTime |> Seq.toArray :> IList<ZonedDateTime>)
        | TimeSpanList value       -> box (value |> Seq.map LocalTime     |> Seq.toArray :> IList<LocalTime>)
        | MapList value            -> box (value |> Seq.map (Dictionary >> unbox<IDictionary>) |> Seq.toArray :> IList<IDictionary>)
        | Raw value                -> value

type QueryParam = string * NeoValue

/// An accumulated context that will be used to construct a query
type QueryContext =
    {
        Query:  string option
        Params: QueryParam list
    }
    static member empty =
        {
            Query  = None
            Params = []
        }

/// Callback which should be called within a session
type CursorCallback<'a> = IResultCursor -> Task<'a>

exception EmptyQueryException
