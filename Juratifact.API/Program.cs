using Juratifact.API.Extensions;
using Juratifact.API.Middlewares;
using Juratifact.Repository;
using Juratifact.Service.BackgroundJobService;
using Juratifact.Service.Category;
using Juratifact.Service.CloudinaryService;
using Juratifact.Service.DiscordService;
using Juratifact.Service.Dispute;
using Juratifact.Service.Identity;
using Juratifact.Service.IdentityDocumentService;
using Juratifact.Service.JwtService;
using Juratifact.Service.MailService;
using Juratifact.Service.MediaService;
using Juratifact.Service.Notification;
using Juratifact.Service.Order;
using Juratifact.Service.Product;
using Juratifact.Service.Promotion;
using Juratifact.Service.Report;
using Juratifact.Service.Sepay;
using Juratifact.Service.SettlementService;
using Juratifact.Service.Shipper;
using Juratifact.Service.User;
using Juratifact.Service.Wallet;
using Microsoft.EntityFrameworkCore;
using Quartz;

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
builder.Services.AddSignalR();

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
builder.Services.AddScoped<ISepayService, SepayService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISettlementService, SettlementService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IShipperService,ShipperService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IDisputeService, DisputeService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

//test thử discord
builder.Services.Configure<DiscordAlertOptions>(
    builder.Configuration.GetSection("DiscordAlertOptions"));
builder.Services.AddHttpClient<IDiscordService, DiscordService>(); // AddHttpClient là do nó tự gọi API ở bên ngoài
// Cụ thể ở đây của mình là tự gọi API webhook của discord

builder.Services.AddQuartz(q =>
{
    // 1. Cấu hình cho CancelOrderJob: Chạy mỗi 1 phút
    var orderJobKey = new JobKey("CancelOrderJob");
    q.AddJob<CancelOrderJob>(opts => opts.WithIdentity(orderJobKey));
    q.AddTrigger(opts => opts
        .ForJob(orderJobKey)
        .WithIdentity("CancelOrderJob-trigger")
        .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever()));

    // 2. Cấu hình cho SubscriptionExpiryJob: Chạy mỗi 1 giờ
    var subJobKey = new JobKey("SubscriptionExpiryJob");
    q.AddJob<SubscriptionExpiryJob>(opts => opts.WithIdentity(subJobKey));
    q.AddTrigger(opts => opts
        .ForJob(subJobKey)
        .WithIdentity("SubscriptionExpiryJob-trigger")
        .WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever()));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.Services.AddQuartz(q =>
{
    // Đăng ký Job của bạn bình thường
    var jobKey = new JobKey("AutoSettlementJob");
    q.AddJob<AutoSettlementJob>(opts => opts.WithIdentity(jobKey));
    
    // Hẹn giờ chạy mỗi 1 tiếng
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("AutoSettlementJob-trigger")
        .WithCronSchedule("0 0 * * * ?")); 
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();
var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
// Configure the HTTP request pipeline.

app.UseSwaggerAPI();
app.MapHub<Juratifact.Service.Hubs.NotificationHub>("/notificationHub");
app.UseCors("AllowFrontend");


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

