let (|&>) x f = f x; x
type O<'a> = { Set : 'a -> unit; Bind: ('a -> unit) -> 'a; V : 'a }
let  o (v: 'a) : O<'a> =
  let curr    = ref v
  let update  = new Event<'a>()
  { Set   = fun v -> curr := v; update.Trigger v
    Bind  = fun f -> update.Publish.Subscribe f |> ignore; curr.Value |&> update.Trigger 
    V     = v}

module Compo =
  type State = {
    Name : O<string>
  }
  let defaultState = {
    Name = o ""
  }

type State = {
  Name : O<string>
  Buttons : Compo.State list  
}

let ds = {
  Name = o""
  ; Buttons = 
    [{Name = o""}]
}

let x =
  ds.Buttons.[0].Name
