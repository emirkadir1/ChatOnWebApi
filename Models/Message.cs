using ChatOnWebApi.Interfaces;
using Org.BouncyCastle.Tls;

namespace ChatOnWebApi.Models
{
    public class Message : INewMessage
    {
        public int Id { get; set; }
        public User Sender { get; set; } = new User();
        public User Reciver { get; set; } = new User();
        public string Body { get; set; } = string.Empty;
        public string CreatedTime { get; set; } = DateTime.Now.ToString();
        public string Translate  { get; set; }=string.Empty;
        public byte IsRecived { get; set; } = 0;

    }
}
