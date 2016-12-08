using HonoursCS.Data;

namespace HonoursCS
{
    /// <summary>
    /// The type of constraint.
    /// Order is important, as we cast to integer to check individual constraints in the
    /// Allocation class.
    /// </summary>
    public enum ConstraintType
    {
        RoomConstraint,
        RoomCapacityConstraint,
        TeacherConstraint,
        TimeslotConstraint,
        CurriculumConstraint,
        MinDaysConstraint,
    }

    /// <summary>
    /// A class that contains two constants that specify which constraints are hard, and
    /// which are soft.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// A list of Hard constraints.
        /// </summary>
        public static readonly ConstraintType[] HardConstraints = new[]
        {
            ConstraintType.RoomConstraint,
            ConstraintType.TimeslotConstraint,
            ConstraintType.TeacherConstraint
        };

        /// <summary>
        /// A list of Soft Constraints.
        /// </summary>
        public static readonly ConstraintType[] SoftConstraints = new[]
        {
            ConstraintType.RoomCapacityConstraint,
            ConstraintType.CurriculumConstraint
        };
    }

    /// <summary>
    /// The interface of a constraint.
    /// </summary>
    public interface IConstraint
    {
        /// <summary>
        /// The type of this constraint. We use this so we can identify the constraint if need be.
        /// </summary>
        ConstraintType Type { get; }

        /// <summary>
        /// Whether this constraint has been violated.
        /// </summary>
        bool IsViolated { get; }

        /// <summary>
        /// Checks the constraint and updates the Violated Field.
        /// </summary>
        void Check(Allocation allocation, Candidate candidate);
    }

    /// <summary>
    /// A constraint to check whether the allocation is in a valid room.
    /// </summary>
    public sealed class RoomConstraint : IConstraint
    {
        public ConstraintType Type { get { return ConstraintType.RoomConstraint; } }

        public bool IsViolated { get; private set; }

        public void Check(Allocation allocation, Candidate _)
        {
            if (allocation.Event != null &&
                allocation.Event.ValidRoomIndices != null &&
                !allocation.Event.ValidRoomIndices.Contains(allocation.RoomIndex))
            {
                IsViolated = true;
            }
            else
            {
                IsViolated = false;
            }
        }
    }

    /// <summary>
    ///  A constraint to check whether the allocation is in a valid Timeslot.
    /// </summary>
    public sealed class TimeslotConstraint : IConstraint
    {
        public ConstraintType Type { get { return ConstraintType.TimeslotConstraint; } }
        public bool IsViolated { get; private set; }

        public void Check(Allocation allocation, Candidate candidate)
        {
            if (allocation.Event != null &&
                allocation.Event.BannedTimeslotIndices != null &&
                allocation.Event.BannedTimeslotIndices.Contains(
                    allocation.TimeslotIndex))
            {
                IsViolated = true;
            }
            else
            {
                IsViolated = false;
            }
        }
    }

    /// <summary>
    /// A constraint to check if an allocation satisfies the room capacity limits.
    /// </summary>
    public sealed class RoomCapacityConstraint : IConstraint
    {
        public ConstraintType Type { get { return ConstraintType.RoomCapacityConstraint; } }
        public bool IsViolated { get; private set; }

        public void Check(Allocation allocation, Candidate candidate)
        {
            Event e = allocation.Event;
            Room r = candidate.Instance.Rooms[(int)allocation.RoomIndex];
            if (e != null && r.Capacity < e.NumStudents)
            {
                IsViolated = true;
            }
            else
            {
                IsViolated = false;
            }
        }
    }

    /// <summary>
    /// A constraint to check if the teacher is expected to be in two places
    /// at once.
    /// </summary>
    public sealed class TeacherConstraint : IConstraint
    {
        public ConstraintType Type { get { return ConstraintType.TeacherConstraint; } }
        public bool IsViolated { get; private set; }

        public void Check(Allocation allocation, Candidate candidate)
        {
            IsViolated = false;
            if (allocation.Event != null)
            {
                Timeslot timeslot = candidate.Instance.Timeslot(allocation.TimeslotIndex);
                Teacher teacher = candidate.Teachers[allocation.Event.TeacherID];
                for (int i = 0; i < teacher.Unavailibility.Count; i++)
                {
                    TimeslotEvent unavailability = teacher.Unavailibility[i];
                    if (unavailability.TimeslotIndex.Equals(timeslot) &&
                        !allocation.Event.Equals(unavailability.Event))
                    {
                        IsViolated = true;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// A constraint to check if two or more classes from the same curriculum are scheduled at the
    /// same time.
    /// </summary>
    public sealed class CurriculumConstraint : IConstraint
    {
        public ConstraintType Type { get { return ConstraintType.CurriculumConstraint; } }
        public bool IsViolated { get; private set; }

        public void Check(Allocation allocation, Candidate candidate)
        {
            if (allocation.Event == null)
            {
                IsViolated = false;
            }
            else
            {
                // NOTE(zac): Timeslot remains constant.
                uint timeslotIndex = allocation.TimeslotIndex;
                // Loop through the rooms, and look to see if there is another event of the same curriculum.
                for (uint roomIndex = 0; roomIndex < candidate.Instance.Rooms.Count; roomIndex++)
                {
                    Allocation other = candidate.AllocationAt(timeslotIndex, roomIndex);
                    // If the other event exists, and the other event isn't this allocation,
                    // check to see if the event is in the same curriculum.
                    if (other.Event != null &&
                        other.Event.CurriculumID.Equals(allocation.Event.CurriculumID) &&
                        roomIndex != allocation.RoomIndex)
                    {
                        // We have determined there is a violation.
                        IsViolated = true;
                        // No sense waiting around.
                        return;
                    }
                }
                // If we got here, there is no violation..
                IsViolated = false;
            }
        }
    }
}