namespace ChatOnWebApi.Interfaces
{
    public interface IUserProfile
    {
        public string UserName { get; set; } 
        public string Email { get; set; } 
        public string? ImageUrl { get; set; } 
        public string? FirstName { get; set; } 
        public string? LastName { get; set; } 
        public string? PhoneNumber { get; set;}
        public DateTime BirthDay { get; set; } 
    }
}
