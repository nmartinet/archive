#load "deps.fsx"

open System.IO
open HtmlAgilityPack

let readfn = System.Console.ReadLine
let wikiLinkToUrl(link:string) = "https://en.wikipedia.org" + link

let fetchData(url:string) =
    let req = System.Net.WebRequest.Create(url)
    let resp = req.GetResponse()
    let stream = resp.GetResponseStream()
    let reader = new StreamReader(stream)
    let data = reader.ReadToEnd()
    resp.Close()
    data

let htmlToDoc(html:string) =
    let doc = new HtmlAgilityPack.HtmlDocument()
    doc.LoadHtml html
    doc

let getBodyLinks(url:string) =
    let doc = url |> fetchData |> htmlToDoc
    let links = doc.DocumentNode.SelectNodes @"//*[@id=""mw-content-text""]/p[1]/a"
    links
        |> Seq.map      (fun x -> x.GetAttributeValue ("href", "no url"), x)
        |> Seq.distinctBy (fun (x,y) -> x)
        |> Seq.map (fun (x,y) -> wikiLinkToUrl x)

let getFirstBodyLink(url:string) =
    let links = getBodyLinks url
    Seq.head links

let rec getToPhilosophy(url:string) =
    printfn "%s" url
    let firstLink = getFirstBodyLink url

    match firstLink with
        | "https://en.wikipedia.org/wiki/Philosophy" -> 0
        | _ -> getToPhilosophy firstLink

getToPhilosophy "https://en.wikipedia.org/wiki/Wikipedia:Getting_to_Philosophy"
