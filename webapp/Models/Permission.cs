namespace webapp.Models
{
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProgramId { get; set; }
    }
}
