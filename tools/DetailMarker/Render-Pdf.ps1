param([Parameter(Mandatory=$true)][string]$PdfPath,[Parameter(Mandatory=$true)][string]$PngPath)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null=[Windows.Storage.StorageFile,Windows.Storage,ContentType=WindowsRuntime]
$null=[Windows.Data.Pdf.PdfDocument,Windows.Data.Pdf,ContentType=WindowsRuntime]
$null=[Windows.Storage.Streams.InMemoryRandomAccessStream,Windows.Storage.Streams,ContentType=WindowsRuntime]
$null=[Windows.Storage.Streams.DataReader,Windows.Storage.Streams,ContentType=WindowsRuntime]
$null=[Windows.Data.Pdf.PdfPageRenderOptions,Windows.Data.Pdf,ContentType=WindowsRuntime]
function Wait-Operation($Operation,[Type]$ResultType) {
    $method=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {$_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'} | Select-Object -First 1
    $task=$method.MakeGenericMethod($ResultType).Invoke($null,@($Operation))
    $task.Wait()
    return $task.Result
}
$file=Wait-Operation ([Windows.Storage.StorageFile]::GetFileFromPathAsync([IO.Path]::GetFullPath($PdfPath))) ([Windows.Storage.StorageFile])
$document=Wait-Operation ([Windows.Data.Pdf.PdfDocument]::LoadFromFileAsync($file)) ([Windows.Data.Pdf.PdfDocument])
$page=$document.GetPage(0)
$stream=New-Object Windows.Storage.Streams.InMemoryRandomAccessStream
$options=New-Object Windows.Data.Pdf.PdfPageRenderOptions
$options.DestinationWidth=1800
$action=$page.RenderToStreamAsync($stream,$options)
$method=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {$_.Name -eq 'AsTask' -and -not $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncAction'} | Select-Object -First 1
$task=$method.Invoke($null,@($action));$task.Wait()
$reader=New-Object Windows.Storage.Streams.DataReader($stream.GetInputStreamAt(0))
$null=Wait-Operation ($reader.LoadAsync([uint32]$stream.Size)) ([uint32])
$bytes=New-Object byte[] $stream.Size
$reader.ReadBytes($bytes)
[IO.File]::WriteAllBytes([IO.Path]::GetFullPath($PngPath),$bytes)
$reader.Dispose();$stream.Dispose();$page.Dispose()
Write-Output "PDF_RENDERED_OK $PngPath"
