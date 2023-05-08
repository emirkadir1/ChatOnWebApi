namespace ChatOnWebApi.Models
{
    public class UserFirstTimeProfile
    {
        public string? UserName { get; set; } = string.Empty;
        public string? FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; } = DateTime.MinValue;
        public string? LanguageCode { get; set; } = string.Empty;
    }
}
