using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace HonoursCS.Util.Tests
{
    [TestClass()]
    public class RandomUtilTests
    {
        [TestMethod()]
        public void ChooseTest()
        {
            Random random = new Random();
            List<int> a = new List<int>(new[] { 0, 1, 3, 5 });
            int initialCount = a.Count;
            int i = RandomUtil.Choose(a, random);
            Assert.IsTrue(a.Contains(i));
            Assert.AreEqual(a.Count, initialCount);
        }

        [TestMethod()]
        public void ChooseRemoveTest()
        {
            Random random = new Random();
            List<int> a = new List<int>(new[] { 0, 1, 3, 5 });
            int initialCount = a.Count;
            int i = RandomUtil.ChooseRemove(a, random);
            Assert.AreEqual(a.Count, initialCount - 1);
        }
    }
}