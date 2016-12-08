using HonoursCS.Data;
using HonoursCS.Util;
using System;
using System.Collections.Generic;

namespace HonoursCS
{
    /// <summary>
    /// A class to hold all data for an individual candidate.
    /// </summary>
    public sealed class Candidate
    {
        /// <summary>
        /// The allocation table, the width dimension is timeslots (by index),
        /// and the height dimension is rooms.
        /// </summary>
        private readonly Array2D<Allocation> m_allocationTable;

        /// <summary>
        /// A reference to the problem instance that this
        /// </summary>
        public Instance Instance { get; private set; }

        /// <summary>
        /// The total number of hard constraint violations of this
        /// candidate.
        /// </summary>
        public ulong HardViolations { get; private set; }

        /// <summary>
        /// The total number of soft constraint violations for this
        /// candidate.
        /// </summary>
        public ulong SoftViolations { get; private set; }

        // TODO(zac): Add some candidate constraints. Consider creating a IConstrained interface.
        /// <summary>
        /// The total violations of this candidate, weighted to exagerate hard violations.
        /// </summary>
        public ulong WeightedViolations { get { return 100 * TotalUnallocated + 5 * HardViolations + SoftViolations; } }

        /// <summary>
        /// List of events that remain unallocated.
        /// </summary>
        private readonly List<Event> m_unallocated;

        /// <summary>
        /// The number of unallocated events that are still on queue.
        /// </summary>
        public ulong TotalUnallocated { get { return (ulong)m_unallocated.Count; } }

        /// <summary>
        /// The Teacher's availability for this candidate solution.
        /// </summary>
        public Dictionary<string, Teacher> Teachers { get; private set; }

        /// <summary>
        /// The total number of allocated events.
        /// </summary>
        public uint TotalAllocated { get; private set; }

        /// <summary>
        /// Returns the allocation table.
        /// </summary>
        /// <returns></returns>
        public Array2D<Allocation> Allocations() // TODO(zac): Try and get rid of it.
        {
            return m_allocationTable;
        }

        /// <summary>
        /// Construct an empty candidate solution of the specified instance.
        /// </summary>
        /// <param name="instance"></param>
        public Candidate(Instance instance)
        {
            uint width = instance.NumTimeslots;
            uint height = (uint)instance.Rooms.Count;
            m_allocationTable = new Array2D<Allocation>(width, height);
            for (uint x = 0; x < width; x++)
            {
                for (uint y = 0; y < height; y++)
                {
                    m_allocationTable.SetAt(x, y, new Allocation(null, x, y));
                }
            }
            Instance = instance;
            m_unallocated = ListUtil.CreateCopy(Instance.Events);
            // Copy the teachers dictionary from the instance.
            Teachers = new Dictionary<string, Teacher>(instance.Teachers.Count);
            foreach (var idTeacher in instance.Teachers)
            {
                Teachers.Add(idTeacher.Key, new Teacher(idTeacher.Value));
            }
        }

        /// <summary>
        /// Create a clone a candidate.
        /// </summary>
        /// <param name="other"></param>
        public Candidate(Candidate other)
        {
            m_allocationTable = new Array2D<Allocation>(other.m_allocationTable.Width, other.m_allocationTable.Height);
            for (uint x = 0; x < m_allocationTable.Width; x++)
            {
                for (uint y = 0; y < m_allocationTable.Height; y++)
                {
                    m_allocationTable.SetAt(x, y, new Allocation(other.m_allocationTable.At(x, y)));
                }
            }
            Instance = other.Instance;
            m_unallocated = ListUtil.CreateCopy(other.m_unallocated);
            Teachers = new Dictionary<string, Teacher>(other.Teachers.Count);
            foreach (var idTeacher in other.Teachers)
            {
                Teachers.Add(idTeacher.Key, new Teacher(idTeacher.Value));
            }
            ReEvaluateConstraints();
        }

        /// <summary>
        /// Access the allocation block at the specified indices.
        /// </summary>
        /// <param name="timeslotIndex"></param>
        /// <param name="roomIndex"></param>
        /// <returns></returns>
        public Allocation AllocationAt(uint timeslotIndex, uint roomIndex)
        {
            return m_allocationTable.At(timeslotIndex, roomIndex);
        }

