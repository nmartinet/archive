open System
open System.IO
open System.Windows
open System.Windows.Controls
open System.Windows.Threading
open System.Reflection
open System.Threading
open System.Xml

open FSharp.Reflection

open ICSharpCode.AvalonEdit
open ICSharpCode.AvalonEdit.CodeCompletion
open ICSharpCode.AvalonEdit.Folding
open ICSharpCode.AvalonEdit.Highlighting

let (|&>) x f = f x; x

type O<'a> = { Set : 'a -> unit; Bind: ('a -> unit) -> 'a; v: 'a }
let o (v: 'a) : O<'a> =
  let curr    = ref v
  let Update  = new Event<'a>()
  { v     = curr.Value
    Set   = fun v -> curr := v; Update.Trigger v
    Bind  = fun f -> Update.Publish.Subscribe f |> ignore; curr.Value |&> Update.Trigger }

type ActQ<'a> = {Action : 'a -> unit}
let actQ actr =
  let _act = MailboxProcessor.Start <| fun inbox ->
      let rec loop() = async {
        let! act = inbox.Receive()
        actr act
        return! loop() }
      loop()
  {Action = fun act -> _act.Post act}

let UIT (f: (unit -> _)) =
  let t = new Thread(f)
  t.SetApartmentState(ApartmentState.STA)
  t.IsBackground <- true
  t

let UIThread ui state actr=
  UIT (fun () ->
    let win = new Window()
    let UI = ui state actr
    win.Content <- UI
    win.Show()
    System.Windows.Threading.Dispatcher.Run();
  )

let doOn e f (el:UIElement) =  el.AddHandler(e, new RoutedEventHandler(f))
let children (l:UIElement list) (o:UIElement) =
  let addr f = l |> List.iter (fun i -> f i |> ignore) |> ignore
  match o with
    | :? DockPanel  as d  -> addr (fun i -> d.Children.Add  i )
    | :? StackPanel as sp -> addr (fun i -> sp.Children.Add i )
    | :? Grid       as g  -> addr (fun i -> g.Children.Add  i )
    | :? Menu       as m  -> addr (fun i -> m.Items.Add     i )
    | :? MenuItem   as mi -> addr (fun i -> mi.Items.Add    i )
    | _ -> ()
  o
let dock pos o = DockPanel.SetDock(o, pos); o

let click f (el:UIElement) =
  match el with
    | :? Button   as btn  -> doOn Button.ClickEvent   (fun s a -> f()) el
    | :? MenuItem as mi   -> doOn MenuItem.ClickEvent (fun s a -> f()) el
    | _ -> ()
  el

let font fontFamily = Media.FontFamily(fontFamily)

let sPanel o    = new StackPanel(Orientation = o)
let sPanelV()   = sPanel Orientation.Vertical
let sPanelH()   = sPanel Orientation.Horizontal
let dockP()     = new DockPanel()
let grid()      = new Grid()
let gl l        =
  match l with
    | 0. ->  GridLength.Auto
    | _  ->  new GridLength(l)

let colDef w (g:#Grid) =
  let cd = new ColumnDefinition()
  if not (w = 0.) then cd.Width <- (new GridLength(w))
  g.ColumnDefinitions.Add(cd)
  g


let rowDef w (g:#Grid) =
  let rd = new RowDefinition()
  if not (w = 0.) then rd.Height <- new GridLength(w)
  g.RowDefinitions.Add(rd)

let colDefs widths g = widths |> List.iter (fun w -> colDef w g |> ignore );g
let rowDefs widths g = widths |> List.iter (fun w -> rowDef w g |> ignore );g


let setRow r el =
  Grid.SetRow(el, r)
  el

let setCol c el =
  Grid.SetColumn(el, c)
  el

let setGrid r c el =
  match r with | Some r -> setRow r el  |> ignore | _ -> ()
  match c with | Some c -> setCol c el  |> ignore | _ -> ()
  el


type TimerState = | Work | Rest

type State = {
  CurrentTime: O<int>
  WorkTime: O<int>
  RestTime: O<int>
  Running: O<bool>
  CurrentType: O<TimerState>
}

let defaultState = {
  CurrentTime = o(60*25)
  WorkTime = o(60*25)
  RestTime = o(60*5)
  Running = o(false)
  CurrentType = o(Work)
  }

type Action = interface end
type AppActs =
  | NewTimer
  | ToggleRun
  | TimerEnd
  interface Action

let Actr (state:State) action =
  match action with
    | NewTimer ->
      state.CurrentTime.Set defaultState.WorkTime.v
      state.Running.Set  true
    | ToggleRun ->
      if state.Running.v then state.Running.Set false else state.Running.Set true
    | TimerEnd ->
      match state.CurrentType.v with
        | Work ->
          state.CurrentTime.Set defaultState.RestTime.v
          state.CurrentType.Set Rest
        | Rest ->
          state.CurrentTime.Set defaultState.WorkTime.v
          state.CurrentType.Set Work
    | _ -> ()


let lable s   = new Label(Content = s)
let button s  = new Button(Content = s)



let ui (state : State) (actr:ActQ<AppActs>) =
  grid()
    |> rowDefs [50.; 0. ; 50.]
    |> children [
          lable "worktype"  |> setRow 0
        ; lable "time"      |> setRow 1
        ; sPanelH()         |> setRow 2 |> children [ button "Start" ]
        ]



let app() =
  let state = defaultState
  let q = actQ (Actr state)

  let t1 = UIThread ui state q
  t1.Start()
  t1.Join()

let main argv =
  app()
  0 // return an integer exit code

#if INTERACTIVE
fsi.CommandLineArgs |> Array.toList |> List.tail |> List.toArray |> main
#else
[<EntryPoint; STAThread>]
let entryPoint args = main args
#endif
