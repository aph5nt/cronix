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
            SuccesMessage(state, params') ExpressionValidated [expr]
        with
        | _ -> FailureMessage InvalidCronExpr [expr]
    
    /// Checks if a job trigger can be added.
    let canAddTrigger (state : ScheduleState, params' : ScheduleParams) =
        let name, _, _ = params'
        match state.ContainsKey name with
        | true -> FailureMessage TriggerExists [name]
        | false -> SuccesMessage(state, params') TriggerCanBeAdded [name]
 
    /// Adds the job to the trigger.
    let addTrigger(state : ScheduleState, params' : ScheduleParams) =
        let name, expr, callback = params'
        state.Add(name, createTrigger(name, expr, callback))
        state.[name].Start()
        SuccesMessage(state) TriggerAdded [name]

    /// Checks if given job exists.
    let triggerExists (state : ScheduleState, name : string) = 
         match state.ContainsKey name with
         | false -> FailureMessage TriggerNotExists [name]
         | true -> SuccesMessage(state, name) TriggerFound [name]

    /// Removes given trigger.
    let removeTrigger (state : ScheduleState, name : string) =
        state.[name].Terminate() |> ignore
        state.[name].PublishRemovedEvent() |> ignore
        state.Remove name |> ignore
        SuccesMessage(state) TriggerRemoved [name]
    
    /// Stops given trigger execution.
    let stopTrigger (state : ScheduleState, name : string) = 
        state.[name].Stop()
        SuccesMessage(state) TriggerDisabled [name]

    /// Starts given trigger.
    let startTrigger (state : ScheduleState, name : string) = 
        state.[name].Start()
        SuccesMessage(state) TriggerEnabled [name]

    /// Fires given trigger.
    let fireTrigger (state : ScheduleState, name : string) = 
        state.[name].Fire()
        SuccesMessage(state) TriggerFired [name]

      /// Fires given trigger.
    let terminateTrigger (state : ScheduleState, name : string) = 
        state.[name].Terminate()
        SuccesMessage(state) TriggerTerminated [name]
        
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
            >>= triggerExists
            >>= removeTrigger
            |> logResult 
    
    /// Stops given job. This method performs job existance check, stops the job and logs the output result.
    let disableTrigger : Stop =
        fun(state, name) ->
            ok (state, name)
            >>= triggerExists
            >>= stopTrigger
            |> logResult

    /// Starts given job. This method performs job existane check, starts the job and logs the output result.
    let enableTrigger : Start =
        fun(state, name) ->
            ok (state, name)
            >>= triggerExists
            >>= startTrigger
            |> logResult

    /// Fires given job. This method performs job existane check, starts the job and logs the output result.
    let fireTriggerR : Fire =
        fun(state, name) ->
            ok (state, name)
            >>= triggerExists
            >>= fireTrigger
            |> logResult

     /// Fires given job. This method performs job existane check, starts the job and logs the output result.
    let terminateTriggerR : Fire =
        fun(state, name) ->
            ok (state, name)
            >>= triggerExists
            >>= terminateTrigger
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

    let bubbleUpStateChanged = new Event<TriggerDetail>()
    let state = new ScheduleState()

    do
        state.OnStateChanged <- (fun sender args -> 
            let key, _ = args
            let findJobState() = 
                    state 
                    |> Seq.map(fun i -> { Name = i.Value.Name; CronExpr = i.Value.CronExpr; TriggerState = i.Value.State; NextOccuranceDate = i.Value.OccurrenceAt })
                    |> Seq.find (fun(i) -> i.Name = key)
            bubbleUpStateChanged.Trigger(findJobState()))

    let tokenSource = new CancellationTokenSource()
    let agent = new MailboxProcessor<_>((fun inbox ->
        let loop = 
            async {
                while true do 
                    let! (message, replyChannel) = inbox.Receive()
                    match message with
                    | ScheduleJob (name, expr, callback) -> scheduleJob(state, (name, expr, callback)) |> reply replyChannel |> ignore
                    | UnScheduleJob name                 -> unscheduleJob(state, name) |> reply replyChannel |> ignore
                    | DisableTrigger name                -> disableTrigger(state, name) |> reply replyChannel |> ignore
                    | EnableTrigger name                 -> enableTrigger(state, name) |> reply replyChannel |> ignore
                    | FireTrigger name                   -> fireTriggerR(state, name) |> reply replyChannel |> ignore
                    | TerminateTrigger name              -> terminateTriggerR(state, name) |> reply replyChannel |> ignore
            }
        loop
    ), tokenSource.Token)

    let stop() =
        tokenSource.Cancel() |> ignore      // stops the agent
        for KeyValue(name, trigger) in state do
            trigger.Terminate() |> ignore

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
        member self.ScheduleJob name expr callback = 
            agent.PostAndAsyncReply(fun replyChannel -> (ScheduleJob( name, expr, callback), replyChannel))
            |> Async.RunSynchronously

        member self.UnScheduleJob name =
            agent.PostAndAsyncReply(fun replyChannel -> (UnScheduleJob(name), replyChannel))
            |> Async.RunSynchronously
        
        member self.EnableTrigger name = 
             agent.PostAndAsyncReply(fun replyChannel -> (EnableTrigger(name), replyChannel))
            |> Async.RunSynchronously

        member self.DisableTrigger name = 
             agent.PostAndAsyncReply(fun replyChannel -> (DisableTrigger(name), replyChannel))
            |> Async.RunSynchronously

        member x.FireTrigger name =
             agent.PostAndAsyncReply(fun replyChannel -> (FireTrigger(name), replyChannel))
            |> Async.RunSynchronously

         member x.TerminateTriggerExecution name =
             agent.PostAndAsyncReply(fun replyChannel -> (TerminateTrigger(name), replyChannel))
            |> Async.RunSynchronously

        member self.TriggerDetails =
            state |> Seq.map(fun i -> { Name = i.Value.Name; CronExpr = i.Value.CronExpr; TriggerState = i.Value.State; NextOccuranceDate = i.Value.OccurrenceAt })
        
        member x.OnTriggerStateChanged
                with get() = bubbleUpStateChanged.Publish

        member self.StopManager() = 
            logger.Debug("stopping agent")
            stop()
        member self.StartManager() = 
            logger.Debug("starting agent")
            agent.Start()
  



