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

let (?) (o : obj) (m : _) : 'R =
    match FSharpType.IsFunction(typeof<'R>) with
        | true  ->
            let argType, resType = FSharpType.GetFunctionElements(typeof<'R>)
            FSharpValue.MakeFunction(typeof<'R>, fun args ->
            let args = if argType = typeof<unit> then [| |]
                        elif not(FSharpType.IsTuple(argType)) then [| args |]
                        else FSharpValue.GetTupleFields(args)

            [for meth in (o.GetType().GetMethods()) do
                if meth.Name = m && meth.GetParameters().Length = args.Length then yield meth]
                .Head.Invoke(o, args)
            )|> unbox<'R>
        | false ->
            o.GetType().GetProperty(m).GetGetMethod(true).Invoke(o, [||]) |> unbox<'R>
let inv o m = o?m

module vf =
    let Co (x:obj) = match x with | :? 'a as a -> a
    let (|Eq|_|) x y = if x=y then Some Eq else None

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

    let (?) o p =
        (o)

    let (|HasMethA|_|) x y = if HasMeth y x then Some HasMethA else None
    let (|HasPropA|_|) x y = if HasProp y x then Some HasPropA else None

    let AddC (x : _ List) (o:obj) =
        match o with
            | HasPropA "Children" ->  x |> List.iter (fun x ->  o?Children?Add x |> ignore)
            | HasPropA "Items" ->  x |> List.iter (fun x -> o?Items?Add  x |> ignore)
            | _ -> ()
        o



    let SP (o : Orientation) (x : List<'t>) = AddC x (new StackPanel(Orientation=o))
    let SPV x = SP Orientation.Vertical x
    let SPH x = SP Orientation.Horizontal x

    let Menu (x : List<'T>) =
        let menu = Menu(HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, VerticalAlignment = System.Windows.VerticalAlignment.Top)
        x |> List.iter (fun x -> menu.Items.Add x |> ignore)
        menu



    let DockPanel (x : List<obj*Dock>) =
        let dock = new DockPanel()
        let setDock (ui:UIElement) (d:Dock) = DockPanel.SetDock(ui, d); ui
        dock.VerticalAlignment <- VerticalAlignment.Stretch
        dock.Height <- Double.NaN
        dock.Width <- Double.NaN

        x |> List.iter (fun (e, d) ->
            dock.Children.Add (setDock (Co e) d) |> ignore
        )

        dock




    let MenuItem h =
        let menuItem = new MenuItem(Header = h)
        menuItem
    let ($) x f = f x; x
    let addCLick f (ctrl : #MenuItem) =
          ctrl.Click.Add f; ctrl
    let subMenu items (ctrl : #MenuItem) =
        items |> List.iter (fun i -> ctrl.Items.Add i |> ignore); ctrl

    let BuildUI (ui : obj) = [(ui, Dock.Bottom)]
    let AddMenu (menu:obj) ui = (menu, Dock.Top) :: ui

    let GetUI ui =
        DockPanel ui



open vf
type public MainWindow() as t =
  inherit Window()

  let p x = printfn "%A" x

  do


    t.Title <- "Editor"
    let menu =  Menu [ MenuItem "File"
                        |> subMenu [MenuItem "Open"; MenuItem "Close"] ]



    t.Content <- sp



let private main (args: string []) =
  let w = MainWindow()
  (new Application()).Run(w)

#if INTERACTIVE
fsi.CommandLineArgs |> Array.toList |> List.tail |> List.toArray |> main
#else
[<EntryPoint; STAThread>]
let entryPoint args = main args
#endif
