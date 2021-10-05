namespace Neo

open Neo4j.Driver
open System.Collections.Generic

module Helpers =
    let toParametersDict (queryParams: QueryParam list) =
        Map.ofSeq [ for key, value in queryParams -> key, NeoValue.toObj value ]
        :> IReadOnlyDictionary<string, obj>

    let localTimeToTimeSpan (value: LocalTime) = value.ToTimeSpan ()
    let localDateToDateTime (value: LocalDate) = value.ToDateTime ()
    let localDateTimeToDateTime (value: LocalDateTime) = value.ToDateTime ()
    let zonedDateTimeToDateTimeOffset (value: ZonedDateTime) = value.ToDateTimeOffset ()
