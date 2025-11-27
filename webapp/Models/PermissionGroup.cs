namespace webapp.Models
{
    public class PermissionGroup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<int> PermissionIds { get; set; } = new List<int>();
        public bool AutoCreated { get; set; } = false;
        public int DepartmentId { get; set; }
    }
}