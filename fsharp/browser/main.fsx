#load "deps.fsx"

#r "libs/cef/CefSharp.Wpf.dll"
#r "libs/cef/CefSharp.dll"

#r "WindowsBase.dll"
#r "PresentationCore.dll"
#r "PresentationFramework.dll"
#r "System.Xaml"

open System
open System.Windows
open System.Windows.Controls
open CefSharp.Wpf
open CefSharp

type StackPanel with
  member o.add x = o.Children.Add x |> ignore

type MainWindow(args) as this =
  inherit Window()

let private main (args: string []) =
  let grid = Grid()

  let settings = new Settings()
  settings.PackLoadingDisabled <- true

  if (CEF.Initialize(settings)) then
    printfn "%A" settings
    let webView = new WebView()
    grid.Children.Add webView |> ignore
    webView.Address <- "http://google.com"
    printfn "%A" webView

  (*
  let wView = new CefSharp.Wpf.WebView()
  stackPanel.add wView
  wView.Address <- "http://www.google.com"

  stackPanel.add (Label(Content="value",Width=50.))
*)
  let window = Window(Title="", Width=200., Height=200., Content=grid)
  (new Application()).Run(window)

#if INTERACTIVE
fsi.CommandLineArgs |> Array.toList |> List.tail |> List.toArray |> main
#else
[<EntryPoint; STAThread>]
let entryPoint args = main args
#endif
