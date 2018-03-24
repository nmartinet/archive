type obsValue<'a> = {
    Set       : ('a -> 'a) -> Async<'a>
    Bind      : ('a -> unit) -> IDisposable
    }

let newObs (v: 'a) =
  let Update = new Event<'a>()
  let current = ref v
  Update.Trigger v

  let SetProc : MailboxProcessor<'a * AsyncReplyChannel<'a>> =
    MailboxProcessor.Start <| fun inbox ->
      let rec loop () = async {
        let! current, chn := inbox.Receive()
        changes.Trigger current.Value
        chn.Reply current.Value
        return! loop () }
        loop ()
      {
        Set (v)     = setProc.PostAndAsyncReply (fun chn -> v, chn)
        Bind        = Update.Publish.Subscribe current
      }




printfn "ok"
