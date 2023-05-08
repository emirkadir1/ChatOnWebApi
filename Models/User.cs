using ChatOnWebApi.Interfaces;
using System.Numerics;

namespace ChatOnWebApi.Models
{
    public class User : IUserRegister,IUserProfile
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } =string.Empty;
        public byte[] PasswordHash { get; set; } = new byte[32];
        public byte[] PasswordSalt { get; set; } = new byte[32]; 
        public string? ConnectionId { get; set; } = string.Empty;
        public bool Online { get; set; } = false;
        public string? LookingAt { get; set; } = string.Empty; 
        public string? ImageUrl { get; set; } = string.Empty;
        public string? FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public DateTime BirthDay { get; set;} 
        //Refresh Token
        public string RefreshToken { get; set;} = string.Empty;
        public DateTime TokenCreated { get;set; }
        public DateTime TokenExpires { get;set; }
        //Language
        public string? LanguageCode { get; set; } = string.Empty;
    }
}
