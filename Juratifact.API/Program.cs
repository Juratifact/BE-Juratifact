using Juratifact.API.Extensions;
using Juratifact.API.Middlewares;
using Microsoft.AspNetCore.Mvc;
using Juratifact.Repository;
using Juratifact.Service.BackgroundJobService;
using Juratifact.Service.Cart;
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
using Juratifact.Service.SellerOrders;
using Juratifact.Service.SettlementService;
using Juratifact.Service.Shipper;
using Juratifact.Service.Transactionss;
using Juratifact.Service.User;
using Juratifact.Service.Wallet;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEnvelopeModelValidation();
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
                                "https://www.juratifact.id.vn",
                                "https://fe-juratifact.vercel.app")
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
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ITransactionServices, TransactionServices>();
builder.Services.AddScoped<ISellerOrderService, SellerOrderService>();


builder.Services.Configure<DiscordAlertOptions>(
    builder.Configuration.GetSection("DiscordAlertOptions"));
builder.Services.AddHttpClient<IDiscordService, DiscordService>(); 


builder.Services.AddQuartz(q =>
{
   
    var orderJobKey = new JobKey("CancelOrderJob");
    q.AddJob<CancelOrderJob>(opts => opts.WithIdentity(orderJobKey));
    q.AddTrigger(opts => opts
        .ForJob(orderJobKey)
        .WithIdentity("CancelOrderJob-trigger")
        .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever()));

    
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
   
    var jobKey = new JobKey("AutoSettlementJob");
    q.AddJob<AutoSettlementJob>(opts => opts.WithIdentity(jobKey));
    
    
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("AutoSettlementJob-trigger")
        .WithCronSchedule("0 0 * * * ?")); 
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();
var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();


app.UseSwaggerAPI();
app.MapHub<Juratifact.Service.Hubs.NotificationHub>("/notificationHub");
app.UseCors("AllowFrontend");


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

