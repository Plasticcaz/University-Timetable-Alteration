using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace HonoursCS.Util.Tests
{
    [TestClass()]
    public class ListOpsTests
    {
        [TestMethod()]
        public void CreateCopyTest()
        {
            var a = new List<int>(new[] { 1, 2, 3 });
            var b = ListUtil.CreateCopy(a);

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
                Assert.AreEqual(a[i], b[i]);
        }

        [TestMethod()]
        public void CreateFilteredCopyTest()
        {
            var a = new List<int>(new[] { 1, 2, 3 });
            var b = ListUtil.CreateFilteredCopy(a, (i) => i != 2);
            // b should not contain 2.
            Assert.IsFalse(b.Contains(2));
            // b should however contain 1 and 3.
            Assert.IsTrue(b.Contains(1) && b.Contains(3));
            Assert.AreEqual(b.Count, 2);
        }

        [TestMethod()]
        public void CreateListFromRangeTest()
        {
            const uint START = 0, END = 10;
            List<uint> a = ListUtil.CreateListFromRange(START, END);
            // We should get a list of items of the length
            Assert.AreEqual((uint)a.Count, END - START);
            for (uint i = 0; i < a.Count; i++)
                Assert.AreEqual(START + i, a[(int)i]);
            // TODO(zac): Should we test what happens when END < START?
        }
    }
}