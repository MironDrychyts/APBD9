namespace WebApplication2.Models.DTOs;

public class AssignClientToTripDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Telephone { get; set; }
    public string Pesel { get; set; }
    public DateTime? PaymentDate { get; set; }
}