using System;

namespace webapp.Models
{
    public class ActionHistory
    {
        public int Id { get; set; }
        public int? UserPermissionId { get; set; }
        public int? DeviceId { get; set; }
        public int? ApplicationId { get; set; }
        public int? ApplicationDetailsId { get; set; }
        public int? UserId { get; set; }
        public DateTime Date { get; set; }
        public int ActionId { get; set; }
        public int ActionHistoryId { get; set; }
        
    }
}

