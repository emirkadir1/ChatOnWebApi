namespace ChatOnWebApi.Models
{
    public class FriendRequests
    {
        public int Id { get; set; }
        public User Sender { get; set; } = new User();
        public User Reciever { get; set; } = new User();
    }
}
