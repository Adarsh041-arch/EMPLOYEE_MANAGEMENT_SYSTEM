# Exports the sample data from a seeded EmployeeManagement database into seed.sql.
# I used this once after the API had seeded the database; it is kept here in case
# the sample data changes.
#   powershell -File database\export-seed.ps1
param(
    [string]$Server = ".\SQLEXPRESS",
    [string]$Database = "EmployeeManagement",
    [string]$OutFile = "$PSScriptRoot\seed.sql"
)

# parent tables first so foreign keys are satisfied
$tables = @(
    @{ Name = "Employees"; Identity = $true },
    @{ Name = "EmployeePersonalDetails"; Identity = $false },
    @{ Name = "Users"; Identity = $true },
    @{ Name = "EmployeeTransferHistory"; Identity = $true }
)

$conn = New-Object System.Data.SqlClient.SqlConnection "Server=$Server;Database=$Database;Integrated Security=True"
$conn.Open()

function Format-Value($value) {
    if ($value -is [DBNull]) { return "NULL" }
    if ($value -is [string]) { return "N'" + $value.Replace("'", "''") + "'" }
    if ($value -is [datetime]) { return "'" + $value.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "'" }
    if ($value -is [bool]) { return [int]$value }
    return [string]::Format([Globalization.CultureInfo]::InvariantCulture, "{0}", $value)
}

$out = New-Object System.Text.StringBuilder
[void]$out.AppendLine("-- Sample data for the Employee Management System.")
[void]$out.AppendLine("-- Run after schema.sql on an empty database.")
[void]$out.AppendLine("-- Departments, Roles and EmployeeCodeSequences rows are created by schema.sql;")
[void]$out.AppendLine("-- the sequence numbers are updated at the bottom of this file.")
[void]$out.AppendLine("-- Aadhaar/PAN values are encrypted with the sample Encryption:Key from the README.")
[void]$out.AppendLine("-- Demo logins: admin / Admin@123, manager / Manager@123, employee / Employee@123")
[void]$out.AppendLine("")
[void]$out.AppendLine("SET NOCOUNT ON;")
[void]$out.AppendLine("BEGIN TRANSACTION;")

foreach ($t in $tables) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT * FROM [$($t.Name)] ORDER BY 1"
    $reader = $cmd.ExecuteReader()
    $columns = @(0..($reader.FieldCount - 1) | ForEach-Object { $reader.GetName($_) })
    $columnList = ($columns | ForEach-Object { "[$_]" }) -join ", "

    [void]$out.AppendLine("")
    [void]$out.AppendLine("-- $($t.Name)")
    if ($t.Identity) { [void]$out.AppendLine("SET IDENTITY_INSERT [$($t.Name)] ON;") }
    while ($reader.Read()) {
        $values = @(0..($reader.FieldCount - 1) | ForEach-Object { Format-Value $reader.GetValue($_) }) -join ", "
        [void]$out.AppendLine("INSERT INTO [$($t.Name)] ($columnList) VALUES ($values);")
    }
    if ($t.Identity) { [void]$out.AppendLine("SET IDENTITY_INSERT [$($t.Name)] OFF;") }
    $reader.Close()
}

[void]$out.AppendLine("")
[void]$out.AppendLine("-- EmployeeCodeSequences")
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT DepartmentId, LastNumber FROM EmployeeCodeSequences ORDER BY DepartmentId"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    [void]$out.AppendLine("UPDATE [EmployeeCodeSequences] SET [LastNumber] = $($reader.GetInt32(1)) WHERE [DepartmentId] = $($reader.GetInt32(0));")
}
$reader.Close()
$conn.Close()

[void]$out.AppendLine("")
[void]$out.AppendLine("COMMIT TRANSACTION;")

[IO.File]::WriteAllText($OutFile, $out.ToString())
Write-Host "Wrote $OutFile"
