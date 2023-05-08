namespace ChatOnWebApi.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public User Sender { get; set; } = new User();

        public string type { get; set; } = string.Empty;
    }
}
