Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

module scaffold
  open System
  open System.IO

  type dep =
    | source
    | nuget : Liststring

  let pkt = "paket.exe"
  let pkt_url = "https://github.com/fsprojects/Paket/releases/download/0.26.3/paket.exe"
  let deps = """
      source https://nuget.org/api/v2
      nuget Suave 0.16.0
      nuget FSharp.Data
      nuget FSharp.Charting
  """

  let get_paket =
    if not (File.Exists pkt) then
        let url = pkt_url
        use wc = new Net.WebClient() in
          let tmp = Path.GetTempFileName() in
            wc.DownloadFile(url, tmp)
        File.Move(tmp,Path.GetFileName url)
    pkt

  let install_deps =
    #r "paket.exe"
    Paket.Dependencies.Install deps

  let toDep l =
    let w_arr = l.Split([|' '|])
    match w_arr with
      | [|"source"|] -> source
      | [|"nuget"|] -> nuget w_arr.[1..]
      | _ ->

  let parse_deps =
    deps.Split([|'\n'|])
      |> List.ofSeq
      |> Seq.map toDep

  let gen_deps =


// Step 1. Resolve and install the packages



// Step 2. Use the packages

#r "packages/Suave/lib/Suave.dll"
#r "packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "packages/FSharp.Charting/lib/net40/FSharp.Charting.dll"

open Suave                 // always open suave
open Suave.Http.Successful // for OK-result
open Suave.Web             // for config

let ctxt = FSharp.Data.WorldBankData.GetDataContext()
let data = ctxt.Countries.Algeria.Indicators.``GDP (current US$)``

web_server default_config (OK (sprintf "Hello World! In 2010 Algeria earned %f " data.[2010]))
