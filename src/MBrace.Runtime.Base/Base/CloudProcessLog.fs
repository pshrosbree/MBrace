﻿namespace Nessos.MBrace.Runtime.Logging

    open System
    open System.Text

    open Nessos.MBrace
    open Nessos.MBrace.Core
    open Nessos.MBrace.Runtime.Store

    // TODO : move string to utils

    module String =
        let inline build (f : StringBuilder -> unit) =
            let sb = new StringBuilder()
            do f sb
            sb.ToString()

        let inline append (sb : StringBuilder) (x : string) = sb.Append x |> ignore

    type CloudLogEntry =
        {
            Date : DateTime
            TaskId : string
            Message : string
            TraceInfo : TraceInfo option
        }
    with
        static member UserInfo taskId msg = { Date = DateTime.Now ; TaskId = taskId ; Message = msg ; TraceInfo = None }
        static member Trace taskId msg traceInfo = { Date = DateTime.Now ; TaskId = taskId ; Message = msg ; TraceInfo = Some traceInfo }

        member e.ToSystemLogEntry (processId : ProcessId) =
            let message =
                match e.TraceInfo with
                | None ->
                    sprintf "[Cloud Process %A][Task %s] User Message: %s" processId e.TaskId e.Message
                | Some tI ->
                    String.build(fun sb ->
                        String.append sb <| sprintf "[Cloud Process %A][Task %s] Trace" processId e.TaskId
                        match tI.File with 
                        | None -> () 
                        | Some f -> String.append sb <| sprintf ", File:%s" f 

                        match tI.Line with
                        | None -> ()
                        | Some l -> String.append sb <| sprintf ", Line:%d" l

                        String.append sb <| sprintf ": %s\n" e.Message
                        String.append sb <| "--- Begin environment dump ---\n"
                        for KeyValue(n,v) in tI.Environment do
                            String.append sb <| sprintf "  val %s = %s\n" n v
                        String.append sb <| "--- End  environment  dump ---\n")

            { Date = e.Date ; Message = message ; Level = Info }

    /// Store interface for cloud process logs

    type StoreCloudLogger(store : ICloudStore, processId : ProcessId, taskId : string) =
        inherit StoreLogger<CloudLogEntry>(store, container = sprintf "cloudProc%d" processId, logPrefix = sprintf "task%s" taskId)

        static member GetReader(store : ICloudStore, processId : ProcessId) =
            new StoreLogReader<CloudLogEntry>(store, container = sprintf "cloudProc%d" processId )

    /// The runtime ICloudLogger implementation

    type RuntimeCloudProcessLogger(processId : ProcessId, taskId : string, ?sysLog : ISystemLogger, ?store : ICloudStore) =
        let storeLogger = store |> Option.map (fun s -> new StoreCloudLogger(s, processId, taskId))

        let logEntry (e : CloudLogEntry) =
            match sysLog with Some s -> s.LogEntry (e.ToSystemLogEntry processId) | None -> ()
            match storeLogger with Some s -> s.LogEntry e | None -> ()

        interface ICloudLogger with
            member __.LogTraceInfo (msg, traceInfo) = logEntry <| CloudLogEntry.Trace taskId msg traceInfo
            member __.LogUserInfo msg = logEntry <| CloudLogEntry.UserInfo taskId msg


    type InMemoryCloudProcessLogger(sysLog : ISystemLogger, processId : ProcessId) =
        
        let taskId() = string <| System.Threading.Thread.CurrentThread.ManagedThreadId
        let logEntry (e : CloudLogEntry) = sysLog.LogEntry (e.ToSystemLogEntry processId)

        interface ICloudLogger with
            member __.LogTraceInfo (msg, traceInfo) = logEntry <| CloudLogEntry.Trace (taskId()) msg traceInfo
            member __.LogUserInfo msg = logEntry <| CloudLogEntry.UserInfo (taskId()) msg


    type NullCloudProcessLogger () =
        interface ICloudLogger with
            member __.LogTraceInfo (_,_) = ()
            member __.LogUserInfo _ = ()