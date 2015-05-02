namespace Cronix

open System
open System.Threading
open System.Collections.Generic
open Chessie.ErrorHandling
open System.Reflection

(* Triggers *) 
/// Describers the trigger state.
type TriggerState =
    | Stopped
    | Idle
    | Executing
    | Terminated 
    | Removed

/// Describers the trigger options.
and TriggerOption =
    | RescheduleAfterFailure
    | Overlap 

/// Describes the trigger.
and ITrigger =
    inherit IDisposable

    /// Returns the trigger state.
    abstract member State: TriggerState with get

    /// Returns the trigger name.
    abstract member Name: TriggerName with get
   
    /// Returns the cron expression.
    abstract member CronExpr : CronExpr with get

    /// Returns the occurance time.
    abstract member OccurrenceAt: OccurrenceAt with get

    /// Starts the trigger.
    abstract member Start: unit -> unit

    /// Stops the trigger.
    abstract member Stop: unit -> unit

    /// Terminates the trigger execution.
    abstract member Terminate: unit -> unit

    /// Fires the trigger.
    abstract member Fire: unit -> unit

    /// Publish remove event
    abstract member PublishRemovedEvent : unit -> unit

    /// On state change event
    abstract member OnStateChanged: IEvent<(TriggerName * TriggerState)> with get


/// The trigger callback delegate
and JobCallback = delegate of (CancellationToken) -> unit

/// Calculates the timer argument function signature. 
and CalculateTimerArgs = CronExpr * DateTime * DateTime -> ChangeArgs

/// The change time argument type
and ChangeArgs = int * int 
 
/// The calculates the occurance at time function signature.
and CalculatetOccurrenceAt = CronExpr * DateTime -> DateTime

/// The occurance at type
and OccurrenceAt = DateTime

/// The trigger callback delegate.
and TimerCallback = TriggerName * JobCallback * CancellationToken -> unit

/// The trigger service facade.
and TriggerServices = {
    calculateTimerArgs : CalculateTimerArgs
    calculatetOccurrenceAt : CalculatetOccurrenceAt
    timerCallback : TimerCallback
}

/// Creates new trigger function signature.
and CreateTrigger = TriggerName * CronExpr * JobCallback  -> ITrigger

/// The trigger factory.
and TriggerFactory = {
    create: CreateTrigger
}

/// The trigger detail dto.
and TriggerDetail = {
    Name : TriggerName;
    CronExpr : CronExpr;
    TriggerState : TriggerState;
    NextOccuranceDate : DateTime;
}

/// The trigger name.
and TriggerName = string

/// The cron expression.
and CronExpr = string
 
(* Scheduling *)
/// The schedule function signature.
type Schedule = ScheduleState * ScheduleParams -> Result<ScheduleState, string>

/// The unschedule function signature.
and UnSchedule = ScheduleState * TriggerName ->  Result<ScheduleState, string>

/// The start function signature.
and Start = ScheduleState * TriggerName ->  Result<ScheduleState, string>

/// The fire trigger function signature.
and Fire = ScheduleState * TriggerName ->  Result<ScheduleState, string>

/// The stop function signature.
and Stop = ScheduleState * TriggerName ->  Result<ScheduleState, string>

/// The schedule state type.
and ScheduleState() =
    inherit Dictionary<string, ITrigger>()

    let mutable onStateChanged : Handler<(TriggerName * TriggerState)> = null
    member x.OnStateChanged 
        with get() = onStateChanged
        and set(value) =  onStateChanged <- value

    /// Adds trigger.
    member x.Add(name, trigger : ITrigger) = 
        trigger.OnStateChanged.AddHandler(onStateChanged)
        base.Add(name, trigger)

    /// Removes trigger by name.
    member x.Remove(name) =
        base.Remove(name) |> ignore

/// The schedule params type.
and ScheduleParams = TriggerName * CronExpr * JobCallback

/// Describes the schedule mannager commands.
and ScheduleManagerCommand = 
    | ScheduleJob of TriggerName * CronExpr * JobCallback
    | UnScheduleJob of TriggerName
    | DisableTrigger of TriggerName
    | EnableTrigger of TriggerName
    | FireTrigger of TriggerName
    | TerminateTrigger of TriggerName
    
/// Manges the job schedules.
and IScheduleManager = 
    inherit IDisposable
    /// Schedules new job.
    abstract member ScheduleJob: string -> string -> JobCallback -> Result<ScheduleState, string>

    /// Unschedules given job.
    abstract member UnScheduleJob: string -> Result<ScheduleState, string>

    /// Starts given trigger.
    abstract member EnableTrigger : string -> Result<ScheduleState, string>

    /// Stops given trigger.
    abstract member DisableTrigger : string -> Result<ScheduleState, string>

    /// Fires given trigger.
    abstract member FireTrigger : string -> Result<ScheduleState, string>

    /// Terminates given trigger execution.
    abstract member TerminateTriggerExecution : string -> Result<ScheduleState, string>

    /// Returns jobs state.
    abstract member TriggerDetails: seq<TriggerDetail>

    /// Returns OnTriggerStateChanged event
    abstract member OnTriggerStateChanged: IEvent<TriggerDetail> with get

    /// Stops schedule manager.
    abstract member StopManager: unit -> unit

    /// Starts schedule manager.
    abstract member StartManager: unit -> unit

