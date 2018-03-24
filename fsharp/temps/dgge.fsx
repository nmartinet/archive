
#load "deps.fsx"

let ctxt = FSharp.Data.WorldBankData.GetDataContext()

let data = ctxt.Countries.Algeria.Indicators.``GDP (current US$)``

open Suave                 // always open suave
open Suave.Http.Successful // for OK-result
open Suave.Web             // for config

web_server default_config (OK (sprintf "Hello World! In 2010 Algeria earned %f " data.[2010]))
