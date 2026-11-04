$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/../CasillasField-Sotrami-20261022/session-api.ps1"
$cadLabelApp = [FieldDemoWindow]::Active()
$cadLabelDoc = $cadLabelApp.ActiveDocument
if ($null -eq $cadLabelDoc) { throw 'No active AutoCAD document is accessible.' }
$report = [ordered]@{ Drawing = [string]$cadLabelDoc.Name; Command = [string]$cadLabelDoc.GetVariable('CMDNAMES'); Scale = [string]$cadLabelDoc.GetVariable('CANNOSCALE'); InsUnits = [string]$cadLabelDoc.GetVariable('INSUNITS') }
$style = $cadLabelDoc.TextStyles.Item('WWL_Texto 2.50mm')
$face = ''; $bold = $false; $italic = $false; $charset = 0; $pitch = 0
$style.GetFont([ref]$face, [ref]$bold, [ref]$italic, [ref]$charset, [ref]$pitch)
$report.TextStyle = [ordered]@{ Name = [string]$style.Name; Height = [double]$style.Height; Width = [double]$style.Width; FontFile = [string]$style.FontFile; Face = [string]$face; Bold = [bool]$bold; Italic = [bool]$italic; CharSet = [int]$charset; Pitch = [int]$pitch }
$mtextResults = @()
foreach ($entity in $cadLabelDoc.ModelSpace) {
    if ($entity.ObjectName -eq 'AcDbMText' -and ($entity.TextString -replace '[^0-9]', '') -match '8302457') {
        $mtextResults += [ordered]@{ Handle = [string]$entity.Handle; Contents = [string]$entity.TextString; Height = [double]$entity.Height; Style = [string]$entity.StyleName; Width = [double]$entity.Width; Rotation = [double]$entity.Rotation; Layer = [string]$entity.Layer; InsertionPoint = @($entity.InsertionPoint | ForEach-Object { [double]$_ }) }
    }
}
$report.ReferenceMText = $mtextResults
$civil = $cadLabelApp.GetInterfaceObject('AeccXUiLand.AeccApplication.13.6')
$civilDoc = $civil.ActiveDocument
if ([string]$civilDoc.Name -ne [string]$cadLabelDoc.Name) { throw 'The Civil document differs from the AutoCAD document.' }
$units = $civilDoc.Settings.DrawingSettings.UnitZoneSettings
$report.CivilUnits = [ordered]@{ Units = [string]$units.DrawingUnits; Scale = [double]$units.DrawingScale }
$styleReports = @()
foreach ($styleName in @('Norte', 'Este', 'Norte [WWL2.5_text]')) {
    $labelStyle = $civilDoc.GeneralLineLabelStyles.Item($styleName)
    $components = @()
    foreach ($component in $labelStyle.TextComponents) {
        $components += [ordered]@{ Name = [string]$component.Name; Height = [double]$component.Height.Value; Contents = [string]$component.Contents.Value; Anchor = [string]$component.AnchorComponent.Value; AnchorPoint = [int]$component.AnchorPoint.Value; Attachment = [int]$component.Attachment.Value; Angle = [double]$component.Angle.Value; Mode = [int]$component.Mode.Value; Justification = [int]$component.Justification.Value; Mask = [bool]$component.BorderStyle.BackgroundMask; BorderGap = [double]$component.BorderStyle.Gap.Value }
    }
    $styleReports += [ordered]@{ Name = $styleName; Handle = [string]$labelStyle.Handle; TextStyle = [string]$labelStyle.LabelProperties.TextStyle.Value; Layer = [string]$labelStyle.LabelProperties.Layer.Value; Components = $components }
}
$report.LabelStyles = $styleReports
$selected = @()
foreach ($entity in $cadLabelDoc.PickfirstSelectionSet) {
    $selected += [ordered]@{ Handle = [string]$entity.Handle; ObjectName = [string]$entity.ObjectName }
}
$report.Selected = $selected
$json = $report | ConvertTo-Json -Depth 12
$json | Set-Content -LiteralPath "$PSScriptRoot/style-source-readonly.json" -Encoding UTF8
$json
