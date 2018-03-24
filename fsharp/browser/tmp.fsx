#r "WindowsBase.dll"
#r "PresentationCore.dll"
#r "PresentationFramework.dll"
#r "System.Xaml"

open System
open System.Windows
open System.Windows.Controls

let inline (|&) x a = x |> a |> ignore; x

let inline fac<'T>() =
  FrameworkElementFactory(typeof<'T>)
let inline (<>+) o v   =
  ( ^a : (member AppendChild : 'b -> unit) (o, v) )



type MainWindow(args) as this =
  inherit Window()

let private main (args: string []) =
  let grid = fac<Grid>() |& fun g ->
    g <>+ (new Label(Content="test"))

  let window = Window(Title="", Width=200., Height=200., Content=grid)
  (new Application()).Run(window)

#if INTERACTIVE
fsi.CommandLineArgs |> Array.toList |> List.tail |> List.toArray |> main
#else
[<EntryPoint; STAThread>]
let entryPoint args = main args
#endif
