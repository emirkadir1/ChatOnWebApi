using Microsoft.EntityFrameworkCore;

namespace ChatOnWebApi.Models
{
    public class UserDbContext:DbContext

    {
        public UserDbContext(DbContextOptions<UserDbContext> options): base(options)
        {

        }
        public DbSet<User> Users => Set<User>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<FriendList> Friends => Set<FriendList>();
        public DbSet<FriendRequests> FriendRequests => Set<FriendRequests>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<NotificationList> NotificationList => Set<NotificationList>();
    }
}
