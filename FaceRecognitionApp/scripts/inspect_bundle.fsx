open System.Reflection

let a1 = Assembly.LoadFile(@"C:\Users\kattilp\.nuget\packages\faceaisharp\0.5.23\lib\net6.0\FaceAiSharp.dll")
let asm = Assembly.LoadFile(@"C:\Users\kattilp\.nuget\packages\faceaisharp.bundle\0.5.23\lib\net6.0\FaceAiSharp.Bundle.dll")

asm.GetTypes() |> Array.iter (fun t -> printfn "TYPE: %s" t.FullName)
