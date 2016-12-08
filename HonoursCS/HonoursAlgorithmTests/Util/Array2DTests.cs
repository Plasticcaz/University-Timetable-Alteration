using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HonoursCS.Util.Tests
{
    [TestClass()]
    public class Array2DTests
    {
        [TestMethod()]
        public void Array2DTest()
        {
            const uint WIDTH = 2, HEIGHT = 2;
            Array2D<int> a = new Array2D<int>(WIDTH, HEIGHT);
            Assert.AreEqual((uint)a.GetInternalData().Length, WIDTH * HEIGHT);
            Assert.AreEqual(a.Width, WIDTH);
            Assert.AreEqual(a.Height, HEIGHT);
        }

        [TestMethod()]
        public void AtTest()
        {
            Array2D<char> a = new Array2D<char>(10, 10);
            char value = 'z';
            a.SetAt(3, 4, value);
            Assert.AreEqual(value, a.At(3, 4));
            // We should not change any other data.
            Assert.AreEqual(0, a.At(1, 1));
            // TODO(zac): Should we test if we ask for width and height? I turn off these checks in release build.
        }
    }
}