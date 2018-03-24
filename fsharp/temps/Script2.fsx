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
open System
open System.IO
open System.Text
open System.Windows
open Microsoft.FSharp.Compiler.Interactive.Shell
open Microsoft.FSharp.Compiler.Interactive
open System
open System.IO
open System.Text


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
open Agents

module Utils =
  let (|&>) x f = f x; x
  type O<'a> = { Set : 'a -> unit; Bind: ('a -> unit) -> 'a; V : 'a }
  let  o (v: 'a) : O<'a> =
    let curr    = ref v
    let update  = new Event<'a>()
    { Set   = fun v -> curr := v; update.Trigger v
      Bind  = fun f -> update.Publish.Subscribe f |> ignore; curr.Value |&> update.Trigger 
      V     = v}
  type ActQ<'a> = {Action : 'a -> unit}
  let  actQ actr =
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
open Utils
module BasicUI =
  //-Events---------------------------------
  let doOn (e:RoutedEvent) f (el:UIElement) =  
    el.AddHandler(e, new RoutedEventHandler(f))
  let click f (el:UIElement) =
    match el with
      | :? Button   as btn  -> doOn Button.ClickEvent   (fun s a -> f()) el
      | :? MenuItem as mi   -> doOn MenuItem.ClickEvent (fun s a -> f()) el
      | _ -> ()
    el
  //-Composition----------------------------
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
  //-Basic Elemtens-------------------------
  let font fontFamily = Media.FontFamily(fontFamily)
  let gl (l:float) = new GridLength(l)
  //-Layout---------------------------------
  //-dock--
  let dockPanel()     = new DockPanel()
  let dock pos o = DockPanel.SetDock(o, pos); o
  //-stackpanel-- 
  let stackPanel o  = new StackPanel(Orientation = o)
  let vStackPanel()  = stackPanel Orientation.Vertical
  let hStackPanel()  = stackPanel Orientation.Horizontal
  //-grid--
  let grid()      = new Grid()
  let colDef w (g:#Grid) =
    let cd = new ColumnDefinition()
    if w <> 0. then cd.Width <- gl w
    g.ColumnDefinitions.Add(cd); g
  let rowDef w (g:#Grid) = 
    let rd = new RowDefinition()
    if w <> 0. then rd.Height <- gl w
    g.RowDefinitions.Add(rd); g
  let colDefs widths (g:#Grid) = widths |> List.iter (fun w -> colDef w g |> ignore); g
  let rowDefs widths (g:#Grid) = widths |> List.iter (fun w -> rowDef w g |> ignore); g
  let setRow r el = Grid.SetRow(el, r); el
  let setCol c el = Grid.SetColumn(el, c); el
  let setGrid r c el = setRow r el; setCol c el
  //-menu----
  let menu()            = new Menu()
  let menuItem header   = new MenuItem(Header = header)
open BasicUI
type Action = interface end
let Do (act : Action) =
  ()
type AppActs =
  | Open
  | Save
  | New
  | Close
  interface Action
 
module Menu =
  type Actions = 
    | Open
    interface Action

  let ui =
    menu() |> children [
              menuItem  "File" |> children [
                  menuItem "Open" |> click (fun () -> Do Open)]] |> dock Dock.Top


let button (ctnt: #string) =
  let btn = new Button()
  btn.Content <- ctnt
  btn

let getHighlight hf =
  use stream = File.OpenRead(hf) in
    use reader = XmlTextReader(stream) in
      Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance )

let editor(ctnt: O<string>) =
  let e = ICSharpCode.AvalonEdit.TextEditor()
  e.FontFamily <- font "Consolas"
  e.FontSize <- 16.
  e.SyntaxHighlighting <- getHighlight "FsHighlight.xshd"
  ctnt.Bind (fun s -> e.Dispatcher.InvokeAsync(fun _ -> e.Text <- s ) |> ignore) |> ignore
  e

type State = {
  FilePath: O<string>
  FileContent: O<string>
}
let defaultState = {
      FilePath    = o ""
    ; FileContent = o ""
    }




let FSIPath = @"Fsi.exe"
type FSI = {
      UI : UIElement
    ; Run: string -> unit}

let openFile() =
  let fn = ref ""
  let t = UIT (fun () ->
    let dlg = new System.Windows.Forms.OpenFileDialog()
    dlg.ShowDialog() |> ignore
    fn:= dlg.FileName)
  t.Start()
  t.Join()
  fn.Value
let loadFile fn = File.ReadAllText(fn)


 
let Actr (state:State) action =
  match action with
    | Open ->
      let fn = openFile()
      state.FilePath.Set fn
      loadFile fn |> state.FileContent.Set
    | _ -> ()


let fsiOutput h (data:O<string>)= 
  let fsiSession = 
    FsiEvaluationSession.Create(
      FsiEvaluationSession.GetDefaultConfiguration()
    , [| FSIPath; "--noninteractive"|]
    , (new StringReader(""))
    , (new StringWriter((new StringBuilder())))
    , (new StringWriter((new StringBuilder()))))

  let evalExpr txt = fsiSession.EvalExpression(txt)
  let lbl  = new Label(Content=h)
  let runTxt() = 
    printfn "click"
    printfn "%A" data.V
    try 
      let txt = data.V
      let res = fsiSession.EvalExpression txt
      printfn "after eval"
      lbl.Content <- res.Value
    with | _ -> ()

  let lbl = new Label()
  let ui = 
    dockP() |> children [
        sPanelH() |>  dock Dock.Top |> children [ button "Run" |> click (fun () -> runTxt() )]
      ; lbl]
  {
    UI = ui
    Run = fun txt -> ()
  }

let ui (state : State) (actr:ActQ<AppActs>) =
  let fsiOut = fsiOutput "test" state.FileContent
  dockP() |> children  [
      
    ; grid()
      |> dock Dock.Bottom
      |> colDefs [0.; 100.]
      |> children [
          editor state.FileContent |> setCol 0
        ; fsiOut.UI |> setCol 1]]

let app args =
  let state = defaultState
  let q = actQ (Actr state)
  state.FilePath.Bind (fun s -> printfn "%A" s) |> ignore

  let t1 = UIThread ui state q
  t1.Start()
  t1.Join()

let main args = app args; 0
#if INTERACTIVE
fsi.CommandLineArgs |> Array.toList |> List.tail |> List.toArray |> main
#else
[<EntryPoint; STAThread>]
let entryPoint args = main args
#endif
















