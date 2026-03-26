using FaceAiSharp;
using FaceAiSharp.Extensions;
using System;
using System.Reflection;

var types = new[] { typeof(ImageCalculations), typeof(GeometryExtensions) };
foreach (var t in types)
{
    Console.WriteLine($"TYPE: {t.FullName}");
    foreach(var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
    {
        var parms = string.Join(", ", System.Linq.Enumerable.Select(m.GetParameters(), p => p.ParameterType.Name + " " + p.Name));
        Console.WriteLine($"  {m.ReturnType.Name} {m.Name}({parms})");
    }
}
