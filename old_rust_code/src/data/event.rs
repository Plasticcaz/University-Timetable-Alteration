use data::constraints::*;
use data::timeslot::*;

pub struct Event {
    course_id: String,
    teacher: String,
    students: usize,
    curriculum_id: Option<String>, // TODO(zac): Can a course be part of multiple curriculums?

    // TODO(zac): differentiate between soft and hard constraints somehow.
    constraints: Vec<Box<Constraint>>,
    banned_timeslots: Vec<TimeSlot>,
    valid_rooms: Vec<usize>,
}

impl Event {
    pub fn new(id: String, teacher: String, students: usize) -> Self {
        Event {
            course_id: id,
            teacher: teacher,
            students: students,
            curriculum_id: None,
            constraints: Vec::new(),
            banned_timeslots: Vec::new(),
            valid_rooms: Vec::new(),
        }
    }

    pub fn add_curriculum_id(&mut self, id: String) {
        self.curriculum_id = Some(id);
    }

    pub fn add_constraint(&mut self, constraint: Box<Constraint>) {
        self.constraints.push(constraint);
    }

    pub fn course_id(&self) -> &str {
        &self.course_id
    }

    pub fn teacher(&self) -> &str {
        &self.teacher
    }

    pub fn num_students(&self) -> usize {
        self.students
    }

    pub fn curriculum_id(&self) -> Option<&String> {
        self.curriculum_id.as_ref()
    }

    pub fn constraints(&self) -> &[Box<Constraint>] {
        &self.constraints
    }

    pub fn set_banned_timeslots(&mut self, banned_timeslots: Vec<TimeSlot>) {
        self.banned_timeslots = banned_timeslots;
    }

    pub fn banned_timeslots(&self) -> &[TimeSlot] {
        &self.banned_timeslots
    }

    pub fn add_banned_timeslot(&mut self, timeslot: TimeSlot) {
        self.banned_timeslots.push(timeslot);
    }

    pub fn set_valid_rooms(&mut self, valid_rooms: Vec<usize>) {
        self.valid_rooms = valid_rooms;
    }

    pub fn valid_rooms(&self) -> Option<&[usize]> {
        if self.valid_rooms.len() == 0 {
            None
        } else {
            Some(&self.valid_rooms)
        }
    }
}
