using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClothingManager.Data;
using ClothingManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingManager.Controllers
{
    public class TagsController : Controller
    {
        private readonly ClothingContext _context;

        public TagsController(ClothingContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> GetTagValues(string tag)
        {
            if (tag.Length == 0)
            {
                return NotFound();
            }
            TagKey tagKey = await _context.TagKeys.Where(t => t.Key.ToLower() == tag.ToLower()).AsNoTracking().SingleOrDefaultAsync();
            if (tagKey == null)
            {
                return NotFound();
            }
            List<string> values = await _context.TagValues.Where(t => t.TagKeyId == tagKey.ID)
                .Select(t => t.Value)
                .Distinct()
                .AsNoTracking().ToListAsync(); 
            
            return Json(values);
        }
    }
}