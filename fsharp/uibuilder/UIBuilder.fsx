#load "utils.fsx"
open Utils

module UIb =
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
  let D                       = new DockPanel()
  let G                       = new Grid()
  let Menu                    = new Menu()
  let MenuItem h              = new MenuItem(Header = h)
  let setDock (x : List<obj*(Dock option)>) =
      x |> List.iter (fun (e, d) ->
        match d with
          | Some d -> DockPanel.SetDock((Co e), d)
          | None -> printfn "%A" e; ()
      )
      x

  let font f = Media.FontFamily(f)
  let BuildUI (ui : obj) = [(ui, None )]
  let AddMenu (menu:obj) ui = (menu, Some Dock.Top) :: ui
  let GetUI ui =
    printfn "%A" ui
    D
      |> C ((setDock ui) |> List.map (fun (x, y) -> x))
