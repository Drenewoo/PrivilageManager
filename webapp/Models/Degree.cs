namespace webapp.Models
{
    public class Degree
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public int ManagerId { get; set; }
    }
}
