use data::*;
use candidate::*;

/// An event allocation.
#[derive(Clone)]
pub struct Allocation {
    event_index: usize,
    timeslot_index: usize,
    room_index: usize,
    violations: usize,
}

impl Allocation {
    pub fn new(event_index: usize, timeslot_index: usize, room_index: usize) -> Self {
        Allocation {
            event_index: event_index,
            timeslot_index: timeslot_index,
            room_index: room_index,
            violations: 0,
        }
    }

    pub fn set_violations(&mut self, violations: usize) {
        self.violations = violations;
    }

    // accessors

    #[inline(always)]
    pub fn event_index(&self) -> usize {
        self.event_index
    }

    #[inline(always)]
    pub fn violations(&self) -> usize {
        self.violations
    }

    #[inline(always)]
    pub fn timeslot_index(&self) -> usize {
        self.timeslot_index
    }

    #[inline(always)]
    pub fn room_index(&self) -> usize {
        self.room_index
    }
}

pub trait AllocationStrategy {
    /// The 'main function' of the allocation strategy.
    fn allocate(&mut self, instance: &Instance) -> Box<[CandidateSolution]>;
}
