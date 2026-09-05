var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// controllers 
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddEndpointsApiExplorer();





var app = builder.Build();



app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();




app.UseRouting();




app.MapGet("/", () =>
{

    return "IgniteAuth V0.9 - Reference System";

});

app.Run();


