using Microsoft.AspNetCore.Mvc;
using EventManagement.Models;
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
    public ActionResult<IEnumerable<Event>> GetAll()
    {
        var events = _eventService.GetAll();
        return Ok(events);
    }
    
    [HttpGet("{id}")]
    public ActionResult<Event> GetById(Guid id)
    {
        var eventItem = _eventService.GetById(id);
        
        if (eventItem == null)
        {
            return NotFound();
        }
        
        return Ok(eventItem);
    }
    
    [HttpPost]
    public ActionResult<Event> Create(Event eventItem)
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
    public ActionResult<Event> Update(Guid id, Event eventItem)
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