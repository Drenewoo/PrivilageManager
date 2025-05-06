namespace webapp.Models
{
    public class UserPermission
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PermissionId { get; set; }
        public int StatusId { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.UnixEpoch;
        public DateTime ResponseDate { get; set; } = DateTime.UnixEpoch;
    }
}
