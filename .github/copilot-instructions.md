## TinyShop

### Backend
- Products project is the backend API.
- Built with .NET Minimal APIs.
- Uses Entity Framework Core with an in-memory database.
- Should follow OpenAPI best practices.

### Frontend
- Store project is a .NET 9 Blazor Server application.
- Uses default Bootstrap styling.
- UI should have a modern look and feel.
- CSS should be in .razor.css files.

## Misc
- To display an image you will always need to use `ImagePrefix` in the web front end which comes from the Configuration["ImagePrefix"] which isc @inject IConfiguration Configuration 
- Ignore all *.md files code in the **lab** folder when making code suggestions.
