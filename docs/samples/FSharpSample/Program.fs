open System
open Cronix
open Chessie.ErrorHandling
open System.Threading

let sampleJob (token : CancellationToken) = 
      printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
      Thread.Sleep 100
 
[<EntryPoint>]
let main argv = 
 
    let startupHandler = 
        new StartupHandler(
            fun(scheduler) ->
                        scheduler.Schedule "scheduled job" <| "* * * * *" <| Callback( sampleJob ) |> ignore
                        scheduler.Schedule "second name" <| frequency Hourly <| Callback( sampleJob ) |> ignore
            )

    let result = BootStrapper.InitService(argv, startupHandler)
    match result with
    | Ok (state, msgs) -> printfn "%s" state
    | Fail msgs -> msgs |> List.iter(fun(s) ->  printfn "%s" s)
    | _ -> ()
    0 // return an integer exit code
