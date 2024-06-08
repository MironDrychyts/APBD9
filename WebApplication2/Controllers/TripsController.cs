using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly ScaffoldContext _context; 
     
    public TripsController(ScaffoldContext context) 
    { 
        _context = context; 
    }

    [HttpGet]
    public async Task<IActionResult> GetTrips(int page = 1, int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest("Page and pageSize must be positive integers.");
        }

        try
        {
            var totalTrips = await _context.Trips.CountAsync();
            var trips = await _context.Trips
                .OrderByDescending(t => t.DateFrom)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TripDto
                {
                    Name = t.Name,
                    Description = t.Description,
                    DateFrom = t.DateFrom,
                    DateTo = t.DateTo,
                    MaxPeople = t.MaxPeople,
                    Countries = t.IdCountries.Select(c => new CountryDTO
                    {
                        Name = c.Name
                    }).ToList(),
                    Clients = t.ClientTrips.Select(ct => new ClientDTO
                    {
                        FirstName = ct.IdClientNavigation.FirstName,
                        LastName = ct.IdClientNavigation.LastName
                    }).ToList()
                })
                .ToListAsync();

            var response = new
            {
                PageNum = page,
                PageSize = pageSize,
                AllPages = (int)Math.Ceiling((double)totalTrips / pageSize),
                Trips = trips
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpDelete("api/clients/{idClient}")]
    public async Task<IActionResult> DeleteClient(int idClient)
    {
        try
        {
          
            var client = await _context.Clients.FindAsync(idClient);
            if (client == null)
            {
                return NotFound("Client not found.");
            }


            var hasTrips = await _context.ClientTrips.AnyAsync(ct => ct.IdClient == idClient);
            if (hasTrips)
            {
                return BadRequest("Client has assigned trips and cannot be deleted.");
            }

           
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            return NoContent(); 
        }
        catch (Exception ex)
        {
           
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpPost("api/trips/{idTrip}/clients")]
public async Task<IActionResult> AssignClientToTrip(int idTrip, [FromBody] AssignClientToTripDto request)
{
    try
    {
       
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Pesel == request.Pesel);
        if (client == null)
        {
        
            client = new Client
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Telephone = request.Telephone,
                Pesel = request.Pesel
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync(); 
        }
        else
        {
       
            var existingAssignment = await _context.ClientTrips
                .AnyAsync(ct => ct.IdClient == client.IdClient && ct.IdTrip == idTrip);
            if (existingAssignment)
            {
                return BadRequest("Client is already assigned to this trip.");
            }
        }

    
        var trip = await _context.Trips.FindAsync(idTrip);
        if (trip == null)
        {
            return NotFound("Trip not found.");
        }

        if (trip.DateFrom <= DateTime.Now)
        {
            return BadRequest("Cannot assign to a trip that has already started.");
        }

      
        var clientTrip = new ClientTrip
        {
            IdClient = client.IdClient,
            IdTrip = idTrip,
            RegisteredAt = DateTime.Now,
            PaymentDate = request.PaymentDate
        };

        _context.ClientTrips.Add(clientTrip);
        await _context.SaveChangesAsync();

        return Ok("Client successfully assigned to the trip.");
    }
    catch (Exception ex)
    {
      
        return StatusCode(500, "Internal server error");
    }
}
} 