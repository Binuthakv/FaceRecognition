using FaceAiSharp;
using System;
using System.Reflection;
using System.Linq;

var calcType = typeof(ImageCalculations);
var eyeBoxMethod = calcType.GetMethod("GetEyeBoxesFromCenterPoints");
Console.WriteLine($"Return type: {eyeBoxMethod.ReturnType.FullName}");
var retType = eyeBoxMethod.ReturnType;
Console.WriteLine($"Is value type: {retType.IsValueType}");
foreach(var p in retType.GetProperties())
    Console.WriteLine($"  PROP: {p.PropertyType.FullName} {p.Name}");
foreach(var f in retType.GetFields(BindingFlags.Public | BindingFlags.Instance))
    Console.WriteLine($"  FIELD: {f.FieldType.FullName} {f.Name}");
