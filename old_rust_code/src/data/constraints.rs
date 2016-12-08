use candidate::CandidateSolution;
use data::Instance;

/// A trait specifying the interface of a constraint.
pub trait Constraint {
    /// Returns the number of violations the allocation has for this constraint.
    fn check_for_violations(&self,
                            candidate: &CandidateSolution,
                            allocation_index: (usize, usize),
                            instance: &Instance)
                            -> usize;
}

/// Allocation must be in one of the specified rooms.
pub struct RoomConstraint;

impl RoomConstraint {
    pub fn new() -> Box<Constraint> {
        Box::new(RoomConstraint)
    }
}

impl Constraint for RoomConstraint {
    fn check_for_violations(&self,
                            candidate: &CandidateSolution,
                            allocation_index: (usize, usize),
                            instance: &Instance)
                            -> usize {
        let allocation = candidate.get_allocation_with_index(allocation_index);
        let mut violations = 0;
        if let Some(allocation) = allocation {
            let event = {
                let event_index = allocation.event_index();
                instance.event(event_index).unwrap()
            };

            let is_valid_room = if let Some(()) = {
                event.valid_rooms()
                    .and_then(|rooms| if rooms.contains(&allocation.room_index()) {
                        Some(())
                    } else {
                        None
                    })
            } {
                true
            } else {
                false
            };

            if !is_valid_room {
                violations = 1;
            }
        }
        violations
    }
}

/// The event must not be in one of the specified timeslots.
pub struct TimeSlotConstraint;

impl TimeSlotConstraint {
    pub fn new() -> Box<Constraint> {
        Box::new(TimeSlotConstraint)
    }
}

impl Constraint for TimeSlotConstraint {
    fn check_for_violations(&self,
                            candidate: &CandidateSolution,
                            allocation_index: (usize, usize),
                            instance: &Instance)
                            -> usize {
        let allocation = candidate.get_allocation_with_index(allocation_index);
        let mut violations = 0;
        if let Some(allocation) = allocation {
            let event = {
                let event_index = allocation.event_index();
                instance.event(event_index).unwrap()
            };

            let timeslot = instance.timeslot(allocation.timeslot_index());
            if event.banned_timeslots().contains(timeslot.unwrap()) {
                violations = 1; // one violation!
            }
        }
        violations
    }
}

/// An event must be allocated in a room that will fit it's students.
pub struct RoomCapacityConstraint;

impl RoomCapacityConstraint {
    pub fn new() -> Box<Constraint> {
        Box::new(RoomCapacityConstraint)
    }
}

impl Constraint for RoomCapacityConstraint {
    fn check_for_violations(&self,
                            candidate: &CandidateSolution,
                            allocation_index: (usize, usize),
                            instance: &Instance)
                            -> usize {
        let allocation = candidate.get_allocation_with_index(allocation_index);
        let mut violations = 0;
        if let Some(allocation) = allocation {
            let room = match instance.room(allocation.room_index()) {
                Some(room) => room,
                None => panic!("Invalid room allocated to an allocation in RoomCapacityConstraint"),
            };

            let event_index = allocation.event_index();

            if let Some(event) = instance.event(event_index) {
                if room.capacity() > event.num_students() {
                    violations = 1;
                }
            } else {
                panic!("Invalid event index found in an allocation. (in RoomCapacityConstraint)");
            }
        }
        violations
    }
}

/// An event must not be allocated at the same time as another unit in its curriculum.
pub struct CurriculumConstraint;

impl CurriculumConstraint {
    pub fn new() -> Box<Constraint> {
        Box::new(CurriculumConstraint)
    }
}

impl Constraint for CurriculumConstraint {
    fn check_for_violations(&self,
                            candidate: &CandidateSolution,
                            allocation_index: (usize, usize),
                            instance: &Instance)
                            -> usize {
        let allocation = candidate.get_allocation_with_index(allocation_index);
        if let Some(allocation) = allocation {
            // Timeslot is fixed.
            let timeslot_index = allocation.timeslot_index();

            let event = {
                let event_index = allocation.event_index();
                instance.event(event_index).expect("Invalid event_index specified.")
            };

            let mut num_matches = 0;
            // Room is changable.
            for room_index in 0..instance.num_rooms() {
                // Get the allocation that we are currently checking against.
                let check_against = candidate.get_allocation(timeslot_index, room_index);
                // Is this allocation not empty?
                if let Some(other_allocation) = check_against {
                    // Get the other allocations' event from the instance.
                    let other_event = {
                        let event_index = other_allocation.event_index();
                        instance.event(event_index).expect("Invalid event_index specified.")
                    };

                    // Check to see if this
                    if other_event.curriculum_id() == event.curriculum_id() {
                        num_matches += 1;
                    }
                }
            }
            // There should only be one match to this curriculum. Anything else is a violation.
            // ie. the number of violations is:
            num_matches - 1
        } else {
            // If this is an empty allocation, there are no violations...
            0
        }
    }
}

/// A Teacher cannot be in more than one place at a time.
pub struct TeacherConstraint;

impl TeacherConstraint {
    pub fn new() -> Box<Constraint> {
        Box::new(TeacherConstraint)
    }
}

impl Constraint for TeacherConstraint {
    fn check_for_violations(&self,
                            candidate: &CandidateSolution,
                            allocation_index: (usize, usize),
                            instance: &Instance)
                            -> usize {
        let allocation = candidate.get_allocation_with_index(allocation_index);
        let mut matches = 0;
        if let Some(allocation) = allocation {
            let timeslot_index = allocation.timeslot_index();
            let event = {
                let event_index = allocation.event_index();
                instance.event(event_index).expect("Invalid event_index provided.")
            };

            for room_index in 0..instance.num_rooms() {
                if let Some(other_allocation) =
                       candidate.get_allocation(timeslot_index, room_index) {
                    let other_event = {
                        let event_index = other_allocation.event_index();
                        instance.event(event_index).expect("Invalid event_index provided.")
                    };

                    if event.teacher() == other_event.teacher() {
                        matches += 1;
                    }
                }
            }
        }

        // We expect to find one match (which is this course).
        // If we find more than that, we have a violation.
        if matches != 1 {
            1
        } else {
            0
        }
    }
}
