param(
    [string]$OutputFile = "Products_FromLocalDB_Insert.sql",
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=TinyShopDB;Integrated Security=true;TrustServerCertificate=True;"
)

function Escape-SqlString {
    param([string]$value)
    if ($null -eq $value) { return $null }
    return ($value -replace "'", "''" -replace "`r", ' ' -replace "`n", ' ')
}

function To-HexLiteral {
    param([byte[]]$bytes)
    if ($null -eq $bytes) { return 'NULL' }
    return '0x' + ([BitConverter]::ToString($bytes) -replace '-', '')
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputPath = Join-Path $scriptDir $OutputFile

$query = @'
SELECT
    Id,
    Name,
    [Description],
    Details,
    Price,
    ImageUrl,
    ImageData,
    DescriptionEmbedding,
    JSON_QUERY((SELECT DescriptionVector FOR JSON PATH, WITHOUT_ARRAY_WRAPPER), '$.DescriptionVector') AS DescriptionVectorJson,
    JSON_QUERY((SELECT NameVector FOR JSON PATH, WITHOUT_ARRAY_WRAPPER), '$.NameVector') AS NameVectorJson
FROM dbo.Products;
'@

Write-Host "Connecting to LocalDB and exporting rows to $outputPath"
Write-Host "This export omits CreatedDate and ModifiedDate so the table defaults can apply."

$rows = [System.Collections.Generic.List[string]]::new()

Add-Type -AssemblyName System.Data
$connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$command = $connection.CreateCommand()
$command.CommandText = $query
$connection.Open()
$reader = $command.ExecuteReader()

while ($reader.Read()) {
    $id = $reader.GetInt32(0)
    $name = Escape-SqlString($reader.GetString(1))
    $description = Escape-SqlString($reader.GetValue(2) -as [string])
    $details = Escape-SqlString($reader.GetValue(3) -as [string])
    $price = $reader.GetDecimal(4).ToString([System.Globalization.CultureInfo]::InvariantCulture)
    $imageUrl = Escape-SqlString($reader.GetValue(5) -as [string])
    $imageData = if ($reader.IsDBNull(6)) { 'NULL' } else { To-HexLiteral($reader.GetValue(6)) }
    $descriptionEmbedding = if ($reader.IsDBNull(7)) { 'NULL' } else { "N'$(Escape-SqlString($reader.GetString(7)))'" }
    $descriptionVectorJson = if ($reader.IsDBNull(8)) { 'NULL' } else { "CAST(N'$(Escape-SqlString($reader.GetString(8)))' AS VECTOR(1536))" }
    $nameVectorJson = if ($reader.IsDBNull(9)) { 'NULL' } else { "CAST(N'$(Escape-SqlString($reader.GetString(9)))' AS VECTOR(384))" }

    $descriptionValue = if ($description -eq $null) { 'NULL' } else { "N'$description'" }
    $detailsValue = if ($details -eq $null) { 'NULL' } else { "N'$details'" }
    $imageUrlValue = if ($imageUrl -eq $null) { 'NULL' } else { "N'$imageUrl'" }

    $values = @(
        $id,
        "N'$name'",
        $descriptionValue,
        $detailsValue,
        $price,
        $imageUrlValue,
        $imageData,
        $descriptionEmbedding,
        $descriptionVectorJson,
        $nameVectorJson
    )

    $rows.Add("INSERT INTO dbo.Products ([Id],[Name],[Description],[Details],[Price],[ImageUrl],[ImageData],[DescriptionEmbedding],[DescriptionVector],[NameVector]) VALUES ($($values -join ', '));")
}

$reader.Close()
$connection.Close()

$rows | Set-Content -Path $outputPath -Encoding UTF8
Write-Host "Export complete. $($rows.Count) rows written."
