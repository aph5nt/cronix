namespace Cronix.Tests

open Xunit
open FsUnit.Xunit
open System.Threading   
open System
open System.Collections.Concurrent

open Cronix
open Triggers
open System.Threading
open NCrontab 
open System
open Logging 
open TriggerEnviroment


[<Trait("Trigger", "Unit Test")>] 
type TriggerTests() =
    
    let date = new DateTime(2015, 02, 05, 20, 52, 1)
    let expr = "* * * * *"

    [<Fact>]
    let ``Should return occurrenceAt`` () =
        calculatetOccurrenceAt(expr, date) |> should equal <| new DateTime(2015, 02, 05, 20, 53, 0)

    [<Fact>]
    let ``Should return changeArgs`` () =
        let due, interval = calculateTimerArgs(expr, date, calculatetOccurrenceAt(expr, date))

        due |> should equal 59000
        interval |> should equal 60000

    [<Fact>]
    let ``Should not throw on failing jobs`` () =
       timerCallback("trowing exception", JobCallback(throwingCallback), tokenSource.Token)
       timerCallback("throwing OperationCanceledException from job", JobCallback(cancellingCallback), tokenSource.Token)
       tokenSource.Cancel()
       timerCallback("throwing OperationCanceledException from timerCallback", JobCallback(callback), tokenSource.Token)


[<Trait("Trigger", "Integration Test")>] 
type TriggerSystemTest() =

    let trigger = new Trigger("job#1", "* * * * *", JobCallback(callback)) :> ITrigger
    let overlapTrigger = new Trigger("job#1", "* * * * *", JobCallback(overlapCallback)) :> ITrigger
    let throwingTrigger = new Trigger("job#1", "* * * * *", JobCallback(throwingCallback)) :> ITrigger
    let cancellingTrigger = new Trigger("job#1", "* * * * *", JobCallback(cancellingCallback)) :> ITrigger

    let triggerStateCallback = TriggerStateCallback(fun _ -> trigger.State)
    let throwingStateCallback = TriggerStateCallback(fun _ -> trigger.State)

    [<Fact>]
    let ``Should not throw on failing jobs`` () =
        throwingTrigger.Fire() |> ignore
        cancellingTrigger.Fire() |> ignore

    [<Fact>]
    let ``Should handle overlaping jobs`` () =
        overlapTrigger.Start() |> ignore
        let firstOccurance = overlapTrigger.OccurrenceAt
        wait firstOccurance
       
        let secondOccurance = overlapTrigger.OccurrenceAt
        wait secondOccurance

        Console.WriteLine("firstOccurance: {0}", firstOccurance)
        Console.WriteLine("secondOccurance: {0}", secondOccurance)
        Console.WriteLine("diff: {0}", (secondOccurance - firstOccurance))

        (secondOccurance - firstOccurance).Minutes |> should equal 1
        overlapTrigger.Terminate()

    [<Fact>]
    let ``Should change the state durring execution`` () =
        waitForState triggerStateCallback Stopped
        trigger.Start() |> ignore 
        waitForState triggerStateCallback Idle
        waitForState triggerStateCallback Executing
        trigger.Stop() |> ignore 
        waitForState triggerStateCallback Stopped
        trigger.Terminate() |> ignore
        waitForState triggerStateCallback Terminated 

    [<Fact>]
    let ``Should not allow to enable / disable/ fire terminate when trigger is disposed`` () =
        trigger.Start() |> ignore
        trigger.Dispose() |> ignore
        waitForState triggerStateCallback Terminated
        trigger.Start() |> ignore
        waitForState triggerStateCallback Terminated
        trigger.Stop() |> ignore
        waitForState triggerStateCallback Terminated
        trigger.Terminate() |> ignore
        waitForState triggerStateCallback Terminated  
        trigger.Fire() |> ignore
        waitForState triggerStateCallback Terminated   

    [<Fact>]
    let ``Should be able to Fire when terminated``() =
        trigger.Terminate() |> ignore
        trigger.Fire() |> ignore
        trigger.State |> should equal Terminated

    [<Fact>]
    let ``Should not change current state when Firing trigger`` () =
        trigger.State |> should equal Stopped
        trigger.Fire() |> ignore
        trigger.State |> should equal Stopped