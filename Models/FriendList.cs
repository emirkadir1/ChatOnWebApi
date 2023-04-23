namespace ChatOnWebApi.Models
{
    public class FriendList
    {
        public int Id { get; set; }
        public User User { get; set; } = new User();
        public List<User> UsersFriendList { get; set; } = new List<User>();
   
    }
}
