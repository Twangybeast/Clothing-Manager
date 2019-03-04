using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClothingManager.Data;
using ClothingManager.Models;
using ClothingManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingManager.Controllers
{
    public class GradientFieldController : Controller
    {
        private readonly ClothingContext _context;
        private readonly IColorSpace _colorSpace;

        public GradientFieldController(ClothingContext context, IColorSpace colorSpace)
        {
            _context = context;
            _colorSpace = colorSpace;
        }

        private readonly static int IMAGE_PIXELS = 100;
        // GET: GradientField
        public async Task<IActionResult> Index()
        {

            var tags = ClothingsController.GetTagsList(k => Request.Query.ContainsKey(k), k => Request.Query[k], true);
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


            ViewBag.TagKeys = await _context.TagKeys.OrderBy(t => t.Key).AsNoTracking().Select(t => t.Key).ToListAsync();

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

            List<GradientItemViewModel> items = new List<GradientItemViewModel>(clothings.Count());
            foreach (Clothing clothing in clothings)
            {
                items.Add(new GradientItemViewModel { Clothing = clothing, Position = _colorSpace.ConvertColorSpace(clothing.Color) });
            }
            AdjustPositions(items);
            (float maxX, float maxY) = getBounds(items);
            //Positions based on top left corner, re-adjust maximums
            maxX += IMAGE_SIZE;
            maxY += IMAGE_SIZE;
            ViewBag.ItemSize = IMAGE_PIXELS;
            ViewBag.width = (int)(maxX / IMAGE_SIZE * IMAGE_PIXELS);
            ViewBag.height = (int)(maxY / IMAGE_SIZE * IMAGE_PIXELS);
            ViewBag.scale = IMAGE_PIXELS / IMAGE_SIZE;
            
            return View(items);
        }

        // GET: GradientField/Details/id
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clothing = await _context.Clothings
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (clothing == null)
            {
                return NotFound();
            }

            return View(clothing);
        }
        private (float, float) getBounds(List<GradientItemViewModel> items)
        {
            float minX = 1, minY = 1, maxX = 0, maxY = 0;
            foreach (var item in items)
            {
                minX = Math.Min(item.Position.Item1, minX);
                minY = Math.Min(item.Position.Item2, minY);
                maxX = Math.Max(item.Position.Item1, maxX);
                maxY = Math.Max(item.Position.Item2, maxY);
            }
            maxX -= minX;
            maxY -= minY;
            foreach (var item in items)
            {
                item.Position = ((item.Position.Item1 - minX), (item.Position.Item2 - minY), item.Position.Item3);
            }
            return (maxX, maxY);
        }

        private readonly int MAX_ITERATIONS = 1000;
        private readonly float IMAGE_SIZE = 0.2f;
        private void AdjustPositions(List<GradientItemViewModel> items)
        {
            AdjustFurther(items);
            AdjustCloser(items);
            AdjustFurther(items);
        }
        private void AdjustCloser(List<GradientItemViewModel> items)
        {
            //Push together
            items.Sort((i1, i2) => DistanceFromCenter(i1.Position).CompareTo(DistanceFromCenter(i2.Position)));
            for (int i = 0; i < items.Count(); i++)
            {
                var item = items.ElementAt(i);
                //Draw a vector, check what items lie on it
                //ax + by + c = 0
                float a = item.Position.Item2 - 0.5f;
                float b = 0.5f - item.Position.Item1;
                float c = -(0.5f * a + 0.5f * b);

                GradientItemViewModel closestItem = null;
                float closestDistance = 0;
                float dBottom = (float)Math.Sqrt(a * a + b * b);
                for (int j = 0; j < items.Count(); j++)
                {
                    if (i == j) continue;
                    var item2 = items.ElementAt(j);
                    //distance between point and line
                    float dTop = Math.Abs(a * item2.Position.Item1 + b * item2.Position.Item2 + c);
                    if (dTop < IMAGE_SIZE * dBottom && DistanceBetween(item2.Position, item.Position) < Math.Sqrt(DistanceFromCenter(item.Position)))
                    {
                        //Collides
                        if (closestItem == null)
                        {
                            closestItem = item2;
                            closestDistance = dTop;
                        }
                        else
                        {
                            if (dTop < closestDistance)
                            {
                                closestItem = item2;
                                closestDistance = dTop;
                            }
                        }
                    }
                }

                //Update to actual distance
                closestDistance = closestDistance / dBottom;

                //Use center if none found
                if (closestItem == null)
                {
                    closestItem = new GradientItemViewModel { Position = (0.5f, 0.5f, 0.5f) };
                }

                //Draw vector connecting closest point to the line
                float a1 = b;
                float b1 = -a;
                //float c1 = -(closestItem.Position.Item1 * a1 + closestItem.Position.Item2 * b1);

                float dy = -a1;
                float dx = b1;
                float mag = VectorMagnitude((dx, dy));
                (float, float) vector = (dx / mag * closestDistance, dy / mag * closestDistance);
                (float, float) p0 = (closestItem.Position.Item1, closestItem.Position.Item2);

                (float, float) p1 = (p0.Item1 + vector.Item1, p0.Item2 + vector.Item2);
                (float, float) p2 = (p0.Item1 - vector.Item1, p0.Item2 - vector.Item2);

                //Update position based on vector intersectino with line
                (float, float) p = (DistanceFromCenter((p1.Item1, p1.Item2, 0)) < DistanceFromCenter((p2.Item1, p2.Item2, 0))) ? p1 : p2;
                float targetDistance = (float)Math.Sqrt(IMAGE_SIZE * IMAGE_SIZE - closestDistance * closestDistance);
                ((float, float, float) newPos, var ignore) = PushApart(item.Position, (p.Item1, p.Item2, 0), targetDistance, false);
                item.Position = newPos;
            }
        }
        private void AdjustFurther(List<GradientItemViewModel> items)
        {
            bool change = true;
            //Push apart
            for (int iteration = 0; iteration < MAX_ITERATIONS && change; iteration++)
            {
                change = false;
                items.Sort((i1, i2) => DistanceFromCenter(i1.Position).CompareTo(DistanceFromCenter(i2.Position)));
                //Check for collsions
                for (int i = 0; i < items.Count(); i++)
                {
                    var item1 = items.ElementAt(i);
                    //Skip to closest item
                    int j = 0;
                    for (; j < items.Count(); j++)
                    {
                        if (i == j) continue;
                        var item2 = items.ElementAt(j);
                        float d1 = DistanceFromCenter(item1.Position);
                        float d2 = DistanceFromCenter(item2.Position);
                        if (Math.Abs(d2 - d1) <= IMAGE_SIZE)
                        {
                            break;
                        }
                    }
                    //Starting from item in range
                    for (; j < items.Count(); j++)
                    {
                        if (i == j) continue;
                        var item2 = items.ElementAt(j);
                        //Push apart items
                        if (ItemsCollide(item1.Position, item2.Position))
                        {
                            change = true;
                            (var p1, var p2) = PushApart(item1.Position, item2.Position, IMAGE_SIZE, true);
                            item1.Position = p1;
                            item2.Position = p2;
                        }
                        float d1 = DistanceFromCenter(item1.Position);
                        float d2 = DistanceFromCenter(item2.Position);
                        if (Math.Abs(d2 - d1) > IMAGE_SIZE)
                        {
                            break;
                        }
                    }
                }
            }
        }
        private float DistanceFromCenter((float, float, float) position)
        {
            float x = position.Item1 - 0.5f;
            float y = position.Item2 - 0.5f;
            return x * x + y * y;
        }
        //Moves items to target distance
        private ((float, float, float), (float, float, float)) PushApart((float, float, float) p1, (float, float, float) p2, float targetDistance, bool shareMove)
        {
            //Find distance between them
            float distance = VectorMagnitude((p2.Item1 - p1.Item1, p2.Item2 - p1.Item2));
            float moveFactor = shareMove ? 2 : 1;
            float moveDistance = (targetDistance - distance)/moveFactor;


            (float, float) vector = (p2.Item1 - p1.Item1, p2.Item2 - p1.Item2);
            //Make unit
            float vectorLength = VectorMagnitude(vector);
            if (vectorLength < 0.0001f)
            {
                vector = (1f, 0);
                vectorLength = 1/2f;
            }
            vector = (vector.Item1 / vectorLength * moveDistance,  vector.Item2 / vectorLength * moveDistance);
            (float, float, float) P1 = (p1.Item1 - vector.Item1, p1.Item2 - vector.Item2, p1.Item3);
            (float, float, float) P2 = (p2.Item1 + vector.Item1, p2.Item2 + vector.Item2, p2.Item3);
            return (P1, P2);
        }
        private float VectorMagnitude((float, float) vector)
        {
            return (float)Math.Sqrt(vector.Item1 * vector.Item1 + vector.Item2 * vector.Item2);
        }
        private Boolean ItemsCollide((float, float, float) p1, (float, float, float) p2)
        {
            return VectorMagnitude((p2.Item1 - p1.Item1, p2.Item2 - p1.Item2)) <= IMAGE_SIZE;
        }
        private float DistanceBetween((float, float, float) p1, (float, float, float) p2)
        {
            return VectorMagnitude((p2.Item1 - p1.Item1, p2.Item2 - p1.Item2));
        }
    }
}