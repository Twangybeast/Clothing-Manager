using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClothingManager.Data;
using ClothingManager.Models;
using Microsoft.AspNetCore.Http;

namespace ClothingManager.Controllers
{
    public class OutfitsController : Controller
    {
        private readonly ClothingContext _context;

        public OutfitsController(ClothingContext context)
        {
            _context = context;
        }

        // GET: Outfits
        public async Task<IActionResult> Index()
        {
            List<Outfit> outfits = await _context.Outfits
                .Include(o => o.ClothingOutfits)
                    .ThenInclude(co => co.Clothing)
                .AsNoTracking()
                .ToListAsync();
            foreach (var outfit in outfits)
            {
                outfit.ClothingOutfits = outfit.ClothingOutfits.OrderBy(co => co.Order).ToList();
            }
            return View(outfits);
        }


        // GET: Outfits/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outfit = await _context.Outfits
                .Include(o => o.ClothingOutfits)
                    .ThenInclude(co => co.Clothing)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (outfit == null)
            {
                return NotFound();
            }
            outfit.ClothingOutfits = outfit.ClothingOutfits.OrderBy(co => co.Order).ToList();

            return View(outfit);
        }

        [HttpPost]
        public async Task<string> ClothingOrderUp(int? clothingId)
        {
            if (clothingId == null)
            {
                return "fail";
            }

            int id = -1;
            if (Request.Cookies.ContainsKey("currentOutfitId"))
            {
                if (!Int32.TryParse(Request.Cookies["currentOutfitId"], out id))
                {
                    id = -1;
                }
            }
            Outfit outfit;
            if (id == -1)
            {
                return "fail";
            }
            else
            {
                outfit = await _context.Outfits
                    .Include(o => o.ClothingOutfits)
                        .ThenInclude(co => co.Clothing)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ID == id);
                if (outfit == null)
                {
                    DeleteCurrentOutfitIdCookie();
                    return "fail";
                }
            }
            ClothingOutfit clothingLower = await _context.ClothingOutfits.FirstOrDefaultAsync(m => m.ClothingId == clothingId && m.OutfitId == id);
            if (clothingLower == null)
            {
                return "fail";
            }
            ClothingOutfit clothingUpper = await _context.ClothingOutfits.FirstOrDefaultAsync(m => m.Order == (clothingLower.Order - 1) && m.OutfitId == id);
            if (clothingUpper == null)
            {
                return "fail";
            }
            clothingLower.Order = clothingLower.Order - 1;
            clothingUpper.Order = clothingUpper.Order + 1;


            await _context.SaveChangesAsync();

