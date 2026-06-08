
using ConsoleApp1;
using DSS_Api.Context;
using Microsoft.EntityFrameworkCore;
using RecentFix.services;
using Syncfusion.XlsIO.Parser.Biff_Records;
using Zenfiles.PermissionService;
using Zenfiles_V8.Services;

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NDAxNEAzMjM4MkUzMTJFMzlMeXJkaVJFV2Z5R3o5ZXNEVnNOQjFqUmx2MW0xZkR2TGdud2MrVGNJRlBzPQ==\r\n");
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<MFilesDbContext>(options =>
    options.UseFirebird(builder.Configuration.GetConnectionString("FirebirdDb")));
builder.Services.AddScoped<IMFilesObjectRepository, MFilesObjectRepository>();
// Register the built-in memory cache
builder.Services.AddMemoryCache();
// Register IObjectTypeProvider (you need to provide the concrete implementation)
// Register your custom service
builder.Services.AddScoped<GetCacheObjects>();
builder.Services.AddScoped<Gettingusersinusergroup>();
// Register UserPerm as IPermission
builder.Services.AddScoped<IPermission, UserPerm>();
builder.Services.AddControllers();
builder.Services.AddCors();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseResponseCaching();
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();
app.UseCors(options =>
{
    options.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
});
app.UseAuthorization();

app.MapControllers();
app.Run();
