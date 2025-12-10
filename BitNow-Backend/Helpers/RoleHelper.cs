using BitNow_Backend.DAL;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.Helpers
{
    public static class RoleHelper
    {
        /// <summary>
        /// Check if user has admin role
        /// </summary>
        public static async Task<bool> IsAdminAsync(BidNowDbContext dbContext, int? userId)
        {
            if (userId == null) return false;
            
            var user = await dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            return user?.UserRoles.Any(ur => ur.Role.ToLower() == "admin") ?? false;
        }

        /// <summary>
        /// Check if user has staff role
        /// </summary>
        public static async Task<bool> IsStaffAsync(BidNowDbContext dbContext, int? userId)
        {
            if (userId == null) return false;
            
            var user = await dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            return user?.UserRoles.Any(ur => ur.Role.ToLower() == "staff") ?? false;
        }

        /// <summary>
        /// Check if user has support role
        /// </summary>
        public static async Task<bool> IsSupportAsync(BidNowDbContext dbContext, int? userId)
        {
            if (userId == null) return false;
            
            var user = await dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            return user?.UserRoles.Any(ur => ur.Role.ToLower() == "support") ?? false;
        }

        /// <summary>
        /// Check if user has any of the specified roles (admin, staff, or support)
        /// </summary>
        public static async Task<bool> HasAnyRoleAsync(BidNowDbContext dbContext, int? userId, params string[] roles)
        {
            if (userId == null) return false;
            
            var user = await dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null) return false;
            
            var userRoles = user.UserRoles.Select(ur => ur.Role.ToLower()).ToList();
            return roles.Any(role => userRoles.Contains(role.ToLower()));
        }

        /// <summary>
        /// Check if user is admin, staff, or support
        /// </summary>
        public static async Task<bool> IsAdminOrStaffOrSupportAsync(BidNowDbContext dbContext, int? userId)
        {
            return await HasAnyRoleAsync(dbContext, userId, "admin", "staff", "support");
        }

        /// <summary>
        /// Get all roles for a user
        /// </summary>
        public static async Task<List<string>> GetUserRolesAsync(BidNowDbContext dbContext, int? userId)
        {
            if (userId == null) return new List<string>();
            
            var user = await dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            return user?.UserRoles.Select(ur => ur.Role).ToList() ?? new List<string>();
        }
    }
}