/// Schedule Manager hub.
type IScheduleManagerHub =
   /// Returns all triggers state.
   abstract member GetData: TriggerDetail[] -> unit

   /// Returns OnStateChanged event.
   abstract member OnStateChanged : TriggerDetail -> unit

   /// Enables given trigger.
   abstract member EnableTrigger: TriggerName -> unit

   /// Disables given trigger.
   abstract member DisableTrigger: TriggerName -> unit

   /// Fires given trigger.
   abstract member FireTrigger: TriggerName -> unit

   /// Terminates given trigger execution.
   abstract member TerminateTrigger: TriggerName -> unit

(* Installer *)
/// The install function signature.
type Install = Assembly -> Result<string, string>

/// The uninstall function signature.
type Uninstall = Assembly -> Result<string, string>


(* BootStrapper *)
/// The startup handler state type.
type StartupHandlerState = {
    scheduleManager : IScheduleManager
    startupHandler : Option<StartupHandler>
}

/// The startup script state type.
and StartupScriptState = {
        scheduleManager : IScheduleManager
        referencedAssemblies : string[]
        source : string
        compiled : Option<Assembly>
}

/// The startup handler delegate.
and StartupHandler = delegate of (IScheduleManager) -> unit

/// The run service function signature.
and RunService = IScheduleManager -> Option<StartupHandler> -> unit

/// The init plugin function signature.
and InitPlugin = IScheduleManager -> unit

/// The initialize service function signature.
and InitService = string[] * StartupHandler -> Result<string, string>

(* Messages *)
/// Module responsible for generic messages.
[<AutoOpen>]
module Messages = 

    /// Describes result message.
    type ResultMessage = 
        | FailureMessage of FailureMessage
        | SuccessMessage of SuccessMessage

    /// Describes the failure message.
    and FailureMessage =
        | InvalidCronExpr
        | TriggerExists
        | TriggerNotExists

    /// Describes the sucess message.
    and SuccessMessage = 
        | ExpressionValidated
        | TriggerCanBeAdded
        | TriggerAdded
        | TriggerFound
        | TriggerRemoved
        | TriggerDisabled
        | TriggerEnabled
        | TriggerFired
        | TriggerTerminated


    /// Returns the failure messages.
    let FailureMessages : Map<FailureMessage, string> =
        Map.empty.
            Add(InvalidCronExpr, "Expr <{0}> is not valid.")
            .Add(TriggerExists, "Job <{0}> already exists.")
            .Add(TriggerNotExists, "Job <{0}> does not exists.")

    /// Returns the success messages.
    let SuccesMessagess : Map<SuccessMessage, string> =
        Map.empty.
            Add(ExpressionValidated, "Expr <{0}> is valid.")
            .Add(TriggerCanBeAdded, "Trigger <{0}> can be added.")
            .Add(TriggerAdded, "Trigger <{0}> has been added.")
            .Add(TriggerFound, "Trigger <{0}> found.")
            .Add(TriggerRemoved, "Trigger <{0}> has been removed.")
            .Add(TriggerDisabled, "Trigger <{0}> has been disabled.")
            .Add(TriggerEnabled, "Trigger <{0}> has been enabled.")
            .Add(TriggerFired, "Trigger <{0}> has been fired.")
            .Add(TriggerTerminated, "Trigger <{0}> has been terminated.")
        

    /// Creates the failure message.
    let FailureMessage (key : FailureMessage) ([<ParamArray>] params' : list<_>) =
        let result = String.Format(FailureMessages.[key], params')
        Fail [result]

    /// Creates the sucess message.
    let SuccesMessage state (key : SuccessMessage) ([<ParamArray>] params' : list<_>) =
        let result = String.Format(SuccesMessagess.[key], params')
        Ok(state, [result])
     
[<AutoOpen>]
module Dsl = 

    /// Describes the frequence rate
    type CronExpr =
        /// 01 * * * *
        | Hourly

        /// 02 4 * * *
        | Daily

        /// 22 4 * * 0
        | Weekly

        /// 42 4 1 * *
        | Monthly

    let frequency expr = 
        match expr with
        | Hourly -> "01 * * * *"
        | Daily -> "02 4 * * *"
        | Weekly -> "22 4 * * 0"
        | Monthly -> "42 4 1 * *"