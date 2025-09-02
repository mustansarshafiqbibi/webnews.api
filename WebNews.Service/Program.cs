using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WebNews.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Adding services to the container.
builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<HackerNewsService>();
builder.Services.AddScoped<IHackerNewsService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var memoryCache = sp.GetRequiredService<IMemoryCache>();
    var logger = sp.GetRequiredService<ILogger<HackerNewsService>>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new HackerNewsService(httpClientFactory, memoryCache, logger, configuration);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4400")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.MapControllers();

app.Run();
