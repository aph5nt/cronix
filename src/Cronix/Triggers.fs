namespace Cronix

/// Module responsible for triggering the jobs.
module Triggers =

    open System.Threading
    open NCrontab 
    open System
    open Logging 
 
    /// Trigger module specific logger.
    let logger = logger()

    /// Calculates the timer arguments.
    let calculateTimerArgs : CalculateTimerArgs =
        fun(expr, date, occurrenceAt) ->
            let nextOccurence = CrontabSchedule.Parse(expr).GetNextOccurrence occurrenceAt
            let due = int (occurrenceAt - date).TotalMilliseconds
            let interval = int (nextOccurence - occurrenceAt).TotalMilliseconds
            logger.Debug("Calculating TimerArgs: due = <%d>; interval = <%d>.", due, interval)
            (due, interval)
   
    /// Calculates the occurance at time.
    let calculatetOccurrenceAt : CalculatetOccurrenceAt =
        fun(expr, date) ->
            CrontabSchedule.Parse(expr).GetNextOccurrence date
    
    /// Invokes the callback function.
    let timerCallback : Cronix.TimerCallback = 
        fun(name, callback, token) ->
            try
                token.ThrowIfCancellationRequested() |> ignore
                logger.Debug("Executing Trigger<'{0}'>", name)
                callback.Invoke token
                logger.Debug("Trigger<'{0}'> has been executed.", name)
            with 
            | :? OperationCanceledException -> logger.Debug("Trigger<'{0}'> has been cancelled", name)
            | _ as ex -> logger.Error("Trigger<'{0}'> failed", name, ex)
        
    /// Mannages the job execution.
    type Trigger(name: string, expr : string, jobCallback : JobCallback) as x =
        let mutable triggerState = Stopped
        let mutable occurrenceAt = DateTime.UtcNow
        let mutable isDisposed = false

        let stateChanged = Event<(TriggerName * TriggerState)>()
        let stateChangedHandler = new Handler<(TriggerName * TriggerState)>(fun sender args -> 
                                                                               let _, state = args
                                                                               triggerState <- state)
        do
            stateChanged.Publish.AddHandler(stateChangedHandler)

        let tokenSource = new CancellationTokenSource()

        let timer = new System.Threading.Timer(fun _ -> 
                stateChanged.Trigger(name, TriggerState.Executing)
                occurrenceAt <- CrontabSchedule.Parse(expr).GetNextOccurrence DateTime.UtcNow
                timerCallback(name, jobCallback, tokenSource.Token)    
                stateChanged.Trigger(name, TriggerState.Idle)
                )

        // internal method to cleanup resources
        let cleanup(disposing : bool) = 
            if not isDisposed then
                isDisposed <- true
                if disposing then
                    tokenSource.Cancel() |> ignore
                    timer.Dispose() |> ignore
                    stateChanged.Trigger(name, TriggerState.Terminated)
                    logger.Debug("Trigger<'{0}'> has been disposed.", name)

        interface IDisposable with
            member x.Dispose() =
                cleanup(true)
                GC.SuppressFinalize(x)

        interface ITrigger with 
            member x.State
                with get () = triggerState
    
            member x.OccurrenceAt
                with get () = occurrenceAt

            member x.Name
                with get() = name

            member x.CronExpr
                with get() = expr

            member x.OnStateChanged
                with get() = stateChanged.Publish

            member x.Start() =
                if isDisposed = false then
                    stateChanged.Trigger(name, TriggerState.Idle)            
                    occurrenceAt <- calculatetOccurrenceAt(expr, DateTime.UtcNow)
                    let due, interval = calculateTimerArgs(expr, DateTime.UtcNow, occurrenceAt)

                    timer.Change(due, interval) |> ignore
                    logger.Debug("Trigger<'{0}'> started. dueTime = {1}; interval '{2}'.", name, due, interval)

            member x.Stop() =
                if isDisposed = false then
                    timer.Change(Timeout.Infinite, Timeout.Infinite) |> ignore
                    stateChanged.Trigger(name, TriggerState.Stopped)
                    logger.Debug("Trigger<'{0}'> stopped.", name)

            member x.Terminate() = 
                if isDisposed = false then
                    tokenSource.Cancel() |> ignore  
                    stateChanged.Trigger(name, TriggerState.Terminated)
                    logger.Debug("Trigger<'{0}'> terminated.", name)

            member x.Fire() = 
                if isDisposed = false then
                    if triggerState = TriggerState.Idle then
                        timer.Change(Timeout.Infinite, Timeout.Infinite) |> ignore
                        stateChanged.Trigger(name, TriggerState.Executing)
                        timerCallback(name, jobCallback, tokenSource.Token)    
                        stateChanged.Trigger(name, TriggerState.Idle)            
                        occurrenceAt <- calculatetOccurrenceAt(expr, DateTime.UtcNow)
                        let due, interval = calculateTimerArgs(expr, DateTime.UtcNow, occurrenceAt)
                        timer.Change(due, interval) |> ignore
                        logger.Debug("Trigger<'{0}'> has been fired.", name)