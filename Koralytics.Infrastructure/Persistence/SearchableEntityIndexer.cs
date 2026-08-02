using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Koralytics.Application.Interfaces;

namespace Koralytics.Infrastructure.Persistence
{
    public class SearchableEntityIndexer : ISearchableEntityIndexer
    {
        private readonly ICohereEmbeddingClient _embeddingClient;
        private readonly string _connectionString;

        public SearchableEntityIndexer(ICohereEmbeddingClient embeddingClient, string connectionString)
        {
            _embeddingClient = embeddingClient;
            _connectionString = connectionString;
        }

        public async Task IndexAsync(string entityType, int referenceId, string textValue, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(textValue))
                return;

            var vector = await _embeddingClient.GetEmbeddingAsync(textValue, ct);
            if (vector == null || vector.Length == 0)
                return;

            var vectorJson = JsonSerializer.Serialize(vector);

            const string ensureTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SearchableEntities')
                BEGIN
                    CREATE TABLE SearchableEntities (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        EntityType VARCHAR(50) NOT NULL,
                        ReferenceId INT NOT NULL,
                        TextValue NVARCHAR(255) NOT NULL,
                        Embedding VECTOR(1024) NOT NULL
                    );
                END";

            const string mergeSql = @"
                MERGE SearchableEntities AS target
                USING (SELECT @EntityType AS EntityType, @ReferenceId AS ReferenceId) AS source
                    ON target.EntityType = source.EntityType AND target.ReferenceId = source.ReferenceId
                WHEN MATCHED THEN
                    UPDATE SET TextValue = @TextValue,
                               Embedding = CAST(CAST(@VectorJson AS NVARCHAR(MAX)) AS VECTOR(1024))
                WHEN NOT MATCHED THEN
                    INSERT (EntityType, ReferenceId, TextValue, Embedding)
                    VALUES (@EntityType, @ReferenceId, @TextValue,
                            CAST(CAST(@VectorJson AS NVARCHAR(MAX)) AS VECTOR(1024)));";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using (var ensureCmd = new SqlCommand(ensureTableSql, conn))
            {
                await ensureCmd.ExecuteNonQueryAsync(ct);
            }

            await using (var cmd = new SqlCommand(mergeSql, conn))
            {
                cmd.Parameters.AddWithValue("@EntityType", entityType);
                cmd.Parameters.AddWithValue("@ReferenceId", referenceId);
                cmd.Parameters.AddWithValue("@TextValue", textValue);
                cmd.Parameters.AddWithValue("@VectorJson", vectorJson);

                await cmd.ExecuteNonQueryAsync(ct);
            }
        }
    }
}
