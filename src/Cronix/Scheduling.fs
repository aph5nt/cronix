namespace Cronix

module Scheduling =

    open Triggers
    open Chessie.ErrorHandling
    open NCrontab 
    open Logging
    open Cronix
    open Messages

    let createTrigger : CreateTrigger =
        fun(name, expr, callback) ->
            new Trigger(name, expr, callback) :> ITrigger

    let validateExpr (state : ScheduleState, params' : ScheduleParams) =
        let _, expr, _ = params'
        try
            CrontabSchedule.Parse(expr).GetNextOccurrence |> ignore
            SuccesMessage(state, params') ValidateExpr [expr]
        with
        | _ -> FailureMessage InvalidCronExpr [expr]

    let canAddJob (state : ScheduleState, params' : ScheduleParams) =
        let name, _, _ = params'
        match state.ContainsKey name with
        | true -> FailureMessage JobExists [name]
        | false -> SuccesMessage(state, params') CanAddJob [name]
 
    let addJob(state : ScheduleState, params' : ScheduleParams) =
        let name, expr, callback = params'
        state.Add(name, { Name = name; CronExpr = expr; Trigger = createTrigger(name, expr, callback); })
        state.[name].Trigger.Start()
        SuccesMessage(state) AddJob [name]

    let jobExists (state : ScheduleState, name : string) = 
         match state.ContainsKey name with
         | false -> FailureMessage JobNotExists [name]
         | true -> SuccesMessage(state, name) JobFound [name]

    let removeJob (state : ScheduleState, name : string) =
        state.[name].Trigger.Terminate() |> ignore
        state.Remove name |> ignore
        SuccesMessage(state) RemoveJob [name]
    
    let stopJob (state : ScheduleState, name : string) = 
        state.[name].Trigger.Stop()
        SuccesMessage(state) StopJob [name]

    let startJob (state : ScheduleState, name : string) = 
        state.[name].Trigger.Start()
        SuccesMessage(state) StartJob [name]

    let schedule : Schedule = 
        fun (state, params') ->
            ok (state, params')
            >>= validateExpr
            >>= canAddJob
            >>= addJob 
            |> logResult

    let unschedule : UnSchedule = 
        fun(state, name) ->
            ok (state, name)
            >>= jobExists
            >>= removeJob
            |> logResult 
     
    let stop : Stop =
        fun(state, name) ->
            ok (state, name)
            >>= jobExists
            >>= stopJob
            |> logResult

    let start : Start =
        fun(state, name) ->
            ok (state, name)
            >>= jobExists
            >>= startJob
            |> logResult

    let notify (reply : AsyncReplyChannel<'a>) (msg : 'a) =
        reply.Reply(msg)

open System.Threading
open System
open System.Collections.Generic
open Cronix
open Scheduling
open Logging

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
                    | Schedule (name, expr, callback) -> schedule(state, (name, expr, callback)) |> notify replyChannel |> ignore
                    | Stop name                       -> stop(state, name) |> notify replyChannel |> ignore
                    | Start name                      -> start(state, name) |> notify replyChannel |> ignore
                    | UnSchedule name                 -> unschedule(state, name) |> notify replyChannel |> ignore
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

        // to version 0.2 !!
        // todo: stop job
        // todo: start job
    
    // todo: missing stopJob will it be scheduled later? or hide this feature for 0.1?
  



