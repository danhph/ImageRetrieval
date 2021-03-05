using System.IO;
using System.Reflection;

namespace ImageRetrieval.Core
{
    public static class Common
    {
        public static readonly string CurrentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string DataFolder = Path.Combine(CurrentFolder, "Data");
        public static readonly string LogDevicePath = Path.Combine(DataFolder, "LogDevice.log");
        public static readonly string ObjectLogDevicePath = Path.Combine(DataFolder, "ObjectLogDevice.log");

        public const string ImageFolderName = "oxbuild_images";
        public const string UploadedFolderName = "uploaded_images";
        public const int MaxNumberOfFeatures = 1000;
    }
}