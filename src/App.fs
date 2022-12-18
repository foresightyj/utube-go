module App

open Fable.Core
open Octokit
open SimpleFs
open System.Text.RegularExpressions

let rec GITHUB_TOKEN =
    Utils.readEnv (nameof GITHUB_TOKEN)
    |> function
        | Some s -> s
        | _ -> failwithf "missing env GTIHUB_TOKEN"

let octokit = new Octokit({ auth = GITHUB_TOKEN })

type Input =
    | UrlInput of string
    | FilePathInput of string

type ValidatedUrl = ValidatedUrl of string

let usage =
    $"Usage: 
    utube-go -u url
    utube-go -p path (where path is path to a text file with each line containing a url)
"

let printAndExit (exitNo: int) (s: string) =
    JS.console.error (s)
    Node.Api.``process``.exit (exitNo)
    failwithf "will never reach here"

let input =
    Utils.getArgs ()
    |> function
        | [| "-u"; u |]
        | [| "--url"; u |] -> UrlInput u
        | [| "-p"; p |]
        | [| "--path"; p |] -> FilePathInput p
        | [| "-h" |]
        | [| "--help" |] -> printAndExit 0 usage
        | _ -> printAndExit 1 usage

let urlPatt = new Regex(@"^http(s?)://")

let validateUrl u : ValidatedUrl =
    if urlPatt.IsMatch(u) then
        ValidatedUrl u
    else
        failwithf "%s is not of valid url format" u


let urlsPromise: JS.Promise<ValidatedUrl list> =
    let urls =
        match input with
        | UrlInput s -> Promise.lift ([ s ])
        | FilePathInput f ->
            promise {
                let! content = SimpleFs.readFile (f, { encoding = Utf8 })

                let lines =
                    content.Split('\n')
                    |> Seq.map (fun l -> l.Trim())
                    |> Seq.filter (fun l -> l.Length > 0)
                    |> Seq.toList

                return lines
            }

    urls |> Promise.map (fun us -> us |> List.map validateUrl)

let sendUrl (ValidatedUrl url) : JS.Promise<unit> =
    let title = "http remove: " + (url.Split('=') |> Seq.last)

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

promise {
    let! urls = urlsPromise
    let total = urls |> List.length
    JS.console.log ("totally", total)

    for (idx, validatedUrl) in urls |> Seq.indexed do
        let idx = idx + 1
        let (ValidatedUrl url) = validatedUrl
        printf "Sending url %d/%d: %s\n" idx total url
        do! sendUrl validatedUrl
        let isLast = (idx = total)

        if isLast then
            let gotoUrl = "https://github.com/foresightyj/utube-download/actions"
            printf "Done sending %d urls...\n\nAnd go %s to checkout results\n\n" total gotoUrl
            return ()
        else
            JS.console.log ("Sleep for 10s...")
            do! Promise.sleep (10000)
}
|> ignore
