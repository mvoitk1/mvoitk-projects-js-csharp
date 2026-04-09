using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using Asp.Versioning;
using com.akaver.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using PublicApi.DTO.v1.Todo;

namespace WebApp.ApiControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TodoPrioritiesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TodoPrioritiesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/TodoPriorities
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoPriority>>> GetTodoPriorities()
        {
            return (await _context
                .TodoPriorities
                .Where(x => x.AppUserId == User.GetUserId())
                .OrderBy(x => x.PrioritySort)
                .ThenBy(x => x.PriorityName)
                .ToListAsync()).Select(t => new TodoPriority()
            {
                Id = t.Id,
                PriorityName = t.PriorityName,
                PrioritySort = t.PrioritySort,
                SyncDt = t.SyncDt
            }).ToList();
        }

        // GET: api/TodoPriorities/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoPriority>> GetTodoPriority(Guid id)
        {
            var t = await _context
                .TodoPriorities
                .FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == User.GetUserId());

            if (t == null) return NotFound();

            var todoPriority = new TodoPriority()
            {
                Id = t.Id,
                PriorityName = t.PriorityName,
                PrioritySort = t.PrioritySort,
                SyncDt = t.SyncDt
            };

            return Ok(todoPriority);
        }

        // PUT: api/TodoPriorities/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        // TODO: IDOR
        [HttpPut("{id}")]
        public async Task<ActionResult> PutTodoPriority(Guid id, TodoPriority todoPriority)
        {
            if (id != todoPriority.Id)
            {
                return BadRequest();
            }

            var todoPriorityEntity = new App.Domain.Todo.TodoPriority()
            {
                AppUserId = User.GetUserId()!.Value,
                Id = todoPriority.Id,
                PriorityName = todoPriority.PriorityName,
                PrioritySort = todoPriority.PrioritySort,
                SyncDt = todoPriority.SyncDt
            };

            _context.TodoPriorities.Entry(todoPriorityEntity).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: api/TodoPriorities
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TodoPriority>> PostTodoPriority(TodoPriorityCreate todoPriority)
        {
            var todoPriorityEntity = new App.Domain.Todo.TodoPriority()
            {
                AppUserId = User.GetUserId()!.Value,
                PriorityName = todoPriority.PriorityName,
                PrioritySort = todoPriority.PrioritySort,
                SyncDt = todoPriority.SyncDt
            };

            
            _context.TodoPriorities.Add(todoPriorityEntity);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTodoPriority",
                new { id = todoPriorityEntity.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() }, todoPriorityEntity);
        }

        // DELETE: api/TodoPriorities/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoPriority(Guid id)
        {
            var todoPriority = await _context
                .TodoPriorities
                .FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == User.GetUserId());
            if (todoPriority == null)
            {
                return NotFound();
            }

            var dependentCount = await _context.TodoTasks
                .CountAsync(t => t.TodoPriorityId == id);
            if (dependentCount > 0)
            {
                return Conflict(new { message = $"Entity cannot be deleted because it has {dependentCount} dependent TodoTask record(s)" });
            }

            _context.TodoPriorities.Remove(todoPriority);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Entity cannot be deleted because it has dependent TodoTask record(s)" });
            }

            return NoContent();
        }
    }
}