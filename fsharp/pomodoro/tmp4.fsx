namespace FLib
module GUI =
  open System
  open System.Windows
  open System.IO
  open System.Windows.Controls

  open Util


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
  let setDock (x : List<obj*(Dock option)>) =
      x |> List.iter (fun (e, d) ->
        match d with
          | Some d -> DockPanel.SetDock((Co e), d)
          | None -> printfn "%A" e; ()
      )
      x


  let BuildUI (ui : obj) = [(ui, Some Dock.Bottom)]
  let AddMenu (menu:obj) ui = (menu, Dock.Top) :: ui
  let GetUI (ui : List<obj*(Dock option)>)  =    D |> C ((setDock ui) |> List.map (fun (x, y) -> x))

  type StateService<'a> = {
    Current   : unit -> 'a
    Subscribe : ('a -> unit) -> IDisposable
    Modify    : ('a -> 'a) -> Async<'a>
    }

  let newState (def: 'a) =
    let current = ref def
    let changes = new Event<'a>()

    let currentProc :  MailboxProcessor<AsyncReplyChannel<'a>> =
      MailboxProcessor.Start <| fun inbox ->
      let rec loop () = async {
        let! chn = inbox.Receive ()
        chn.Reply current.Value
        return! loop ()}
      loop ()

    let modifyProc : MailboxProcessor<('a -> 'a) * AsyncReplyChannel<'a>> =
      MailboxProcessor.Start <| fun inbox ->
        let rec loop () = async {
          let! f, chn = inbox.Receive ()
          let v = current.Value
          try
            current := f v
            changes.Trigger current.Value
            chn.Reply current.Value
          with
            | e -> chn.Reply current.Value
          return! loop () }
        loop ()
    {
      Current     = fun () -> currentProc.PostAndReply id
      Subscribe   = fun f  -> changes.Publish.Subscribe f
      Modify      = fun f  -> modifyProc.PostAndAsyncReply (fun chn -> f, chn)
    }
