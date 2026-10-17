using TruthWeaver.Samples.AspNetCore;

// A minimal API with TruthWeaver rule policies. Try it with the X-User and X-Roles headers, for example:
//   curl -H "X-User: alice" http://localhost:5000/documents/doc-1
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddDocumentApp();

WebApplication app = builder.Build();
app.UseDocumentApp();
await app.RunAsync();
