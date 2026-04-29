var builder = DistributedApplication.CreateBuilder(args);

var tinyShopDb = builder.AddSqlServer("sqlserver")
    .WithImageTag("2025-latest")
    .AddDatabase("TinyShopDB");

var embeddings = builder.AddContainer("embeddings", "ghcr.io/huggingface/text-embeddings-inference:cpu-latest")
    .WithArgs("--model-id", "sentence-transformers/all-mpnet-base-v2")
    .WithHttpEndpoint(port: 8001, targetPort: 80);

var products = builder.AddProject<Projects.Products>("products")
    .WithReference(tinyShopDb)
    .WaitFor(tinyShopDb)
    .WaitFor(embeddings)
    .WithEnvironment("EMBEDDING_SERVICE__ENDPOINT", "http://localhost:8001/embed");

var agentGateway = builder.AddProject<Projects.AgentGateway>("agent-gateway")
    .WaitFor(products)
    .WithReference(products);

builder.AddProject<Projects.Store>("store")
    .WaitFor(products)
    .WithReference(products);

builder.Build().Run();
