var builder = DistributedApplication.CreateBuilder(args);

var products = builder.AddProject<Projects.Products>("products");

builder.AddProject<Projects.Store>("store")
    .WaitFor(products)
    .WithReference(products);

builder.Build().Run();
