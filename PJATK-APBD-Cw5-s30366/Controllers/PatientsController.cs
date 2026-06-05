using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PJATK_APBD_Cw5_s30366.Data;
using PJATK_APBD_Cw5_s30366.DTOs;
using PJATK_APBD_Cw5_s30366.Models;

namespace PJATK_APBD_Cw5_s30366.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly HospitalContext _context;

    public PatientsController(HospitalContext context)
    {
        _context = context;
    }
    [HttpPost("{pesel}/bedassignments")]
    public async Task<IActionResult> AssignBed(string pesel, [FromBody] CreateBedAssignmentRequest request)
    {
        var patientExists = await _context.Patients.AnyAsync(p => p.Pesel == pesel);
        if (!patientExists)
            return NotFound($"Patient with PESEL '{pesel}' not found.");
        
        if (!await _context.Wards.AnyAsync(w => w.Name == request.Ward))
            return NotFound($"Ward '{request.Ward}' not found.");

        if (!await _context.BedTypes.AnyAsync(bt => bt.Name == request.BedType))
            return NotFound($"Bed typ '{request.BedType}' not found.");
        
        var bed = await _context.Beds
            .Where(b => b.BedType.Name == request.BedType
                        && b.Room.Ward.Name == request.Ward)
            .Where(b => !b.BedAssignments.Any(ba =>
                (ba.To == null || ba.To > request.From) &&
                (request.To == null || ba.From < request.To)))
            .FirstOrDefaultAsync();

        if (bed == null)
            return NotFound($"No free bed of type '{request.BedType}' in ward '{request.Ward}' is available for the requested.");
        
        var assignment = new BedAssignment
        {
            PatientPesel = pesel,
            BedId = bed.Id,
            From = request.From,
            To = request.To
        };

        _context.BedAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        return Created(
            $"/api/patients/{pesel}/bedassignments/{assignment.Id}",
            new { assignment.Id, BedId = bed.Id, request.From, request.To });
    }
    [HttpGet]
    public async Task<IActionResult> GetPatients([FromQuery] string? search)
    {
        var query = _context.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(p =>
                EF.Functions.Like(p.FirstName, pattern) ||
                EF.Functions.Like(p.LastName, pattern));
        }

        var result = await query
            .Select(p => new PatientDto
            {
                Pesel = p.Pesel,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Age = p.Age,
                Sex = p.Sex ? "Male" : "Female",
                Admissions = p.Admissions.Select(a => new AdmissionDto
                {
                    Id = a.Id,
                    AdmissionDate = a.AdmissionDate,
                    DischargeDate = a.DischargeDate,
                    Ward = new WardDto
                    {
                        Id = a.Ward.Id,
                        Name = a.Ward.Name,
                        Description = a.Ward.Description
                    }
                }).ToList(),
                BedAssignments = p.BedAssignments.Select(ba => new BedAssignmentDto
                {
                    Id = ba.Id,
                    From = ba.From,
                    To = ba.To,
                    Bed = new BedDto
                    {
                        Id = ba.Bed.Id,
                        BedType = new BedTypeDto
                        {
                            Id = ba.Bed.BedType.Id,
                            Name = ba.Bed.BedType.Name,
                            Description = ba.Bed.BedType.Description
                        },
                        Room = new RoomDto
                        {
                            Id = ba.Bed.Room.Id,
                            HasTv = ba.Bed.Room.HasTv,
                            Ward = new WardDto
                            {
                                Id = ba.Bed.Room.Ward.Id,
                                Name = ba.Bed.Room.Ward.Name,
                                Description = ba.Bed.Room.Ward.Description
                            }
                        }
                    }
                }).ToList()
            })
            .ToListAsync();

        return Ok(result);
    }
}