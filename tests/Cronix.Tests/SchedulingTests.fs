namespace Cronix.Tests
 
open System
open System.Threading   
open System.Collections.Generic
open Xunit
open FsUnit.Xunit
open Cronix
open Scheduling
open Chessie.ErrorHandling
    
   
[<Trait("Scheduling", "Unit Test")>]
type SchedulingTests() =
    let jobName1 = "jobname1"
    let exprArgValid = "* * * * *"
    let exprArgInValid = "0/15 * * * * *"
    let mutable state = new ScheduleState()

    let compareResults (actual : Result<'TSuccess, 'TMessage>) (expected : Result<'TSuccess, 'TMessage>)  =
       actual |> should equal expected

    let callback (token : CancellationToken) = 
       printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
       Thread.Sleep 300

    [<Fact>]
    let ``validate exprArgValid expression``() = 
        let params' = (jobName1, exprArgValid, JobCallback(callback))
        let actual = validateExpr(state, params')
        let expected = SuccesMessage(state, params') ExpressionValidated [exprArgValid]
        compareResults actual expected |> ignore

    [<Fact>]
    let ``validate exprArgInValid expression``() = 
        let params' = (jobName1, exprArgInValid, JobCallback(callback))
        let actual = validateExpr(state, params')
        let expected = FailureMessage InvalidCronExpr [exprArgInValid]
        compareResults actual expected |> ignore     


    [<Fact>]
    let ``can add trigger``() =
        let params' = (jobName1, exprArgValid, JobCallback(callback))
        let actual = canAddTrigger(state, params') 
        let expected = SuccesMessage(state, params') TriggerCanBeAdded [jobName1]
        compareResults actual expected |> ignore
    
        state.Add(jobName1, createTrigger(jobName1, exprArgValid, JobCallback(callback)))

        let actual2 = canAddTrigger(state, params') 
        let expected2 = FailureMessage TriggerExists [jobName1]
        compareResults actual2 expected2 |> ignore

    [<Fact>]
    let ``add trigger``() =
        let params' = (jobName1, exprArgValid, JobCallback(callback))
        let actual = addTrigger(state, params') 
        let expected = SuccesMessage(state) TriggerAdded [jobName1]
        compareResults actual expected |> ignore

    [<Fact>]
    let ``fire trigger``() = 
         let params' = (jobName1, exprArgValid, JobCallback(callback))
         addTrigger(state, params') |> ignore
         let actual = fireTrigger(state, jobName1) 
         let expected = SuccesMessage(state) TriggerFired [jobName1]
         compareResults actual expected |> ignore
        

    [<Fact>]
    let ``terminate trigger``() = 
         let params' = (jobName1, exprArgValid, JobCallback(callback))
         addTrigger(state, params') |> ignore
         fireTrigger(state, jobName1) |> ignore
         let actual = terminateTrigger(state, jobName1)
         let expected = SuccesMessage(state) TriggerTerminated [jobName1]
         compareResults actual expected |> ignore

    [<Fact>]
    let ``trigger should do the cleanup when disposing``() = 
        let params' = (jobName1, exprArgValid, JobCallback(callback))
        addTrigger(state, params') |> ignore
        state.[jobName1].State |> should equal Idle
        state.[jobName1].Dispose()
        state.[jobName1].State |> should equal Terminated

[<Trait("Scheduling", "Integration Test")>]
type ScheduleManagerTests() =
    let manager = new ScheduleManager() :> IScheduleManager
    let mutable triggerState = Idle
    let stateChangedHandler = new Handler<(TriggerDetail)>(fun sender args -> triggerState <- args.TriggerState)
    let sampleJob (token : CancellationToken) = 
        printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
        Thread.Sleep 100
    do
        manager.StartManager()

    [<Fact>]
    let ``schedule a job``() =
        let result = manager.ScheduleJob "job1" 
                     <| "* * * * *" 
                     <| JobCallback(sampleJob)

        match result with
        | Ok (_, msgs) -> 
            msgs |> should contain "Expr <[* * * * *]> is valid."
            msgs |> should contain "Trigger <[job1]> can be added."
            msgs |> should contain "Trigger <[job1]> has been added."
        | _ -> failwith "Expected Success Tee"
    

    [<Fact>]
    let ``schedule not the same job twice``() =
        manager.ScheduleJob "job1" 
        <| "* * * * *" 
        <| JobCallback(sampleJob)  
         |> ignore

        let result = manager.ScheduleJob "job1" "* * * * *" <| JobCallback(sampleJob)
        match result with
        | Fail msgs ->  msgs |> should contain "Job <[job1]> already exists."
        | _  -> failwith "Expected Failure Tee"    

    [<Fact>]
    let ``schedule not job with invalid expression``() =
        let result = manager.ScheduleJob "job1"
                     <| "a * * * *"
                     <| JobCallback(sampleJob)

        match result with
        | Fail msgs ->  msgs |> should contain "Expr <[a * * * *]> is not valid."
        | _ -> failwith "Expected Failure Tee" 

    [<Fact>]
    let ``unschedule a job``() =
        manager.ScheduleJob "job1" 
        <| "* * * * *"
        <| JobCallback(sampleJob) 
        |> ignore

        let result = manager.UnScheduleJob "job1"
        match result with
        | Ok (_, msgs) -> 
            msgs |> should contain "Trigger <[job1]> found."
            msgs |> should contain "Trigger <[job1]> has been removed."
        | _ -> failwith "Expected Success Tee"
        
    [<Fact>]
    let ``unschedule not existing job``() =
        let result = manager.UnScheduleJob "job1"
        match result with
        | Fail msgs ->  msgs |> should contain "Job <[job1]> does not exists."
        | _ -> failwith "Expected Failure Tee" 

    [<Fact>]
    let ``enable and disable trigger``() =
        manager.ScheduleJob "job1" 
                <| "* * * * *" 
                <| JobCallback(sampleJob)
                 |> ignore

        let stopped = manager.DisableTrigger("job1")
        match stopped with
        | Ok (_, msgs) -> 
             msgs |> should contain "Trigger <[job1]> has been disabled."
        | _ -> failwith "Expected Success Tee"

        let result = manager.EnableTrigger("job1")
        match result with
        | Ok (_, msgs) -> 
             msgs |> should contain "Trigger <[job1]> has been enabled."
        | _ -> failwith "Expected Success Tee"

    [<Fact>]
    let ``get trigger details``() =
         manager.ScheduleJob "job1" 
         <| "* * * * *"
         <| JobCallback(sampleJob)
          |> ignore

         manager.TriggerDetails |> Seq.toArray |> Array.find(fun(i) -> i.Name = "job1") |> should not' Null
    
    [<Fact>]
    let ``fire trigger``() = 
        manager.ScheduleJob "job1" 
                     <| "* * * * *" 
                     <| JobCallback(sampleJob)
                      |> ignore
        let result = manager.FireTrigger("job1")
        match result with
        | Ok (_, msgs) -> 
             msgs |> should contain "Trigger <[job1]> has been fired."
        | _ -> failwith "Expected Success Tee"

    [<Fact>]
    let ``terminate trigger execution``() = 
        manager.ScheduleJob "job1" 
                     <| "* * * * *" 
                     <| JobCallback(sampleJob)
                      |> ignore
        manager.FireTrigger("job1") |> ignore

        let result = manager.TerminateTriggerExecution("job1")
        match result with
        | Ok (_, msgs) -> 
             msgs |> should contain "Trigger <[job1]> has been terminated."
        | _ -> failwith "Expected Success Tee"

    [<Fact>]
    let ``handle OnTriggerStateChanged event``() = 
         triggerState <- Terminated
         manager.OnTriggerStateChanged.AddHandler(stateChangedHandler)
         manager.ScheduleJob "job1" 
                     <| "* * * * *" 
                     <| JobCallback(sampleJob)
                      |> ignore
         manager.FireTrigger("job1") |> ignore
         Thread.Sleep(1000)
         triggerState |> should equal Idle
         

    interface IDisposable with
        member x.Dispose() = manager.StopManager()     