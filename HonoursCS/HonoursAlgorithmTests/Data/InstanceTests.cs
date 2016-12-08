using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HonoursCS.Data.Tests
{
    [TestClass()]
    public class InstanceTests
    {
        [TestMethod()]
        public void TimeslotTest()
        {
            Instance instance = new Instance();
            instance.Days = 5;
            instance.PeriodsPerDay = 5;

            // We shuld be able to to get a timeslot back from it's
            // index.
            Timeslot timeslot = new Timeslot(1, 2);
            uint index = instance.TimeslotIndex(timeslot);
            Assert.AreEqual(instance.Timeslot(index), timeslot);

            // Mirrored timeslots should not give the same index.
            Timeslot otherTimeslot = new Timeslot(2, 1);
            uint otherIndex = instance.TimeslotIndex(otherTimeslot);
            Assert.AreNotEqual(index, otherIndex);
        }
    }
}