using ChatOnWebApi.Hubs;
using ChatOnWebApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using System.Text.Json.Serialization;
using OpenAI.GPT3.Extensions;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//Json handler for cycle error
builder.Services.AddControllers()
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
     });
builder.Services.AddControllers();

//Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.WithOrigins("https://localhost:7122")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials());
});
builder.Services.AddCors(options => options.
AddDefaultPolicy(policy => policy.
AllowAnyMethod().
AllowAnyHeader().
WithOrigins("https://localhost:7122").
AllowAnyOrigin()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey

    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});
//SignalR
builder.Services.AddSignalR();
//Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});
//Db setup
builder.Services.AddDbContext<UserDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlCon"));
});
//Gpt setup
builder.Services.AddOpenAIService(settings =>
settings.ApiKey = "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");


var app = builder.Build();
// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("CorsPolicy");

app.UseCors();

app.UseAuthentication();

app.MapHub<ChatHub>("/ChatHub");

app.UseAuthorization();

app.MapControllers();

app.MapControllers();

app.Run();
