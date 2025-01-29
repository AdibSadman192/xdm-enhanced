using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using XDM.Core.BrowserMonitoring;

namespace XDM.Tests
{
    [TestClass]
    public class BrowserMonitorTests
    {
        [TestMethod]
        public void TestVideoUrlDetection()
        {
            var url = "https://example.com/video.mp4";
            var isVideo = VideoUrlHelper.IsVideoUrl(url);
            Assert.IsTrue(isVideo, "Should detect video URL correctly");
        }

        [TestMethod]
        public void TestNonVideoUrlDetection()
        {
            var url = "https://example.com/page.html";
            var isVideo = VideoUrlHelper.IsVideoUrl(url);
            Assert.IsFalse(isVideo, "Should not detect non-video URL as video");
        }

        [TestMethod]
        public void TestBrowserDetection()
        {
            var browserInfo = new Browser
            {
                Name = "Chrome",
                Path = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
            };
            Assert.IsNotNull(browserInfo, "Browser info should not be null");
            Assert.AreEqual("Chrome", browserInfo.Name, "Browser name should match");
        }
    }
}
