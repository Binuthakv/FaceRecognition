using FaceAiSharp;
using System;
using System.Reflection;

var t = typeof(EyeBoxes);
Console.WriteLine($"TYPE: {t.FullName}");
foreach(var p in t.GetProperties())
    Console.WriteLine($"  PROP: {p.PropertyType.FullName} {p.Name}");
foreach(var f in t.GetFields())
    Console.WriteLine($"  FIELD: {f.FieldType.FullName} {f.Name}");
