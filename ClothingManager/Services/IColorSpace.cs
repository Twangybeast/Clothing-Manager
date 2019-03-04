using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingManager.Services
{
    public interface IColorSpace
    {
        (float, float, float) ConvertColorSpace(Color Color);
    }
}
