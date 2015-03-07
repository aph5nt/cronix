namespace Cronix

module Triggers =

    open System.Threading
    open NCrontab 
    open System
    open Logging 
 
    let logger = logger()

    let calculateTimerArgs : CalculateTimerArgs =
        fun(expr, date, occurrenceAt) ->
            let nextOccurence = CrontabSchedule.Parse(expr).GetNextOccurrence occurrenceAt
            let due = int (occurrenceAt - date).TotalMilliseconds
            let interval = int (nextOccurence - occurrenceAt).TotalMilliseconds
            logger.Debug("Calculating TimerArgs: due = <%d>; interval = <%d>.", due, interval)
            (due, interval)
   
    let calculatetOccurrenceAt : CalculatetOccurrenceAt =
        fun(expr, date) ->
            CrontabSchedule.Parse(expr).GetNextOccurrence date

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
        
    type Trigger(name: string, expr : string, jobCallback : Callback) =
        let mutable triggerState = Stopped
        let mutable occurrenceAt = DateTime.UtcNow
        let tokenSource = new CancellationTokenSource()

        let timer = new System.Threading.Timer(fun _ -> 
                triggerState <- TriggerState.Executing
                occurrenceAt <- CrontabSchedule.Parse(expr).GetNextOccurrence DateTime.UtcNow
                timerCallback(name, jobCallback, tokenSource.Token)
                triggerState <- TriggerState.Idle         
                )

        let terminate() =
            if triggerState <> Terminated then
                tokenSource.Cancel() |> ignore
                timer.Dispose() |> ignore
                triggerState <- TriggerState.Terminated
                logger.Debug("Trigger<'{0}'> terminated.", name)

        override x.Finalize() = 
            terminate()

        interface ITrigger with 
            member x.State
                with get () = triggerState
    
            member x.OccurrenceAt
                with get () = occurrenceAt

            member x.Start() =
                if triggerState <> Terminated then
                    triggerState <- TriggerState.Idle            
                    occurrenceAt <- calculatetOccurrenceAt(expr, DateTime.UtcNow)
                    let due, interval = calculateTimerArgs(expr, DateTime.UtcNow, occurrenceAt)

                    timer.Change(due, interval) |> ignore
                    logger.Debug("Trigger<'{0}'> started. dueTime = {1}; interval '{2}'.", name, due, interval)

            member x.Fire() = 
                logger.Debug("Firing Trigger<'{0}'>", name)
                timerCallback(name, jobCallback, tokenSource.Token)

            member x.Stop() =
                if triggerState <> Terminated then
                    timer.Change(Timeout.Infinite, Timeout.Infinite) |> ignore
                    triggerState <- TriggerState.Stopped
                    logger.Debug("Trigger<'{0}'> stopped.", name)

            member x.Terminate() = 
                terminate()
