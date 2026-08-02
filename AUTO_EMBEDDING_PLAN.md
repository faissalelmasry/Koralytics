# Auto-Embedding Plan — Player Registration Only (Phase 1)

## Goal
Whenever a new player registers, automatically generate a Cohere embedding for
their name and save it into `SearchableEntities` — in the background, without
slowing down the registration request. This phase only touches **Player
registration**. Academy/Team/Tournament will follow the exact same pattern
later, once this is tested and working.

---

## Where Everything Goes (Clean Architecture Layers)

| Layer | New/Changed Item | Type |
|---|---|---|
| **Application** | `ICohereEmbeddingClient` | Interface (contract only) |
| **Application** | `ISearchableEntityIndexer` | Interface (contract only) |
| **Application** | `PlayerService.RegisterPlayerAsync` | Existing class — one new constructor dependency + one new line inside the method |
| **Infrastructure** | `CohereOptions` | Plain settings class |
| **Infrastructure** | `CohereEmbeddingClient` | Implements `ICohereEmbeddingClient` |
| **Infrastructure** | `SearchableEntityIndexer` | Implements `ISearchableEntityIndexer` |
| **Presentation / API** | `Program.cs` | DI registration only |
| **Presentation / API** | `appsettings.json` | Config values only |

**Nothing changes in the Domain layer.** No new entities, no changes to the
`Player` entity itself. This is a pure Application + Infrastructure concern.

`IBackgroundTaskQueue` is your existing interface — not touched or
re-implemented here, just consumed.

---

## 1. Application Layer — Interfaces

### `Application/Interfaces/ICohereEmbeddingClient.cs`
```csharp
namespace Koralytics.Scouting.Application.Interfaces
{
    public interface ICohereEmbeddingClient
    {
        // Turns a piece of text (e.g. a player's full name) into a
        // 1024-number vector that can be stored and later compared for similarity.
        Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
    }
}
```

### `Application/Interfaces/ISearchableEntityIndexer.cs`
```csharp
namespace Koralytics.Scouting.Application.Interfaces
{
    public interface ISearchableEntityIndexer
    {
        // One method for every entity type. For Phase 1 we only ever call
        // this with entityType = "Player", but it's written generically so
        // Academy/Team/Tournament can reuse it later without any changes here.
        Task IndexAsync(string entityType, int referenceId, string textValue, CancellationToken ct = default);
    }
}
```

---

## 2. Infrastructure Layer — Implementations

### `Infrastructure/Configuration/CohereOptions.cs`
```csharp
namespace Koralytics.Scouting.Infrastructure.Configuration
{
    public class CohereOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "embed-multilingual-v3.0";
        public string BaseUrl { get; set; } = "https://api.cohere.com/v1";
    }
}
```

### `Infrastructure/ExternalServices/CohereEmbeddingClient.cs`
```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Koralytics.Scouting.Application.Interfaces;
using Koralytics.Scouting.Infrastructure.Configuration;

namespace Koralytics.Scouting.Infrastructure.ExternalServices
{
    public class CohereEmbeddingClient : ICohereEmbeddingClient
    {
        private readonly HttpClient _http;
        private readonly CohereOptions _options;

        public CohereEmbeddingClient(HttpClient http, IOptions<CohereOptions> options)
        {
            _http = http;
            _options = options.Value;
            _http.BaseAddress = new Uri(_options.BaseUrl);
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
        {
            var payload = new
            {
                texts = new[] { text },
                model = _options.Model,
                // "search_document" because this text is being STORED for later
                // searching. When you later embed a user's search query, use
                // "search_query" instead — that's what makes similarity
                // matching accurate (asymmetric embeddings).
                input_type = "search_document"
            };

            var response = await _http.PostAsJsonAsync("embed", payload, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CohereEmbedResponse>(cancellationToken: ct);
            return result?.Embeddings?.FirstOrDefault() ?? Array.Empty<float>();
        }

        private class CohereEmbedResponse
        {
            public List<float[]>? Embeddings { get; set; }
        }
    }
}
```

