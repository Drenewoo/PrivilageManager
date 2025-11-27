namespace webapp.Models
{
    public class Device
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DeviceTypeId { get; set; }
        public string Serial { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public DateTime StatusUpdate { get; set; } = DateTime.UnixEpoch;
    }
}
