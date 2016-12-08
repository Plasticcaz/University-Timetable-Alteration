using System;
using System.Collections.Generic;

namespace HonoursCS.Data
{
    public sealed class Instance
    {
        public string Name { get; set; }

        public Tuple<uint, uint> DailyLectures { get; set; }

        public uint Days { get; set; }

        public uint PeriodsPerDay { get; set; }

        public uint NumTimeslots { get { return Days * PeriodsPerDay; } }

        private List<uint> m_timeslotIndices;

        public List<uint> TimeslotIndices
        {
            get
            {
                if (m_timeslotIndices == null)
                {
                    m_timeslotIndices = new List<uint>((int)NumTimeslots);
                    for (int i = 0; i < NumTimeslots; i++)
                        m_timeslotIndices.Add((uint)i);
                }
                return m_timeslotIndices;
            }
        }

        private List<uint> m_roomIndices;

        public List<uint> RoomIndices
        {
            get
            {
                if (m_roomIndices == null)
                {
                    m_roomIndices = Util.ListUtil.CreateListFromRange(0, (uint)Rooms.Count);
                }
                return m_roomIndices;
            }
        }

        public IList<Room> Rooms { get; private set; }

        public IList<Event> Events { get; private set; }

        public IList<string> CourseIDs { get; private set; }

        public IDictionary<string, Teacher> Teachers { get; private set; }

        public Instance()
        {
            Rooms = new List<Room>();
            Events = new List<Event>();
            Teachers = new Dictionary<string, Teacher>();
            CourseIDs = new List<string>();
        }

        public uint IndexOfRoom(string roomID)
        {
            for (uint i = 0; i < Rooms.Count; i++)
            {
                if (Rooms[(int)i].RoomID.Equals(roomID))
                {
                    return i;
                }
            }
            // We shouldn't get here.
            throw new InvalidOperationException("Asked for the index of a room that doesn't exist.");
        }

        public Timeslot Timeslot(uint timeslotIndex)
        {
            uint day = timeslotIndex / Days;
            uint period = timeslotIndex % PeriodsPerDay;
            return new Timeslot(day, period);
        }

        public uint TimeslotIndex(Timeslot timeslot)
        {
            return timeslot.Day * PeriodsPerDay + timeslot.Period;
        }
    }
}