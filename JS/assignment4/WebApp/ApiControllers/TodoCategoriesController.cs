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
    public class TodoCategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TodoCategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/TodoCategories
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IEnumerable<PublicApi.DTO.v1.Todo.TodoCategory>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PublicApi.DTO.v1.Todo.TodoCategory>>> GetTodoCategories()
        {
            return await _context
                .TodoCategories
                .Where(x => x.AppUserId == User.GetUserId())
                .OrderBy(x => x.CategorySort)
                .ThenBy(x => x.CategoryName)
                .Select(x => new PublicApi.DTO.v1.Todo.TodoCategory()
                {
                    Id = x.Id,
                    CategoryName = x.CategoryName,
                    CategorySort = x.CategorySort,
                    SyncDt = x.SyncDt,
                    Tag = x.Tag
                })
                .ToListAsync();
        }

        // GET: api/TodoCategories/5
        [HttpGet("{id}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(PublicApi.DTO.v1.Todo.TodoCategory), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PublicApi.DTO.v1.Todo.TodoCategory>> GetTodoCategory(Guid id)
        {
            var todoCategory = await _context
                .TodoCategories
                .Where(x => x.Id == id && x.AppUserId == User.GetUserId())
                .Select(x => new PublicApi.DTO.v1.Todo.TodoCategory()
                {
                    Id = x.Id,
                    CategoryName = x.CategoryName,
                    CategorySort = x.CategorySort,
                    SyncDt = x.SyncDt,
                    Tag = x.Tag,
                })
                .FirstOrDefaultAsync();

            if (todoCategory == null)
            {
                return NotFound();
            }

            return Ok(todoCategory);
        }

        // PUT: api/TodoCategories/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PublicApi.DTO.v1.Todo.TodoCategory>> PutTodoCategory(Guid id,
            PublicApi.DTO.v1.Todo.TodoCategory todoCategory)
        {
            if (id != todoCategory.Id)
            {
                return BadRequest();
            }

            var dbTodoCategory = await _context
                .TodoCategories
                .FirstAsync(x => x.Id == id && x.AppUserId == User.GetUserId());

            dbTodoCategory.CategoryName = todoCategory.CategoryName;
            dbTodoCategory.CategorySort = todoCategory.CategorySort;
            dbTodoCategory.Tag = todoCategory.Tag;
            dbTodoCategory.SyncDt = todoCategory.SyncDt;
            _context.TodoCategories.Update(dbTodoCategory);

            await _context.SaveChangesAsync();

            return Ok(dbTodoCategory.MapToDTO());
        }

        // POST: api/TodoCategories
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Produces("application/json")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<PublicApi.DTO.v1.Todo.TodoCategory>> PostTodoCategory(
            PublicApi.DTO.v1.Todo.TodoCategoryCreate todoCategory)
        {
            var entity = todoCategory.MapToEntity();
            entity.AppUserId = User.GetUserId()!.Value;
            _context.TodoCategories.Add(entity);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("GetTodoCategory",
                new { id = entity.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() }, entity.MapToDTO());
        }

        // DELETE: api/TodoCategories/5?syncdt=4564564
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTodoCategory(Guid id)
        {
            var todoCategory =
                await _context.TodoCategories.FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == User.GetUserId());
            if (todoCategory == null)
            {
                return NotFound();
            }

            var dependentCount = await _context.TodoTasks
                .CountAsync(t => t.TodoCategoryId == id);
            if (dependentCount > 0)
            {
                return Conflict(new { message = $"Entity cannot be deleted because it has {dependentCount} dependent TodoTask record(s)" });
            }

            _context.TodoCategories.Remove(todoCategory);
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

    // TODO: sync endpoint? list of objects to upsert or delete
    // TODO: verify syncdt? only edit/delete when existing record is in past
    // TODO: after sync return statuses (synced or not) and data for every entity?
}