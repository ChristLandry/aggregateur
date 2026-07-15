using AggregatorPlatform.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.Persistence;

/// <summary>
/// Service demarrage : dechiffre les colonnes qui ne sont plus mappees via
/// EncryptionValueConverter (BankAccount / PhoneNumber sur Subscriptions,
/// Transactions et Clients). Utilise ADO.NET brut pour bypasser tout
/// ValueConverter EF ; ecrit le plaintext en place.
///
/// Idempotent : si la valeur n'est pas dechiffrable (deja plaintext ou format
/// non attendu), <see cref="IEncryptionService.Decrypt"/> renvoie l'entree telle
/// quelle ; on ne met a jour la ligne QUE si le dechiffrement a produit une
/// chaine differente. Donc les lignes deja plaintext ne sont pas touchees.
/// </summary>
public class DecryptLegacySensitiveColumnsHostedService : IHostedService
{
    private static readonly (string Table, string KeyColumn, string ValueColumn, bool Required)[] Targets =
    {
        ("Subscriptions", "SubscriptionId", "BankAccountNumber", true),
        ("Subscriptions", "SubscriptionId", "PhoneNumber",       true),
        ("Transactions",  "TransactionId",  "BankAccount",       false),
        ("Transactions",  "TransactionId",  "PhoneNumber",       false),
        ("Clients",       "ClientId",       "PhoneNumber",       false),
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DecryptLegacySensitiveColumnsHostedService> _logger;

    public DecryptLegacySensitiveColumnsHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<DecryptLegacySensitiveColumnsHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Tout est encapsule dans un try/catch top-level : ce service ne DOIT PAS
        // faire echouer le demarrage de l'application. Si la BD est indisponible
        // ou si le schema ne correspond pas, on log et on continue.
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AggregatorDbContext>();
            var enc = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

            if (!db.Database.IsRelational())
            {
                _logger.LogDebug("DecryptLegacySensitiveColumns: skipped (non-relational provider).");
                return;
            }

            var connection = db.Database.GetDbConnection();
            var mustOpen = connection.State != System.Data.ConnectionState.Open;
            if (mustOpen)
            {
                try { await connection.OpenAsync(cancellationToken); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DecryptLegacySensitiveColumns: DB connection failed, skipping.");
                    return;
                }
            }

            try
            {
                foreach (var (table, keyCol, valueCol, required) in Targets)
                {
                    try
                    {
                        var updated = await DecryptColumnAsync(connection, table, keyCol, valueCol, required, enc, cancellationToken);
                        if (updated > 0)
                            _logger.LogInformation("Decrypted {Count} row(s) on {Table}.{Column}.", updated, table, valueCol);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Decryption pass failed on {Table}.{Column}.", table, valueCol);
                    }
                }
            }
            finally
            {
                if (mustOpen)
                {
                    try { await connection.CloseAsync(); } catch { /* best-effort */ }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DecryptLegacySensitiveColumns: unexpected error, skipped.");
        }
    }

    private static async Task<int> DecryptColumnAsync(
        System.Data.Common.DbConnection connection,
        string table, string keyColumn, string valueColumn, bool valueRequired,
        IEncryptionService enc, CancellationToken ct)
    {
        // On lit ID + ciphertext, on decrypte cote client, on ecrit le plaintext
        // via une update parametree. Rien de dynamique dans les valeurs SQL.
        var rows = new List<(object Key, string Value)>();
        await using (var read = connection.CreateCommand())
        {
            read.CommandText = $"SELECT [{keyColumn}], [{valueColumn}] FROM [{table}]" +
                               (valueRequired ? string.Empty : $" WHERE [{valueColumn}] IS NOT NULL");
            await using var reader = await read.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var key = reader.GetValue(0);
                if (reader.IsDBNull(1)) continue;
                var value = reader.GetString(1);
                rows.Add((key, value));
            }
        }

        var updated = 0;
        foreach (var (key, cipher) in rows)
        {
            var plain = enc.Decrypt(cipher);
            if (string.Equals(plain, cipher, StringComparison.Ordinal)) continue; // deja plaintext

            await using var upd = connection.CreateCommand();
            upd.CommandText = $"UPDATE [{table}] SET [{valueColumn}] = @val WHERE [{keyColumn}] = @key";
            var pv = upd.CreateParameter(); pv.ParameterName = "@val"; pv.Value = plain; upd.Parameters.Add(pv);
            var pk = upd.CreateParameter(); pk.ParameterName = "@key"; pk.Value = key;   upd.Parameters.Add(pk);
            await upd.ExecuteNonQueryAsync(ct);
            updated++;
        }
        return updated;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
