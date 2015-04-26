[<AutoOpen>]
module AppSettings
    open System
    open System.Configuration

    let port (defaultPort: int) =
        if ConfigurationManager.AppSettings.["host.port"] <> null then ConfigurationManager.AppSettings.["host.port"] |> Int32.Parse else defaultPort
