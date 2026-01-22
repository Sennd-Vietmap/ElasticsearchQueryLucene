$output = & dotnet run --project .\Example\ElasticsearchQueryLucene.Demo\ElasticsearchQueryLucene.Demo.csproj 2>&1
$output | Out-File -FilePath fulloutput.txt -Encoding UTF8 -Width 500
$output | ForEach-Object { $_.ToString() }
