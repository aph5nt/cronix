namespace Cronix

/// Module responsible for managing the job schedules.
module Scheduling =

    open Triggers
    open Chessie.ErrorHandling
    open NCrontab 
    open Logging
    open Cronix

    /// Creates the job trigger
    let createTrigger : CreateTrigger =
        fun(name, expr, callback) ->
            new Trigger(name, expr, callback) :> ITrigger

    /// Validates the job cron expression
    let validateExpr (state : ScheduleState, params' : ScheduleParams) =
        let _, expr, _ = params'
        try
            CrontabSchedule.Parse(expr).GetNextOccurrence |> ignore
            SuccesMessage(state, params') ValidateExprMsg [expr]
        with
        | _ -> FailureMessage InvalidCronExpr [expr]
    
    /// Checks if a job trigger can be added.
    let canAddTrigger (state : ScheduleState, params' : ScheduleParams) =
        let name, _, _ = params'
        match state.ContainsKey name with
        | true -> FailureMessage JobExists [name]
        | false -> SuccesMessage(state, params') CanAddJobMsg [name]
 
    /// Adds the job to the trigger.
    let addTrigger(state : ScheduleState, params' : ScheduleParams) =
        let name, expr, callback = params'
        state.Add(name, { Name = name; CronExpr = expr; Trigger = createTrigger(name, expr, callback); })
        state.[name].Trigger.Start()
        SuccesMessage(state) AddJobMsg [name]

    /// Checks if given job exists.
    let jobExists (state : ScheduleState, name : string) = 
         match state.ContainsKey name with
         | false -> FailureMessage JobNotExists [name]
         | true -> SuccesMessage(state, name) JobFoundMsg [name]

    /// Removes given trigger.
    let removeTrigger (state : ScheduleState, name : string) =
        state.[name].Trigger.Terminate() |> ignore
        state.Remove name |> ignore
        SuccesMessage(state) RemoveJobMsg [name]
    
    /// Stops given trigger execution.
    let stopTrigger (state : ScheduleState, name : string) = 
        state.[name].Trigger.Stop()
        SuccesMessage(state) StopJobMsg [name]

    /// Starts given trigger.
    let startTrigger (state : ScheduleState, name : string) = 
        state.[name].Trigger.Start()
        SuccesMessage(state) StartJobMsg [name]

    /// Schedules  given job. This method performs cron expression validation, job existance check, job adding and logs the output result.
    let scheduleJob : Schedule = 
        fun (state, params') ->
            ok (state, params')
            >>= validateExpr
            >>= canAddTrigger
            >>= addTrigger
            |> logResult

    /// Unschedules given job. This method performs job existance check, job removal and logs the output result.
    let unscheduleJob : UnSchedule = 
        fun(state, name) ->
            ok (state, name)
            >>= jobExists
            >>= removeTrigger
            |> logResult 
    
    /// Stops given job. This method performs job existance check, stops the job and logs the output result.
    let stopJob : Stop =
        fun(state, name) ->
            ok (state, name)
            >>= jobExists
            >>= stopTrigger
            |> logResult

    /// Starts given job. This method performs job existane check, starts the job and logs the output result.
    let startJob : Start =
        fun(state, name) ->
            ok (state, name)
            >>= jobExists
            >>= startTrigger
            |> logResult

    /// Replies the message 
    let reply (reply : AsyncReplyChannel<'a>) (msg : 'a) =
        reply.Reply(msg)

open System.Threading
open System
open System.Collections.Generic
open Cronix
open Scheduling
open Logging

/// Mannages the scheduled jobs.
type ScheduleManager() = 
    let logger = logger()
    let mutable disposed = false;
    let state = new Dictionary<string, Job>()
    let tokenSource = new CancellationTokenSource()
    let agent = new MailboxProcessor<_>((fun inbox ->
        let loop = 
            async {
                while true do 
                    let! (message, replyChannel) = inbox.Receive()
                    match message with
                    | ScheduleJob (name, expr, callback) -> scheduleJob(state, (name, expr, callback)) |> reply replyChannel |> ignore
                    | StopJob name                       -> stopJob(state, name) |> reply replyChannel |> ignore
                    | StartJob name                      -> startJob(state, name) |> reply replyChannel |> ignore
                    | UnScheduleJob name                 -> unscheduleJob(state, name) |> reply replyChannel |> ignore
            }
        loop
    ), tokenSource.Token)

    let stop() =
        tokenSource.Cancel() |> ignore      // stops the agent
        for KeyValue(k, v) in state do
            v.Trigger.Terminate() |> ignore

     // internal method to cleanup resources
    let cleanup(disposing : bool) = 
        if not disposed then
            disposed <- true
            if disposing then stop()

    interface IDisposable with
        member self.Dispose() =
            cleanup(true)
            GC.SuppressFinalize(self)

    override self.Finalize() = 
        cleanup(false)

    interface IScheduleManager with
        member self.Schedule name expr callback = 
            agent.PostAndAsyncReply(fun replyChannel -> (ScheduleJob( name, expr, callback), replyChannel))
            |> Async.RunSynchronously

        member self.UnSchedule name =
            agent.PostAndAsyncReply(fun replyChannel -> (UnScheduleJob(name), replyChannel))
            |> Async.RunSynchronously
        
        member self.StartJob name = 
             agent.PostAndAsyncReply(fun replyChannel -> (StartJob(name), replyChannel))
            |> Async.RunSynchronously

        member self.StopJob name = 
             agent.PostAndAsyncReply(fun replyChannel -> (StopJob(name), replyChannel))
            |> Async.RunSynchronously

        member self.State =
            state |> Seq.map(fun i -> { Name = i.Value.Name; CronExpr = i.Value.CronExpr; TriggerState = i.Value.Trigger.State })

        member self.Stop() = 
            logger.Debug("stopping agent")
            stop()
        member self.Start() = 
            logger.Debug("starting agent")
            agent.Start()
  



