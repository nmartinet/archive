#load "deps.fsx"
#if INTERACTIVE
#r "WindowsBase.dll"
#r "PresentationCore.dll"
#r "PresentationFramework.dll"
#r "System.Xaml"
#r "packages/AvalonEdit/lib/Net40/ICSharpCode.AvalonEdit.dll"
#endif

open System
open System.IO
open System.Windows
open System.Windows.Controls
open System.Xml
open System.Reflection

open ICSharpCode.AvalonEdit
open ICSharpCode.AvalonEdit.CodeCompletion
open ICSharpCode.AvalonEdit.Folding
open ICSharpCode.AvalonEdit.Highlighting

#load "utils.fsx"
#load "UIBuilder.fsx"

open UIBuilder.UIb

module GuiApp =
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

  let newLoop state getter actions =
    let rec ml state =
        let act = getter()
        if act = Action Quit then
            actions act state |> ignore
        else
            ml (actions act state)
    ml state

  type public MainWindow() as t =
    inherit Window()

  let r() = System.Console.ReadLine()
  let p x = printfn "%A" x


  let NewApp =
    let getter() : Acts =
          match r() with
              | "s" | "save" -> Action Save
              | "o" | "open" -> Action Open
              | "q" -> Action Quit
              | _ -> Action NoAct

    let actions (a : Acts) =
          match a with
              | IsAction Save -> (fun s -> printfn "save"; s)
              | IsAction Open -> (fun s -> printfn "%A" s; r())
              | _    -> (fun s -> (); s)

    let w = MainWindow()
    (new Application()).Run(w)
    newLoop "test" getter actions


open GuiApp
let private main (args: string []) =
  NewApp
