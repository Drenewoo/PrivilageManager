namespace webapp.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? DeviceId { get; set; }
        public int? ProgramId { get; set; }
        public int? PermissionId { get; set; }
        public string? MessageText { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.UnixEpoch;
        public int? UPermissionsId { get; set; }
    }
}
