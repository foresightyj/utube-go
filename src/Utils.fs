[<RequireQualifiedAccess>]
module Utils

open Fable.Core

[<Emit("process.env[$0]")>]
let readEnv (key: string) : string option = jsNative


[<Emit("process.argv.slice(2)")>]
let inline getArgs () : string array = jsNative

