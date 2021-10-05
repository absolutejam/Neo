namespace Neo

open System
open Serilog.Events

module Logger =
    /// Create a `Neo4j.Driver.ILogger` from a `Serilog.ILogger`
    let ofSerilogLogger (logger: Serilog.ILogger) =
        { new Neo4j.Driver.ILogger with
            member __.Error (exn, message: string, [<ParamArray>] args: obj[]) =
                logger.Error (exn, message, args)
            member __.Warn (exn, message: string, [<ParamArray>] args: obj[]) =
                logger.Warning (exn, message, args)
            member __.Debug (message: string, [<ParamArray>] args: obj[]) =
                logger.Debug (message, args)
            member __.Info (message: string, [<ParamArray>] args: obj[]) =
                logger.Information (message, args)
            member __.Trace (message: string, [<ParamArray>] args: obj[]) =
                logger.Debug (message, args)
            member __.IsDebugEnabled () =
                logger.IsEnabled LogEventLevel.Debug
            member __.IsTraceEnabled () =
                logger.IsEnabled LogEventLevel.Debug
         }
