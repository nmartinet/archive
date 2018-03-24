module rndWiki

open FSharp.Data

let readfn = System.Console.ReadLine
let getPage(url:string) = HtmlDocument.Load(url)
let printPage page = printfn "%s" (page.ToString())


"https://en.wikipedia.org/wiki/Gibraltar" |> getPage |> printPage

[<EntryPoint>]
let main argv =
  printfn "%A" argv


  //"http://en.wikipedia.org/" |> getPage |> printPage
  let wiki = "https://en.wikipedia.org/wiki/Gibraltar" |> getPage
  let firstLink = wiki.Descendants ["a"] |> Seq.map (fun x -> x.InnerText())
  firstLink |> printfn "%A"

  readfn()
  0 // return an integer exit code
