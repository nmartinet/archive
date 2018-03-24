open System
open System.Threading
open System.Threading.Tasks
open System.Reactive.Linq
open System.Reactive.Threading
open System.Reactive.Concurrency
open System.Reactive.Subjects

module Observable =

  let observeOn (scheduler:IScheduler) (xs:IObservable<'a>) =
    Observable.ObserveOn(xs, scheduler)

  let subscribeOn (scheduler:IScheduler) (xs:IObservable<'a>) =
    Observable.SubscribeOn(xs, scheduler)

  let window (f: Unit -> IObservable<'b>) (xs:IObservable<'a>)  =
    Observable.Window(xs, new Func<IObservable<'b>>(f))

  let subscribe' f g (xs:IObservable<'a>) =
    ObservableExtensions.Subscribe(xs, new Action<'a>(f), new Action(g))


let splitBy separator (xs:IObservable<'a>) =
    xs |> Observable.window (fun () -> xs |> Observable.filter (fun x -> x = separator))
       |> Observable.map (Observable.filter (fun y -> y <> separator))

type MorseCode = Node of string * MorseCode * MorseCode
               | Leaf of string
               | Empty

let morseCodeTree =
            let zeroNode =  Leaf("0")
            let nineNode =  Leaf("9")
            let dashNode =  Node("", zeroNode, nineNode)
            let nullLeaf =  Empty
            let eightNode =  Leaf("8")
            let dotNode =  Node("", nullLeaf, eightNode)
            let oNode =  Node("O", dashNode, dotNode)
            let qNode =  Node("Q", nullLeaf, nullLeaf)
            let sevenNode =  Leaf("7")
            let zNode =  Node("Z", nullLeaf, sevenNode)
            let gNode =  Node("G", qNode, zNode)
            let mNode =  Node("M", oNode, gNode)
            let yNode =  Leaf("Y")
            let cNode =  Leaf("C")
            let kNode =  Node("K", yNode, cNode)
            let xNode =  Leaf("X")
            let sixNode =  Leaf("6")
            let bNode =  Node("B", nullLeaf, sixNode)
            let dNode =  Node("D", xNode, bNode)
            let nNode =  Node("N", kNode, dNode)
            let tNode =  Node("T", mNode, nNode)
            let oneNode =  Leaf("1")
            let jNode =  Node("J", oneNode, nullLeaf)
            let pNode =  Leaf("P")
            let wNode =  Node("W", jNode, pNode)
            let lNode =  Leaf("L")
            let rNode =  Node("R", nullLeaf, lNode)
            let aNode =  Node("A", wNode, rNode)
            let twoNode =  Leaf("2")
            let udNode =  Node("", twoNode, nullLeaf)
            let fNode =  Leaf("F")
            let uNode =  Node("U", udNode, fNode)
            let threeNode =  Leaf("3")
            let vNode =  Node("V", threeNode, nullLeaf)
            let fourNode =  Leaf("4")
            let fiveNode =  Leaf("5")
            let hNode =  Node("H", fourNode, fiveNode)
            let sNode =  Node("S", vNode, hNode)
            let iNode =  Node("I", uNode, sNode)
            let eNode =  Node("E", aNode, iNode)
            Node("", tNode, eNode)

let extractChar n = match n with
                    | Node (ch, _, _) -> ch
                    | Leaf ch -> ch
                    | Empty -> ""

let processChar acc ch = match (ch, acc) with
                         | '-', Node (_, dash, _) -> dash
                         | '.', Node (_, _, dot) -> dot
                         | _ -> Empty

let translateMorseCode xs =
              xs |> splitBy ' '
                 |> Observable.map(Observable.scan processChar morseCodeTree >> Observable.map extractChar)


let processKeyPress(subject:ISubject<char>) =
              let mutable info:ConsoleKeyInfo option = None
              while (info.IsNone || info.Value.Key <> ConsoleKey.Enter) do
                       info <- Some (Console.ReadKey())
                       subject.OnNext info.Value.KeyChar
              Environment.Exit(0)

let getKeyPresses =
            let subject = new Subject<char>()
            Task.Run(fun () -> processKeyPress(subject)) |> ignore
            subject |> Observable.observeOn(CurrentThreadScheduler.Instance)

let mutable index = 0

let writeCode (x:string) =
    let oldpos = Console.CursorLeft
    Console.SetCursorPosition (index, 1)
    Console.Write x
    Console.SetCursorPosition (oldpos, 0)

getKeyPresses |> translateMorseCode
              |> Observable.subscribe(Observable.subscribe' writeCode  (fun () -> index <- index + 1) >> ignore)
              |> ignore

Thread.Sleep -1
