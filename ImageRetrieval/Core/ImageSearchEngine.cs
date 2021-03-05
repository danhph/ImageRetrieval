using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using FASTER.core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ImageRetrieval.Core
{
    public class ImageSearchEngine : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IWebHostEnvironment _env;
        private readonly FasterKV<string, List<byte>> _store;

        public ImageSearchEngine(ILogger<ImageSearchEngine> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
            _store = new FasterKV<string, List<byte>>(
                1L << 20,
                new LogSettings
                {
                    LogDevice = Devices.CreateLogDevice(Common.LogDevicePath),
                    ObjectLogDevice = Devices.CreateLogDevice(Common.ObjectLogDevicePath)
                },
                serializerSettings: new SerializerSettings<string, List<byte>>
                {
                    valueSerializer = () => new MessagePackFasterSerializer<List<byte>>()
                },
                checkpointSettings: new CheckpointSettings
                {
                    CheckpointDir = Common.DataFolder
                });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _store.RecoverAsync(cancellationToken: cancellationToken).GetAwaiter().GetResult();
                var cpManager = new LocalCheckpointManager(Common.DataFolder);
                cpManager.PurgeAll();
            }
            catch (FasterException)
            {
                //ignore
            }

            {
                using var indexer = new ImageIndexer(_logger, _env, _store, cancellationToken);
                indexer.Index();
            }
            _store.TakeFullCheckpoint(out _);
            _store.CompleteCheckpointAsync(cancellationToken).GetAwaiter().GetResult();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public List<string> GetAllImages()
        {
            return _env.WebRootFileProvider
                .GetDirectoryContents(Common.ImageFolderName)
                .Select(fi => fi.Name)
                .ToList();
        }

        public List<string> GetMatchImages(string webPath)
        {
            var filePath = _env.WebRootFileProvider.GetFileInfo(webPath).PhysicalPath;
            var img = CvInvoke.Imread(filePath, ImreadModes.Grayscale);
            if (img.Rows == 0)
                return GetAllImages();

            using var orb = new ORBDetector(Common.MaxNumberOfFeatures);
            var descriptor = orb.GetDescriptor(img);

            var dict = new ConcurrentDictionary<string, double>();

            using var session = _store.NewSession(new SimpleFunctions<string, List<byte>>());
            var iterate = session.Iterate();

            var taskList = new List<Task>();
            while (iterate.GetNext(out _))
            {
                var key = iterate.GetKey();
                var val = iterate.GetValue();
                taskList.Add(Task.Run(() => { dict[key] = Cosine.Distance(descriptor, val); }));
            }

            foreach (var task in taskList)
                task.Wait();

            return dict
                .OrderBy(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();
        }
    }
}