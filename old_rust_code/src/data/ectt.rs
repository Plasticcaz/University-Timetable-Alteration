use std;

use data::{Instance, Event, TimetableDataResult, Room, TimeSlot, RoomConstraint,
           TimeSlotConstraint};

pub struct Loader {
    instance: Instance,

    lines: std::io::Lines<std::io::BufReader<std::fs::File>>,

    num_courses: usize,
    num_rooms: usize,
    num_curricula: usize,
    num_unavailable_constraints: usize,
    num_room_constraints: usize,
}

impl Loader {
    pub fn new(path: &str) -> Self {
        Loader {
            instance: Instance::new(),
            lines: get_lines_from_file(path),
            num_courses: 0,
            num_rooms: 0,
            num_curricula: 0,
            num_unavailable_constraints: 0,
            num_room_constraints: 0,
        }
    }

    pub fn load(mut self) -> TimetableDataResult {
        self.read_header();
        self.read_courses();
        self.read_rooms();
        self.read_curricula();
        self.read_timeslot_constraints();
        self.read_room_constraints();

        Ok(self.instance)
    }

    /// Read the header of the ectt file.
    fn read_header(&mut self) {
        self.read_name();
        self.read_num_courses();
        self.read_num_rooms();
        self.read_timeslots();
        self.read_num_curricula();
        self.read_min_max_daily_lectures();
        self.read_num_unavailable_constraints();
        self.read_num_room_constraints();
    }

    fn read_name(&mut self) {
        let next_line = self.next_line();
        let name = next_line.split_whitespace().nth(1).unwrap();
        self.instance.set_name(name.to_owned());
    }

    fn read_num_courses(&mut self) {
        let next_line = self.next_line();
        let num_courses: usize = next_line.split_whitespace().nth(1).unwrap().parse().unwrap();
        self.num_courses = num_courses;
    }

    fn read_num_rooms(&mut self) {
        let next_line = self.next_line();
        let num_rooms: usize = next_line.split_whitespace().nth(1).unwrap().parse().unwrap();
        self.num_rooms = num_rooms;
    }

    fn read_timeslots(&mut self) {
        let days = self.read_num_days();
        let periods = self.read_periods_per_day();
        self.instance.set_timeslots(days, periods);
    }

    fn read_num_days(&mut self) -> usize {
        let next_line = self.next_line();
        next_line.split_whitespace().nth(1).unwrap().parse().unwrap()
    }

    fn read_periods_per_day(&mut self) -> usize {
        let next_line = self.next_line();
        next_line.split_whitespace().nth(1).unwrap().parse().unwrap()
    }

    fn read_num_curricula(&mut self) {
        let next_line = self.next_line();
        let num_curricula: usize = next_line.split_whitespace().nth(1).unwrap().parse().unwrap();
        self.num_curricula = num_curricula;
    }

    fn read_min_max_daily_lectures(&mut self) {
        let next_line = self.next_line();
        let mut split = next_line.split_whitespace();
        let min: usize = split.nth(1).unwrap().parse().unwrap();
        let max: usize = split.next().unwrap().parse().unwrap();
        let min_max = (min, max);
        self.instance.set_daily_lectures(min_max);
    }

    fn read_num_unavailable_constraints(&mut self) {
        let next_line = self.next_line();
        let num_constraints: usize = next_line.split_whitespace().nth(1).unwrap().parse().unwrap();
        self.num_unavailable_constraints = num_constraints;
    }

    fn read_num_room_constraints(&mut self) {
        let next_line = self.next_line();
        let num_constraints: usize = next_line.split_whitespace().nth(1).unwrap().parse().unwrap();
        self.num_room_constraints = num_constraints;
    }

    fn read_courses(&mut self) {
        // Skip whitepspace
        let _ = self.next_line();
        // Skip "COURSES:"
        let _ = self.next_line();

        for _ in 0..self.num_courses {
            self.read_course();
        }
    }

    fn read_course(&mut self) {
        let line = self.next_line();
        let mut split = line.split_whitespace();

        let course_id = split.next().unwrap().to_owned();
        let teacher_id = split.next().unwrap().to_owned();
        let num_lectures: usize = split.next().unwrap().parse().unwrap();
        // TODO(zac): Figure out how to use this feature we extracted from the data.
        let min_days: usize = split.next().unwrap().parse().unwrap();
        let num_students: usize = split.next().unwrap().parse().unwrap();
        let double_lectures: usize = split.next().unwrap().parse().unwrap();

        let double_lectures = match double_lectures {
            0 => false,
            1 => true,
            _ => panic!("Encountered {} when expected 0 or 1", double_lectures),
        };

        // TODO(zac): Figure out how to use this feature we extracted from the data.
        let num_timeslots = if double_lectures {
            2
        } else {
            1
        };

        // Schedule an event for the number of lectures for this course.
        for _ in 0..num_lectures {
            let event = Event::new(course_id.clone(), teacher_id.clone(), num_students);
            self.instance.add_event(event);
        }
    }

    fn read_rooms(&mut self) {
        // skip whitespace
        let _ = self.next_line();
        // skip "ROOMS:"
        let _ = self.next_line();

        for _ in 0..self.num_rooms {
            self.read_room();
        }
    }

