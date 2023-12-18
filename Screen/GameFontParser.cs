using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using MabiCommerce.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MabiCommerce.Screen
{
    internal class GameFontParser : IScreenParser
    {
        private Tesseract NumberOcr;

        public GameFontParser()
        {
			NumberOcr = new Tesseract("tessdata/", "eng", OcrEngineMode.TesseractLstmCombined, "1234567890") {
                PageSegMode = PageSegMode.SingleLine,
            };
        }

        public int ParseNumber(Image<Gray, byte> img)
        {
            throw new NotImplementedException();
        }

        public int ParseProfit(Image<Gray, byte> img, Rectangle region)
        {
			using Mat result = new();
			img.ROI = region;
			CvInvoke.Threshold(img, result, 150, 255, ThresholdType.Binary);

			NumberOcr.SetImage(result);
			if (NumberOcr.Recognize() != 0)
                throw new Exception("Failed to recognize image");

			var text = NumberOcr.GetUTF8Text();

#if DEBUG
			using var outputBmp = result.ToBitmap();
            OCROutput output = new(outputBmp, text); // lines.Aggregate((current, next) => current + "\n" + next));
            output.Show();
            output.Activate();
#endif

			return int.Parse(text);
		}
    }
}
