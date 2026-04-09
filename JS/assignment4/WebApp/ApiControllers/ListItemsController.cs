using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using App.Domain.ApiKey.SimpleList;
using Asp.Versioning;

namespace WebApp.ApiControllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiController]
    public class ListItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ListItemsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ListItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ListItem>>> GetListItems(Guid apiKey, bool? completed)
        {
            if (!IsApiKeyValid(apiKey))
            {
                return BadRequest("{'error':'Problem with ApiKey!'}");
            }

            var query = _context.ListItems
                .Include(l => l.ApiKey)
                .Where(l => l.ApiKey!.SecretKey == apiKey)
                .AsQueryable();
            if (completed != null)
            {
                query = query.Where(l => l.Completed == completed);
            }
            
            return await query.ToListAsync();
        }

 // GET: api/ListItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ListItem>> GetListItem(Guid apiKey, Guid id)
        {
            if (!IsApiKeyValid(apiKey))
            {
                return BadRequest("{'error':'Problem with ApiKey!'}");
            }


            var listItem =
                await _context.ListItems.FirstOrDefaultAsync(l => l.Id == id && l.ApiKey!.SecretKey == apiKey);

            if (listItem == null)
            {
                return NotFound();
            }

            return listItem;
        }

        // PUT: api/ListItems/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutListItem(Guid id, Guid apiKey, ListItem listItem)
        {
            if (!IsApiKeyValid(apiKey))
            {
                return BadRequest("{'error':'Problem with ApiKey!'}");
            }

            if (id != listItem.Id)
            {
                return BadRequest();
            }

            var dbListItem = await _context.ListItems
                .Where(l => l.Id == id && l.ApiKey!.SecretKey == apiKey)
                .FirstOrDefaultAsync();
            if (dbListItem == null)
            {
                return BadRequest();
            }

            dbListItem.Completed = listItem.Completed;
            dbListItem.Description = listItem.Description;
            _context.ListItems.Update(dbListItem);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ListItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/ListItems
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<ListItem>> PostListItem(Guid apiKey, ListItem listItem)
        {
            if (!IsApiKeyValid(apiKey))
            {
                return BadRequest("{'error':'Problem with ApiKey!'}");
            }

            var dbApiKey = await _context.ApiKeys.FirstAsync(k => k.SecretKey == apiKey);

            listItem.ApiKeyId = dbApiKey.Id;


            _context.ListItems.Add(listItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetListItem", new {id = listItem.Id}, listItem);
        }

        // DELETE: api/ListItems/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ListItem>> DeleteListItem(Guid apiKey, Guid id)
        {
            if (!IsApiKeyValid(apiKey))
            {
                return BadRequest("{'error':'Problem with ApiKey!'}");
            }

            var listItem =
                await _context.ListItems.FirstOrDefaultAsync(l => l.Id == id && l.ApiKey!.SecretKey == apiKey);
            if (listItem == null)
            {
                return NotFound();
            }

            _context.ListItems.Remove(listItem);
            await _context.SaveChangesAsync();

            return listItem;
        }


        private bool ListItemExists(Guid id)
        {
            return _context.ListItems.Any(e => e.Id == id);
        }
 

        private bool IsApiKeyValid(Guid apiKey)
        {
            return _context.ApiKeys.Any(e => e.SecretKey == apiKey);
        }

    }
}
