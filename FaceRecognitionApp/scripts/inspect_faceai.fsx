open System.Reflection
open System.IO

let loadAsm path =
    let bytes = File.ReadAllBytes(path : string)
    try Assembly.Load(bytes) with _ -> null

let deps = [
    @"C:\Users\kattilp\.nuget\packages\sixlabors.imagesharp\3.1.2\lib\net6.0\SixLabors.ImageSharp.dll"
    @"C:\Users\kattilp\.nuget\packages\simplesim d\3.3.1\lib\net6.0\SimpleSIMD.dll"
]

AppDomain.CurrentDomain.add_AssemblyResolve(fun _ args ->
    let name = AssemblyName(args.Name).Name
    let paths = [
        @"C:\Users\kattilp\.nuget\packages\sixlabors.imagesharp\3.1.2\lib\net6.0"
        @"C:\Users\kattilp\.nuget\packages\simplesim d\3.3.1\lib\net6.0"
        @"C:\Users\kattilp\.nuget\packages\microsoft.extensions.caching.memory\8.0.0\lib\net8.0"
    ]
    paths
    |> List.tryPick (fun dir ->
        let f = Path.Combine(dir, name + ".dll")
        if File.Exists f then Some (Assembly.LoadFrom f) else None)
    |> Option.defaultValue null)

let asm = Assembly.LoadFile(@"C:\Users\kattilp\.nuget\packages\faceaisharp\0.5.23\lib\net6.0\FaceAiSharp.dll")

try
    asm.GetTypes()
    |> Array.iter (fun t -> printfn "TYPE: %s" t.FullName)
with :? ReflectionTypeLoadException as ex ->
    ex.Types |> Array.choose id |> Array.iter (fun t -> printfn "TYPE: %s" t.FullName)
    ex.LoaderExceptions |> Array.iter (fun e -> printfn "ERR: %s" e.Message)
