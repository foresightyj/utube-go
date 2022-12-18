// ts2fable 0.7.1
module rec Minimist

open System
open Fable.Core
open Fable.Core.JS

[<Import("*", "minimist")>]
let minimist: Minimist.IExports = jsNative

[<AllowNullLiteral>]
type IExports =
    /// <summary>Return an argument object populated with the array arguments from args</summary>
    /// <param name="args">An optional argument array (typically `process.argv.slice(2)`)</param>
    /// <param name="opts">An optional options object to customize the parsing</param>
    abstract minimist: ?args: ResizeArray<string> * ?opts: Minimist.Opts -> Minimist.ParsedArgs

module Minimist =

    [<AllowNullLiteral>]
    type Opts =
        /// A string or array of strings argument names to always treat as strings
        abstract string: U2<string, ResizeArray<string>> option with get, set
        /// A boolean, string or array of strings to always treat as booleans. If true will treat
        /// all double hyphenated arguments without equals signs as boolean (e.g. affects `--foo`, not `-f` or `--foo=bar`)
        abstract boolean: U3<bool, string, ResizeArray<string>> option with get, set
        /// An object mapping string names to strings or arrays of string argument names to use as aliases
        abstract alias: OptsAlias option with get, set
        /// An object mapping string argument names to default values
        abstract ``default``: OptsDefault option with get, set
        /// When true, populate argv._ with everything after the first non-option
        abstract stopEarly: bool option with get, set
        /// A function which is invoked with a command line parameter not defined in the opts
        /// configuration object. If the function returns false, the unknown option is not added to argv
        abstract unknown: (string -> bool) option with get, set
        /// When true, populate argv._ with everything before the -- and argv['--'] with everything after the --.
        /// Note that with -- set, parsing for arguments still stops after the `--`.
        abstract ``--``: bool option with get, set

    [<AllowNullLiteral>]
    type ParsedArgs =
        [<Emit "$0[$1]{{=$2}}">]
        abstract Item: arg: string -> obj option with get, set

        /// If opts['--'] is true, populated with everything after the --
        abstract ``--``: ResizeArray<string> option with get, set
        /// Contains all the arguments that didn't have an option associated with them
        abstract ``_``: ResizeArray<string> with get, set

    [<AllowNullLiteral>]
    type OptsAlias =
        [<Emit "$0[$1]{{=$2}}">]
        abstract Item: key: string -> U2<string, ResizeArray<string>> with get, set

    [<AllowNullLiteral>]
    type OptsDefault =
        [<Emit "$0[$1]{{=$2}}">]
        abstract Item: key: string -> obj option with get, set
