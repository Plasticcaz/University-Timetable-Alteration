using System.Collections.Generic;

namespace HonoursCS.Data
{
    /// <summary>
    /// A struct to hold Timeslot-Event tuples for the Teacher's unavailability
    /// list.
    /// This struct is not intended to "own" these items, merely to hold some references
    /// to them.
    /// </summary>
    public struct TimeslotEvent
    {
        /// <summary>
        /// The index of the timeslot that the teacher has unavailable.
        /// </summary>
        public uint TimeslotIndex { get; set; }

        /// <summary>
        /// The event that caused the unavailablity.
        /// Could be null, if it is not caused by an event.
        /// </summary>
        public Event Event { get; set; }

        /// <summary>
        /// Constructs a timeslot and index.
        /// </summary>
        /// <param name="timeslotIndex"></param>
        /// <param name="eventIndex"></param>
        public TimeslotEvent(uint timeslotIndex, Event eventIndex)
        {
            TimeslotIndex = timeslotIndex;
            Event = eventIndex;
        }
    }

    /// <summary>
    /// A class to hold all information to do with a teacher.
    /// </summary>
    public class Teacher
    {
        /// <summary>
        /// The Id of the teacher.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// List of timeslots that are unavailable.
        /// </summary>
        public List<TimeslotEvent> Unavailibility { get; private set; }

        /// <summary>
        /// Creates a teacher with the specified id.
        /// </summary>
        /// <param name="id"></param>
        public Teacher(string id)
        {
            Id = id;
            Unavailibility = new List<TimeslotEvent>();
        }

        /// <summary>
        /// Clone the other teacher.
        /// </summary>
        /// <param name="other"></param>
        public Teacher(Teacher other)
        {
            Id = other.Id;
            Unavailibility = new List<TimeslotEvent>(other.Unavailibility.Count);
            foreach (var te in other.Unavailibility)
            {
                this.Unavailibility.Add(te);
            }
        }

        /// <summary>
        /// Add a timeslot to the list of unavailable timeslots for
        /// the teacher.
        /// </summary>
        /// <param name="te"></param>
        public void AddTimeslotToUnavailability(TimeslotEvent te)
        {
            Unavailibility.Add(te);
        }

        /// <summary>
        /// Remove a timeslot from the list of unavailable timeslots for the
        /// teacher.
        /// </summary>
        /// <param name="te"></param>
        public void RemoveTimeslotFromUnavailability(TimeslotEvent te)
        {
            Unavailibility.Remove(te);
        }

        /// <summary>
        /// Checks to see if this timeslot is unavailable.
        /// </summary>
        /// <param name="timeslotIndex"></param>
        /// <returns></returns>
        public bool IsUnavailable(uint timeslotIndex)
        {
            foreach (var unavailability in Unavailibility)
            {
                if (unavailability.TimeslotIndex.Equals(timeslotIndex))
                {
                    return true;
                }
            }
            return false;
        }
    }
}