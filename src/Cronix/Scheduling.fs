namespace Cronix

/// Module responsible for managing the job schedules.
module Scheduling =

    open Triggers
    open Chessie.ErrorHandling
    open NCrontab 
    open Logging
    open Cronix
    open Messages

    /// Creates the job trigger
    let createTrigger : CreateTrigger =
        fun(name, expr, callback) ->
            new Trigger(name, expr, callback) :> ITrigger

    /// Validates the job cron expression
    let validateExpr (state : ScheduleState, params' : ScheduleParams) =
        let _, expr, _ = params'
        try
            CrontabSchedule.Parse(expr).GetNextOccurrence |> ignore
            SuccesMessage(state, params') ValidateExpr [expr]
        with
        | _ -> FailureMessage InvalidCronExpr [expr]
    
    /// Checks if job can be added.
    let canAddJob (state : ScheduleState, params' : ScheduleParams) =
        let name, _, _ = params'
        match state.ContainsKey name with
        | true -> FailureMessage JobExists [name]
        | false -> SuccesMessage(state, params') CanAddJob [name]
 
    /// Adds the job.
    let addJob(state : ScheduleState, params' : ScheduleParams) =
        let name, expr, callback = params'
        state.Add(name, { Name = name; CronExpr = expr; Trigger = createTrigger(name, expr, callback); })
        state.[name].Trigger.Start()
        SuccesMessage(state) AddJob [name]

    /// Checks if given job exists.
    let jobExists (state : ScheduleState, name : string) = 
         match state.ContainsKey name with
         | false -> FailureMessage JobNotExists [name]
         | true -> SuccesMessage(state, name) JobFound [name]

    /// Removes the job.
    let removeJob (state : ScheduleState, name : string) =
        state.[name].Trigger.Terminate() |> ignore
        state.Remove name |> ignore
        SuccesMessage(state) RemoveJob [name]
    
    /// Stops given job execution.
    let stopJob (state : ScheduleState, name : string) = 
        state.[name].Trigger.Stop()
        SuccesMessage(state) StopJob [name]

    /// Starts given job.
    let startJob (state : ScheduleState, name : string) = 
        state.[name].Trigger.Start()
        SuccesMessage(state) StartJob [name]

    /// Schedules  given job. This method performs cron expression validation, job existance check, job adding and logs the output result.
    let schedule : Schedule = 
        fun (state, params') ->
            ok (state, params')
            >>= validateExpr
            >>= canAddJob
            >>= addJob 
            |> logResult

    /// Unschedules given job. This method performs job existance check, job removal and logs the output result.
    let unschedule : UnSchedule = 
        fun(state, name) ->
            ok (state, name)
            >>= jobExists
            >>= removeJob
            |> logResult 
    
    /// Stops given job. This method performs job existance check, stops the job and logs the output result.
    let stop : Stop =
        fun(state, name) ->
            ok (state, name)
            >>= jobExists
            >>= stopJob
            |> logResult

    /// Starts given job. This method performs job existane check, starts the job and logs the output result.
    let start : Start =
        fun(state, name) ->
            ok (state, name)
            >>= jobExists
            >>= startJob
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
                    | Schedule (name, expr, callback) -> schedule(state, (name, expr, callback)) |> reply replyChannel |> ignore
                    | Stop name                       -> stop(state, name) |> reply replyChannel |> ignore
                    | Start name                      -> start(state, name) |> reply replyChannel |> ignore
                    | UnSchedule name                 -> unschedule(state, name) |> reply replyChannel |> ignore
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
            agent.PostAndAsyncReply(fun replyChannel -> (Schedule( name, expr, callback), replyChannel))
            |> Async.RunSynchronously

        member self.UnSchedule name =
            agent.PostAndAsyncReply(fun replyChannel -> (UnSchedule(name), replyChannel))
            |> Async.RunSynchronously

        member self.State =
            state |> Seq.map(fun i -> { Name = i.Value.Name; CronExpr = i.Value.CronExpr; TriggerState = i.Value.Trigger.State })

        member self.Stop() = 
            logger.Debug("stopping agent")
            stop()
        member self.Start() = 
            logger.Debug("starting agent")
            agent.Start()
  



