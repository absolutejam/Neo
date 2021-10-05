namespace Neo

open System
open Neo4j.Driver

exception EntityPropertyNotFoundException of message: string

/// The properties of an entity (Node or Relationship)
type EntityProperties =
    EntityProperties of Map<string, obj> with

    static member ofEntity (entity: #IEntity) =
        EntityProperties (Map.ofSeq [ for kv in entity.Properties -> kv.Key, kv.Value ])

    static member properties (EntityProperties properties) = properties

    // Static

    static member tryFindProperty<'t> key (EntityProperties map) =
        map |> Map.tryFind key |> Option.map unbox<'t>

    static member findProperty<'t> key (EntityProperties map) =
        match map |> Map.tryFind key with
        | Some value -> unbox<'t> value
        | None -> raise <| EntityPropertyNotFoundException $"Entity does not have property: {key}"

    // Instance

    member this.property<'t> key = EntityProperties.findProperty<'t> key this
    member this.propertyOrNone<'t> key = EntityProperties.tryFindProperty<'t> key this
    member this.string key = EntityProperties.findProperty<string> key this
    member this.stringOrNone key = EntityProperties.tryFindProperty<string> key this
    member this.int key = EntityProperties.findProperty<int64> key this
    member this.intOrNone key = EntityProperties.tryFindProperty<int64> key this
    member this.bool key = EntityProperties.findProperty<bool> key this
    member this.boolOrNone key = EntityProperties.tryFindProperty<bool> key this
    member this.date key = EntityProperties.findProperty<LocalDate> key this |> Helpers.localDateToDateTime
    member this.dateOrNone key = EntityProperties.tryFindProperty<LocalDate> key this |> Option.map Helpers.localDateToDateTime
    member this.dateTime key = EntityProperties.findProperty<LocalDateTime> key this |> Helpers.localDateTimeToDateTime
    member this.dateTimeOrNone key = EntityProperties.tryFindProperty<LocalDateTime> key this |> Option.map Helpers.localDateTimeToDateTime
    member this.timeSpan key = EntityProperties.findProperty<LocalTime> key this |> Helpers.localTimeToTimeSpan
    member this.timeSpanOrNone key = EntityProperties.tryFindProperty<LocalTime> key this |> Option.map Helpers.localTimeToTimeSpan
    member this.dateTimeOffset key = EntityProperties.findProperty<ZonedDateTime> key this |> Helpers.zonedDateTimeToDateTimeOffset
    member this.dateTimeOffsetOrNone key = EntityProperties.tryFindProperty<ZonedDateTime> key this |> Option.map Helpers.zonedDateTimeToDateTimeOffset


[<AutoOpen>]
module EntityPropertyExtensions =
    type INode with
            member this.EntityProperties with get () = EntityProperties.ofEntity this

    type IRelationship with
            member this.EntityProperties with get () = EntityProperties.ofEntity this
