using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MabiCommerce.Screen
{
    public class BitmapFontParser : IScreenParser, IDisposable
    {
        // For the profit text
        private readonly Bitmap PositiveArrow;
        private readonly Bitmap[] Numbers;

        public BitmapFontParser()
        {
            // Convert bitmaps to binary data using opencv
            using var bmpArrow = new Bitmap("data/img/bitmap/positive.png");
            using var imgArrow = bmpArrow.ToImage<Gray, byte>();
            PositiveArrow = imgArrow.ToBitmap();

            Numbers = new Bitmap[10];
            for (int i = 0; i < 10; i++)
            {
                using var bmpNumber = new Bitmap("data/img/bitmap/" + i + ".png");
                using var imgNumber = bmpNumber.ToImage<Gray, byte>();
                Numbers[i] = imgNumber.ToBitmap();
            }
        }

        // Assume ROI has been set and img has been thresholded
        public int ParseNumber(Image<Gray, byte> img)
        {
            var result = 0;
            using var needleInHaystack = new NeedleInHaystack(img.Copy());

            List<Tuple<Point, int>> characters = new();
            for (int i = 0; i < Numbers.Length; i++)
            {
                var positions = needleInHaystack.Find(Numbers[i]);
                if (positions.Count == 0)
                    continue;

                for (int j = 0; j < positions.Count; j++)
                    characters.Add(new(positions[j], i));
            }

            foreach (var c in characters.OrderBy(c => c.Item1.X))
            {
                if (result > 0)
                    result *= 10;

                result += c.Item2;
            }

            return result;
        }

        public int ParseProfit(Image<Gray, byte> img, Rectangle region)
        {
            img.ROI = region;
            using var needleInHaystack = new NeedleInHaystack(img.Copy());

            bool negative = false;

            var arrowPositions = needleInHaystack.Find(PositiveArrow);
            if (arrowPositions.Count == 0)
                negative = true;

            int profit = ParseNumber(img);
            return negative ? -profit : profit;
        }

        public void Dispose()
        {
            // Dispose of all bitmaps
            PositiveArrow.Dispose();
            foreach (var n in Numbers)
                n.Dispose();
        }
    }
}
