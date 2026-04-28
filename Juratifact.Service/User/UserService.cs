using Juratifact.Repository;
using Juratifact.Service.MailService;
using Juratifact.Service.MediaService;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Juratifact.Service.Util;
using Microsoft.AspNetCore.Http;

namespace Juratifact.Service.User;

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly IMailService _mailService;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IMediaService _mediaService;
    
    
    public UserService(AppDbContext dbContext, IMailService mailService, IHttpContextAccessor httpContext, IMediaService mediaService)
    {
        _dbContext = dbContext;
        _mailService = mailService;
        _httpContext = httpContext;
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
            IsVerify = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
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
            CreatedAt = DateTimeOffset.UtcNow
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

    public async Task<string> UpdateUser(Guid id, Request.UpdateUserRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        
        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }
        
        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName;
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
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
            user.PhoneNumber = request.PhoneNumber;
        }
        

        if (!string.IsNullOrWhiteSpace(request.Address))
        {
            user.Address = request.Address;
        }
        
        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            user.UserName = request.UserName;
        }
        
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.HashedPassword = Argon2Hasher.HashPassword(request.Password);
        }
        
        if (request.ProfilePicture != null)
        {
            var imageUrl = await _mediaService.UploadAsync(request.ProfilePicture);
            user.ProfilePicture = imageUrl;
        }
        
        user.UpdatedAt = DateTimeOffset.UtcNow;
        
        await _dbContext.SaveChangesAsync();
        
        return "User updated successfully";
    }

    public async Task<string> SoftDeleteUser(Guid id)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
        
        var userIdGuid = Guid.Parse(userId);

        // Get current user with roles to check if admin
        var currentUser = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userIdGuid);

        if (currentUser == null)
        {
            throw new ArgumentException("User not found.");
        }

        // Check if current user is admin
        bool isAdmin = currentUser.UserRoles.Any(ur => ur.Role.Name == "Admin");

        // Check authorization
        if (!isAdmin && id != userIdGuid)
        {
            throw new UnauthorizedAccessException("You can only delete your own account. Only admin can delete other users.");
        }

        // Get user to delete
        var userToDelete = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (userToDelete == null)
        {
            throw new ArgumentException("User to delete not found.");
        }

        // If current user is admin and deleting another user, delete all their roles
        if (isAdmin && id != userIdGuid)
        {
            var userRolesToDelete = await _dbContext.UserRoles
                .Where(ur => ur.UserId == id)
                .ToListAsync();
            
            _dbContext.UserRoles.RemoveRange(userRolesToDelete);
            await _dbContext.SaveChangesAsync();
        }
        
        userToDelete.IsDeleted = true;
        userToDelete.UpdatedAt = DateTimeOffset.UtcNow;
        _dbContext.Update(userToDelete);
        await _dbContext.SaveChangesAsync();
        
        return "User deleted successfully";
    }

    public async Task<Response.GetUserResponse> GetUserProfile(Guid id)
    {
        var query = _dbContext.Users.Where (x => x.Id == id);

        if (query == null)
        {
            throw new ArgumentException("User not found");
        }
    
        var selectedQuery = query.Select(x => new Response.GetUserResponse()
        {
            UserName = x.UserName!,
            FullName = x.FullName,
            Email = x.Email,
            PhoneNumber = x.PhoneNumber,
            Address = x.Address!,
            ProfilePicture = x.ProfilePicture!,
            TrustScore = x.TrustScore,
        });
        var result = await selectedQuery.FirstOrDefaultAsync();
        
        return result!;
    }

    public async Task<Response.GetUserResponse> GetUserByName(string userName)
    {
        var query = _dbContext.Users.Where (x => x.UserName == userName);

        if (query == null)
        {
            throw new ArgumentException("User not found");
        }
    
        var selectedQuery = query.Select(x => new Response.GetUserResponse()
        {
            UserName = x.UserName!,
            FullName = x.FullName,
            Email = x.Email,
            PhoneNumber = x.PhoneNumber,
            Address = x.Address!,
            ProfilePicture = x.ProfilePicture!,
        });
        var result = await selectedQuery.FirstOrDefaultAsync();
        
        return result!;
    }

    public async Task<Base.Response.PageResult<Response.GetUserResponse>> GetAllUser(string? searchTerm, int pageIndex, int pageSize)
    {
        var query = _dbContext.Users.Where(x => true);
        if (searchTerm != null)
        {
            query = query.Where(x => x.UserName!.Contains(searchTerm) ||
                                     x.Email.Contains(searchTerm) ||
                                     x.PhoneNumber.Contains(searchTerm));
        }
        
        query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

        var selectedQuery = query.Select(x => new Response.GetUserResponse()
        {
            UserName = x.UserName!,
            FullName = x.FullName,
            Email = x.Email,
            PhoneNumber = x.PhoneNumber,
            Address = x.Address!,
            ProfilePicture = x.ProfilePicture!,
            TrustScore = x.TrustScore,
        });
        
        var listResult = await selectedQuery.ToListAsync();
        var totalItems = listResult.Count;
 
        var result = new Base.Response.PageResult<Response.GetUserResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
        return result;
        

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