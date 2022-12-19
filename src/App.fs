module App

open Fable.Core
open Fable.Core.JsInterop
open Octokit
open SimpleFs
open System.Text.RegularExpressions

let rec GITHUB_TOKEN =
    Utils.readEnv (nameof GITHUB_TOKEN)
    |> function
        | Some s -> s
        | _ -> failwithf "missing env GTIHUB_TOKEN"

let octokit = new Octokit({ auth = GITHUB_TOKEN })

type ValidatedUrl = ValidatedUrl of string

type Preferences = { AutoRemove: bool; SleepSeconds: int }

type Input =
    | HelpInput
    | ErrorInput of string
    | UrlInput of string * Preferences
    | FilePathInput of string * Preferences

let usage =
    $"Usage: utube-go [options]
    
    Cli utility that helps send urls to utube-download
    
    Options:
    -u, --url [url]             Download single url
    -p, --path [file_path]      Download all urls specified in a text file with each url in a separate line
    
    Additional preferences:
    --auto-remove               Remove the issue after download
    --sleep [n]                 Specifies the time to sleep in seconds after sending each url
"

let printAndExit (exitNo: int) (s: string) =
    JS.console.error (s)
    Node.Api.``process``.exit (exitNo)
    failwithf "will never reach here"

let validateUrl =
    let urlPatt = new Regex(@"^http(s?)://")

    let validator (u: string) : ValidatedUrl =
        if urlPatt.IsMatch(u) then
            ValidatedUrl u
        else
            failwithf "%s is not of valid url format" u

    validator

let parseArgAsInput () : Input =
    let opts: Minimist.Opts = createEmpty<Minimist.Opts>
    opts.boolean <- Some !^[| "help"; "auto-remove" |]
    opts.string <- Some !^[| "url"; "path"; "sleep" |]
    let aliasOpt = createEmpty<Minimist.OptsAlias>
    aliasOpt.Item("help") <- !^ "h"
    aliasOpt.Item("url") <- !^ "u"
    aliasOpt.Item("path") <- !^ "p"
    opts.alias <- Some aliasOpt
    let args = Minimist.minimist (Utils.getArgs ()) opts

    let help =
        if unbox<bool> (args.Item "help") then
            Some HelpInput
        else
            None

    let autoremove = unbox<bool option> (args.Item "auto-remove")
    let sleep = unbox<string option> (args.Item "sleep")

    let pref: Preferences =
        { AutoRemove = autoremove |> Option.defaultValue false
          SleepSeconds = sleep |> Option.map int |> Option.defaultValue 10 }

    let url =
        unbox<string option> (args.Item "url")
        |> Option.map (fun u -> UrlInput(u, pref))

    let path =
        unbox<string option> (args.Item "path")
        |> Option.map (fun p -> FilePathInput(p, pref))

    [ help; url; path ]
    |> Seq.choose id
    |> Seq.tryHead
    |> Option.defaultValue (ErrorInput "Please specify --url or --path")


let sendUrl (ValidatedUrl url) (secretive: bool) : JS.Promise<unit> =
    let title =
        if secretive then
            "http remove: " + (url.Split('=') |> Seq.last)
        else
            url

    let issueReq: ICreateIssueRequest =
        { owner = "foresightyj"
          repo = "utube-download"
          title = title
          body = url }

    promise {
        let! res = octokit.rest.issues.create issueReq

        if res.status < 300 then
            ()
        else
            failwithf "Failed to create issue for url: %s with title: %s\n" url title
    }

do
    let input = parseArgAsInput ()

    let (urlsPromise, pref) =
        match input with
        | HelpInput -> printAndExit 0 usage
        | ErrorInput err -> printAndExit 1 (sprintf "%s\n\n%s" err usage)
        | UrlInput(s, pref) -> (Promise.lift ([ s ]), pref)
        | FilePathInput(p, pref) ->
            (promise {
                let! content = SimpleFs.readFile (p, { encoding = Utf8 })

                let lines =
                    content.Split('\n')
                    |> Seq.map (fun l -> l.Trim())
                    |> Seq.filter (fun l -> l.Length > 0)
                    |> Seq.toList

                return lines
             },
             pref)

    let validatedUrls =
        urlsPromise |> Promise.map (fun us -> us |> List.map validateUrl)

    promise {
        let! urls = validatedUrls
        let total = urls |> List.length
        JS.console.log ("totally", total)

        for (idx, validatedUrl) in urls |> Seq.indexed do
            let idx = idx + 1
            let (ValidatedUrl url) = validatedUrl
            printf "Sending url %d/%d: %s" idx total url
            do! sendUrl validatedUrl pref.AutoRemove
            let isLast = (idx = total)

            if isLast then
                let gotoUrl = "https://github.com/foresightyj/utube-download/actions"
                printf "Done sending %d urls...\n\nAnd go %s to checkout results\n\n" total gotoUrl
                return ()
            else
                printf "Sleep for 10s..."
                do! Promise.sleep (pref.SleepSeconds * 1000)
    }
    |> ignore