        /// <summary>
        /// Allocate the event at the specified indices.
        /// If an event is already allocated there, it will be placed back on the unallocated.
        /// </summary>
        /// <param name="timeslotIndex"></param>
        /// <param name="roomIndex"></param>
        /// <param name="event"></param>
        public void AllocateEvent(uint timeslotIndex, uint roomIndex, Event @event)
        {
            Allocation allocation = m_allocationTable.At(timeslotIndex, roomIndex);

            // If there is an event in the slot we are allocating to.
            if (allocation.Event != null)
            {
                this.m_unallocated.Add(allocation.Event);
                Teachers[allocation.Event.TeacherID].RemoveTimeslotFromUnavailability(new TimeslotEvent(timeslotIndex, allocation.Event));
            }

            // If the caller wants no event to be at this allocation slot...
            if (@event != null)
            {
                Teachers[@event.TeacherID].AddTimeslotToUnavailability(new TimeslotEvent(timeslotIndex, @event));
                var e = m_unallocated.Find((_e) => _e.Equals(@event));
                if (e != null)
                {
                    m_unallocated.Remove(e);
                }
            }

            // Remove the effect of the old violations on this candidate (if they exist).
            HardViolations -= allocation.HardViolations;
            SoftViolations -= allocation.SoftViolations;

            // Allocate the new event, and update violations for this allocation.
            allocation.SetEvent(@event, this);
            HardViolations += allocation.HardViolations;
            SoftViolations += allocation.SoftViolations;
        }

        /// <summary>
        /// Deallocate the event at the specified timeslotIndex and roomIndex.
        /// Use this instead of passing null to AllocateEvent in order to make intent
        /// clearer.
        /// </summary>
        /// <param name="timeslotIndex"></param>
        /// <param name="roomIndex"></param>
        public void DeallocateAt(uint timeslotIndex, uint roomIndex)
        {
            AllocateEvent(timeslotIndex, roomIndex, null);
        }

        /// <summary>
        /// Re-evaluate the constraints of all allocations in this candidate, in order to make sure
        /// everything is up to date.
        /// </summary>
        public void ReEvaluateConstraints()
        {
            TotalAllocated = 0;
            HardViolations = 0;
            SoftViolations = 0;
            for (uint x = 0; x < m_allocationTable.Width; x++)
            {
                for (uint y = 0; y < m_allocationTable.Height; y++)
                {
                    Allocation allocation = m_allocationTable.At(x, y);
                    if (!allocation.IsEmpty) TotalAllocated += 1;
                    allocation.ReEvaluateConstraints(this);

                    HardViolations += allocation.HardViolations;
                    SoftViolations += allocation.SoftViolations;
                }
            }
#if DEBUG
            if (TotalAllocated + TotalUnallocated != (uint)Instance.Events.Count)
                throw new InvalidOperationException("We have somehow ended up with a different number of events inside a " +
                    "candidate than we do in the instance.");
#endif
        }

        /// <summary>
        /// Counts all events allocated on the specified day.
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        public uint CountEventsOnDay(uint day)
        {
            uint eventCount = 0;
            for (uint period = 0; period < Instance.PeriodsPerDay; period++)
            {
                Timeslot t = new Timeslot(day, period);
                uint timeslotIndex = Instance.TimeslotIndex(t);
                for (uint roomIndex = 0; roomIndex < Instance.Rooms.Count; roomIndex++)
                {
                    if (AllocationAt(timeslotIndex, roomIndex).Event != null)
                    {
                        eventCount += 1;
                    }
                }
            }
            return eventCount;
        }

        /// <summary>
        /// Compares the number of events that have been moved in/from the other candidate,
        /// not counting any previously unallocated events that have been allocated.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public uint CompareDifferencesWith(Candidate other)
        {
#if DEBUG
            if (this.TotalUnallocated != 0 && other.TotalUnallocated != 0)
            {
                throw new InvalidOperationException("This algorithm does not support both " +
                    "candidates having unallocated events.");
            }
#endif
            uint numDifferent = 0;
            for (uint t = 0; t < Instance.TimeslotIndices.Count; t++)
            {
                for (uint r = 0; r < Instance.Rooms.Count; r++)
                {
                    Allocation a = this.AllocationAt(t, r);
                    Allocation b = other.AllocationAt(t, r);
                    if (a.Event == null && b.Event != null ||
                        a.Event != null && b.Event == null ||
                        a.Event != null && b.Event != null &&
                        !a.Event.Equals(b.Event))
                    {
                        numDifferent += 1;
                    }
                }
            }
            return numDifferent - (uint)TotalUnallocated - (uint)other.TotalUnallocated;
        }

