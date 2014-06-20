﻿
#load "preamble.fsx"
open Nessos.MBrace
open Nessos.MBrace.Client

let aqn  = "Nessos.MBrace.Azure.AzureStoreFactory, MBrace.Azure, Version=0.5.0.0, Culture=neutral, PublicKeyToken=null"
let conn = "DefaultEndpointsProtocol=https;AccountName=mbraceclusterstorage;AccountKey=cq2knJyPSCP9uNcyDPbFAgHyiPpJVMcR/59yN2RW9uNmrHJyT4ZwdLYxCXuUo6w5xJ7iMjKy0+WxQQ+f2nSseQ=="
MBraceSettings.StoreProvider <- StoreProvider.Parse(aqn,conn)

open System.IO
[<Cloud>]
let writeString container filename (content : string) =
    cloud {
        return! CloudFile.Create(container, filename,
                    (fun (stream : Stream) ->
                        async {
                            use sw = new StreamWriter(stream)
                            sw.Write(content) }))
    }


let cr = MBrace.RunLocal(writeString "folder" "foobar.txt" "hello world")
cr.Dispose() |> Async.RunSynchronously


let rt = MBrace.InitLocal 3
let it = rt.Run <@ cloud { return 42  } @>
it.Read() |> Async.RunSynchronously

rt.Nodes.Length

let ns = Node.SpawnMultiple 2
ns |> Seq.map (fun n -> n.DeploymentId)


rt.Attach ns
rt.Nodes.Length

let ns = [1..3] |> List.map (fun i -> MBraceNode("clusterVM" + string i, 2675))

//let ns = [ Node("10.0.1.4", 2675); Node("10.0.1.5", 2675); Node("10.0.1.6", 2675)]
ns |> List.map (fun n -> n.Ping())

ns |> List.map (fun n -> n.ShowInfo())
ns |> List.map (fun n -> n.ShowSystemLogs())
ns |> List.map (fun n -> n.IsActive)
ns |> List.map (fun n -> n.GetInfo())


let rt = MBrace.Connect("clusterVM1", 2675)
let rt = MBrace.Connect("10.0.1.4", 2675)

let rt = MBrace.Boot ns

let ps = rt.CreateProcess <@ cloud { return 42 } @>

rt.ShowInfo(true)


let nodes = 
    [   Node("mbrace://10.0.1.4:54072/")
        Node("mbrace://10.0.1.4:54080/")
        Node("mbrace://10.0.1.4:54088/") ]

nodes |> List.map (fun n -> n.Ping())

let rt = MBrace.Boot nodes
rt.ShowInfo(true)

let ps = rt.Run <@ cloud { return 42 } @>

