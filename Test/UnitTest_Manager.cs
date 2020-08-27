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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using FakeItEasy;
using GitHubHelper.Libs;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace GitHubHelper.Test {
    [TestClass]
    public class UnitTest_Manager {

        [TestMethod]
        public void Test_GenerateRepoContentUrl() {
            var manager = new Manager("INVALID_TOKEN");
            
            foreach (var item in new [] {
                Tuple.Create(
                    @"https://github.com/stevanusronald/GitHubHelper/tree/master/Source",
                    @"https://api.github.com/repos/stevanusronald/GitHubHelper/contents/Source?ref=master"),
                Tuple.Create(
                    @"https://github.com/stevanusronald/GitHubHelper/blob/master/Source/IManager.cs",
                    @"https://api.github.com/repos/stevanusronald/GitHubHelper/contents/Source/IManager.cs?ref=master")
            }) {
                var actual = manager.GenerateRepoContentUrl(item.Item1);
                Assert.AreEqual(item.Item2, actual);
            }
        }

        [TestMethod]
        public void Test_DownloadFile() {
            var fakeWebRequestHelper = A.Fake<WebRequestHelper>();
            var manager = new Manager(fakeWebRequestHelper, "INVALID_TOKEN");

            var url = @"https://github.com/stevanusronald/GitHubHelper/blob/master/Source/IManager.cs";
            var resourceName = "Sample_RepoContent_IManager.json";

            var repoContentUrl = manager.GenerateRepoContentUrl(url);
            
            HttpStatusCode statusCode;
            A.CallTo(() => fakeWebRequestHelper.GetData<JToken>(null, out statusCode, null, null, 1, "GET", "application/json"))
                .WhenArgumentsMatch(e => {
                    return (e[0].ToString().Equals(repoContentUrl));
                })
                .Returns(JToken.Parse(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Resources", resourceName))))
                .AssignsOutAndRefParameters(HttpStatusCode.OK);

            var targetDirectory = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString());
            try {
                var actual = manager.Download(url, targetDirectory);
                Assert.AreEqual(1, actual);
            }
            finally {
                Directory.Delete(targetDirectory, true);
            }
        }

        [TestMethod]
        public void Test_DownloadDirectory() {
            var fakeWebRequestHelper = A.Fake<WebRequestHelper>();
            var manager = new Manager(fakeWebRequestHelper, "INVALID_TOKEN");

            var url = @"https://github.com/stevanusronald/GitHubHelper/blob/master/Source";
            var resourceName = "Sample_RepoContent_Source.json";

            var repoContentUrl = manager.GenerateRepoContentUrl(url);
            
            HttpStatusCode statusCode;
            A.CallTo(() => fakeWebRequestHelper.GetData<JToken>(null, out statusCode, null, null, 1, "GET", "application/json"))
                .WhenArgumentsMatch(e => {
                    return (e[0].ToString().Equals(repoContentUrl));
                })
                .Returns(JToken.Parse(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Resources", resourceName))))
                .AssignsOutAndRefParameters(HttpStatusCode.OK);

            var totalDownloadFileInvoke = 0;
            A.CallTo(() => fakeWebRequestHelper.DownloadFile(null, null, out statusCode, null, 1, 128))
                .WhenArgumentsMatch(e => {
                    return e[0].ToString().Contains(@"stevanusronald/GitHubHelper/master/Source");
                })
                .Invokes(() => {
                    totalDownloadFileInvoke++;
                });

            var targetDirectory = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString());
            try {
                manager.Download(url, targetDirectory);
            }
            finally {
                Directory.Delete(targetDirectory, true);
            }

            Assert.AreEqual(5, totalDownloadFileInvoke);
        }
    }
}
