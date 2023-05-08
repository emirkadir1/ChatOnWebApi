namespace ChatOnWebApi.Models
{
    public class NotificationList
    {
        public int Id { get; set; }
        public User User { get; set; } =new User();
        public List<Notification> UsersNotificationList { get; set; } = new List<Notification>();
    }
}