        /// <summary>
        /// Count the number of events allocated in the specified room.
        /// </summary>
        /// <param name="roomIndex"></param>
        /// <returns></returns>
        public uint CountEventsInRoom(uint roomIndex)
        {
            uint numEvents = 0;
            for (uint timeslotIndex = 0; timeslotIndex < Instance.NumTimeslots; timeslotIndex++)
            {
                if (AllocationAt(timeslotIndex, roomIndex).Event != null)
                {
                    numEvents += 1;
                }
            }
            return numEvents;
        }

        /// <summary>
        /// Count the number of events allocated on a specific Timeslot in any room.
        /// </summary>
        /// <param name="timeslotIndex"></param>
        /// <returns></returns>
        public uint CountEventsOnTimeslot(uint timeslotIndex)
        {
            uint numEvents = 0;
            for (uint roomIndex = 0; roomIndex < Instance.Rooms.Count; roomIndex++)
            {
                if (AllocationAt(timeslotIndex, roomIndex) != null)
                {
                    numEvents += 1;
                }
            }
            return numEvents;
        }

        /// <summary>
        /// Deallocate all events in the list.
        /// </summary>
        /// <param name="events"></param>
        internal void DeallocateEvents(List<Event> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                Event e = events[i];
                if (e != null)
                    DeallocateEvent(e);
            }
        }

        /// <summary>
        /// Deallocate a specific event.
        /// </summary>
        /// <param name="event"></param>
        internal void DeallocateEvent(Event @event)
        {
            for (uint t = 0; t < Instance.NumTimeslots; t++)
            {
                for (uint r = 0; r < Instance.Rooms.Count; r++)
                {
                    var allocation = this.AllocationAt(t, r);
                    if (!allocation.IsEmpty && allocation.Event.Equals(@event))
                    {
                        DeallocateAt(t, r);
                        return; // No point in sticking around.
                    }
                }
            }
        }

        /// <summary>
        /// Return the unallocated event at the start of the queue.
        /// </summary>
        /// <returns></returns>
        public Event NextUnallocated()
        {
            Event @event = m_unallocated[0];
            m_unallocated.RemoveAt(0);
            return @event;
        }

        /// <summary>
        /// Sort the Unallocated queue to be perfect for
        /// </summary>
        internal void SortUnallocatedForGreedy()
        {
            // Sort the queue, with least constrained events at the end, and highly constrained events at the start.
            m_unallocated.Sort((a, b) => b.ConstraintLevel(Instance).CompareTo(a.ConstraintLevel(Instance)));
        }

        /// <summary>
        /// Get a list of all valid timeslots for the specified event for this candidate.
        /// </summary>
        /// <param name="toAllocate"></param>
        /// <returns></returns>
        public List<uint> GetValidTimeslots(Event toAllocate)
        {
            List<uint> timeslotIndices = null;
            // NOTE(zac): If every timeslot is banned, we can't help it... just let them pick any timeslot.
            if (toAllocate.BannedTimeslotIndices != null)
            {
                // Create a copy, filtered by teacher availability.
                timeslotIndices = ListUtil.CreateFilteredCopy(Instance.TimeslotIndices, (tIndex) =>
                {
                    var teacher = Teachers[toAllocate.TeacherID];
                    return !(toAllocate.BannedTimeslotIndices.Contains(tIndex) || teacher.IsUnavailable(tIndex));
                });
            }

            if (timeslotIndices == null || timeslotIndices.Count == 0)
            {
                timeslotIndices = ListUtil.CreateCopy(Instance.TimeslotIndices);
            }

            return timeslotIndices;
        }

        /// <summary>
        /// Get a list of all valid rooms for the event for this candidate.
        /// </summary>
        /// <param name="toAllocate"></param>
        /// <returns></returns>
        public List<uint> GetValidRooms(Event toAllocate)
        {
            List<uint> roomIndices = null;
            // NOTE(zac): If there are no valid rooms, we can't help it. Just got to pick a room and hope
            // for the best.
            if (toAllocate.ValidRoomIndices != null)
            {
                roomIndices = ListUtil.CreateCopy(toAllocate.ValidRoomIndices);
            }

            if (roomIndices == null || roomIndices.Count == 0)
            {
                roomIndices = ListUtil.CreateCopy(Instance.RoomIndices);
            }
            return roomIndices;
        }

        /// <summary>
        /// If the allocation has an event, ban that event from this allocation.
        /// </summary>
        /// <param name="allocation"></param>
        public void BanTimeslotForCourse(Allocation allocation)
        {
            if (allocation.Event == null) return;

            // Update all events of this course id.
            foreach (var @event in Instance.Events)
            {
                if (@event != null && @event.CourseID == allocation.Event.CourseID)
                {
                    if (@event.BannedTimeslotIndices == null)
                        @event.BannedTimeslotIndices = new List<uint>(1);
                    @event.BannedTimeslotIndices.Add(allocation.TimeslotIndex);
                }
            }
            DeallocateAt(allocation.TimeslotIndex, allocation.RoomIndex);
            ReEvaluateConstraints();
        }

