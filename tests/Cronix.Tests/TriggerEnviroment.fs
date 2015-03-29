module TriggerEnviroment
    
    open System
    open System.Threading
    open Xunit
    open FsUnit.Xunit
    open Chessie.ErrorHandling
    open Cronix
    open Triggers


    let rec wait dateTime =
        let now = DateTime.UtcNow
        if now < dateTime then
            Console.WriteLine("now: {0}, datetime: {1}", now, dateTime)
            Thread.Sleep(5000)
            wait dateTime

    type TriggerStateCallback = delegate of unit -> TriggerState

    let rec waitForState (actual : TriggerStateCallback) (expected : TriggerState) =
        let invokedActual = actual.Invoke()
        let pa =  invokedActual.ToString()
        let pe = expected.ToString()
        printf "actual: %s expected %s" pa pe |> ignore
        if invokedActual <> expected then
            Thread.Sleep(100)
            waitForState actual expected |> ignore
 
    let tokenSource = new CancellationTokenSource()
   
    (* Callbacks *)
   
    let callback (token : CancellationToken) = 
        printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
        Thread.Sleep 100

    let overlapCallback (token : CancellationToken) = 
        printf "overlapCallback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
        Thread.Sleep (60 * 1000 * 2) // 2minutes

    let throwingCallback (token : CancellationToken) = 
        Thread.Sleep 500
        failwith "exception message"

    let cancellingCallback (token : CancellationToken) = 
        Thread.Sleep 500
        raise (OperationCanceledException())