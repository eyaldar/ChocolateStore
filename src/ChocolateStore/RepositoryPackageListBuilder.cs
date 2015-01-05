using ChocolateStore.ChocolateyFeedService;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChocolateStore
{
    public class RepositoryPackagesListBuilder
    {
        private const string packageUrlFormat = "{0}/{1}/{2}";
        private readonly string source;
        private readonly string packagesPath;
        private readonly FeedContext_x0060_1 feedService;

        public RepositoryPackagesListBuilder(string source)
            : this(source, string.Empty)
        {
        }

        public RepositoryPackagesListBuilder(string source, string packagesPath)
        {
            this.source = source;
            this.packagesPath = packagesPath;
            this.PackagesUris = new List<string>();

            feedService = new FeedContext_x0060_1(new Uri(source))
            {
                IgnoreMissingProperties = true,
                IgnoreResourceNotFoundException = true,
                MergeOption = MergeOption.NoTracking
            };
        }

        public void Build()
        {
            var feedQuery = GetFeedQuery(0);

            int totalPackagesCount = feedQuery.Count();

            int packageIndex = 1;

            while (packageIndex < totalPackagesCount)
            {
                foreach (var item in feedQuery)
                {
                    string packageUrl = string.Format(packageUrlFormat, source, packagesPath, item.Id);
                    PackagesUris.Add(packageUrl);

                    Console.WriteLine(packageUrl);

                    packageIndex++;
                }

                feedQuery = GetFeedQuery(packageIndex);
            }
        }

        public void Update()
        {
            PackagesUris.Clear();
            Build();
        }

        private IQueryable<V2FeedPackage> GetFeedQuery(int startIndex)
        {
            IQueryable<V2FeedPackage> feedQuery = feedService.Packages.Where(package => package.IsPrerelease == false);
            feedQuery = feedQuery.Where(package => package.IsLatestVersion || package.IsAbsoluteLatestVersion);

            return feedQuery.Skip(startIndex);
        }

        public List<string> PackagesUris
        {
            get;
            protected set;
        }

        public string Source
        {
            get
            {
                return source;
            }
        }
    }
}
