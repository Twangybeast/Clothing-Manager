using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingManager.Services
{
    public class YUVColorSpace : IColorSpace
    {
        private static readonly float U_max = 0.436f;
        private static readonly float V_max = 0.615f;
        //Using https://en.wikipedia.org/wiki/YUV BT.601
        public (float, float, float) ConvertColorSpace(Color Color)
        {
            float r = Color.R / 255f;
            float g = Color.G / 255f;
            float b = Color.B / 255f;
            float Y = (float)((0.299 * r) + (0.587 * g) + (0.114 * b));
            float U = (float)((-0.14713 * r) + (-0.28886 * g) + (0.436 * b));
            float V = (float)((0.615 * r) + (-0.51499 * g) + (-0.10001 * b));

            //Rescale to 0, 1
            U = rescale(U, U_max);
            V = rescale(V, V_max);
            return (U, V, Y);
        }
        private float rescale(float f, float f_max)
        {
            return (f + f_max) / (2 * f_max);
        }
    }
}
