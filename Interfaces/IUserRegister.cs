namespace ChatOnWebApi.Interfaces
{
    interface IUserRegister
    {
        string UserName { get; set; }
        string Email { get; set; }
        byte[] PasswordHash { get; set; }
        byte[] PasswordSalt { get; set; }
    }
}
