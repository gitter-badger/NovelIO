﻿(*
   Copyright 2015 Philip Curzon

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*)

namespace NovelFS.NovelIO

/// A type representing a generic binary read format
type IBinaryReadFormat =
    /// Skip past this structure in the supplied binary reader
    abstract member Skip : System.IO.BinaryReader -> unit

/// Base class for binary read formats
[<AbstractClass>]
type BinaryReadFormat<'a>() =
    /// Read this format structure from a supplied binary reader
    abstract member Read : System.IO.BinaryReader -> 'a
    /// The minimum number of bytes that will be read when this format is read from a stream
    abstract member MinByteCount : int
    /// Skip past this structure in the supplied binary reader
    member this.Skip br = this.Read br |> ignore
    /// IBinaryReadFormat implementation
    interface IBinaryReadFormat with
        member this.Skip br = this.Skip br

/// A binary read format of one type
and private BinaryReadFormatSingle<'a> (readFunc, ?size : int) =
    inherit BinaryReadFormat<'a>()
    let size = 
        match size with
        |Some sz -> sz
        |None -> sizeof<'a>
    /// Read this format structure from a supplied binary reader
    override this.Read lst =
        readFunc lst
    /// The minimum number of bytes that will be read when this format is read from a stream
    override this.MinByteCount = size

/// Encapsulates the current state of binary file reading.  Reading from the same token will, except in exceptional circumstances, produce the same result.
type BinaryFileState(fname : string, br : System.IO.BinaryReader) =
    let mutable valid = true
    /// Get the reader associated with this state.  If the state is valid, using the existing one, otherwise make a new one nand move to the correct position
    let getReader() =
        match valid with
        |true -> br
        |false -> raise <| System.InvalidOperationException ""
    /// Disposes the stream associated with this binary file state and invalidates the token
    member internal this.Dispose() =
        valid <- false
        br.Dispose()
    /// Read from the current binary file state using the supplied binary read format
    member internal this.ReadUsing (readFormat : BinaryReadFormat<_>) =
        let reader = getReader()
        let result = readFormat.Read <| reader
        let newToken = BinaryFileState(fname, reader)
        valid <- false
        result, newToken
    interface IIO

/// Functions for performing binary IO operations
module BinaryIO =
    /// Create a binary read token for a supplied file name
    let private createToken fName =
        BinaryFileState( fName, new System.IO.BinaryReader(System.IO.File.OpenRead(fName)) )
    /// Create a binary read token for a supplied file name
    let private destroyToken (token : BinaryFileState) =
        token.Dispose()
    /// Read from a supplied binary state using a supplied binary read format
    let run fName bfs =
        match bfs <| createToken fName with
        |IOSuccess (res, token) ->
            destroyToken token
            IOSuccess(res, ())
        |IOError e -> IOError e
    
    let private readBasic f (brt : BinaryFileState) = 
        IO.performIoWithExceptionCheck (fun () -> brt.ReadUsing f)
    /// A binary read format for reading bytes
    let readByte brt = readBasic (BinaryReadFormatSingle(fun br -> br.ReadByte()) :> BinaryReadFormat<_>) brt
    /// A binary read format for reading chars
    let readChar brt = readBasic (BinaryReadFormatSingle(fun br -> br.ReadChar()) :> BinaryReadFormat<_>) brt
    /// A binary read format for reading a decimal number
    let readDecimal brt = readBasic (BinaryReadFormatSingle(fun br -> br.ReadDecimal()) :> BinaryReadFormat<_>) brt
    /// A binary read format for reading 16 bit ints
    let readInt16 brt = readBasic (BinaryReadFormatSingle(fun br -> br.ReadInt16()) :> BinaryReadFormat<_>) brt
    /// A binary read format for reading (32 bit) ints
    let readInt32 brt = readBasic (BinaryReadFormatSingle(fun br -> br.ReadInt32()) :> BinaryReadFormat<_>) brt
    /// A binary read format for reading 64 bit ints
    let readInt64 brt = readBasic (BinaryReadFormatSingle(fun br -> br.ReadInt64()) :> BinaryReadFormat<_>) brt
    /// A binary read format for reading (double-precision) floats
    let readFloat brt = readBasic (BinaryReadFormatSingle(fun br -> br.ReadDouble()) :> BinaryReadFormat<_>) brt
    /// A binary read format for reading single-precision floating point numbers
    let readFloat32 brt =  readBasic (BinaryReadFormatSingle(fun br -> br.ReadDouble()) :> BinaryReadFormat<_>) brt
    /// A binary read format for reading a length prefixed string
    let readString brt = readBasic (BinaryReadFormatSingle(fun br -> br.ReadString()) :> BinaryReadFormat<_>) brt