open System
open System.IO
open System.Threading
open System.Windows
open System.Windows.Controls
open System.Collections.Specialized
open System.Collections.ObjectModel
open FSharp.Control.Reactive
open FSharp.Reflection
open System.Reflection

open ICSharpCode.AvalonEdit
open ICSharpCode.AvalonEdit.CodeCompletion
open ICSharpCode.AvalonEdit.Folding
open ICSharpCode.AvalonEdit.Highlighting

type IAction = interface end

module Utils = 
  let p x = printfn "%A" x
  let (|&>) x f = f x; x
  let I = ignore
  let inline (!|) x = x |> I
  let CLArgs args =
    args
      |> Array.toList 
      |> List.tail 
      |> List.toArray 
open Utils

module Agents = 
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
  
  type Dispatch() =
    let agent = new Agent()
    let s = new Reactive.Subjects.Subject<IAction>()
    let sub (strm:Event<_>) = !|strm.Publish.Subscribe(s)
  
    member __.Stream = s
    member __.Register (strm:Event<_>) = agent.Async sub strm
open Agents

module Threading = 
  let ApartmentThread (f: (unit -> _)) =
    let t = new Thread(f)
    t.SetApartmentState(ApartmentState.STA)
    t.IsBackground <- true
    t
open Threading

module UI =
  let inline (|=|) (o:#Panel) (c:UIElement list) = c |> List.map o.Children.Add |> I; o
  let inline (|=>) (o:#Menu) (c:#MenuItem list) = c |> List.map o.Items.Add |> I; o

  let dockPanel() = new DockPanel()
  let stackPanel ori = new StackPanel(Orientation = ori)
  let hStackPanel() = stackPanel Orientation.Horizontal
  let vStackPanel() = stackPanel Orientation.Vertical

  let dock pos o = DockPanel.SetDock(o, pos); o

  let label content = new Label(Content = content)
  let button text = new Button(Content = text)

  let menu() = new Menu()
  let menuItem lable = new MenuItem(Header = lable)

  let doOn (e:RoutedEvent) f (el:UIElement) = el.AddHandler(e, new RoutedEventHandler(f)); el

  let tWindow ui =
    ApartmentThread <| fun _ ->
      let mw = new Window(Content=(ui()))
      mw.Show()
      System.Windows.Threading.Dispatcher.Run();
    
  let uiBuilder (d:Dispatch) (ui: ( _ -> _)) = 
    let evnt = new Event<IAction>()
    let Do act = evnt.Trigger(act)
    let DoOn (evnt:RoutedEvent) (act:IAction) (el:UIElement) = 
      doOn evnt (fun sndr args -> Do act) el |> I; el

    d.Register evnt
    ui DoOn
open UI

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
open StateService

module Records =
  let findField s f = 
    (FSharpType.GetRecordFields(s.GetType())
      |> Array.filter (fun t -> t.Name = f)).[0] 

  let rGet s field =
    let t = findField s field
    FSharpValue.GetRecordField(s, t)

  let rSet (s:'T) (field : string) (fn : ('E -> 'E)) : 'T =
    FSharpValue.MakeRecord(
      s.GetType(), 
      Array.zip (FSharpType.GetRecordFields(s.GetType())) (FSharpValue.GetRecordFields(s)) 
        |> Array.map (fun (f, v) -> 
          if (f.Name = field) then (box (fn (unbox v))) 
          else v))
open Records

//-- Model ------------
type State = 
  { Time : int ; Other : string}
  member s.TimeString = 
    TimeSpan.FromSeconds((float)s.Time).ToString(@"hh\:mm\:ss\:fff")

let defaultState = { Time = 1500; Other = "test"}

let d = new Dispatch()
let s = stateService defaultState

type Actions = 
  | Tick
  | Start
  | Stop
  | Reset
  interface IAction

//-- View ----------------
let ui () =
  (fun DoOn ->
    dockPanel() |=| [
      menu() |=>
        [
           menuItem "File"
           menuItem "Edit"
           menuItem "View"
        ]
      |> dock Dock.Top
      (fun () -> 
        let lbl = label (s.Current().TimeString)
        s.Subscribe
          |> Observable.subscribe (fun nState -> lbl.Dispatcher.Invoke(fun _ -> lbl.Content <- nState.TimeString )  ) |> I
        lbl
      )()
    ]
  ) |> uiBuilder d 

type Ticker(tick) =
  let timer = new System.Timers.Timer(1000.)
  let evnt = new Event<IAction>()
  let cancellationSource = new CancellationTokenSource()

  do
    d.Register evnt
    timer.AutoReset <- true
    timer.Elapsed.Add(fun _ -> evnt.Trigger Tick )

  member t.Start() =
    Async.Start(async{timer.Start() }, cancellationSource.Token )
  member t.Stop() = 
    cancellationSource.Cancel()
  
  


let t = new Ticker()

//-- Controller ----------
let controller (act:IAction) =
  match (act :?> Actions) with
    | Tick  -> 
      //s.Modify (fun cState -> rSet cState "Time" (fun t -> t-1) :?> State ) |> I
      s.Modify (fun cState -> {cState with Time = (cState.Time - 1) }) |> I
    | Start -> t.Start()
    | Stop  -> t.Stop()
    | Reset -> printfn "reset"

let app args =
  d.Stream |> Observable.subscribe controller |> I
  
  let win = tWindow ui
  win.Start()
  win.Join()
  
  0

#if INTERACTIVE
app (CLArgs fsi.CommandLineArgs)
#else
[<EntryPoint; STAThread>]
let entryPoint args = app args
#endif