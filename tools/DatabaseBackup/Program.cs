using System.Text.Json;
using Microsoft.Data.SqlClient;
using System.Data;

if (args.Length != 1 || !Path.IsPathFullyQualified(args[0]))
{
    Console.Error.WriteLine("Usage: DatabaseBackup <absolute-sql-backup-file>");
    return 2;
}

var configPath = Environment.GetEnvironmentVariable("ATOKIC_CONFIG_PATH")
    ?? "/home/apartmani-tokic/atokicasp/appsettings.json";
try
{
    using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
    var connectionString = config.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    var name = connection.Database;
    if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("No database selected.");
    var quotedName = "[" + name.Replace("]", "]]") + "]";
    await using (var backup = new SqlCommand($"BACKUP DATABASE {quotedName} TO DISK = @path WITH COPY_ONLY, CHECKSUM", connection))
    {
        backup.CommandTimeout = 600;
        backup.Parameters.Add("@path", SqlDbType.NVarChar, 4000).Value = args[0];
        await backup.ExecuteNonQueryAsync();
    }
    await using (var verify = new SqlCommand("RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM", connection))
    {
        verify.CommandTimeout = 600;
        verify.Parameters.Add("@path", SqlDbType.NVarChar, 4000).Value = args[0];
        await verify.ExecuteNonQueryAsync();
    }
    Console.WriteLine("SQL backup created and verified.");
    return 0;
}
catch (Exception exception)
{
    // The connection string is never included in diagnostic output.
    Console.Error.WriteLine($"Database backup failed: {exception.GetType().Name}: {exception.Message}");
    return 1;
}

