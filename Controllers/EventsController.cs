using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProServ_ClubCore_Server_API.Database;
using ProServ_ClubCore_Server_API.DTO;
using ProServ_ClubCore_Server_API.Models;

namespace ProServ_ClubCore_Server_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDbContextFactory<ProServDbContext> _contextFactory;

        public EventsController(UserManager<IdentityUser> userManager, IDbContextFactory<ProServDbContext> contextFactory)
        {
            _userManager = userManager;
            _contextFactory = contextFactory;
        }

        [HttpGet("get-my-events")]
        [Authorize]
        public async Task<IActionResult> GetMyEvents([FromQuery] DateTimeOffset date, [FromQuery] string dateOption)
        {
            try
            {
                var identUser = await _userManager.GetUserAsync(User);
                if (identUser == null) return Unauthorized("User does not exist");

                using (var db = _contextFactory.CreateDbContext())
                {
                    DateTime startDate, endDate;
                    switch (dateOption.ToLower())
                    {
                        case "day":
                            startDate = date.Date;
                            endDate = startDate.AddDays(1).AddTicks(-1);
                            break;
                        case "week":
                            int delta = DayOfWeek.Monday - date.DayOfWeek; 
                            startDate = date.AddDays(delta).Date;
                            endDate = startDate.AddDays(7).AddTicks(-1);
                            break;
                        case "month":
                            startDate = new DateTime(date.Year, date.Month, 1);
                            endDate = startDate.AddMonths(1).AddDays(-1);
                            break;
                        default:
                            return BadRequest("Invalid date option");
                    }

                    //secondary try/catch to potentially save request from failing 
                    try
                    {
                        startDate = startDate.ToUniversalTime();
                        endDate = endDate.ToUniversalTime();

                        var events = await db.CalendarEvents
                            .Where(e => e.User_ID == identUser.Id &&
                                        e.StartDate >= startDate &&
                                        e.EndDate <= endDate)
                            .Select(e => new Calendar_Event_SA_DTO
                            {
                                title = e.Title,
                                description = e.Description,
                                startDate = e.StartDate,
                                endDate = e.EndDate,
                                color = e.Color,
                                Event_ID = e.Event_ID.ToString(),
                                assignedBy = e.Creator_ID,
                                canUpdate = e.Creator_ID == identUser.Id,
                                dateCreated = e.Date_Created
                            })
                            .ToListAsync();

                        //sort events by start date and time
                        events.Sort((e1, e2) => e1.startDate.CompareTo(e2.startDate));

                        //foreach event use the assignedBy field to get the name of the user who assigned the event
                        foreach (var e in events)
                        {
                            var assignedByUser = await db.Users.FirstOrDefaultAsync(u => u.User_ID == e.assignedBy);
                            if (assignedByUser != null)
                            {
                                e.assignedBy = assignedByUser.First_Name + " " + assignedByUser.Last_Name;
                            }
                            else
                            {
                                e.assignedBy = "Unknown";
                            }
                        }   

                        return Ok(events);
                    }
                    catch (Exception ex)
                    {
                        //generate empty list of events
                        var eventsDTO = new List<Calendar_Event_SA_DTO>();

                        //print to console error message
                        Console.WriteLine(ex.Message);

                        return Ok(eventsDTO);
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [HttpPost("sa-add-event")] //for users assigning stuff to themselves. Will be mostly athletes and not coaches
        [Authorize]
        public async Task<IActionResult> AddCalendarEvent_SA(Calendar_Event_SA_DTO calendarEvent)
        {
            try
            {
                var identUser = await _userManager.GetUserAsync(User);

                if(identUser == null)
                {
                    return Unauthorized("User does not exist");
                }

                using(var db = _contextFactory.CreateDbContext())
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.User_ID == identUser.Id);

                    if(user == null)
                    {
                        return BadRequest("User does not exist");
                    }

                    Calendar_Event newEvent = new Calendar_Event
                    {
                        Title = calendarEvent.title,
                        Description = calendarEvent.description,
                        StartDate = calendarEvent.startDate,
                        EndDate = calendarEvent.endDate,
                        Color = calendarEvent.color,
                        User_ID = user.User_ID,
                        Creator_ID = user.User_ID,
                        Date_Created = DateTimeOffset.UtcNow
                    };

                    await db.CalendarEvents.AddAsync(newEvent);
                    await db.SaveChangesAsync();

                    Calendar_Event_SA_DTO newEventDTO = new Calendar_Event_SA_DTO
                    {
                        Event_ID = newEvent.Event_ID.ToString(),
                        title = newEvent.Title,
                        description = newEvent.Description,
                        startDate = newEvent.StartDate,
                        endDate = newEvent.EndDate,
                        color = newEvent.Color,
                        assignedBy = user.First_Name + " " + user.Last_Name,
                        canUpdate = true,
                        dateCreated = newEvent.Date_Created
                    };

                    return Ok(newEventDTO);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [HttpPut("sa-update-event")]
        [Authorize]
        public async Task<IActionResult> UpdateCalendarEvent_SA(Calendar_Event_SA_DTO calendarEvent)
        {
            try
            {
                var identUser = await _userManager.GetUserAsync(User);

                if(identUser == null)
                {
                    return Unauthorized("User does not exist");
                }

                using(var db = _contextFactory.CreateDbContext())
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.User_ID == identUser.Id);
                    var eventToUpdate = await db.CalendarEvents.FirstOrDefaultAsync(e => e.Event_ID == Guid.Parse(calendarEvent.Event_ID));

                    if(user == null)
                    {
                        return BadRequest("User does not exist");
                    }

                    if(eventToUpdate == null)
                    {
                        return BadRequest("Event does not exist");
                    }

                    eventToUpdate.Title = calendarEvent.title;
                    eventToUpdate.Description = calendarEvent.description;
                    eventToUpdate.StartDate = calendarEvent.startDate;
                    eventToUpdate.EndDate = calendarEvent.endDate;
                    eventToUpdate.Color = calendarEvent.color;

                    await db.SaveChangesAsync();

                    Calendar_Event_SA_DTO updatedEventDTO = new Calendar_Event_SA_DTO
                    {
                        title = eventToUpdate.Title,
                        description = eventToUpdate.Description,
                        startDate = eventToUpdate.StartDate,
                        endDate = eventToUpdate.EndDate,
                        color = eventToUpdate.Color
                    };

                    return Ok(updatedEventDTO);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [HttpDelete("sa-delete-event")]
        [Authorize]
        public async Task<IActionResult> DeleteCalendarEvent_SA(string eventID)
        {
            try
            {
                var identUser = await _userManager.GetUserAsync(User);

                if(identUser == null)
                {
                    return Unauthorized("User does not exist");
                }

                using(var db = _contextFactory.CreateDbContext())
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.User_ID == identUser.Id);
                    var eventToDelete = await db.CalendarEvents.FirstOrDefaultAsync(e => e.Event_ID == Guid.Parse(eventID));

                    if(user == null)
                    {
                        return BadRequest("User does not exist");
                    }

                    if(eventToDelete == null)
                    {
                        return BadRequest("Event does not exist");
                    }

                    db.CalendarEvents.Remove(eventToDelete);
                    await db.SaveChangesAsync();

                    return Ok("Event deleted");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
    
    }
}
