using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Ionic.Zip;
using System.Xml.Linq;
using System.Dynamic;
using System.Collections.Generic;

namespace ChocolateStore
{
    class PackageCacher
    {

        private const string INSTALL_FILE = "tools/chocolateyInstall.ps1";

        public delegate void FileHandler(string fileName);
        public delegate void DownloadFailedHandler(string url, Exception ex);

        public event FileHandler SkippingFile = delegate { };
        public event FileHandler DownloadingFile = delegate { };
        public event DownloadFailedHandler DownloadFailed = delegate { };

        public void CachePackage(string dir, string url)
        {
            var packagePath = DownloadFile(url, dir);

            using (var zip = ZipFile.Read(packagePath))
            {
                var packageName = Path.GetFileNameWithoutExtension(packagePath);

                var entry = zip.FirstOrDefault(x => string.Equals(x.FileName, INSTALL_FILE, StringComparison.OrdinalIgnoreCase));

                if (entry != null)
                {
                    string content = GetZipEntryContent(entry);

                    string newContent = CacheUrlFiles(Path.Combine(dir, packageName), content);
                    newContent.Remove(0, newContent.IndexOf(content.First()));
                    zip.UpdateEntry(INSTALL_FILE, newContent);
                    zip.Save();

                }

                GetDependencies(dir, url, zip);
            }
        }

        private void GetDependencies(string dir, string url, ZipFile zip)
        {
            var nuspecFile = zip.FirstOrDefault(x => x.FileName.EndsWith("nuspec", StringComparison.OrdinalIgnoreCase));
            List<string> dependenciesList = GetDependenciesListFromNuspec(nuspecFile);

            var packageNameWithoutVersion = Path.GetFileNameWithoutExtension(nuspecFile.FileName);
            var urlFormat = url.Remove(url.IndexOf(packageNameWithoutVersion)) + "{0}";

            GetDependencies(dependenciesList, dir, urlFormat);
        }

        private void GetDependencies(List<string> dependenciesList, string dir, string url)
        {
            foreach (var dependecy in dependenciesList)
            {
                CachePackage(dir, string.Format(url, dependecy));
            }
        }

        private static string GetZipEntryContent(ZipEntry entry)
        {
            string content = null;
            using (MemoryStream ms = new MemoryStream())
            {
                entry.Extract(ms);
                content = Encoding.Default.GetString(ms.ToArray());
            }
            return content;
        }

        private static List<string> GetDependenciesListFromNuspec(ZipEntry nuspecFileEntry)
        {
            ExpandoObject root = new ExpandoObject();
            List<string> dependenciesList = new List<string>();
            string content = GetZipEntryContent(nuspecFileEntry);
            var xDoc = XDocument.Load(new StringReader(content));

            XmlToDynamic.Parse(root, xDoc.Elements().First(), "dependencies");

            if (root.Any())
            {
                dynamic result = root;
                var dependencies = result.dependencies as List<dynamic>;
                foreach (var dependency in dependencies)
                {
                    var realDependency = dependency as IDictionary<String, object>;
                    dependenciesList.Add(realDependency["id"] as string);
                }
            }

            return dependenciesList;
        }

        private string CacheUrlFiles(string folder, string content)
        {

            const string pattern = "(?<=['\"])http\\S*(?=['\"])";

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return Regex.Replace(content, pattern, new MatchEvaluator(m => DownloadFile(m.Value, folder)));

        }

        private string DownloadFile(string url, string destination)
        {
            try
            {
                var request = WebRequest.Create(url);
                var response = request.GetResponse();
                var fileName = Path.GetFileName(response.ResponseUri.LocalPath);
                var filePath = Path.Combine(destination, fileName);

                if (File.Exists(filePath))
                {
                    SkippingFile(fileName);
                }
                else
                {
                    DownloadingFile(fileName);
                    using (var fs = File.Create(filePath))
                    {
                        response.GetResponseStream().CopyTo(fs);
                    }
                }

                return filePath;
            }
            catch (Exception ex)
            {
                DownloadFailed(url, ex);
                return url;
            }

        }

    }
}