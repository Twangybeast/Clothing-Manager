using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingManager.Services
{
    public class HueSaturationBrightnessColorSpace : IColorSpace
    {
        public (float, float, float) ConvertColorSpace(Color color)
        {

            float hue = (float)(color.GetHue() / 180 * Math.PI);
            float sat = color.GetSaturation();
            float b = color.GetBrightness();

            return ((float)(Math.Cos(hue) * sat), (float)(Math.Sin(hue) * sat), b);
        }
    }
}
