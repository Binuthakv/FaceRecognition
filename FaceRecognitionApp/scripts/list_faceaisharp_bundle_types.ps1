$basePath = Join-Path $env:USERPROFILE '.nuget\packages'
$faceAiPath = Join-Path $basePath 'faceaisharp\0.5.23\lib\net6.0\FaceAiSharp.dll'
$bundlePath = Join-Path $basePath 'faceaisharp.bundle\0.5.23\lib\net6.0\FaceAiSharp.Bundle.dll'
[System.Reflection.Assembly]::LoadFrom($faceAiPath) | Out-Null
$asm = [System.Reflection.Assembly]::LoadFrom($bundlePath)
try {
    $asm.GetTypes() | Select-Object -ExpandProperty FullName
}
catch [System.Reflection.ReflectionTypeLoadException] {
    $_.LoaderExceptions | ForEach-Object { $_.Message }
}
