using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, _) =>
    {
        document.Info.Title = "Hopital API";
        document.Info.Version = "v1";
        document.Info.Description = "API REST de la Clinique Saint-Lucas — Workshop WKS 2026";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Clinique Saint-Lucas — API";
        options.Theme = ScalarTheme.BluePlanet;
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ──────────────────────────────────────────────
// TEST ENDPOINT
// ──────────────────────────────────────────────

app.MapGet("/test/image", (HttpContext context) =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    return Results.Ok(new
    {
        imageUrl = "/images/SuccesImage.png"
    });
})
.WithName("TestImage")
.WithSummary("Test image")
.WithDescription("Retourne l'URL d'une image de test. Le front utilise imageUrl (chemin relatif) ou fullUrl (URL absolue).")
.WithTags("Test");



app.MapGet("/test", () =>
{
    return Results.Ok(new
    {
        success = true,
        message = "L'API de la Clinique Saint-Lucas fonctionne correctement.",
        timestamp = DateTime.UtcNow
    });
})
.WithName("Test")
.WithSummary("Test de connectivité")
.WithDescription("Endpoint de test — vérifie que l'API est bien démarrée et accessible.")
.WithTags("Test");

app.Run();
