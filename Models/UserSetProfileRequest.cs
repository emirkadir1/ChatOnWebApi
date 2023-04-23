namespace ChatOnWebApi.Models
{
    public class UserSetProfileRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string BirthDay { get; set; }  = string.Empty;
    }
}
