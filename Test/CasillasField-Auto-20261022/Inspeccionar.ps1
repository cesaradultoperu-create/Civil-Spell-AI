$ErrorActionPreference='Stop'
$source=Get-Content 'C:/Users/Cesar Ugaz/civil3D traductor/repos/CivilSpellAI/tools/AttributeLocator/Field/Open-Field.ps1' -Raw
Add-Type ([regex]::Match($source,"(?s)Add-Type @'\r?\n(.*?)\r?\n'@").Groups[1].Value)
foreach($cadProcess in Get-Process acad){Write-Output ('PROCESS '+$cadProcess.Id); foreach($window in [FieldDemoWindow]::Windows($cadProcess.Id)){Write-Output ('WINDOW '+$window+' '+[FieldDemoWindow]::Title($window))}}
foreach($doc in [FieldDemoWindow]::Documents('C:/Users/Cesar Ugaz/Pictures/cambio de numero/Ejemplo-localizar-casillas-2024/Field-v3/Ejemplo-Field-v3.dwg')){Write-Output ('DOC '+$doc.FullName+' HWND '+$doc.Application.HWND+' TYPE '+$doc.GetType().FullName)}