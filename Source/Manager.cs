/*
 * Copyright 2020 Stevanus Ronald Riantono
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace GitHubHelper.Libs {
    public class Manager : IManager {
        private readonly IWebRequestHelper _webRequestHelper;
        private readonly string _tokenAuth;
        private readonly string _apiPrefixUrl;

        public static class API_PREFIX_URL {
            public const string DEFAULT_GITHUB = @"api.{HOST}";

            public const string ENTERPRISE_GITHUB_V3 = @"{HOST}/api/v3";
        }

        public Manager(string tokenAuth, string apiPrefixUrl = API_PREFIX_URL.DEFAULT_GITHUB)
            : this(new WebRequestHelper(), tokenAuth, apiPrefixUrl) {
        }

        public Manager(IWebRequestHelper webRequestHelper, string tokenAuth, string apiPrefixUrl = API_PREFIX_URL.DEFAULT_GITHUB) {
            if (webRequestHelper == null) {
                throw new ArgumentNullException("webRequestHelper");
            }

            if (String.IsNullOrEmpty(tokenAuth)) {
                throw new ArgumentNullException("tokenAuth");
            }

            if (String.IsNullOrEmpty(apiPrefixUrl)) {
                throw new ArgumentNullException("apiPrefixUrl");
            }

            _webRequestHelper = webRequestHelper;
            _tokenAuth = tokenAuth;
            _apiPrefixUrl = apiPrefixUrl;
        }

        private JToken GetRepoContent(string repoContentUrl) {
            return _webRequestHelper.GetData<JToken>(repoContentUrl, out HttpStatusCode statusCode, null,
                x => {
                    x.Headers["Authorization"] = String.Format("Bearer {0}", _tokenAuth);
                });
        }

        private bool ProcessFileRepoContent(JObject jFileRepoContent, string targetDirectory) {
            var type = jFileRepoContent["type"].Value<string>();
            if (String.IsNullOrEmpty(type) || !type.Equals("file", StringComparison.CurrentCultureIgnoreCase)) {
                throw new ArgumentNullException("Invalid Type of Repository Content!");
            }

            if (!Directory.Exists(targetDirectory)) {
                Directory.CreateDirectory(targetDirectory);
            }

            var filename = jFileRepoContent["name"].Value<string>();
            var targetFilename = Path.Combine(targetDirectory, filename);

            var encoding = jFileRepoContent["encoding"].Value<string>();
            switch (encoding) {
                case "base64": {
                    var encodedData = jFileRepoContent["content"].Value<string>();
                    File.WriteAllBytes(targetFilename, Convert.FromBase64String(encodedData));
                    return true;
                }

                default: {
                    var downloadUrl = jFileRepoContent["download_url"].Value<string>();
                    _webRequestHelper.DownloadFile(downloadUrl, targetFilename, out HttpStatusCode statusCode,
                        x => {
                            x.Headers["Authorization"] = String.Format("Bearer {0}", _tokenAuth);
                        });
                    return statusCode == HttpStatusCode.OK;
                }
            }
        }

        private int ProcessDirectoryRepoContent(JArray jDirectoryRepoContent, string targetDirectory) {
            if (!Directory.Exists(targetDirectory)) {
                Directory.CreateDirectory(targetDirectory);
            }

            var totalProcessItem = 0;

            foreach (var jItem in jDirectoryRepoContent) {
                var name = jItem["name"].Value<string>();

                var type = jItem["type"].Value<string>();
                switch (type) {
                    case "file": {
                        var downloadUrl = jItem["download_url"].Value<string>();
                        var targetFilename = Path.Combine(targetDirectory, name);
                        _webRequestHelper.DownloadFile(downloadUrl, targetFilename, out HttpStatusCode statusCode,
                            x => {
                                x.Headers["Authorization"] = String.Format("Bearer {0}", _tokenAuth);
                            });

                        if (statusCode == HttpStatusCode.OK) {
                            totalProcessItem++;
                        }
                        break;
                    }

                    case "dir": {
                        var repoContentUrl = jItem["url"].Value<string>();
                        var jRepoContent = GetRepoContent(repoContentUrl);
                        var targetSubDirectory = Path.Combine(targetDirectory, name);
                        totalProcessItem += ProcessRepoContent(jRepoContent, targetSubDirectory);
                        break;
                    }

                    default:
                        Debug.Fail("Type of Sub-Item from Directory Repo Content is not implemented!");
                        break;
                }
            }

            return totalProcessItem;
        }

        private int ProcessRepoContent(JToken jRepoContent, string targetDirectory) {
            if (jRepoContent is JObject) {
                if (ProcessFileRepoContent((JObject)jRepoContent, targetDirectory)) {
                    return 1;
                }
            }
            else if (jRepoContent is JArray) {
                return ProcessDirectoryRepoContent((JArray)jRepoContent, targetDirectory);
            }
            else if (jRepoContent != null) {
                Debug.Fail("Json Type of Repo Content is not implemented!");
            }

            return 0;
        }

        public virtual string GenerateRepoContentUrl(string repoBlobOrTreeUrl) {
            // Generate the URL? https://developer.github.com/v3/repos/contents/#get-repository-content
            var uri = new Uri(repoBlobOrTreeUrl);

            var urlType = uri.Segments[3];
            if (String.IsNullOrEmpty(urlType) ||
                !(urlType.Equals("blob", StringComparison.InvariantCultureIgnoreCase) || urlType.Equals("tree", StringComparison.InvariantCultureIgnoreCase))) {
                throw new ArgumentException("Url is neither blob or tree.");
            }

            var owner = uri.Segments[1].Trim('\\', '/');
            var repo = uri.Segments[2].Trim('\\', '/');
            var branch = uri.Segments[4].Trim('\\', '/');
            var relativePath = String.Join('/', uri.Segments.Skip(5).Select(x => x.Trim('\\', '/')));

            var apiPrefixUrl = _apiPrefixUrl.Replace("{HOST}", uri.Host);

            return String.Format(@"{0}://{1}/repos/{2}/{3}/contents/{4}?ref={5}",
                uri.Scheme, apiPrefixUrl, owner, repo, relativePath, branch);
        }

        public virtual int Download(string repoBlobOrTreeUrl, string targetDirectory) {
            var repoContentUrl = GenerateRepoContentUrl(repoBlobOrTreeUrl);
            var jRepoContent = GetRepoContent(repoContentUrl);
            return ProcessRepoContent(jRepoContent, targetDirectory);
        }
    }
}
