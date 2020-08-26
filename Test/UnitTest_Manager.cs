using Microsoft.VisualStudio.TestTools.UnitTesting;
using GitHubHelper.Libs;
using System;

namespace GitHubHelper.Test {
    [TestClass]
    public class UnitTest_Manager {

        [TestMethod]
        public void Test_GenerateRepoContentUrl() {
            var manager = new Manager("INVALID_TOKEN");
            
            foreach (var item in new [] {
                Tuple.Create(
                    "https://github.com/stevanusronald/GitHubHelper/tree/master/Source",
                    "https://github.com/api/v3/repos/stevanusronald/GitHubHelper/contents/Source?ref=master"),
                Tuple.Create(
                    "https://github.com/stevanusronald/GitHubHelper/blob/master/Source/IManager.cs",
                    "https://github.com/api/v3/repos/stevanusronald/GitHubHelper/contents/Source/IManager.cs?ref=master")
            }) {
                var actual = manager.GenerateRepoContentUrl(item.Item1);
                Assert.AreEqual(item.Item2, actual);
            }
        }
    }
}