            return "success";
        }

        [HttpPost]
        public async Task<string> ClothingOrderDown(int? clothingId)
        {
            if (clothingId == null)
            {
                return "fail";
            }

            int id = -1;
            if (Request.Cookies.ContainsKey("currentOutfitId"))
            {
                if (!Int32.TryParse(Request.Cookies["currentOutfitId"], out id))
                {
                    id = -1;
                }
            }
            Outfit outfit;
            if (id == -1)
            {
                return "fail";
            }
            else
            {
                outfit = await _context.Outfits
                    .Include(o => o.ClothingOutfits)
                        .ThenInclude(co => co.Clothing)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ID == id);
                if (outfit == null)
                {
                    DeleteCurrentOutfitIdCookie();
                    return "fail";
                }
            }
            ClothingOutfit clothingUpper = await _context.ClothingOutfits.FirstOrDefaultAsync(m => m.ClothingId == clothingId && m.OutfitId == id);
            if (clothingUpper == null)
            {
                return "fail";
            }
            ClothingOutfit clothingLower = await _context.ClothingOutfits.FirstOrDefaultAsync(m => m.Order == (clothingUpper.Order + 1) && m.OutfitId == id);
            if (clothingLower == null)
            {
                return "fail";
            }
            clothingLower.Order = clothingLower.Order - 1;
            clothingUpper.Order = clothingUpper.Order + 1;


            await _context.SaveChangesAsync();

            return "success";
        }

        [HttpPost]
        public async Task<string> AddClothing(int? clothingId)
        {
            if (clothingId == null)
            {
                return "fail";
            }

            int id = -1;
            if (Request.Cookies.ContainsKey("currentOutfitId"))
            {
                if (!Int32.TryParse(Request.Cookies["currentOutfitId"], out id))
                {
                    id = -1;
                }
            }
            Outfit outfit = new Outfit { Name = "", ClothingOutfits = new List<ClothingOutfit>() };
            if (id == -1)
            {
                await Create(outfit);
                SetCurrentOutfitIdCookie(outfit.ID);
            }
            else
            {
                outfit = await _context.Outfits
                    .Include(o => o.ClothingOutfits)
                        .ThenInclude(co => co.Clothing)
                    .FirstOrDefaultAsync(m => m.ID == id);
                if (outfit == null)
                {
                    DeleteCurrentOutfitIdCookie();
                    return "fail";
                }
            }
            Clothing clothing;
            if ((clothing = await _context.Clothings.AsNoTracking().FirstOrDefaultAsync(m => m.ID == clothingId)) == null)
            {
                return "fail";
            }
            if (outfit.Clothings != null && outfit.Clothings.Contains(clothing))
            {
                return "already exists";
            }
            outfit.ClothingOutfits.Add(new ClothingOutfit { OutfitId = outfit.ID, ClothingId = (int)clothingId, Order = outfit.ClothingOutfits.Count() });
            await _context.SaveChangesAsync();

            return "success";
        }

        [HttpPost]
        public async Task<string> RemoveClothing(int? clothingId)
        {
            if (clothingId == null)
            {
                return "fail";
            }

            int id = -1;
            if (Request.Cookies.ContainsKey("currentOutfitId"))
            {
                if (!Int32.TryParse(Request.Cookies["currentOutfitId"], out id))
                {
                    id = -1;
                }
            }
            if (id == -1)
            {
                return "fail";
            }
            Outfit outfit = await _context.Outfits
                .Include(o => o.ClothingOutfits)
                    .ThenInclude(co => co.Clothing)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (outfit == null)
            {
                DeleteCurrentOutfitIdCookie();
                return "fail";
            }
            ClothingOutfit clothingOutfit = await _context.ClothingOutfits.FirstOrDefaultAsync(m => m.ClothingId == clothingId && m.OutfitId == outfit.ID);
            if (clothingOutfit == null)
            {
                return "fail";
            }
            _context.Remove(clothingOutfit);
            await _context.SaveChangesAsync();

            return "success";
        }

        [HttpPost]
        public async Task<IActionResult> Info()
        {
            int id = -1;
            if (Request.Cookies.ContainsKey("currentOutfitId"))
            {
                if (!Int32.TryParse(Request.Cookies["currentOutfitId"], out id))
                {
                    id = -1;
                }
            }
            if (id == -1)
            {
                return NotFound();
            }

            var outfit = await _context.Outfits
                .Include(o => o.ClothingOutfits)
                    .ThenInclude(co => co.Clothing)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (outfit == null)
            {
                DeleteCurrentOutfitIdCookie();
                return NotFound();
            }


            outfit.ClothingOutfits = outfit.ClothingOutfits.OrderBy(co => co.Order).ToList();

            return View(outfit);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<string> Save([Bind("Name")] Outfit outfit)
        {
            int id = -1;
            if (Request.Cookies.ContainsKey("currentOutfitId"))
            {
                if (!Int32.TryParse(Request.Cookies["currentOutfitId"], out id))
                {
                    id = -1;
                }
            }
            if (id != -1 && await _context.Outfits.Where(o => o.ID == id).AsNoTracking().SingleOrDefaultAsync() != null)
            {
                //Model already exists
                return await Edit(id, outfit);
            }
            else
            {
                return await Create(outfit);
            }
        }

        private async Task<string> Create([Bind("Name")] Outfit outfit)
        {
            if (ModelState.IsValid)
            {
                _context.Add(outfit);
                await _context.SaveChangesAsync();
                SetCurrentOutfitIdCookie(outfit.ID);
                return "success";
            }
            return "fail";
        }
        
        private async Task<string> Edit(int id, [Bind("Name")] Outfit outfit)
        {

            var outfitToUpdate = await _context.Outfits
                .Include(o => o.ClothingOutfits)
                    .ThenInclude(co => co.Clothing)
                .SingleOrDefaultAsync(o => o.ID == id);
            if (outfitToUpdate == null)
            {
                return "fail";
            }
            if (await TryUpdateModelAsync<Outfit>(outfitToUpdate, "", o => o.Name))
            {
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. " + "Try again, and if the problem persists, " + "see your system administrator.");
                    return "fail";
                }
                return "success";
            }
            return "fail";
        }

        // GET: Outfits/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outfit = await _context.Outfits
                .Include(o => o.ClothingOutfits)
                    .ThenInclude(co => co.Clothing)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (outfit == null)
            {
                return NotFound();
            }

            outfit.ClothingOutfits = outfit.ClothingOutfits.OrderBy(co => co.Order).ToList();
            return View(outfit);
        }

        // POST: Outfits/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var outfit = await _context.Outfits.FindAsync(id);
            _context.Outfits.Remove(outfit);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OutfitExists(int id)
        {
            return _context.Outfits.Any(e => e.ID == id);
        }

        private void SetCurrentOutfitIdCookie(int id)
        {
            CookieOptions option = new CookieOptions();

            //option.Expires = DateTime.Now.AddHours(24);
            option.Path = "/";

            Response.Cookies.Append("currentOutfitId", id.ToString(), option);

        }
        private void DeleteCurrentOutfitIdCookie()
        {
            CookieOptions option = new CookieOptions();

            option.Expires = DateTime.Now.AddDays(-10);
            option.Path = "/";

            Response.Cookies.Append("currentOutfitId", null, option);

        }
    }
}
