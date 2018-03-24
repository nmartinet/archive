// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System
open System.Windows
open System.IO
open System.Xml
open System.Windows.Controls
open ICSharpCode.AvalonEdit
open ICSharpCode.AvalonEdit.CodeCompletion
open ICSharpCode.AvalonEdit.Folding
open ICSharpCode.AvalonEdit.Highlighting
open FSharp.Reflection
open System.Reflection
open Microsoft.FSharp.Reflection

let (?) (o : _) (m : _) : 'R =
    match FSharpType.IsFunction(typeof<'R>) with
        | true  ->
            let argType, resType = FSharpType.GetFunctionElements(typeof<'R>)
            FSharpValue.MakeFunction(typeof<'R>, fun args ->
            let args = if argType = typeof<unit> then [| |]
                        elif not(FSharpType.IsTuple(argType)) then [| args |]
                        else FSharpValue.GetTupleFields(args)

            o.GetType().GetMethod(m).Invoke(o, args)
            )|> unbox<'R>
        | false ->
            o.GetType().GetProperty(m).GetGetMethod(true).Invoke(o, [||]) |> unbox<'R>
let Co (x:obj) = match x with | :? 'a as a -> a

let HasMeth o p =
    if (o.GetType().GetMethods()
        |> Seq.map      (fun x -> x.Name)
        |> Seq.filter   (fun x -> if x = p then true else false)
        |> List.ofSeq).Length = 0 then false else true
let HasProp o p =
    if (o.GetType().GetProperties()
        |> Seq.map      (fun x -> x.Name)
        |> Seq.filter   (fun x -> if x = p then true else false)
        |> List.ofSeq).Length = 0 then false else true

let (|HasMethA|_|) x y = if HasMeth y x then Some HasMethA else None
let (|HasPropA|_|) x y = if HasProp y x then Some HasPropA else None

module vf =
    let C (x : _ List) (o:obj) =
        match o with
            | HasPropA "Children" ->    x |> List.iter (fun x ->  o?Children?Add x |> ignore)
            | HasPropA "Items" ->       x |> List.iter (fun x -> o?Items?Add  x |> ignore)
            | _ -> ()
        o
    let addCLick f ctrl = ctrl?Click?Add f; ctrl

    let SP (o : Orientation)    = new StackPanel(Orientation=o)
    let SPV                     = SP Orientation.Vertical
    let SPH                     = SP Orientation.Horizontal
    let D                       = new DockPanel(VerticalAlignment = VerticalAlignment.Stretch,
                                                Height = Double.NaN,
                                                Width = Double.NaN)
    let Menu = new Menu( HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                         VerticalAlignment = System.Windows.VerticalAlignment.Top)
    let MenuItem h = new MenuItem(Header = h)
    let setDock (x : List<obj*Dock>) =
        x |> List.iter (fun (e, d) -> DockPanel.SetDock((Co e), d));x

    let BuildUI (ui : obj) = [(ui, Dock.Bottom)]
    let AddMenu (menu:obj) ui = (menu, Dock.Top) :: ui
    let GetUI ui = D |> C (setDock ui)




open vf



type public MainWindow() as t =
  inherit Window()

  let createMenu() =
    Menu
      |> C [MenuItem "File"
        |> C [MenuItem "Open"; MenuItem "Close"]]

  do

    t.Title <- "Editor"
    let e = new ICSharpCode.AvalonEdit.TextEditor()
    t.Content <- e

type Acts = interface end
let Action x = x :> Acts
let (|IsAction|_|) x y = if Action x = Action y then Some IsAction else None
type BasicAct =
    | Quit
    | NoAct
    interface Acts

type Actions =
    | Save
    | Open
    interface Acts

let newApp getter actions state =
    let rec ml state =
        let act = getter()
        if act = Action Quit then
            actions act state |> ignore
        else
            ml (actions act state)
    ml


let getter() : Acts =
        match r() with
            | "s" | "save" -> Action Save
            | "o" | "open" -> Action Open
            | "q" -> Action Quit
            | _ -> Action NoAct

    let actions (a : Acts) =
        match a with
            | IsAction Save -> (fun s -> printfn "save";)
            | IsAction Open -> (fun s -> printfn "%A" s;)
            | _    -> (fun s -> (); s)

    let app =  newApp getter actions

let r() = System.Console.ReadLine()
let p x = printfn "%A" x
let private main (args: string []) =

    app "test"
    0
//  let w = MainWindow()
//  (new Application()).Run(w)

#if INTERACTIVE
fsi.CommandLineArgs |> Array.toList |> List.tail |> List.toArray |> main
#else
[<EntryPoint; STAThread>]
let entryPoint args = main args
#endif
