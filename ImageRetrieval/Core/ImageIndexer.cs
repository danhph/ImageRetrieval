using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Emgu.CV;
using Emgu.CV.Features2D;
using FASTER.core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ImageRetrieval.Core
{
    public class ImageIndexer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IWebHostEnvironment _env;
        private readonly FasterKV<string, List<byte>> _store;

        private readonly SHA1 _sha1;

        private long _leftCount;
        private readonly ManualResetEvent _stopEvent;
        private readonly CancellationToken _cancellationToken;

        public ImageIndexer(
            ILogger logger,
            IWebHostEnvironment env,
            FasterKV<string, List<byte>> store,
            CancellationToken cancellationToken)
        {
            _logger = logger;
            _env = env;
            _store = store;

            _sha1 = SHA1.Create();

            _leftCount = 0;
            _stopEvent = new ManualResetEvent(false);
            _cancellationToken = cancellationToken;

            var limit = Math.Max(Environment.ProcessorCount - 1, 1);
            ThreadPool.SetMaxThreads(limit, limit);
        }

        public void Index()
        {
            _leftCount = 0;
            _stopEvent.Reset();

            var sw = new Stopwatch();

            var allImages = _env.WebRootFileProvider
                .GetDirectoryContents(Common.ImageFolderName)
                .Select(fi => fi.Name)
                .ToList();
            var toIndex = new List<string>(allImages);

            _logger.LogInformation($"Index images in: {Common.ImageFolderName}");
            sw.Start();

            using (var session = _store.NewSession(new SimpleFunctions<string, List<byte>>()))
            {
                var toDelete = new List<string>();

                var iterate = session.Iterate();
                while (iterate.GetNext(out _))
                {
                    var key = iterate.GetKey();
                    if (!allImages.Contains(key))
                        toDelete.Add(key);
                    else
                        toIndex.Remove(key);
                }

                foreach (var key in toDelete)
                    session.Delete(key);
            }

            foreach (var fileName in toIndex)
            {
                Interlocked.Increment(ref _leftCount);
                ThreadPool.QueueUserWorkItem(IndexFile, fileName);
            }

            if (!_stopEvent.WaitOne(5000))
            {
                while (true)
                {
                    _logger.LogInformation($"Total: {toIndex.Count} | Left: {_leftCount}");
                    if (_stopEvent.WaitOne(5000))
                        break;
                }
            }

            sw.Stop();
            _logger.LogInformation($"Elapsed time: {sw.Elapsed}");
        }

        private void IndexFile(object param)
        {
            if (!_cancellationToken.IsCancellationRequested)
            {
                var fileName = (string) param;
                var saved = false;
                do
                {
                    var filePath = Path.Combine(_env.WebRootPath, Common.ImageFolderName, fileName);
                    using var session = _store.NewSession(new SimpleFunctions<string, List<byte>>());
                    if (session.Read(fileName, out _) == Status.OK)
                        break;

                    var img = CvInvoke.Imread(filePath);
                    if (img.Rows == 0)
                        break;
                    using var orb = new ORBDetector(Common.MaxNumberOfFeatures);
                    var descriptor = orb.GetDescriptor(img);
                    if (descriptor.Count == 0)
                        break;
                    session.Upsert(fileName, descriptor);
                    saved = true;
                } while (false);

                if (!saved)
                {
                    _logger.LogWarning($"Unable to detect and compute for: {fileName}");
                }
            }

            if (Interlocked.Decrement(ref _leftCount) == 0)
                _stopEvent.Set();
        }

        public void Dispose()
        {
            _sha1?.Dispose();
            _stopEvent?.Dispose();
        }
    }
}