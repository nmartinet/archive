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
