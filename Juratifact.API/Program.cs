using Juratifact.API.Extensions;
using Juratifact.API.Middlewares;
using Juratifact.Repository;
using Juratifact.Service.CloudinaryService;
using Juratifact.Service.Identity;
using Juratifact.Service.IdentityDocumentService;
using Juratifact.Service.JwtService;
using Juratifact.Service.MailService;
using Juratifact.Service.MediaService;
using Juratifact.Service.Product;
using Juratifact.Service.Promotion;
using Juratifact.Service.Report;
using Juratifact.Service.User;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(  
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddJwtServices(builder.Configuration);
builder.Services.AddSwaggerServices();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", 
                                "https://juratifact.id.vn",        
                                "https://www.juratifact.id.vn")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); 
        });
});

builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IMediaService, CloudinaryService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IIdentityDocumentService, IdentityDocumentService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();

builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();
var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
// Configure the HTTP request pipeline.

app.UseSwaggerAPI();
    
app.UseCors("AllowFrontend");


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