        /// <summary>
        /// Ban a room for all events.
        /// </summary>
        /// <param name="bannedRoomIndex"></param>
        public void BanRoomForAll(uint bannedRoomIndex)
        {
            // Update the Instance variables.
            foreach (var @event in Instance.Events)
            {
                if (@event.ValidRoomIndices != null)
                {
                    for (int i = 0; i < @event.ValidRoomIndices.Count; i++)
                    {
                        if (@event.ValidRoomIndices[i] == bannedRoomIndex)
                        {
                            @event.ValidRoomIndices.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    @event.ValidRoomIndices = ListUtil.CreateFilteredCopy(Instance.RoomIndices,
                        (roomIndex) => roomIndex != bannedRoomIndex);
                }
            }
            // Deallocate all eventss in this room.
            for (uint timeslotIndex = 0; timeslotIndex < Instance.NumTimeslots; timeslotIndex++)
            {
                DeallocateAt(timeslotIndex, bannedRoomIndex);
            }
        }

        /// <summary>
        /// Ban the specifed timeslot for all events.
        /// </summary>
        /// <param name="bannedTimeslotIndex"></param>
        public void BanTimeslotForAll(uint bannedTimeslotIndex)
        {
            // Update the Instance variables.
            foreach (var @event in Instance.Events)
            {
                // Don't want to potentially add this timslot index more than once...
                if (@event.BannedTimeslotIndices != null)
                {
                    if (!@event.BannedTimeslotIndices.Contains(bannedTimeslotIndex))
                    {
                        @event.BannedTimeslotIndices.Add(bannedTimeslotIndex);
                    }
                }
                else
                {
                    @event.BannedTimeslotIndices = new List<uint>(1);
                    @event.BannedTimeslotIndices.Add(bannedTimeslotIndex);
                }
            }
            // Deallocate all eventss in this timeslot.
            for (uint roomIndex = 0; roomIndex < Instance.Rooms.Count; roomIndex++)
            {
                DeallocateAt(bannedTimeslotIndex, roomIndex);
            }
        }

        /// <summary>
        /// Ban the room for any events for that allocation, and put the offending event
        /// into the unallocated queue.
        /// </summary>
        /// <param name="allocation"></param>
        public void BanRoomForCourse(Allocation allocation)
        {
            if (allocation.Event == null) return;
            // Update all events of this course id.
            foreach (var ev in Instance.Events)
            {
                if (ev != null && ev.CourseID == allocation.Event.CourseID)
                {
                    if (ev.ValidRoomIndices == null)
                    {
                        ev.ValidRoomIndices = new List<uint>();
                        for (uint index = 0; index < Instance.Rooms.Count; index++)
                        {
                            if (index != allocation.RoomIndex)
                            {
                                ev.ValidRoomIndices.Add(index);
                            }
                        }
                    }
                    else // if not null
                    {
                        ev.ValidRoomIndices.Remove(allocation.RoomIndex);
                    }
                }
            }
            DeallocateAt(allocation.TimeslotIndex, allocation.RoomIndex);
            ReEvaluateConstraints();
        }

        /// <summary>
        /// Ban all courses on the specified day.
        /// </summary>
        /// <param name="day"></param>
        public void BanDayForAll(uint day)
        {
            if (day < 0 || day >= Instance.Days)
                throw new InvalidOperationException("day to ban is out of range.");
            // Ban all timeslots on that day.
            for (uint period = 0; period < Instance.PeriodsPerDay; period++)
            {
                Timeslot t = new Timeslot(day, period);
                uint tIndex = Instance.TimeslotIndex(t);

                // Ban them in the instance of this candidate.
                foreach (Event @event in Instance.Events)
                {
                    if (@event.BannedTimeslotIndices == null)
                        @event.BannedTimeslotIndices = new List<uint>();
                    // We don't want to ban a timeslot twice... that's wasteful.
                    if (!@event.BannedTimeslotIndices.Contains(tIndex))
                        @event.BannedTimeslotIndices.Add(tIndex);
                }
                // Remove all events on that day in this candidate.
                foreach (uint rIndex in Instance.RoomIndices)
                {
                    if (AllocationAt(tIndex, rIndex) != null)
                        DeallocateAt(tIndex, rIndex);
                }
            }
            ReEvaluateConstraints();
        }
    }
}