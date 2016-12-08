using System.Collections.Generic;

namespace HonoursCS.Data
{
    /// <summary>
    /// A class to hold all information to do with an event.
    /// </summary>
    public sealed class Event
    {
        /// <summary>
        /// An internal value to ensure that we can tell the difference between individual
        /// events of the same course.
        /// </summary>
        private readonly int m_internalID;

        /// <summary>
        /// The id of the course this event belongs to.
        /// </summary>
        public string CourseID { get; private set; }

        /// <summary>
        /// The id of the teacher who teaches this event.
        /// </summary>
        public string TeacherID { get; private set; }

        /// <summary>
        /// The number of students attending this event.
        /// </summary>
        public uint NumStudents { get; private set; }

        /// <summary>
        /// TODO(zac):
        /// </summary>
        public uint MinDays { get; private set; }

        /// <summary>
        /// The id of the curriculum to which this event belongs.
        /// </summary>
        public string CurriculumID { get; set; }

        /// <summary>
        /// A list of timeslots that are banned for this event.
        /// If the list is null, any timeslot is valid.
        /// </summary>
        public List<uint> BannedTimeslotIndices { get; set; }

        /// <summary>
        /// A list of valid room indices.
        /// If the list is null, any room is valid.
        /// </summary>
        public List<uint> ValidRoomIndices { get; set; }

        /// <summary>
        /// Constructs an event with the specified parameters.
        /// </summary>
        /// <param name="courseID"></param>
        /// <param name="teacherID"></param>
        /// <param name="numStudents"></param>
        /// <param name="minDays"></param>
        /// <param name="internalId"></param>
        public Event(string courseID, string teacherID, uint numStudents, uint minDays, int internalId)
        {
            m_internalID = internalId;
            CourseID = courseID;
            TeacherID = teacherID;
            NumStudents = numStudents;
            MinDays = minDays;
            CurriculumID = null;
            BannedTimeslotIndices = null;
        }

        /// <summary>
        /// Checks to see if the event objects represent the same event. The objects are not necessarily
        /// the same objects.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Event other)
        {
            return CourseID.Equals(other.CourseID) && m_internalID == other.m_internalID;
        }

        /// <summary>
        /// Returns a string made up of the course id, and the number event of this course.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{CourseID}#{m_internalID}";
        }

        /// <summary>
        /// Calculate and return the constraint level of this event.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public int ConstraintLevel(Instance instance)
        {
            int constraintLevel = 0;
            // The more valid rooms an event has, the less constrained it is.
            // If it is null, rooms do not effect constraint level at all.
            if (ValidRoomIndices != null)
                constraintLevel += instance.Rooms.Count - ValidRoomIndices.Count;
            // The more banned timeslots we have, the more constrained we are.
            // If it is null, timeslots do not effect constraint level at all.
            if (BannedTimeslotIndices != null)
                constraintLevel += BannedTimeslotIndices.Count;
            return constraintLevel;
        }
    }
}