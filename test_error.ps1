try {
    & ".\Example\ElasticsearchQueryLucene.Demo\bin\Debug\net10.0\ElasticsearchQueryLucene.Demo.exe"
} catch {
    Write-Host "Error: $_"
    Write-Host "Exception: $($_.Exception)"
    Write-Host "StackTrace: $($_.Exception.StackTrace)"
}
