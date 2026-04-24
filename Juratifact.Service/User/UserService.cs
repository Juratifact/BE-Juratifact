using Juratifact.Repository;
using Juratifact.Service.MailService;
using Juratifact.Service.MediaService;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Juratifact.Service.Util;

namespace Juratifact.Service.User;

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly IMailService _mailService;
    private readonly IMediaService _mediaService;
    
    public UserService(AppDbContext dbContext, IMailService mailService, IMediaService mediaService)
    {
        _dbContext = dbContext;
        _mailService = mailService;
        _mediaService = mediaService;
    }
    public async Task<string> CreateUser(Request.CreateUserRequest request)
    {
        string secureHashedPassword = Argon2Hasher.HashPassword(request.Password);
        
        var existingUserQuery = _dbContext.Users
            .Where(x => x.Email == request.Email);
        
        bool isExistUser = await existingUserQuery.AnyAsync();
        
        if (isExistUser)
        {
            throw new ArgumentException("User exist with mail.");
        }
        
        // Validate email format
        if (!IsValidEmail(request.Email))
        {
            throw new ArgumentException("Invalid email format.");
        }
        
        // Check PhoneNumber
        if (!IsValidPhoneNumber(request.PhoneNumber))
        {
            throw new ArgumentException("Invalid phone number format.");
        }
        
        // Check duplicate phone number
        var existingPhoneQuery = _dbContext.Users
            .Where(x => x.PhoneNumber == request.PhoneNumber);
        
        bool isPhoneExist = await existingPhoneQuery.AnyAsync();
        
        if (isPhoneExist)
        {
            throw new ArgumentException("Phone number already exists.");
        }
        
        
        var user = new Repository.Entity.User()
        {
            Email = request.Email,
            HashedPassword = secureHashedPassword, 
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            UserName = request.UserName,
            Address = request.Address,
            IsVerify = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        if (request.ProfilePicture != null)
        {
            var media = await _mediaService.UploadAsync(request.ProfilePicture);
            user.ProfilePicture = media;
        }
        
        _dbContext.Add(user);
        await _dbContext.SaveChangesAsync();
        
        var buyerRole = await _dbContext.Roles
            .FirstOrDefaultAsync(x => x.Name == "Buyer");
        
        if (buyerRole == null)
        {
            buyerRole = new Repository.Entity.Role()
            {
                Name = "Buyer",
                CreatedAt = DateTimeOffset.UtcNow
            };
            
            _dbContext.Roles.Add(buyerRole);
            await _dbContext.SaveChangesAsync();
        }
        
        var userRole = new Repository.Entity.UserRole()
        {
            UserId = user.Id,
            RoleId = buyerRole.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        _dbContext.UserRoles.Add(userRole);
        await _dbContext.SaveChangesAsync();

        var userWallet = new Repository.Entity.Wallet()
        {
            UserId = user.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            Balance = 0,
            PendingBalance = 0
        };
        
        _dbContext.Wallets.Add(userWallet);
        await _dbContext.SaveChangesAsync();

        var userCart = new Repository.Entity.Cart()
        {
            UserId = user.Id,
        };
        
        _dbContext.Carts.Add(userCart);
        await _dbContext.SaveChangesAsync();
        
        // Send welcome email
        try
        {
            var mailContent = new MailContent
            {
                To = request.Email,
                Subject = "Welcome to Juratifact",
                Body = $@"
                    <h2>Welcome to Juratifact!</h2>
                    <p>Hello {request.FullName},</p>
                    <p>Your account has been successfully created!</p>
                    <br/>
                    <p><strong>Account Information:</strong></p>
                    <ul>
                        <li>Email: {request.Email}</li>
                        <li>Full Name: {request.FullName}</li>
                        <li>Role: Buyer</li>
                    </ul>
                    <br/>
                    <p>You can now log in and start shopping with us.</p>
                    <br/>
                    <p>Best regards,<br/><strong>Juratifact Team</strong></p>
                "
            };
            
            await _mailService.SendMail(mailContent);
        }
        catch
        {
            throw new Exception("User registered but failed to send welcome email.");
            // Log email sending error but don't fail the user creation
        }
        return "User registered successfully!";
    }
    
    private bool IsValidEmail(string email)
    {
        try
        {
            // Regex pattern for email validation
            // Pattern: [anything]@[anything].[anything]
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }
        catch
        {
            return false;
        }
    }
    
    private bool IsValidPhoneNumber(string phoneNumber)
    {
        try
        {
            // Regex pattern for phone number validation
            // Pattern: 10-15 digits, can include +, -, space, ()
            string pattern = @"^[0-9+\-\s()]{10,15}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }
        catch
        {
            return false;
        }
    }
}