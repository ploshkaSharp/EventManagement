using Microsoft.AspNetCore.Mvc;
using EventManagement.Models;
using EventManagement.DTOs;
using EventManagement.Services;

namespace EventManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    
    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<EventDTO>> GetAll()
    {
        var events = _eventService.GetAll();
        return Ok(events);
    }
    
    [HttpGet("{id}")]
    public ActionResult<EventDTO> GetById(Guid id)
    {
        var eventItem = _eventService.GetById(id);
        
        if (eventItem == null)
        {
            return NotFound();
        }
        
        return Ok(eventItem);
    }
    
    [HttpPost]
    public ActionResult<EventDTO> Create(CreateEventDTO eventItem)
    {
        try
        {
            var createdEvent = _eventService.Create(eventItem);
            return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPut("{id}")]
    public ActionResult<Event> Update(Guid id, UpdateEventDTO eventItem)
    {
        try
        {
            var updatedEvent = _eventService.Update(id, eventItem);
            
            if (updatedEvent == null)
            {
                return NotFound();
            }
            
            return Ok(updatedEvent);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        var deleted = _eventService.Delete(id);
        
        if (!deleted)
        {
            return NotFound();
        }
        
        return NoContent();
    }
}