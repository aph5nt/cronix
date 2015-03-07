namespace Cronix

open System
open System.Threading
open System.Collections.Generic
open Chessie.ErrorHandling
open System.Reflection

(* Triggers *) 
type TriggerState =
    | Stopped
    | Idle
    | Executing
    | Terminated 
and TriggerOption =
    | RescheduleAfterFailure
    | Overlap 
and ITrigger =
    abstract member State: TriggerState with get
    abstract member OccurrenceAt: DateTime with get
    abstract member Start: unit -> unit
    abstract member Stop: unit -> unit
    abstract member Terminate: unit -> unit
    abstract member Fire: unit -> unit

and Callback = delegate of (CancellationToken) -> unit

and CalculateTimerArgs = JobCronExpr * DateTime * DateTime -> ChangeArgs

and ChangeArgs = int * int 
 
and CalculatetOccurrenceAt = JobCronExpr * DateTime -> DateTime

and OccurrenceAt = DateTime

and TimerCallback = JobName * Callback * CancellationToken -> unit

and TriggerServices = {
    calculateTimerArgs : CalculateTimerArgs
    calculatetOccurrenceAt : CalculatetOccurrenceAt
    timerCallback : TimerCallback
}

and CreateTrigger = JobName * JobCronExpr * Callback  -> ITrigger
and TriggerFactory = {
    create: CreateTrigger
}

(* Jobs *)
and Job = {
    Name : JobName;
    CronExpr : JobCronExpr;
    Trigger : ITrigger;
}
and JobName = string
and JobCronExpr = string
and JobState = {
    Name : JobName;
    CronExpr : JobCronExpr;
    TriggerState : TriggerState
}

(* Scheduling *)
type Schedule = ScheduleState * ScheduleParams -> Result<ScheduleState, string>
and UnSchedule = ScheduleState * JobName ->  Result<ScheduleState, string>
and Start = ScheduleState * JobName ->  Result<ScheduleState, string>
and Stop = ScheduleState * JobName ->  Result<ScheduleState, string>
and ScheduleState = Dictionary<string, Job>
and ScheduleParams = JobName * JobCronExpr * Callback
and ScheduleServices = {
    schedule : Schedule
    unschedule : UnSchedule
    start : Start
    stop : Stop
}
and ScheduleManagerCommand = 
    | Schedule of JobName * JobCronExpr * Callback
    | UnSchedule of JobName
    | Stop of JobName
    | Start of JobName
and IScheduleManager = 
    inherit IDisposable
    abstract member Schedule: string -> string -> Callback -> unit
    abstract member UnSchedule: string -> unit
    abstract member State: seq<JobState>
    abstract member Stop: unit -> unit

type ResultMessage = 
    | FailureMessage of FailureMessage
    | SuccessMessage of SuccessMessage
and FailureMessage =
    | InvalidCronExpr
    | JobExists
    | JobNotExists
and SuccessMessage = 
    | ValidateExpr
    | CanAddJob
    | AddJob
    | JobFound
    | RemoveJob
    | StopJob
    | StartJob


(* BootStrapper *)

type StartupHandlerState = {
    scheduleManager : IScheduleManager
    startupHandler : Option<StartupHandler>
}

and StartupScriptState = {
        scheduleManager : IScheduleManager
        referencedAssemblies : string[]
        source : string
        compiled : Option<Assembly>
}

and StartupHandler = delegate of (IScheduleManager) -> unit
and RunService = IScheduleManager -> Option<StartupHandler> -> unit

module Messages = 

    let FailureMessages : Map<FailureMessage, string> =
        Map.empty.
            Add(InvalidCronExpr, "expr <{0}> is not valid.")
            .Add(JobExists, "Job <{0}> already exists.")
            .Add(JobNotExists, "Job <{0}> does not exists.")

    let SuccesMessagess : Map<SuccessMessage, string> =
        Map.empty.
            Add(ValidateExpr, "expr <{0}> is valid.")
            .Add(CanAddJob, "Job <{0}> can be added.")
            .Add(AddJob, "Job <{0}> has been added.")
            .Add(JobFound, "Job <{0}> found.")
            .Add(RemoveJob, "Job <{0}> has been removed.")
            .Add(StopJob, "Job <{0}> has been stopped.")
            .Add(StartJob, "Job <{0}> has been started.")
        
    let FailureMessage (key : FailureMessage) ([<ParamArray>] params' : list<_>) =
        let result = String.Format(FailureMessages.[key], params')
        Fail [result]
     
    let SuccesMessage state (key : SuccessMessage) ([<ParamArray>] params' : list<_>) =
        let result = String.Format(SuccesMessagess.[key], params')
        Ok(state, [result])
     