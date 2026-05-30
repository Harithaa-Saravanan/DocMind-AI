using DocMind.Api.Services;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);

// 1. Register the native, high-performance Qdrant Client (Defaulting to localhost:6334)
builder.Services.AddSingleton(sp => new QdrantClient("localhost", 6334));

// 2. Register our newly constructed operational RAG Logic Services
builder.Services.AddScoped<DocumentIngestionService>();
builder.Services.AddScoped<RagChatService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Configure CORS policies to allow your local React frontend to talk to this API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite dev server standard port
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Activate CORS protection using the custom policy defined above
app.UseCors("AllowReactApp");

app.UseAuthorization();
app.MapControllers();

app.Run();