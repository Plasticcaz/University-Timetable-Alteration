use data::*;

pub type TimetableDataResult = Result<Instance, String>;

/// A storage of data loaded from some source. This displays the specific problem, as loaded from
/// some source.
pub struct Instance {
    name: Option<String>,
    daily_lectures: (usize, usize),
    timeslots: Vec<TimeSlot>,
    days: usize,
    periods_per_day: usize,
    rooms: Vec<Room>,
    events: Vec<Event>,
    /// Any constraints that are not specific to individual events.
    constraints: Vec<Box<Constraint>>,
}

impl Instance {
    pub fn new() -> Self {
        let mut constraints = Vec::new();
        constraints.push(RoomCapacityConstraint::new());
        constraints.push(CurriculumConstraint::new());
        constraints.push(TeacherConstraint::new());

        Instance {
            name: None,
            daily_lectures: (0, 24),
            timeslots: Vec::new(),
            days: 0,
            periods_per_day: 0,
            rooms: Vec::new(),
            events: Vec::new(),
            constraints: constraints,
        }
    }

    // mut methods: These methods are only callable if one has a mutable binding to the Instance.

    pub fn set_name(&mut self, name: String) {
        self.name = Some(name);
    }

    pub fn set_timeslots(&mut self, days: usize, periods_per_day: usize) {
        self.days = days;
        self.periods_per_day = periods_per_day;
        for day in 0..days {
            for period in 0..periods_per_day {
                let timeslot = TimeSlot::new(day, period);
                self.timeslots.push(timeslot);
            }
        }
    }

    pub fn set_daily_lectures(&mut self, min_max: (usize, usize)) {
        self.daily_lectures = min_max;
    }

    pub fn events(&self) -> &[Event] {
        &self.events
    }

    pub fn add_event(&mut self, event: Event) {
        self.events.push(event);
    }

    pub fn add_room(&mut self, room: Room) {
        self.rooms.push(room);
    }

    // Accessors:

    /// Retreives the name of this instance, or an empty &str instead.
    pub fn name(&self) -> Option<&str> {
        if let Some(ref name) = self.name {
            Some(name)
        } else {
            None
        }
    }

    pub fn timeslot(&self, timeslot_index: usize) -> Option<&TimeSlot> {
        self.timeslots.get(timeslot_index)
    }

    pub fn timeslots(&self) -> &[TimeSlot] {
        &self.timeslots
    }

    /// Get the timeslot index given a day and a period.
    pub fn to_timeslot_index(&self, day: usize, period: usize) -> usize {
        // NOTE(zac): See BoxedSlice2D's index calculation if this seems
        // foreign to you.
        self.periods_per_day * day + period
    }

    pub fn days(&self) -> usize {
        self.days
    }

    pub fn periods_per_day(&self) -> usize {
        self.periods_per_day
    }

    /// Borrow the event at the specified instance. Returns None if index is out of bounds,
    /// Some(&Event) otherwise.
    pub fn event(&self, event_index: usize) -> Option<&Event> {
        self.events.get(event_index)
    }

    /// Mutably borrow the event at the specified instance. Returns None if index is out of bounds,
    /// Some(&mut Event) otherwise.
    pub fn event_mut(&mut self, event_index: usize) -> Option<&mut Event> {
        self.events.get_mut(event_index)
    }

    /// Borrow the list of instance-wide constraints.
    pub fn constraints(&self) -> &[Box<Constraint>] {
        &self.constraints
    }

    pub fn mut_events_with_course_id(&mut self, course_id: &str) -> Vec<&mut Event> {
        self.events.iter_mut().filter(|ref event| event.course_id() == course_id).collect()
    }

    pub fn room(&self, room_index: usize) -> Option<&Room> {
        self.rooms.get(room_index)
    }

    pub fn rooms(&self) -> &[Room] {
        &self.rooms
    }

    pub fn num_timeslots(&self) -> usize {
        self.timeslots.len()
    }

    pub fn num_rooms(&self) -> usize {
        self.rooms.len()
    }

    /// Retreives the number of courses in the instance.
    pub fn num_events(&self) -> usize {
        self.events.len()
    }
}
