using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using Point = System.Drawing.Point;

namespace MabiCommerce.Screen
{
    internal class NeedleInHaystack : IDisposable
    {
        private readonly Image<Gray, byte> Haystack;
        private readonly byte[][] HaystackArray;

        public static byte[][] ImageToArray(Bitmap bitmap)
        {
            var result = new byte[bitmap.Height][];
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                bitmap.PixelFormat);

            for (int y = 0; y < bitmap.Height; ++y)
            {
                result[y] = new byte[bitmap.Width];
                Marshal.Copy(bitmapData.Scan0 + y * bitmapData.Stride, result[y], 0, result[y].Length);
            }

            bitmap.UnlockBits(bitmapData);

            return result;
        }

        public static byte[][] ImageToArray(Image<Gray, byte> image)
        {
            var result = new byte[image.Height][];
            for (int y = 0; y < image.Height; ++y)
            {
                result[y] = new byte[image.Width];
                for (int x = 0; x < image.Width; ++x)
                {
                    result[y][x] = image.Data[y, x, 0];
                    if (result[y][x] != 255 && result[y][x] != 0)
                        throw new Exception("Invalid pixel value");
                }
            }

            return result;
        }

        public static bool ContainSameElements(byte[] first, int firstStart, byte[] second, int secondStart, int length)
        {
            for (int i = 0; i < length; ++i)
            {
                if (first[i + firstStart] != second[i + secondStart])
                    return false;
            }

            return true;
        }

        public NeedleInHaystack(Image<Gray, byte> haystack)
        {
            Haystack = haystack;
            HaystackArray = ImageToArray(Haystack);
        }

        public List<Point> Find(Bitmap needle)
        {
            List<Point> positions = new();
            if (needle == null)
                throw new ArgumentNullException(nameof(needle));

            if (Haystack.Width < needle.Width || Haystack.Height < needle.Height)
                throw new ArgumentException("Needle is bigger than haystack");

            var needleArray = ImageToArray(needle);

            int scanHeight = Haystack.Height - needle.Height + 1;
            int scanWidth = Haystack.Width - needle.Width;

            for (int y = 0; y < scanHeight; y++)
                for (int x = 0; x < scanWidth; x++)
                    if (ContainSameElements(HaystackArray[y], x, needleArray[0], 0, needle.Width))
                        if (IsNeedlePresentAtLocation(needleArray, new Point(x, y), y))
                            positions.Add(new Point(x, y));

            return positions;
        }

        private IEnumerable<Point> FindMatch(IEnumerable<byte[]> haystackLines, byte[] needleLine)
        {
            var y = 0;
            foreach (var haystackLine in haystackLines)
            {
                int scanWidth = haystackLine.Length - needleLine.Length;
                for (int x = 0; x < scanWidth; ++x)
                    if (ContainSameElements(haystackLine, x, needleLine, 0, needleLine.Length))
                        yield return new Point(x, y);

                y += 1;
            }
        }

        private bool IsNeedlePresentAtLocation(byte[][] needle, Point point, int alreadyVerified)
        {
            //we already know that "alreadyVerified" lines already match, so skip them
            for (int y = alreadyVerified; y < needle.Length; ++y)
                if (!ContainSameElements(HaystackArray[y + point.Y], point.X, needle[y], 0, needle[y].Length))
                    return false;

            return true;
        }

        public void Dispose()
        {
            Haystack.Dispose();
            Array.Clear(HaystackArray, 0, HaystackArray.Length);
        }
    }
}