### `Infrastructure/Persistence/SearchableEntityIndexer.cs`
```csharp
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Koralytics.Scouting.Application.Interfaces;

namespace Koralytics.Scouting.Infrastructure.Persistence
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
                return; // nothing meaningful to embed

            // Step 1: turn the text into a vector using Cohere
            var vector = await _embeddingClient.GetEmbeddingAsync(textValue, ct);
            var vectorJson = JsonSerializer.Serialize(vector);

            // Step 2: save it. MERGE instead of plain INSERT so that if this
            // same entity (same type + same id) gets re-indexed later (e.g. a
            // player's name gets corrected), we UPDATE the existing row
            // instead of creating a duplicate.
            const string sql = @"
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

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EntityType", entityType);
            cmd.Parameters.AddWithValue("@ReferenceId", referenceId);
            cmd.Parameters.AddWithValue("@TextValue", textValue);
            cmd.Parameters.AddWithValue("@VectorJson", vectorJson);

            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
```

---

## 3. Application Layer — Wiring It Into `PlayerService`

Add `IBackgroundTaskQueue` as a constructor dependency (alongside whatever
`PlayerService` already has), then add **one block** right after the existing
save/`SaveChangesAsync` call inside `RegisterPlayerAsync`:

```csharp
using Koralytics.Scouting.Application.Interfaces;
// ... your existing usings ...

namespace Koralytics.Scouting.Application.Services
{
    public class PlayerService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        // ... your other existing dependencies (unit of work, logger, etc.) stay as they are ...

        public PlayerService(
            IBackgroundTaskQueue taskQueue
            /*, ...your other existing constructor parameters... */)
        {
            _taskQueue = taskQueue;
            // ... assign your other existing fields ...
        }

        public async Task<int> RegisterPlayerAsync(/* your existing params */)
        {
            // ... your existing player-creation logic ...
            // ... your existing SaveChangesAsync call ...

            int newPlayerId = 0;   // <- replace with the real new player's Id after save
            string fullName = "";  // <- replace with the real full name you just built

            // NEW: enqueue the embedding job. Returns instantly — the actual
            // Cohere call + DB write happens later, in the background.
            _taskQueue.QueueBackgroundWorkItem(async (serviceProvider, ct) =>
            {
                var indexer = serviceProvider.GetRequiredService<ISearchableEntityIndexer>();
                await indexer.IndexAsync("Player", newPlayerId, fullName, ct);
            });

            return newPlayerId;
        }
    }
}
```

`using Microsoft.Extensions.DependencyInjection;` is needed in that file for
`GetRequiredService`.

---

## 4. Presentation/API Layer — DI Registration

### `Program.cs` additions
```csharp
builder.Services.Configure<CohereOptions>(builder.Configuration.GetSection("Cohere"));
builder.Services.AddHttpClient<ICohereEmbeddingClient, CohereEmbeddingClient>();
builder.Services.AddScoped<ISearchableEntityIndexer>(sp =>
    new SearchableEntityIndexer(
        sp.GetRequiredService<ICohereEmbeddingClient>(),
        builder.Configuration.GetConnectionString("Default")!));
```

You already have `IBackgroundTaskQueue` and its worker registered elsewhere —
don't touch that registration.

### `appsettings.json` additions
```json
"Cohere": {
  "ApiKey": "your-new-cohere-api-key-here",
  "Model": "embed-multilingual-v3.0",
  "BaseUrl": "https://api.cohere.com/v1"
}
```

> Use a brand-new API key here — rotate the one that was previously exposed
> in the shared script, from https://dashboard.cohere.com/api-keys.

---

## 5. Testing Checklist (Player only)

1. Register a new player through your existing endpoint.
2. Confirm the response comes back immediately (no noticeable delay from Cohere).
3. Check `SearchableEntities` a few seconds later — a new row should appear
   with `EntityType = 'Player'`, the correct `ReferenceId`, `TextValue`
   matching the player's full name, and a populated `Embedding` column.
4. Re-register / update the same player's name (if you have an update path)
   and confirm the row gets **updated**, not duplicated.
5. Temporarily break the Cohere API key and confirm registration still
   succeeds (the background job should fail and log, without affecting the
   player registration response).

---

## 6. What Comes Later (not built yet, same pattern)
Once Player registration is confirmed working end-to-end, the exact same
three steps — enqueue after save, call `ISearchableEntityIndexer.IndexAsync`,
pass the right `entityType` string — get repeated for:
- `AcademyService.CreateAcademyAsync` → `"Academy"`
- `TeamService.CreateTeamAsync` → `"Team"`
- `TournamentService.CreateTournamentAsync` → `"Tournament"`

No new interfaces or classes needed for those — `ISearchableEntityIndexer`
and `CohereEmbeddingClient` are already generic enough to handle them as-is.
