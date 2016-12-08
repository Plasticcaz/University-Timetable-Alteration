use data::*;
use allocation::Allocation;
use boxed_slice2d::BoxedSlice2D;

/// A possible solution to a Timetable Problem instance.
/// Really a wrapper to the set of allocations an algorithm has
/// generated.
#[derive(Clone)]
pub struct CandidateSolution {
    allocation_table: BoxedSlice2D<Option<Allocation>>,
    violations: usize, // TODO(zac): Might want to add hard and soft constraints later.
    num_unallocated_events: usize,
}

impl CandidateSolution {
    /// Creates a CandidateSolution object. Note that nothing has yet been allocated.
    /// Uses the instance in order to setup one allocation struct per course.
    pub fn new(instance: &Instance) -> Self {
        let width = instance.timeslots().len();
        let height = instance.rooms().len();
        let table = BoxedSlice2D::new(width, height);
        CandidateSolution {
            allocation_table: table,
            violations: 0,
            num_unallocated_events: usize::max_value(),
        }
    }

    /// Allocate a specified event (index) to the specified room (index) and timeslot (index),
    /// You can deallocate an event by specifying None as the event_index.
    pub fn allocate_event(&mut self,
                          timeslot_index: usize,
                          room_index: usize,
                          event_index: Option<usize>,
                          instance: &Instance) {
        let mut allocation = if let Some(event_index) = event_index {
            Some(Allocation::new(event_index, timeslot_index, room_index))
        } else {
            None
        };

        // Check for violations:
        if let Some(allocation) = allocation.as_mut() {
            // TODO(zac): We are not checking all constraints yet.
            let event_index = event_index.unwrap();
            let event = instance.event(event_index)
                .expect("Invalid event index provided to CandidateSolution::allocate_event()");

            let mut violations = 0;
            for constraint in event.constraints().iter() {
                violations +=
                    constraint.check_for_violations(self, (timeslot_index, room_index), instance);
            }

            for constraint in instance.constraints().iter() {
                violations +=
                    constraint.check_for_violations(self, (timeslot_index, room_index), instance);
            }

            // Update the violations field of allocation.
            allocation.set_violations(violations);
            // Update candidate violations.
            self.violations += violations;
        }
        // slot borrow scope:
        {
            let slot = &mut self.allocation_table[(timeslot_index, room_index)];
            if let &mut Some(ref slot) = slot {
                // Remove the cost of this slot from the allocation.
                self.violations -= slot.violations();
            }
            *slot = allocation;
        }
        self.check_all_events_allocated(instance);
    }

    #[inline(always)]
    pub fn violations(&self) -> usize {
        self.violations + self.num_unallocated_events()
    }

    #[inline(always)]
    pub fn num_timeslots(&self) -> usize {
        self.allocation_table.width()
    }

    #[inline(always)]
    pub fn num_rooms(&self) -> usize {
        self.allocation_table.height()
    }

    #[inline(always)]
    pub fn num_unallocated_events(&self) -> usize {
        self.num_unallocated_events
    }

    /// Checks and updates the number of unallocated events.
    pub fn check_all_events_allocated(&mut self, instance: &Instance) {
        let mut events_allocated = 0;
        for t_index in 0..instance.num_timeslots() {
            for r_index in 0..instance.num_rooms() {
                let allocation = self.get_allocation(t_index, r_index);
                if allocation.is_some() {
                    events_allocated += 1;
                }
            }
        }
        self.num_unallocated_events = instance.num_events() - events_allocated;
    }

    pub fn get_allocation(&self, timeslot: usize, room: usize) -> Option<&Allocation> {
        self.get_allocation_with_index((timeslot, room))
    }

    pub fn get_allocation_with_index(&self, index: (usize, usize)) -> Option<&Allocation> {
        self.allocation_table[index].as_ref()
    }
}
