namespace webapp.Models;

public class ApplicationDetails
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string Message { get; set; }
    public DateTime ExpireDate { get; set; }
}