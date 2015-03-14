namespace ``Scheduling tests ``
//    
//    open System
//    open System.Threading   
//    open System.Collections.Generic
//    open FsUnit
//    open NUnit.Framework
//    open Cronix
//    open Messages
//    open Scheduling
//    open TestHelpers
//
//
//    [<TestFixture>]
//    [<Category("Scheduling Rop")>]
//    type SchedulingRopTests() =
//    
//        let jobName1 = "jobname1"
//        let mutable state = new Dictionary<string, Job>()
//
//        [<SetUp>]
//        member public x.``run before test``() =
//            state = new Dictionary<string, Job>()
//
//        [<TestCase("* * * * *", true)>]
//        [<TestCase("* * * * a", false)>]
//        member x.``validateExpr test``(exprArg, isValid) = 
//            let params' = (jobName1, exprArg, Callback(callback))
//            let actual = validateExpr(state, params') 
//            if isValid then
//                let expected = SuccesMessage(state, params') ValidateExpr [exprArg]
//                compareResults actual expected |> ignore
//            else
//                let expected = FailureMessage InvalidCronExpr [exprArg]
//                compareResults actual expected |> ignore     
//
//        [<Test>]    
//        member x.``canAddJob test``() =
//            let params' = (jobName1, "* * * * *", Callback(callback))
//            let actual = canAddJob(state, params') 
//            let expected = SuccesMessage(state, params') CanAddJob [jobName1]
//            compareResults actual expected |> ignore
//            
//    
//            state.Add(jobName1, { Name = jobName1; CronExpr = "* * * * *"; Trigger = createTrigger(jobName1, "* * * * *", Callback(callback)) })
//
//            let actual2 = canAddJob(state, params') 
//            let expected2 = FailureMessage JobExists [jobName1]
//            compareResults actual2 expected2 |> ignore
//
//        [<Test>]
//        member x.``addJob test``() =
//            let params' = (jobName1, "* * * * *", Callback(callback))
//            let actual = addJob(state, params') 
//            let expected = SuccesMessage(state) AddJob [jobName1]
//            compareResults actual expected |> ignore
//
//
//
//    
// 
//            
////    [<Fact>]
////    let ``Should schedule a job and start trigger 1`` () =
////        schedule(state, (jobName1, expr, Callback(callback))) |> ignore
////        state.ContainsKey jobName1 |> should be True 
////        state.[jobName1].Trigger.State |> should equal Idle
////
////    [<Fact>]
////    let ``Should schedule a job and start trigger 2`` () =
////        schedule(state, (jobName1, expr, Callback(callback))) |> ignore
////        schedule(state, (jobName1, expr, Callback(callback))) |> ignore
////        // railway programming!! return failed result here !!
////        // for each function -. seperate test case (trait)
////
////
////[<TraitAttribute("Scheduling", "Long running")>] 
////type SchedulingScenarios() = 
////
////    //let manager = new ScheduleManager() :> IScheduleManager
////
////    let callback (token : CancellationToken) = 
////       Thread.Sleep 100
////     
////    [<Fact>]
////    let ``Schedule single task`` () =
////       
////       //manager.Schedule("job1", "* * * * *", Callback(callback))
////       
////       Thread.Sleep 1000
////
////     //  manager.State() 
////      // |> Seq.iter (fun state -> printf "name: %s" state.Name)
////           
////     //  manager.Stop()
////
////
////
////
////       // todo:
////      
////       // unittest mangaer; aading multiple jobs at at same time (concurrent); andd terminating
////       // - triggert with callback with cancelation tokens
////       // - move types to types files
////       // - add interfaces for triggers and schedulemanger
////       // add file definitions
////       // 