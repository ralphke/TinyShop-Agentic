# Plan: TinyShop Hybrid Search Dialog

## Decisions
- Trigger: Search icon in NavMenu sidebar
- Mode: Hybrid — always runs keyword + semantic, merged with RRF
- Multi-MCP-source: Code extension point only (`IStoreSearchProvider` interface), no UI yet
- SQL vector: Use `VECTOR_DISTANCE` via `usp_SearchProductsBySimilarity` (works on SQL Server 2025 today); gate `VECTOR_SEARCH` (ANN/approximate) behind a runtime version check for when CU3 bug is fixed

---

## Phase 1 — SQL Server Vector Layer (Products project)

### 1a. Fix `usp_HybridProductSearch.sql`
- Change dimension: `VECTOR(1536)` → `VECTOR(768, FLOAT32)`
- Remove `p.CategoryId` filter (column doesn't exist) 
- Remove `p.Category` from SELECT (column doesn't exist)
- Keep the `VECTOR_SEARCH` syntax as-is but note it needs a future CU fix
- File: `src/Products/SQL/usp_HybridProductSearch.sql`

### 1b. Wire `usp_SearchProductsBySimilarity` into `SearchBySemanticAsync`
- In `ProductSearchService.SearchBySemanticAsync`: when `_context.Database.IsSqlServer()`, call the SP via raw SQL (ExecuteReaderAsync) instead of loading all products + in-memory cosine
- SP accepts `@QueryVector NVARCHAR(MAX)` (JSON array), `@SearchType`, `@TopN`, `@DistanceMetric`
- Serialize query embedding to JSON → pass as `@QueryVector`
- Call for BOTH `@SearchType='name'` and `@SearchType='description'`; return merged with weighted scoring (nameWeight=0.6, descWeight=0.4) after SQL returns top-N candidates for each
- Keep in-memory path as fallback (non-SQL Server / no embeddings)
- File: `src/Products/Services/ProductSearchService.cs`

### 1c. Add `SearchByHybridAsync` method to `ProductSearchService`
- Runs `SearchByKeywordAsync` and `SearchBySemanticAsync` in parallel (`Task.WhenAll`)
- Merges results using Reciprocal Rank Fusion: `score = Σ 1/(60 + rank_i)` over both ranked lists (k=60 is standard RRF constant)
- De-duplicates by Product.Id, sums RRF scores, re-sorts descending
- Applies pagination on the merged list
- Returns `SearchResult`
- File: `src/Products/Services/ProductSearchService.cs`

### 1d. Activate `EnsureVectorIndexAsync`
- Try to create DiskANN vector index: `CREATE VECTOR INDEX IX_Products_DescriptionVector ON dbo.Products(DescriptionVector) WITH (METRIC='cosine')`
- Wrap in try/catch; log warning on failure (SQL Server version may not support it yet)
- Same for `NameVector`
- File: `src/Products/Data/ProductDataContext.cs` (~line 765)

---

## Phase 2 — Public Search Endpoint (Products API)

### 2a. Add `GET /api/Product/search` to `ProductEndpoints.cs`
- Route: `GET /api/Product/search?q={query}&page={page}&size={size}`
- Calls `searchService.SearchByHybridAsync(q, page, size)`
- `.WithName("SearchProducts").Produces<ProductSearchService.SearchResult>(200)`
- No agent headers required (public endpoint for Store UI)
- File: `src/Products/Endpoints/ProductEndpoints.cs`

---

## Phase 3 — MCP Extension Point (AgentGateway)

### 3a. Add `HybridSearchProducts` MCP tool
- New `[McpServerTool]` in `TinyShopMcpTools.cs` wrapping `GET /api/Product/search`
- Parameters: `query (string)`, `page (int = 1)`
- Makes AgentGateway-based agents and future MCP clients use hybrid search
- File: `src/AgentGateway/TinyShopMcpTools.cs`

### 3b. Define `IStoreSearchProvider` in Store (extensibility seam)
- Interface: `Task<SearchDialogResult> SearchAsync(string query, int page, CancellationToken ct)`
- `SearchDialogResult`: `List<SearchHit>`, `int Total`, `int Page`, `int TotalPages`
- `SearchHit`: `int ProductId`, `string Name`, `string? Description`, `decimal Price`, `string ImageUrl`
- `ProductsApiSearchProvider` implements it, calls `ProductService.SearchProductsAsync`
- Register in Store DI as `IStoreSearchProvider` with a name/tag
- Future `TravelGuideSearchProvider` drops in without touching the dialog
- Files: `src/Store/Services/IStoreSearchProvider.cs` (new), `src/Store/Services/ProductsApiSearchProvider.cs` (new)

---

## Phase 4 — Store ProductService (HTTP client)

### 4a. Add `SearchProductsAsync` to `ProductService.cs`
- `GET /api/Product/search?q={query}&page={page}&size=5`
- Deserializes into a new `ProductSearchResult` record (mirrors `ProductSearchService.SearchResult`)
- File: `src/Store/Services/ProductService.cs`

---

## Phase 5 — SearchDialog Blazor Component

### 5a. Create `SearchDialog.razor`
- File: `src/Store/Components/Shared/SearchDialog.razor`
- `@inject IStoreSearchProvider SearchProvider` + `@inject NavigationManager Nav`
- Parameters: `[Parameter] EventCallback OnClose`
- State: `string _query`, `bool _loading`, `SearchDialogResult? _result`, `int _page`
- On query input: debounce 300ms (timer in OnInput handler), then call `SearchProvider.SearchAsync`
- Results list: product image (via `src="/api/images/{hit.ProductId}"`), name, price, description snippet (truncated to 80 chars)
- Click on result → `Nav.NavigateTo($"/products/{hit.ProductId}")` + `OnClose.InvokeAsync()`
- Pagination: Prev/Next buttons based on `_result.TotalPages`
- "AI-powered" badge (shown always since hybrid mode is always active)
- Close on Escape key (JS interop listener) + backdrop click
- Loading spinner from Bootstrap `spinner-border`

### 5b. Create `SearchDialog.razor.css`
- File: `src/Store/Components/Shared/SearchDialog.razor.css`
- Modal overlay: fixed, full-screen backdrop (`rgba(0,0,0,0.5)`)
- Dialog box: centered, `max-width: 640px`, `border-radius: 12px`, white background
- Search input: full-width, large (`font-size: 1.1rem`)
- Result item: flex row, image 48x48, hover highlight
- "AI-powered" badge: small pill, accent color
- Responsive: full-screen on mobile (`<576px`)

---

## Phase 6 — NavMenu Integration

### 6a. Add search icon button to `NavMenu.razor`
- Add `bool _searchOpen = false` state field
- Add a `<button>` with magnifying glass SVG (inline, matching existing nav icon style) above the nav links
- `@onclick="() => _searchOpen = true"`
- Conditionally render `<SearchDialog OnClose="() => _searchOpen = false" />` when `_searchOpen`
- File: `src/Store/Components/Layout/NavMenu.razor`

### 6b. Style the search button in `NavMenu.razor.css`
- Match existing nav icon button styling from NavMenu
- File: `src/Store/Components/Layout/NavMenu.razor.css`

---

## Relevant Files

- `src/Products/Services/ProductSearchService.cs` — Add `SearchByHybridAsync`; update `SearchBySemanticAsync` to use SP on SQL Server
- `src/Products/Endpoints/ProductEndpoints.cs` — Add `GET /api/Product/search`
- `src/Products/Data/ProductDataContext.cs` — Activate `EnsureVectorIndexAsync`
- `src/Products/SQL/usp_HybridProductSearch.sql` — Fix schema mismatches
- `src/AgentGateway/TinyShopMcpTools.cs` — Add `HybridSearchProducts` tool
- `src/Store/Services/ProductService.cs` — Add `SearchProductsAsync`
- `src/Store/Services/IStoreSearchProvider.cs` (new) — Extensibility interface
- `src/Store/Services/ProductsApiSearchProvider.cs` (new) — Products implementation
- `src/Store/Components/Shared/SearchDialog.razor` (new) — Dialog component
- `src/Store/Components/Shared/SearchDialog.razor.css` (new) — Dialog styles
- `src/Store/Components/Layout/NavMenu.razor` — Add search icon trigger
- `src/Store/Components/Layout/NavMenu.razor.css` — Style search button

---

## Verification

1. `dotnet test src/TinyShop.sln` passes with no regressions
2. `aspire run` starts cleanly; Products API registers `SearchByHybridAsync` endpoint at `GET /api/Product/search`
3. Navigate to `/products` in the Store → search icon appears in sidebar
4. Click icon → dialog opens; type "outdoor lighting" → spinner → hybrid results appear; first result should be the camping lantern
5. Toggle to page 2 inside dialog → pagination works
6. Click a result → navigates to product detail page, dialog closes
7. Press Escape → dialog closes
8. AgentGateway: `POST /mcp` with `HybridSearchProducts` tool invocation returns merged results

---

## Out of Scope
- `VECTOR_SEARCH` (ANN) activation — gated behind version check, not active until SQL Server CU bug is fixed
- Source selector UI — extension point in code only; no UI until a second provider exists
- Travel guide or external MCP source implementation
