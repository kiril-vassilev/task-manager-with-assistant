using Microsoft.EntityFrameworkCore;
using TaskManager.BLL;
using TaskManager.BLL.Orchestration;
using TaskManager.BLL.Search;
using TaskManager.DAL;
using TaskManager.Domain;


IConfigurationRoot configRoot = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly())
    .Build();

TaskManagerConfiguration.Initialize(configRoot);


var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TaskDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=tasks.db"));

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddSingleton<TaskSearchService>();

builder.Services.AddSingleton<AgentConfiguration>();
builder.Services.AddSingleton<AgentService>();

// Register the hosted service for async initialization
builder.Services.AddHostedService<AgentServiceInitializer>();


// Register a named HttpClient for the Razor Pages UI
builder.Services.AddHttpClient("TaskApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:5199"); // 👈 use the HTTP port
});


var app = builder.Build();

// Ensure database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    db.Database.EnsureCreated();
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapRazorPages();

app.Run();
