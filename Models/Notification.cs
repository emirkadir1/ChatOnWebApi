namespace ChatOnWebApi.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Sender { get; set; } = string.Empty;

        public string type { get; set; } = string.Empty;
    }
}
