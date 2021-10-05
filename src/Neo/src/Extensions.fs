namespace Neo

[<AutoOpen>]
module internal CoreExtensions =
    module Option =
        /// Append to a `'t list option`
        let append value opts =
            match opts with
            | None -> Some [value]
            | Some values -> Some <| values @ [value]

        /// Concat with a `'t list option`
        let concat newValues opts =
            match opts with
            | None -> Some newValues
            | Some values -> Some <| values @ newValues
