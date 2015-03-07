module TriggerTests
//
//open Xunit
//open FsUnit.Xunit
//open System.Threading   
//open Types
//open System
//open Triggers
//open System.Collections.Concurrent
//
//(* Commmon *)
//type TriggerStateCallback = delegate of unit -> TriggerState
//
//let rec wait dateTime =
//    if DateTime.UtcNow < dateTime then
//        Thread.Sleep(100)
//        wait dateTime
//
//let rec waitForState (actual : TriggerStateCallback) (expected : TriggerState) =
//    let invokedActual = actual.Invoke()
//    let pa =  invokedActual.ToString()
//    let pe = expected.ToString()
//    printf "actual: %s expected %s" pa pe |> ignore
//    if invokedActual <> expected then
//        Thread.Sleep(100)
//        waitForState actual expected |> ignore
// 
//(* Callbacks *)      
//let tokenSource = new CancellationTokenSource()
//  
//let callback (token : CancellationToken) = 
//    printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
//    Thread.Sleep 100
//
//let overlapCallback (token : CancellationToken) = 
//    printf "overlapCallback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
//    Thread.Sleep (60 * 1000 * 2) // 2minutes
//
//let throwingCallback (token : CancellationToken) = 
//    Thread.Sleep 500
//    failwith "exception message"
//
//let cancellingCallback (token : CancellationToken) = 
//    Thread.Sleep 500
//    raise (OperationCanceledException())
//
//[<TraitAttribute("Trigger", "Fast running")>] 
//type TriggerTests() =
//    
//    let date = new DateTime(2015, 02, 05, 20, 52, 1)
//    let expr = "* * * * *"
//
//    [<Fact>]
//    let ``Should return occurrenceAt`` () =
//        calculatetOccurrenceAt(expr, date) |> should equal <| new DateTime(2015, 02, 05, 20, 53, 0)
//
//    [<Fact>]
//    let ``Should return changeArgs`` () =
//        let due, interval = calculateTimerArgs(expr, date, calculatetOccurrenceAt(expr, date))
//
//        due |> should equal 59000
//        interval |> should equal 60000
//
//    [<Fact>]
//    let ``Should not throw on failing jobs`` () =
//       timerCallback("trowing exception", Callback(throwingCallback), tokenSource.Token)
//       timerCallback("throwing OperationCanceledException from job", Callback(cancellingCallback), tokenSource.Token)
//       tokenSource.Cancel()
//       timerCallback("throwing OperationCanceledException from timerCallback", Callback(callback), tokenSource.Token)
//
//
//[<TraitAttribute("Trigger", "Long running")>] 
//type TriggerScenarios() =
//
//    let trigger = new Trigger("job#1", "* * * * *", Callback(callback)) :> ITrigger
//    let overlapTrigger = new Trigger("job#1", "* * * * *", Callback(overlapCallback)) :> ITrigger
//    let throwingTrigger = new Trigger("job#1", "* * * * *", Callback(throwingCallback)) :> ITrigger
//    let cancellingTrigger = new Trigger("job#1", "* * * * *", Callback(cancellingCallback)) :> ITrigger
//
//    let triggerStateCallback = TriggerStateCallback(fun _ -> trigger.State)
//    let throwingStateCallback = TriggerStateCallback(fun _ -> trigger.State)
//
//    [<Fact>]
//    let ``Should not throw on failing jobs`` () =
//        throwingTrigger.Fire() |> ignore
//        cancellingTrigger.Fire() |> ignore
//
//    [<Fact>]
//    let ``Should handle overlaping jobs`` () =
//        overlapTrigger.Start() |> ignore
//        let firstOccurance = overlapTrigger.OccurrenceAt
//        wait firstOccurance
//       
//        let secondOccurance = overlapTrigger.OccurrenceAt
//        wait secondOccurance
//
//        (secondOccurance - firstOccurance).Minutes |> should equal 1
//        overlapTrigger.Terminate()
//
//    [<Fact>]
//    let ``Should have equal occurance dates`` () =
//        trigger.Start() |> ignore
//        
//        let firstOccurance = trigger.OccurrenceAt
//        wait firstOccurance
//       
//        let secondOccurance = trigger.OccurrenceAt
//        wait secondOccurance
//
//        let tirdOccurance = trigger.OccurrenceAt
//        wait tirdOccurance
//
//        (secondOccurance - firstOccurance).Minutes |> should equal 1
//        (tirdOccurance - secondOccurance).Minutes |> should equal 1
//
//    [<Fact>]
//    let ``Should change the state durring execution`` () =
//        waitForState triggerStateCallback Stopped
//        trigger.Start() |> ignore 
//        waitForState triggerStateCallback Idle
//        waitForState triggerStateCallback Executing
//        trigger.Stop() |> ignore 
//        waitForState triggerStateCallback Stopped
//        trigger.Terminate() |> ignore
//        waitForState triggerStateCallback Terminated 
//
//    [<Fact>]
//    let ``Should not allow to Start / Stop / Terminate when trigger was already termianted`` () =
//        trigger.Start() |> ignore
//        trigger.Terminate() |> ignore
//        waitForState triggerStateCallback Terminated
//        trigger.Start() |> ignore
//        waitForState triggerStateCallback Terminated
//        trigger.Stop() |> ignore
//        waitForState triggerStateCallback Terminated
//        trigger.Terminate() |> ignore
//        waitForState triggerStateCallback Terminated   
//
//    [<Fact>]
//    let ``Should be able to Fire when terminated``() =
//        trigger.Terminate() |> ignore
//        trigger.Fire() |> ignore
//        trigger.State |> should equal Terminated
//
//    [<Fact>]
//    let ``Should not change current state when Firing trigger`` () =
//        trigger.State |> should equal Stopped
//        trigger.Fire() |> ignore
//        trigger.State |> should equal Stopped