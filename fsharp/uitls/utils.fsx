open FSharp
open FSharp.Reflection

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
