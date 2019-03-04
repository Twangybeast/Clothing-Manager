using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClothingManager.Data;
using ClothingManager.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using ImageMagick;
using System.Diagnostics;
using System.Collections;

namespace ClothingManager.Controllers
{
    public class ClothingsController : Controller
    {
        private readonly ClothingContext _context;
        private readonly IHostingEnvironment hostingEnvironment;

        public ClothingsController(ClothingContext context, IHostingEnvironment environment)
        {
            _context = context;
            hostingEnvironment = environment;
        }

        // GET: Clothings
        public async Task<IActionResult> Index(string orderOption)
        {
            Dictionary<string, Comparison<Clothing>> orderByDict = new Dictionary<string, Comparison<Clothing>>
            {
                {"Newest", (c1, c2) => -c1.ID.CompareTo(c2.ID) },
                {"Oldest", (c1, c2) => c1.ID.CompareTo(c2.ID) },
                {"Alphabetical", (c1, c2) => c1.Name.CompareTo(c2.Name)},
                {"Reverse Alphabetical", (c1, c2) => -c1.Name.CompareTo(c2.Name)},

                //Based on step sorting https://www.alanzucconi.com/2015/09/30/colour-sorting/
                {"Color", (c1, c2) => ColorStep(c1.Color, 8).CompareTo(ColorStep(c2.Color, 8))}
            };
            var orderOptions = orderByDict.Keys;
            ViewData["order-options"] = orderOptions;
            if (orderOption == null || !orderOptions.Contains(orderOption))
            {
                orderOption = orderOptions.First();
            }
            ViewData["order-option"] = orderOption;


            var tags = GetTagsList(k => Request.Query.ContainsKey(k), k => Request.Query[k], true);
            //Forced tagvalues
            IList<TagValue> tagValues = new List<TagValue>(tags.Count());
            foreach (var tag in tags)
            {
                tagValues.Add(new TagValue { Value = tag.Item2, TagKey = new TagKey { Key = tag.Item1 } });
            }
            ViewData["tags"] = tagValues;

            var selectedTags = new Dictionary<string, ISet<string>>();
            foreach ((string key, string value) in tags)
            {
                if (!selectedTags.ContainsKey(key))
                {
                    selectedTags.Add(key, new HashSet<string>());
                }
                if (value.Length > 0)
                { 
                    selectedTags[key].Add(value.ToLower());
                }
            }

            List<Clothing> clothings = await _context.Clothings
                .Include(c => c.TagValues)
                    .ThenInclude(t => t.TagKey)
                .AsNoTracking()
                .ToListAsync();


            clothings.RemoveAll(c =>
            {
                var clothingTags = new Dictionary<string, string>();
                foreach (TagValue value in c.TagValues)
                {
                    clothingTags[value.TagKey.Key.ToLower()] = value.Value;
                }
                foreach (string key in selectedTags.Keys)
                {
                    if (!clothingTags.ContainsKey(key.ToLower()))
                    {
                        return true;
                    }
                    if (selectedTags[key].Count == 0)
                    {
                        //Blank entries in forms are wildcard for tag value
                        continue;
                    }
                    if (!selectedTags[key].Contains(clothingTags[key.ToLower()].ToLower()))
                    {
                        return true;
                    }
                }
                return false;
            });


            clothings.Sort(orderByDict[orderOption]);


            ViewBag.TagKeys = await _context.TagKeys.OrderBy(t => t.Key).AsNoTracking().Select(t => t.Key).ToListAsync();

            return View(clothings);
        }

        // GET: Clothings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clothing = await _context.Clothings
                .Include(i => i.TagValues)
                    .ThenInclude(i => i.TagKey)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (clothing == null)
            {
                return NotFound();
            }

            clothing.TagValues = clothing.TagValues.ToList();
            ((List<TagValue>)clothing.TagValues).Sort((v1, v2) => v1.TagKey.Key.ToLower().CompareTo(v2.TagKey.Key.ToLower()));

            return View(clothing);
        }

