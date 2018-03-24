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

type O<'a> = { Set : 'a -> unit; Bind: ('a -> unit) -> 'a }
let o (v: 'a) : O<'a> =
  let curr    = ref v
  let Update  = new Event<'a>()
  { Set   = fun v -> curr := v; Update.Trigger v
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

  match w with
    | 0. ->  ()
    | _  ->  cd.Width <- (new GridLength(w))
  g.ColumnDefinitions.Add(cd)
  g

let colDefs widths (g:#Grid) =
  widths |> List.iter  (fun w -> colDef w g |> ignore )
  g

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


let menu()        = new Menu()
let menuItem hdr  = new MenuItem(Header = hdr)

let button (ctnt: O<string>) f =
  let btn = new Button()
  ctnt.Bind (fun s -> btn.Dispatcher.InvokeAsync(fun _ -> btn.Content <- s ) |> ignore) |> ignore
  doOn Button.ClickEvent (fun s a -> f() ) btn
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

type State = {
  FilePath: O<string>
  FileContent: O<string>
}

let defaultState = {
      FilePath    = o ""
    ; FileContent = o ""

    }

type Action = interface end
type AppActs =
  | Open
  | Save
  | New
  | Close
  interface Action

let fsiOutput h =
  new Label(Content = h)


let Actr (state:State) action =
  match action with
    | Open ->
      let fn = openFile()
      state.FilePath.Set fn
      loadFile fn |> state.FileContent.Set
    | _ -> ()

let ui (state : State) (actr:ActQ<AppActs>) =
  dockP() |> children  [
      menu() |> children [
          menuItem  "File" |> children [
              menuItem "Open" |> click (fun () -> actr.Action Open)]] |> dock Dock.Top
    ; grid()
      |> dock Dock.Bottom
      |> colDefs [0.; 100.]
      |> children [
          editor state.FileContent |> setCol 0
        ; fsiOutput "test" |> setCol 1]]



let app() =
  let state = defaultState
  let q = actQ (Actr state)
  state.FilePath.Bind (fun s -> printfn "%A" s) |> ignore

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
