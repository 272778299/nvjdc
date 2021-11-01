using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.OpenCVServer
{
    public class OpenCVServer
    {
        public List<Point> GetPoints2(Rect rect)
        {
            var nn = GetRandNum(1, 3);
            var t = Math.Round(nn * 0.1, 2);
            var rate = t * GetRandNum(1, 3);
            var tv = Math.Round(GetRandNum(1, 9) * 0.01, 2);
            List<Point> points = new List<Point>();
            int MaxX = rect.X + rect.Width * 3 + GetRandNum(1, 3);
            int NMaxX = rect.X + rect.Width + GetRandNum(1, 3);
            int a = Convert.ToInt32(MaxX / t);
            if (MaxX % t != 0)
                a++;
            int py = 0;
            for (int i = 0; i < a; i++)
            {
                var y = tv * i * MaxX;
                if ((int)y == 0) continue;
                if (y > MaxX)
                {
                    py = i - 1;
                    break;
                }
                Point point = new Point();
                point.X = (int)y;
                point.Y = 0;
                points.Add(point);
            }
            for (int i = py; i > 0; i--)
            {
                var y = tv * i * MaxX;
                if ((int)y == 0) continue;
                if (y < NMaxX)
                {
                    break;
                }
                Point point = new Point();
                point.X = (int)y;
                point.Y = 0;
                points.Add(point);
            }
            Point point2 = new Point();
            point2.X = NMaxX;
            point2.Y = 0;
            points.Add(point2);
            return points;
        }
        public List<Point> GetPoints(Rect rect)
        {
            var nn = GetRandNum(1, 3);
            var t = Math.Round(nn * 0.1, 2);
            var rate = t * GetRandNum(1, 3);
            var tv = Math.Round(GetRandNum(1, 9) * 0.01, 2);
            List<Point> points = new List<Point>();
            int MaxX = rect.X + rect.Width + GetRandNum(1, 3);
            int a = Convert.ToInt32(MaxX / t);
            if (MaxX % t != 0)
                a++;
            for (int i = 0; i < a; i++)
            {
                var y = tv * i * MaxX;
                if ((int)y == 0) continue;
                if (y > MaxX) break;
                Point point = new Point();
                point.X = (int)y;
                point.Y = 0;
                points.Add(point);
            }

            Point point2 = new Point();
            point2.X = MaxX;
            point2.Y = 0;
            points.Add(point2);
            return points;
        }
        public int GetRandNum(int min, int max)
        {
            Random r = new Random(Guid.NewGuid().GetHashCode());
            return r.Next(min, max);
        }
        public Rect test(System.Drawing.Bitmap source, System.Drawing.Bitmap find)
        {
            Mat converted1 = BitmapConverter.ToMat(source);
            Mat find1 = BitmapConverter.ToMat(find);
            return getOffsetX(converted1, find1, "test");
        }
        public Rect getOffsetX(System.Drawing.Bitmap source, System.Drawing.Bitmap find)
        {
            Mat converted1 = BitmapConverter.ToMat(source);
            Mat find1 = BitmapConverter.ToMat(find);
            return getOffsetX(converted1, find1, "");
        }
        public Rect getOffsetX(Mat source, Mat find, String Name)
        {
            Mat sourceGrey = new Mat(source.Size(), MatType.CV_8UC1);
            Cv2.CvtColor(source, sourceGrey, ColorConversionCodes.RGB2GRAY);
            Mat template = new Mat();
            Cv2.CvtColor(find, template, ColorConversionCodes.BGR2GRAY);
            Size size = new Size(sourceGrey.Cols - template.Cols + 1, sourceGrey.Rows - template.Rows + 1);
            Mat result = new Mat(size, MatType.CV_32FC1);
            Cv2.MatchTemplate(sourceGrey, template, result, TemplateMatchModes.CCorrNormed);
            double minVal = 0;
            double maxVal = 0;
            Point min = new Point();
            Point max = new Point();
            Cv2.MinMaxLoc(result, out minVal, out maxVal, out min, out max, null);
            Rect rect = new Rect(max.X, max.Y, template.Cols, template.Rows);
            Cv2.Rectangle(source, rect, new Scalar(0, 0, 255, 0), 2, 0, 0);
            if (!string.IsNullOrEmpty(Name))
            {
                string FPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", Name + "out.jpeg");
                Cv2.ImWrite(FPath, source);
            }
            
            return rect;
        }
    }
}
