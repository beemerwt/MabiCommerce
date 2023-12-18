using Emgu.CV.Structure;
using Emgu.CV;
using System.Drawing;

namespace MabiCommerce.Screen
{
    public interface IScreenParser
    {
        public int ParseNumber(Image<Gray, byte> img);

        public int ParseProfit(Image<Gray, byte> img, Rectangle region);
    }
}
