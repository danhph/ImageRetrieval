using System;
using System.IO;
using System.Threading.Tasks;
using ImageRetrieval.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImageRetrieval
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var host = CreateHostBuilder(args).Build();

                using (var scope = host.Services.CreateScope())
                {
                    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                    Directory.CreateDirectory(Path.Combine(env.WebRootPath, Common.UploadedFolderName));
                    // Directory.Delete(Common.DataFolder, true);
                    Directory.CreateDirectory(Common.DataFolder);
                }
                host.Run();
            }
            catch (TaskCanceledException)
            {
                // skip
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}