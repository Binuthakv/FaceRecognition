using FaceAiSharp;
using System;
using System.Reflection;
var types = new[] { typeof(IFaceDetector), typeof(IFaceLandmarksDetector), typeof(IFaceEmbeddingsGenerator), typeof(IEyeStateDetector) };
foreach (var t in types)
{
    Console.WriteLine($"IFACE: {t.Name}");
    foreach(var m in t.GetMethods())
    {
        var parms = string.Join(", ", System.Linq.Enumerable.Select(m.GetParameters(), p => p.ParameterType.FullName + " " + p.Name));
        Console.WriteLine($"  SIG: {m.ReturnType.FullName} {m.Name}({parms})");
        if (m.ReturnType.IsGenericType)
            foreach(var ga in m.ReturnType.GetGenericArguments())
            {
                Console.WriteLine($"    GARG: {ga.FullName}");
                foreach(var gp in ga.GetProperties())
                    Console.WriteLine($"      PROP: {gp.PropertyType.Name} {gp.Name}");
            }
    }
}
