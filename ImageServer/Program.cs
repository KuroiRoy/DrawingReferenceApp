using ImageServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<FileServerSettings>(builder.Configuration.GetSection("FileServer"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseRouting();
app.MapControllers();
app.UseHttpsRedirection();
app.Run();