        // GET: Clothings/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.TagKeys = await _context.TagKeys.OrderBy(t => t.Key).AsNoTracking().Select(t => t.Key).ToListAsync();
            return View();
        }

        // POST: Clothings/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Clothing clothing, IFormFile image, bool autocolor)
        {
            var tags = GetTagsList(k => Request.Form.ContainsKey(k), k => Request.Form[k], false);
            if (ModelState.IsValid && clothing.Name != null && clothing.Name.Length > 0)
            {
                await EnsureTagKeysExist(tags);
                clothing.TagValues = new List<TagValue>();
                AddTagsToClothing(clothing, tags);

                try
                {
                    clothing.Argb = Convert.ToInt32(Request.Form["Argb"], 16);
                }
                catch (Exception) { }

                //Save image
                if (image != null)
                {
                    clothing.ImagePath = await SaveImage(image);
                }
                if (autocolor && clothing.ImagePath != null)
                {
                    Color color = IdentifyImageDominantColor(clothing.ImagePath);
                    if (!color.IsEmpty)
                    {
                        clothing.Color = color;
                    }
                }


                _context.Add(clothing);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ForcefullyAddTagsToClothing(clothing, tags);
            ViewBag.TagKeys = await _context.TagKeys.OrderBy(t => t.Key).AsNoTracking().Select(t => t.Key).ToListAsync();
            return View(clothing);
        }

        // GET: Clothings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var clothing = await _context.Clothings
                .Include(i => i.TagValues)
                    .ThenInclude(i => i.TagKey)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ID == id);
            if (clothing == null)
            {
                return NotFound();
            }
            clothing.TagValues = clothing.TagValues.ToList();
            ((List<TagValue>)clothing.TagValues).Sort((v1, v2)=>v1.TagKey.Key.ToLower().CompareTo(v2.TagKey.Key.ToLower()));
            ViewBag.TagKeys = await _context.TagKeys.OrderBy(t => t.Key).AsNoTracking().Select(t => t.Key).ToListAsync();

            return View(clothing);
        }

        // POST: Clothings/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id, IFormFile image, bool autocolor)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tags = GetTagsList(k => Request.Form.ContainsKey(k), k => Request.Form[k], false);
            var clothingToUpdate = await _context.Clothings
                .Include(i => i.TagValues)
                    .ThenInclude(i => i.TagKey)
                .SingleOrDefaultAsync(c => c.ID == id);
            if (await TryUpdateModelAsync<Clothing>(clothingToUpdate, "", c => c.Name) && clothingToUpdate.Name != null && clothingToUpdate.Name.Length > 0)
            {
                await EnsureTagKeysExist(tags);
                AddTagsToClothing(clothingToUpdate, tags);

                try
                {
                    clothingToUpdate.Argb = Convert.ToInt32(Request.Form["Argb"], 16);
                }
                catch (Exception) { }

                //Save image
                if (image != null)
                {
                    clothingToUpdate.ImagePath = await SaveImage(image);
                    
                }
                if (autocolor && clothingToUpdate.ImagePath != null)
                {
                    Color color = IdentifyImageDominantColor(clothingToUpdate.ImagePath);
                    if (!color.IsEmpty)
                    {
                        clothingToUpdate.Color = color;
                    }
                }



                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException /* ex */)
                {
                    //Log the error (uncomment ex variable name and write a log.)
                    ModelState.AddModelError("", "Unable to save changes. " +
                        "Try again, and if the problem persists, " +
                        "see your system administrator.");
                }
            }
            ForcefullyAddTagsToClothing(clothingToUpdate, tags);
            ViewBag.TagKeys = await _context.TagKeys.OrderBy(t => t.Key).AsNoTracking().Select(t => t.Key).ToListAsync();
            return View(clothingToUpdate);
        }

        // GET: Clothings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clothing = await _context.Clothings
                .Include(i => i.TagValues)
                    .ThenInclude(i => i.TagKey)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (clothing == null)
            {
                return NotFound();
            }
            clothing.TagValues = clothing.TagValues.ToList();
            ((List<TagValue>)clothing.TagValues).Sort((v1, v2) => v1.TagKey.Key.ToLower().CompareTo(v2.TagKey.Key.ToLower()));

            return View(clothing);
        }

        // POST: Clothings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var clothing = await _context.Clothings
                .Include(i => i.TagValues)
                .SingleOrDefaultAsync(i => i.ID == id);
            foreach (var item in clothing.TagValues)
            {
                _context.TagValues.Remove(item);
            }
            _context.Clothings.Remove(clothing);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClothingExists(int id)
        {
            return _context.Clothings.Any(e => e.ID == id);
        }



        //Helper methods
        [NonAction]
        public static ICollection<Tuple<string, string>> GetTagsList(Func<string, bool> containsKey, Func<string, string> getKey, bool allowEmptyValue)
        {
            var tags = new LinkedList<Tuple<string, string>>();
            for (int i = 1; containsKey("tagkey" + i); i++)
            {
                if (!containsKey("tagvalue" + i)) break;
                //TODO validation of tag key and tag value state
                string key = getKey("tagkey" + i);
                string value = getKey("tagvalue" + i);
                key = key.Trim();
                value = value.Trim();
                if (key.Length == 0) continue;
                if (!allowEmptyValue && value.Length == 0) continue;

                tags.AddLast(new Tuple<string, string>(key, value));
            }
            return tags;
        }
        private async Task EnsureTagKeysExist(ICollection<Tuple<string, string>> tags)
        {
            Boolean changes = false;
            foreach ((string key, string value) in tags)
            {
                //Check if key already exists
                var tagKey = await _context.TagKeys.Where(t => t.Key.ToLower().Equals(key.ToLower())).SingleOrDefaultAsync();
                if (tagKey == null)
                {
                    changes = true;
                    tagKey = new TagKey { Key = key, Values = "" };
                    _context.Add(tagKey);
                }
            }
            if (changes)
            {
                //Update database to get new IDs for tagkeys
                await _context.SaveChangesAsync();
            }
        }
        private void AddTagsToClothing(Clothing clothing, ICollection<Tuple<string, string>> tags)
        {
            var selectedTags = new Dictionary<string, string>();
            foreach ((string key, string value) in tags)
            {
                selectedTags[key.ToLower()] = value;
            }
            HashSet<int> clothingTags;
            if (clothing.TagValues == null)
            {
                clothingTags = new HashSet<int>();
            } else
            {
                clothingTags = new HashSet<int>(clothing.TagValues.Select(t => t.TagKeyId));

            }
            foreach (var tagKey in _context.TagKeys)
            {
                string value;
                if (selectedTags.TryGetValue(tagKey.Key.ToLower(), out value))
                {
                    if (!clothingTags.Contains(tagKey.ID))
                    {
                        clothing.TagValues.Add(new TagValue { ClothingId = clothing.ID, TagKeyId = tagKey.ID, Value = value });
                    }
                    else
                    {
                        clothing.TagValues.Where(t => t.TagKeyId == tagKey.ID).Single().Value = value;
                    }
                }
                else
                {
                    if (clothingTags.Contains(tagKey.ID))
                    {
                        TagValue tagToRemove = clothing.TagValues.SingleOrDefault(i => i.TagKeyId == tagKey.ID);
                        _context.Remove(tagToRemove);
                    }
                }
            }
        }

        private void ForcefullyAddTagsToClothing(Clothing clothing, ICollection<Tuple<string, string>> tags)
        {
            clothing.TagValues = new List<TagValue>(tags.Count());
            foreach ((string key, string value) in tags)
            {
                //sloppy cuz this is just for display purposes
                TagValue tagValue = new TagValue { Value = value };
                tagValue.TagKey = new TagKey { Key = key, Values = "" };
                clothing.TagValues.Add(tagValue);
            }
        }

        private async Task<string> SaveImage(IFormFile file)
        {
            var uniqueFileName = GetUniqueFileName(file.FileName);
            var filePath = GetFilePath(uniqueFileName);
            var fileStream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fileStream);
            fileStream.Flush();
            fileStream.Close();
            using (MagickImage image = new MagickImage(filePath))
            {
                ExifProfile profile = image.GetExifProfile();
                image.AutoOrient();
                if (profile != null)
                {
                    profile.SetValue(ExifTag.Orientation, (UInt16)0);
                }
                int sizeLimit = 400;

                if (image.Width < image.Height)
                {
                    image.Resize(sizeLimit, 0);

                    image.Write(filePath);

                    int y = (image.Height - sizeLimit) / 2;
                    image.Crop(new MagickGeometry { Width = sizeLimit, Height = sizeLimit, X = 0, Y = y });
                }
                else
                {

                    image.Resize(0, sizeLimit);

                    image.Write(filePath);

                    int x = (image.Width- sizeLimit) / 2;
                    image.Crop(new MagickGeometry { Width = sizeLimit, Height = sizeLimit, X = x, Y = 0 });
                }
                image.Write(GetFilePath("circle_" + uniqueFileName));
            }
            return uniqueFileName;
        }
        private Color IdentifyImageDominantColor(string uniqueFileName)
        {
            var filePath = GetFilePath(uniqueFileName);
            using (MagickImage image = new MagickImage(filePath))
            {
                int width = image.Width / 3;
                int height = image.Height / 3;
                image.Crop(new MagickGeometry(width, height, width, height));
                image.Quantize(new QuantizeSettings
                {
                    Colors = 3,
                    ColorSpace = ColorSpace.HSB,
                    DitherMethod = DitherMethod.No
                });
                var hist = image.Histogram();
                MagickColor dominant = null;
                foreach (MagickColor key in hist.Keys)
                {
                    if (dominant == null || hist[dominant] < hist[key])
                    {
                        dominant = key;
                    }
                }
                if (dominant == null)
                {
                    return Color.Empty;
                }
                else
                {
                    int r, g, b;
                    HsvToRgb((dominant.R >> 8) / 256.0 * 360, (dominant.G >> 8) / 256.0, (dominant.B >> 8) / 256.0, out r, out g, out b);
                    return Color.FromArgb(r, g, b);
                }
            }
        }
        private string GetFilePath(string uniqueFileName)
        {
            var uploads = Path.Combine(hostingEnvironment.WebRootPath, "uploads");
            return Path.Combine(uploads, uniqueFileName);
        }

        private string GetUniqueFileName(string fileName)
        {
            fileName = Path.GetFileName(fileName);
            return Guid.NewGuid().ToString()
                      + Path.GetExtension(fileName);
        }

        void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
        
        public long ColorStep(Color color, int repetitions = 1)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float lum = (float) Math.Sqrt(0.241 * r + 0.691 * g + 0.068 * b);
            float v = Math.Max(r, Math.Max(g, b));
            float h = color.GetHue() / 360f;

            int h2 = (int)(h * repetitions);
            int lum2 = (int)(lum * repetitions);
            int v2 = (int)(v * repetitions);

            if (h2 % 2 == 1)
            {
                v2 = repetitions - v2;
                lum2 = repetitions - lum2;
            }

            return (h2 * repetitions * repetitions) + (lum2 * repetitions) + v2;
        }
    }
}
