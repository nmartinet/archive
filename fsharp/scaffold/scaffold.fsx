open System
open System.IO

#if INTERACTIVE
if not (File.Exists "paket.dependencies") then
  File.WriteAllText("paket.dependencies", "")
if not (File.Exists "paket.exe") then
    let url = "https://github.com/fsprojects/Paket/releases/download/0.26.3/paket.exe"
    use wc = new Net.WebClient()
    let tmp = Path.GetTempFileName()
    wc.DownloadFile(url, tmp);
    File.Move(tmp,Path.GetFileName url)

#r "paket.exe"
#endif



Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

module FFS =
  let DownloadIfNeeded (url : string) (file : string) =
    if not (File.Exists file) then
      use wc = new Net.WebClient() in
        let tmp = Path.GetTempFileName() in
          wc.DownloadFile(url, tmp)
      File.Move(tmp,Path.GetFileName url)


  let rec FirstExistingFile files =
    match files with
      | [] -> None
      | x :: xs -> if (File.Exists x)  then Some x
                                       else FirstExistingFile xs

  let (|FileEndsWith|_|) ext (file : string) =
    if file.EndsWith(ext) then Some() else None

module Pkt =
  type PaketDependency =
      | DepSource
      | DepNuget of string
      | Err

  let DepToString = function
    | DepNuget x -> x
    | DepSource -> ""
    | Err -> ""

  let StringToDep (d : string) =
      let w_arr = List.ofSeq(d.Trim().Split([|' '|]))
      match w_arr with
          | "source"  :: tail -> DepSource
          | "nuget"   :: tail -> DepNuget w_arr.[1]
          | _                 -> Err

  let pkt_exe = "paket.exe"
  let pkt_url = "https://github.com/fsprojects/Paket/releases/download/0.26.3/paket.exe"

  let getPkt() =
    FFS.DownloadIfNeeded pkt_url pkt_exe

  let Install (deps : string) =
    Paket.Dependencies.Install deps

  let GetLoadStatementFromFile = function
    | FFS.FileEndsWith ".fsx" -> Some @"#load "
    | FFS.FileEndsWith ".dll" -> Some @"#r "
    | _                   -> None


  let GetLibPath (s : string) =
    let base_p =  @"./packages/" + s + @"/"
    let lib_p = base_p + @"lib/"

    FFS.FirstExistingFile [base_p + s + @".fsx";
                       lib_p + s + ".dll";
                       lib_p + @"net40/" + s + ".dll"]


  let GetLoadStatementFromDep (depStr : string) =
    match StringToDep(depStr) with
      | DepSource | Err -> None
      | DepNuget d ->
        match GetLibPath(d) with
          | Some p ->
            match GetLoadStatementFromFile(p) with
              | Some stmnt -> Some (stmnt + @"""" + p + @"""")
              | None -> None
          | None -> None

  let GetLoaderContentFromDeps (deps : string) =
    deps.Split([|'\n'|])
      |> List.ofSeq
      |> Seq.map GetLoadStatementFromDep
      |> Seq.choose id

module Scaffold =
  let OutputLoader (f:string) (d:string) =
    let d_list = d |> Pkt.GetLoaderContentFromDeps
    File.WriteAllLines(f, d_list)

  let DepString =
    File.ReadAllText("paket.dependencies")

  let Install() =
    let d = DepString
    Pkt.Install d
    OutputLoader @"deps.fsx" d

  let Init() =
    Pkt.getPkt()
    Install()

let main args =
  match args with
    | "init"    :: tail -> Scaffold.Init()
    | "install" :: tail -> Scaffold.Install()
    | []                ->
      printfn "No arguments given - installing packages"
      Scaffold.Install()
    | _                 ->
      printfn "Error -- Unrecognised argument"

  0


#if INTERACTIVE
printfn "%A" fsi.CommandLineArgs

fsi.CommandLineArgs
  |> Array.toList
  |> List.tail
  |> main
#else
[<EntryPoint; STAThread>]
let entryPoint args =
  main args
#endif
