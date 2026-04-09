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
    public class TodoTasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TodoTasksController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/TodoTasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoTask>>> GetTodoTasks()
        {
            var res = await _context
                .TodoTasks
                .Where(t =>
                    t.TodoCategory!.AppUserId == User.GetUserId() &&
                    t.TodoPriority!.AppUserId == User.GetUserId()
                )
                .OrderBy(x => x.TaskSort)
                .ThenBy(x => x.CreatedDt)
                .ToListAsync();

            var finalRes = res.Select(t => t.MapToDTO()).ToList();

            return Ok(finalRes);
        }

        // GET: api/TodoTasks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoTask>> GetTodoTask(Guid id)
        {
            var entity = await _context
                .TodoTasks
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.TodoCategory!.AppUserId == User.GetUserId() &&
                    t.TodoPriority!.AppUserId == User.GetUserId()
                );

            if (entity == null)
            {
                return NotFound();
            }

            var todoTask = entity.MapToDTO();

            return Ok(todoTask);
        }

        // PUT: api/TodoTasks/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ActionResult<TodoTask>> PutTodoTask(Guid id, TodoTask todoTask)
        {
            if (id != todoTask.Id)
            {
                return BadRequest();
            }

            if (await _context.TodoCategories.AnyAsync(x =>
                    x.Id == todoTask.TodoCategoryId && x.AppUserId == User.GetUserId()) &&
                await _context.TodoPriorities.AnyAsync(x =>
                    x.Id == todoTask.TodoPriorityId && x.AppUserId == User.GetUserId()) &&
                await _context.TodoTasks.AnyAsync(x =>
                    x.Id == todoTask.Id && x.TodoCategory!.AppUserId == User.GetUserId() &&
                    x.TodoPriority!.AppUserId == User.GetUserId())
               )
            {
                var entity = todoTask.MapToEntity();
                _context.TodoTasks.Update(entity);
                await _context.SaveChangesAsync();
            }
            else
            {
                return BadRequest("Could not find taks or category or priority for current user!");
            }


            return Ok(todoTask);
        }

        // POST: api/TodoTasks
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TodoTask>> PostTodoTask(TodoTaskCreate todoTask)
        {
            var entity = todoTask.MapToEntity();

            if (await _context.TodoCategories.AnyAsync(x =>
                    x.Id == todoTask.TodoCategoryId && x.AppUserId == User.GetUserId()) &&
                await _context.TodoPriorities.AnyAsync(x =>
                    x.Id == todoTask.TodoPriorityId && x.AppUserId == User.GetUserId()))
            {
                _context.TodoTasks.Add(entity);
                await _context.SaveChangesAsync();
            }
            else
            {
                return BadRequest("Could not find category or priority for current user!");
            }

            return CreatedAtAction("GetTodoTask",
                new { id = entity.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() }, entity.MapToDTO());
        }

        // DELETE: api/TodoTasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoTask(Guid id)
        {
            var todoTask = await _context.TodoTasks.FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.TodoCategory!.AppUserId == User.GetUserId() &&
                t.TodoPriority!.AppUserId == User.GetUserId()
            );
            if (todoTask == null)
            {
                return NotFound();
            }

            _context.TodoTasks.Remove(todoTask);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}