open System
open System.IO

let pkt = "paket.exe"
let pkt_url = "https://github.com/fsprojects/Paket/releases/download/0.26.3/paket.exe"

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

if not (File.Exists pkt) then
    let url = pkt_url
    use wc = new Net.WebClient() in
      let tmp = Path.GetTempFileName() in
        wc.DownloadFile(url, tmp)
    File.Move(tmp,Path.GetFileName url)

// Step 1. Resolve and install the packages

#r "paket.exe"

Paket.Dependencies.Install """
    source https://nuget.org/api/v2
    nuget Suave 0.16.0
    nuget FSharp.Data
    nuget FSharp.Charting
""";;

// Step 2. Use the packages


#load "deps.fsx"

let ctxt = FSharp.Data.WorldBankData.GetDataContext()

let data = ctxt.Countries.Algeria.Indicators.``GDP (current US$)``

open Suave                 // always open suave
open Suave.Http.Successful // for OK-result
open Suave.Web             // for config

web_server default_config (OK (sprintf "Hello World! In 2010 Algeria earned %f " data.[2010]))
