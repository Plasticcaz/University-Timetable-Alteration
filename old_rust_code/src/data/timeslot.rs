#[derive(PartialEq, Clone, Copy)]
pub struct TimeSlot {
    pub day: usize,
    pub period: usize,
}

impl TimeSlot {
    pub fn new(day: usize, period: usize) -> Self {
        TimeSlot {
            day: day,
            period: period,
        }
    }
}
