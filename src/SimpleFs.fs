module SimpleFs

open Fable.Core
open Fable.Core.JS

[<StringEnum>]
type FileEncoding = | [<CompiledName("utf-8")>] Utf8

type ReadFileOption = { encoding: FileEncoding }

type Fsp =
    abstract member readFile: path: string * option: ReadFileOption -> Promise<string>

[<Import("promises", from = "fs")>]
let SimpleFs: Fsp = jsNative
