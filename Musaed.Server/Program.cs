using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

// This creates a minimal web server (Kestrel)
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// This is the health check endpoint our ProcessManager will call.
// It simply responds with "Healthy!" and a 200 OK status code.
app.MapGet("/health", () => Results.Ok("Healthy!"));

// A simple root page for our WebView2 to load.
app.MapGet("/", () => "Welcome to the Musaed Local Server!");

// Configure the server to listen on the URL we specified in our mock settings.
app.Run("http://localhost:8001");