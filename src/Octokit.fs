module Octokit

open Fable.Core
open Fable.Core.JS

type OctokitConfig = { auth: string }

type ICreateIssueRequest =
    { owner: string
      repo: string
      title: string
      body: string }

type ICreateIssueResponse = { status: int }

type IIssues =
    abstract member create: ICreateIssueRequest -> Promise<ICreateIssueResponse>

type IRestApi =
    abstract member issues: IIssues

[<ImportMember("octokit")>]
type Octokit(config: OctokitConfig) =
    member x.rest: IRestApi = jsNative
