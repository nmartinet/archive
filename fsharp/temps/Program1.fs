open System
open FSharp.Control.Reactive
type IAction = interface end

let p x = printfn "%A" x
let (|&>) x f = f x; x
let I = ignore
let inline (!|) x = x |> I


module Agents = 
  type SyncReply =
    | Value of obj
    | Exception of Exception

  type Message =
    | AsyncCall of (obj->unit)  * obj
    | SyncCall  of (obj->obj)   * obj * AsyncReplyChannel<SyncReply>

  type Agent() =
    let agent = MailboxProcessor.Start <| fun inbox -> async {
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

type Dispatcher() =
  let agent = new Agents.Agent()
  let s = new Reactive.Subjects.Subject<IAction>()
  let sub (strm:Event<_>) = strm.Publish.Subscribe(s) |> ignore
  
  member __.Stream = s
  member __.Register (strm:Event<_>) = agent.Async sub strm

module StateService = 
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


(*
let Dispatcher =
  getAction
  ApplyActionOnStore
  updateStore
let Store = 0
  onChange
  detect changes
  UpdateView
let View = 0
  renderStore
  CallActions

let Actions = {}
*)



type Application(ctrl, str) = 
  let dispatcher = new Dispatcher()
  let controller = ctrl
  let store      = str
  

[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    0 // return an integer exit code





type Actions =
  | Add       
  | Substract 
  | Multiply   
  | Divide   
  | History
  interface IAction

type State = {
  Total : int
  History : (Actions * int) list
}

let defaultState = {Total = 0; History = [(Add, 0)]}

let parseInput(s:string) =
  let splitVal = s.Split([|' '|])
  match s.Split([|' '|]) with 
    | [|"Add"; x|] -> Add, (int)x
    | [|"Sub"; x|] -> Substract, (int)x
    | [|"Mul"; x|] -> Multiply, (int)x
    | [|"Div"; x|] -> Divide, (int)x
    | [|"Hist"|] -> History, 0
    | _ -> History, 0
let View(state:State) =
  printfn "Current Total = %A" state.Total
  printfn "Do:"
  parseInput(Console.ReadLine())
let Controller arg (state : State) : State =
  (*
  match op with 
    | History ->
      state.History
        |> List.iter (fun (act, num) -> printfn "%A %A" act num)
      state
    | _ ->
      let num = (int)arg
      let history = List.append state.History [(op, num)]
      let total = match op with
        | Add       -> state.Total + num
        | Substract -> state.Total - num 
        | Multiply  -> state.Total * num 
        | Divide    -> state.Total / num 
        | History   -> 0
      { Total = total; History = history} 
      *)
  state


open System
open FSharp.Control.Reactive
open Microsoft.FSharp.Core
open System.Threading 

//to do
//change to rx
// actions are sent trhugh obs
// view init then rerender shoudl be called
// in controller - attach a view to the state
// views have own dispatch
// update view through taht dispatch
let p x = printfn "%A" x
let (|&>) x f = f x; x

type IAction = interface end
type EvntMsg<'T> = {
  Action : IAction
  Content: 'T
}

type SysAction =
  | Quit
  interface IAction

module Agents = 
  type SyncReply =
    | Value of obj
    | Exception of Exception

  type Message =
    | AsyncCall of (obj->unit)  * obj
    | SyncCall  of (obj->obj)   * obj * AsyncReplyChannel<SyncReply>

  type Agent() =
    let agent = MailboxProcessor.Start <| fun inbox -> async {
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
type Dispatcher() =
  let agent = new Agents.Agent()
  let s = new Reactive.Subjects.Subject<EvntMsg<_>>()
  let sub (strm:Event<EvntMsg<_>>) = strm.Publish.Subscribe(s) |> ignore
  
  member __.Stream = s
  member __.Register (strm:Event<EvntMsg<_>>) = agent.Async sub strm

type O<'a> = { 
  Set : 'a -> unit
  Bind: ('a -> unit) -> 'a 
}
let o (v: 'T) : O<'T> =
  let curr  = ref v
  let Update = new Event<'T>()
  {
    Set   = fun v -> 
      curr:= v
      Update.Trigger v
    Bind  = fun f -> 
      Update.Publish.Subscribe f |> ignore
      curr.Value |&> Update.Trigger 
  }

let createStateObject (name:string) (x:'T) =
  let obsVal = o x
  (name, obsVal)

type App(initialState, controller, views) =
  let createState state =
    state

  let cancelationToken = new CancellationTokenSource()
  let state = createState initialState
  let dispatcher = new Dispatcher()
  
  let updateSystem (arg:EvntMsg<'T>) =
    match (arg.Action :?> SysAction) with
      | Quit -> cancelationToken.Cancel()

  let registerView dispatcher state view =
    view

  let updateState (oldState) (newState) =
    ignore

  let update (arg:EvntMsg<_>) =
    match arg.Action with
      | (:? SysAction) -> updateSystem arg 
      | _         -> 
        controller arg state
          |> updateState state
          |> ignore
  do
    dispatcher.Stream
      |> Observable.subscribe update
      |> ignore

  member __.run() =
    let rec loop() = async{
      do! Async.Sleep(1)
      return! loop()
    }
    Async.StartImmediate(loop(), cancelationToken.Token)

 type Actions =
  | Print
  interface IAction

type State = {
  Value : String
}
let state = {
  Value = "test"
}

let controller (arg:EvntMsg<Actions>) (state : State) =
  state



let CLIView(dispatcher) =
  let evnt = new Event<_>()
  dispatcher.Register evnt
  let parseInput input =
    if input = "q" then
        evnt.Trigger {Action = Quit; Content = ""}
      else
        evnt.Trigger {Action = Print; Content = input}
    
  let rec loop() = async{
    parseInput (Console.ReadLine())
    return! loop()
  }
  
      
          
type Store (data) =
  let _data = data

type App(store) =
  let _store = store

type CLIRepl(store) =
  

      
    

[<EntryPoint>]
let main argv = 
  let app = new App(state, controller, 1  )
  app.run()
  Console.ReadLine()
  0
    