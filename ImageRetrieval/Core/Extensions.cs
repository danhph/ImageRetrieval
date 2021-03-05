using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Util;

namespace ImageRetrieval.Core
{
    public static class Extensions
    {
        private static readonly Random Rng = new();

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = Rng.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static string GetFilenameExtension(this ImageFormat format)
        {
            try
            {
                return ImageCodecInfo.GetImageEncoders()
                    .First(x => x.FormatID == format.Guid)
                    .FilenameExtension
                    .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
                    .First()
                    .Trim('*')
                    .ToLower();
            }
            catch (Exception)
            {
                return "." + format.ToString().ToLower();
            }
        }

        public static double[] AsDoubleArray(this byte[] src)
        {
            var dst = new double[src.Length];
            for (var i = 0; i < src.Length; i++)
                dst[i] = src[i];
            return dst;
        }

        public static List<byte> GetDescriptor(this ORBDetector detector, Mat img)
        {
            var keyPoints = new VectorOfKeyPoint();
            var descriptors = new Mat();
            detector.DetectAndCompute(img, null, keyPoints, descriptors, false);
            if (keyPoints.Size == 0)
                return new List<byte>();
            return new List<byte>(descriptors.GetRawData());
        }


        public static List<byte> GetDescriptor(this Brisk detector, Mat img)
        {
            var keyPoints = new VectorOfKeyPoint();
            detector.DetectAndCompute(img, null, keyPoints, new Mat(), false);
            if (keyPoints.Size == 0)
                return new List<byte>();

            keyPoints = new VectorOfKeyPoint(keyPoints
                .ToArray()
                .OrderByDescending(kp => kp.Response)
                .Take(Common.MaxNumberOfFeatures)
                .ToArray());

            var descriptors = new Mat();
            detector.Compute(img, keyPoints, descriptors);

            return new List<byte>(descriptors.GetRawData());
        }
    }
}