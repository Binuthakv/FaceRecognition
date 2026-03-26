using FaceAiSharp;
using System;
using System.Reflection;
var t = typeof(FaceAiSharpBundleFactory);
foreach(var m in t.GetMethods(BindingFlags.Public|BindingFlags.Static))
{
    Console.WriteLine($"RETURN: {m.ReturnType.FullName}  METHOD: {m.Name}");
    foreach(var iface in m.ReturnType.GetInterfaces())
        Console.WriteLine($"  IMPLEMENTS: {iface.FullName}");
    foreach(var ifaceMethod in m.ReturnType.GetMethods())
    {
        var parms = string.Join(", ", System.Linq.Enumerable.Select(ifaceMethod.GetParameters(), p => p.ParameterType.Name + " " + p.Name));
        Console.WriteLine($"  METH: {ifaceMethod.ReturnType.Name} {ifaceMethod.Name}({parms})");
    }
}
