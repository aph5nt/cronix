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
    let mutable state = new Dictionary<string, Job>()

    let compareResults (actual : Result<'TSuccess, 'TMessage>) (expected : Result<'TSuccess, 'TMessage>)  =
       actual |> should equal expected

    let callback (token : CancellationToken) = 
       printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
       Thread.Sleep 100

    [<Fact>]
    let ``validate exprArgValid expression``() = 
        let params' = (jobName1, exprArgValid, Callback(callback))
        let actual = validateExpr(state, params')
        let expected = SuccesMessage(state, params') ValidateExprMsg [exprArgValid]
        compareResults actual expected |> ignore

    [<Fact>]
    let ``validate exprArgInValid expression``() = 
        let params' = (jobName1, exprArgInValid, Callback(callback))
        let actual = validateExpr(state, params')
        let expected = FailureMessage InvalidCronExpr [exprArgInValid]
        compareResults actual expected |> ignore     


    [<Fact>]
    let ``can add job``() =
        let params' = (jobName1, exprArgValid, Callback(callback))
        let actual = canAddTrigger(state, params') 
        let expected = SuccesMessage(state, params') CanAddJobMsg [jobName1]
        compareResults actual expected |> ignore
    
        state.Add(jobName1, { Name = jobName1; CronExpr = exprArgValid; Trigger = createTrigger(jobName1, exprArgValid, Callback(callback)) })

        let actual2 = canAddTrigger(state, params') 
        let expected2 = FailureMessage JobExists [jobName1]
        compareResults actual2 expected2 |> ignore

    [<Fact>]
    let ``add job``() =
        let params' = (jobName1, exprArgValid, Callback(callback))
        let actual = addTrigger(state, params') 
        let expected = SuccesMessage(state) AddJobMsg [jobName1]
        compareResults actual expected |> ignore

[<Trait("Scheduling", "Integration Test")>]
type ScheduleManagerTests() =
    let manager = new ScheduleManager() :> IScheduleManager
    let sampleJob (token : CancellationToken) = 
        printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
        Thread.Sleep 100
    do
        manager.Start()

    [<Fact>]
    let ``schedule a job``() =
        let result = manager.Schedule "job1" "* * * * *" <| Callback(sampleJob)
        match result with
        | Ok (_, msgs) -> 
            msgs |> should contain "Expr <[* * * * *]> is valid."
            msgs |> should contain "Job <[job1]> can be added."
            msgs |> should contain "Job <[job1]> has been added."
        | _ -> failwith "Expected Success Tee"


    [<Fact>]
    let ``schedule not the same job twice``() =
        manager.Schedule "job1" "* * * * *" <| Callback(sampleJob) |> ignore
        let result = manager.Schedule "job1" "* * * * *" <| Callback(sampleJob)
        match result with
        | Fail msgs ->  msgs |> should contain "Job <[job1]> already exists."
        | _  -> failwith "Expected Failure Tee"    

    [<Fact>]
    let ``schedule not job with invalid expression``() =
        let result = manager.Schedule "job1" "a * * * *" <| Callback(sampleJob)
        match result with
        | Fail msgs ->  msgs |> should contain "Expr <[a * * * *]> is not valid."
        | _ -> failwith "Expected Failure Tee" 

    [<Fact>]
    let ``unschedule a job``() =
        manager.Schedule "job1" "* * * * *" <| Callback(sampleJob) |> ignore
        let result = manager.UnSchedule "job1"
        match result with
        | Ok (_, msgs) -> 
            msgs |> should contain "Job <[job1]> found."
            msgs |> should contain "Job <[job1]> has been removed."
        | _ -> failwith "Expected Success Tee"

    [<Fact>]
    let ``unschedule not existing job``() =
        let result = manager.UnSchedule "job1"
        match result with
        | Fail msgs ->  msgs |> should contain "Job <[job1]> does not exists."
        | _ -> failwith "Expected Failure Tee" 

    [<Fact>]
    let ``stop and start a job``() =
        let result = manager.Schedule "job1" "a * * * *" <| Callback(sampleJob)
        let stopped = manager.StopJob("job1")
        Thread.Sleep(200)
        match stopped with
        | Ok (_, msgs) -> 
             msgs |> should contain "Job <[job1]> has been stopped."
        | _ -> failwith "Expected Success Tee"

        let started = manager.StartJob("job1")
        match started with
        | Ok (_, msgs) -> 
             msgs |> should contain "Job <[job1]> has been started."
        | _ -> failwith "Expected Success Tee"

    [<Fact>]
    let ``get manager state``() =
         manager.Schedule "job1" "* * * * *" <| Callback(sampleJob) |> ignore
         manager.State |> Seq.toArray |> Array.find(fun(i) -> i.Name = "job1") |> should not' Null

    interface IDisposable with
        member x.Dispose() = manager.Stop()     