module main

open Dispatch

open System
open System.IO
open System.Threading
open System.Windows
open System.Windows.Controls
open FSharp.Control.Reactive


open System
open FSharp.Control.Reactive

let (|&>) x f = f x; x

type SyncReply =
  | Value of obj
  | Exception of Exception

type Message =
  | AsyncCall of (obj->unit) * obj
  | SyncCall of (obj->obj) * obj * AsyncReplyChannel<SyncReply>

type Agent() =
    let agent = MailboxProcessor.Start <| fun inbox ->
      async {
          while true do
            let! msg = inbox.Receive()
            match msg with
              | AsyncCall(f, args) -> f args
              | SyncCall(f, args, replyChannel) -> f args |> Value |> replyChannel.Reply
      }
   
    member __.Async (f:'T->unit) (args:'T) =
      let f' (o:obj) = f (o :?> 'T)
      agent.Post( AsyncCall(f', args) )

    member __.Sync (f:'T->'U) (args:'T) : 'U =
      let f' (o:obj) = f (o :?> 'T) :> obj
      let reply = agent.PostAndReply( fun replyChannel -> SyncCall (f', args, replyChannel) )
      match reply with
      | Exception ex -> raise ex
      | Value v -> v :?> 'U
  

type UpdateResult = |Ok |Error of string

type StateService<'T> = {
  Current   : unit -> 'T
  Subscribe : IEvent<'T>
  Modify    : ('T -> 'T) -> Async<UpdateResult>
}

let stateService (state: 'T) =
  let currentState = ref state
  let changes = new Event<'T>()

  let currentProc :  MailboxProcessor<AsyncReplyChannel<'T>> =
    MailboxProcessor.Start <| fun inbox ->
      let rec loop () =
        async {
          let! chn = inbox.Receive ()
          chn.Reply !currentState
          return! loop ()
        }
      loop ()

  let modifyProc : MailboxProcessor<('T -> 'T) * AsyncReplyChannel<UpdateResult>> =
    MailboxProcessor.Start <| fun inbox ->
      let rec loop () =
        async {
          let! f, chn = inbox.Receive ()
          let v = !currentState
          try
            currentState := f v
            changes.Trigger !currentState
            chn.Reply UpdateResult.Ok
          with
            | e -> chn.Reply (UpdateResult.Error e.Message)
          return! loop () }
      loop ()
  {
    Current   = fun () -> currentProc.PostAndReply id
    Subscribe = changes.Publish
    Modify    = fun f -> modifyProc.PostAndAsyncReply (fun chn -> f, chn)
  }

type Dispatch() =
  let agent = new Agent()
  let s = new Reactive.Subjects.Subject<_>()
  let sub (strm:Event<_>) = strm.Publish.Subscribe(s) |> ignore
  
  member __.Stream = s
  member __.Register (strm:Event<_>) = agent.Async sub strm



type App(defaultState) as A =
  let dispatch = new Dispatch()
  let state = stateService defaultState
  let 

  
  
  








[<EntryPoint>]
let main argv = 
  let dispatch = new Dispatch()
  let controller (act) =
    printfn "%A" act
  dispatch.Stream 
    |> Observable.subscribe controller
    |> ignore



  0 // return an integer exit code