    fn read_room(&mut self) {
        let line = self.next_line();
        let mut split = line.split_whitespace();

        let room_id = split.next().unwrap().to_owned();
        let capacity: usize = split.next().unwrap().parse().unwrap();
        let building_id = split.next().unwrap().to_owned();

        let room = Room::new(room_id, capacity, building_id);
        self.instance.add_room(room);
    }

    fn read_curricula(&mut self) {
        // skip whitespace
        let _ = self.next_line();
        // skip "CURRICULA:"
        let _ = self.next_line();

        for _ in 0..self.num_curricula {
            self.read_curriculum();
        }
    }

    fn read_curriculum(&mut self) {
        let line = self.next_line();
        let mut split = line.split_whitespace();

        let curriculum_id = split.next().unwrap().to_owned();
        let num_courses: usize = split.next().unwrap().parse().unwrap();
        for _ in 0..num_courses {
            let course_id = split.next().unwrap();
            let events = self.instance.mut_events_with_course_id(course_id);
            for mut event in events {
                event.add_curriculum_id(curriculum_id.clone());
            }
        }

    }

    fn read_timeslot_constraints(&mut self) {
        // skip whitespace
        let _ = self.next_line();
        // skip "UNAVAILABILITY_CONSTRAINTS:"
        let _ = self.next_line();

        let mut course_timeslot_tuples = Vec::with_capacity(self.num_unavailable_constraints);
        for _ in 0..self.num_unavailable_constraints {
            course_timeslot_tuples.push(self.read_timeslot_constraint());
        }
        // Make sure course tuples are all grouped together.
        course_timeslot_tuples.sort_by(|a, b| a.0.cmp(&b.0));
        while course_timeslot_tuples.len() != 0 {
            let current_id = course_timeslot_tuples[0].0.clone();

            let mut banned_timeslots = Vec::new();
            // While there are tuples of courses and timeslots
            // with the current id...
            while course_timeslot_tuples.get(0)
                .into_iter()
                .filter(|tuple| tuple.0 == current_id)
                .next()
                .is_some() {
                // Remove the tuple from this list, and
                let tuple = course_timeslot_tuples.remove(0);
                // Put it into the banned timeslots list for the current it.
                banned_timeslots.push(tuple.1);
            }

            for mut event in self.instance.mut_events_with_course_id(&current_id).iter_mut() {
                event.add_constraint(TimeSlotConstraint::new());
                event.set_banned_timeslots(banned_timeslots.clone());
            }
        }


    }

    fn read_timeslot_constraint(&mut self) -> (String, TimeSlot) {
        let line = self.next_line();
        let mut split = line.split_whitespace();

        let course_id = split.next().unwrap();
        let day: usize = split.next().unwrap().parse().unwrap();
        let period: usize = split.next().unwrap().parse().unwrap();

        let timeslot = TimeSlot::new(day, period);
        (course_id.to_owned(), timeslot)
    }

    fn read_room_constraints(&mut self) {
        // skip whitespace
        let _ = self.next_line();
        // skip "ROOM_CONSTRAINTS:"
        let _ = self.next_line();

        let mut course_room_tuples = Vec::with_capacity(self.num_room_constraints);
        for _ in 0..self.num_room_constraints {
            course_room_tuples.push(self.read_room_constraint());
        }
        // Make sure the course tuples are all grouped together.
        course_room_tuples.sort_by(|a, b| a.0.cmp(&b.0));
        while course_room_tuples.len() != 0 {
            let current_id = course_room_tuples[0].0.clone();

            let mut valid_rooms = Vec::new();
            // While there are tuples of courses and rooms
            // with the current id...
            while course_room_tuples.get(0)
                .into_iter()
                .filter(|tuple| tuple.0 == current_id)
                .next()
                .is_some() {
                // Remove the tuple from this list, and
                let tuple = course_room_tuples.remove(0);
                let room_index =
                    self.instance.rooms().iter().position(|ref room| room.id() == tuple.1).unwrap();
                // Put it into the valid_rooms list for the current it.
                valid_rooms.push(room_index);
            }

            for mut event in self.instance.mut_events_with_course_id(&current_id).iter_mut() {
                event.add_constraint(RoomConstraint::new());
                event.set_valid_rooms(valid_rooms.clone());
            }
        }
    }

    /// Reads a tuple that is (CourseID, RoomID)
    fn read_room_constraint(&mut self) -> (String, String) {
        let line = self.next_line();
        let mut split = line.split_whitespace();

        let course_id = split.next().unwrap().to_owned();
        let room_id: String = split.next().unwrap().to_owned();
        (course_id, room_id)
    }

    /// Get the next line from the lines interator.
    fn next_line(&mut self) -> String {
        self.lines.next().unwrap().unwrap()
    }
}

// TODO(zac): put in a file io utils folder.
fn get_lines_from_file(path: &str) -> std::io::Lines<std::io::BufReader<std::fs::File>> {
    use std::fs::File;
    use std::io::{BufRead, BufReader};
    let file = match File::open(path) {
        Ok(file) => BufReader::new(file),
        Err(msg) => panic!(msg),
    };

    file.lines()
}
