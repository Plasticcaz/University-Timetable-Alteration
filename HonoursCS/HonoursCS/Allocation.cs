using HonoursCS.Data;
using System;

namespace HonoursCS
{
    /// <summary>
    /// An allocation slot of a candidate.
    /// Each Allocation Slot manages it's own constraints.
    /// </summary>
    public sealed class Allocation
    {
        /// <summary>
        /// The event that is currently allocated in this allocation slot.
        /// May be null, if no event is allocated.
        /// </summary>
        private Event m_event;

        /// <summary>
        /// The event that is currently allocated in this allocation slot.
        /// May be null, if no event is allocated.
        /// </summary>
        public Event Event
        {
            get { return m_event; }
        }

        /// <summary>
        /// The timeslot index of this allocation.
        /// </summary>
        public uint TimeslotIndex { get; private set; }

        /// <summary>
        /// The room index of this allocation.
        /// </summary>
        public uint RoomIndex { get; set; }

        /// <summary>
        /// An array of constraints held by this allocation.
        /// </summary>
        private IConstraint[] m_constraints;

        /// <summary>
        /// The number of Hard Constraints violated by this allocation slot.
        /// </summary>
        public ulong HardViolations
        {
            get
            {
                ulong violations = 0;
                for (int i = 0; i < Constraints.HardConstraints.Length; i++)
                {
                    var constraintType = Constraints.HardConstraints[i];
                    if (IsViolated(constraintType))
                        violations += 1;
                }
                return violations;
            }
        }

        /// <summary>
        /// The number of soft constraints violated by this allocation slot.
        /// </summary>
        public ulong SoftViolations
        {
            get
            {
                ulong violations = 0;
                for (int i = 0; i < Constraints.SoftConstraints.Length; i++)
                {
                    var constraintType = Constraints.SoftConstraints[i];
                    if (IsViolated(constraintType))
                        violations += 1;
                }
                return violations;
            }
        }

        /// <summary>
        /// Whether this allocation slot does not contain an event.
        /// </summary>
        public bool IsEmpty { get { return m_event == null; } }

        /// <summary>
        /// Constructs an allocation slot with the specified event.
        /// </summary>
        /// <param name="event"></param>
        /// <param name="timeslotIndex"></param>
        /// <param name="roomIndex"></param>
        public Allocation(Event @event, uint timeslotIndex, uint roomIndex)
        {
            m_event = @event;
            TimeslotIndex = timeslotIndex;
            RoomIndex = roomIndex;
            InitializeConstraints();
        }

        /// <summary>
        /// Clones the other allocation.
        /// </summary>
        /// <param name="other"></param>
        public Allocation(Allocation other)
        {
            m_event = other.m_event;
            TimeslotIndex = other.TimeslotIndex;
            RoomIndex = other.RoomIndex;
            InitializeConstraints();
        }

        /// <summary>
        /// Checks to see
        /// </summary>
        /// <param name="classType"></param>
        /// <returns></returns>
        public bool IsViolated(ConstraintType classType)
        {
            // NOTE(zac): This depends on the order of the constraints.
            return m_constraints[(int)classType].IsViolated;
        }

        /// <summary>
        /// Allocates the Event in this allocation slot. Requires a reference to the owning candidate.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="candidate"></param>
        public void SetEvent(Event e, Candidate candidate)
        {
            if (m_event != null)
            {
                // Update the teacher's availability.
                candidate.Teachers[m_event.TeacherID].RemoveTimeslotFromUnavailability(new TimeslotEvent(TimeslotIndex, m_event));
            }

            // set the new event:
            m_event = e;
            // Check constraints for this new allocation.
            ReEvaluateConstraints(candidate);

            if (m_event != null)
            {
                candidate.Teachers[m_event.TeacherID].AddTimeslotToUnavailability(new TimeslotEvent(TimeslotIndex, m_event));
            }
        }

        /// <summary>
        /// Rechecks this allocation for constraint violations.
        /// </summary>
        /// <param name="owner">The candidate that owns this allocation.</param>
        public void ReEvaluateConstraints(Candidate owner)
        {
            for (int i = 0; i < m_constraints.Length; i++)
            {
                m_constraints[i].Check(this, owner);
            }
        }

        /// <summary>
        /// Initialize the constraints for this Allocation.
        /// Called from within the constructors.
        /// </summary>
        private void InitializeConstraints()
        {
            m_constraints = new IConstraint[]
            {
                new RoomConstraint(),
                new RoomCapacityConstraint(),
                new TeacherConstraint(),
                new TimeslotConstraint(),
                new CurriculumConstraint(),
            };
            // Ideally I wouldn't need this, but because I wish to index by constraint type,
            // I need to sort...
            // NOTE(zac): In order to lessen the overhead, please try to keep the above array in the same order as the
            // enum. IT DOES IMPACT PERFORMANCE!
            Array.Sort(m_constraints, (a, b) => a.Type.CompareTo(b.Type));
        }
    }
}