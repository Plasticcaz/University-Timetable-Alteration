extern crate time_table;

use time_table::*;
use time_table::data::*;
use time_table::candidate::*;

#[macro_use]
mod timer;

/// Test program to help figure out if this is working.
fn main() {
    let options = options::load_options("options.toml").unwrap();
    println!("Parsed options.toml");

    let result = {
        let args: Vec<String> = std::env::args().collect();
        let path = if args.len() == 2 {
            args[1].as_ref()
        } else {
            "../test_data/comp01.ectt"
        };

        data::load(path)
    };

    match result {
        Ok(instance) => {
            println!("Successfully loaded {}!", instance.name().unwrap());

            let mut strategy = options.strategy;
            let candidates;
            let elapsed = time!{
                candidates = strategy.allocate(&instance);
            };
            print!("[");
            for candidate in candidates.iter() {
                print!("{}, ", candidate.violations());
            }
            println!("]");
            println!("Allocation took {}:{} secs",
                     elapsed.as_secs(),
                     elapsed.subsec_nanos());


        }
        Err(msg) => {
            println!("Error: {}", msg);
        }
    }
}


// TODO(zac): Put this stuff in it's own module.
// TODO(zac): Find a way to start testing this.

/// the aspect of the currently allocated event to change.
pub enum ToChange {
    /// Change the room.
    Room,
    /// Change the timeslot.
    Timeslot,
}

/// The event and "offending" item that needs changing.
pub struct ReallocateTask {
    /// The index of the event to reallocate in the Instance.
    pub event_index: usize,
    /// The specific aspect that needs to be changed.
    pub to_change: ToChange,
}

/// Unallocate the events that are specified in the tasks, adjusting the availability
/// in the instance.
fn unallocate_events(tasks: Vec<ReallocateTask>,
                     candidate: &mut CandidateSolution,
                     instance: &mut Instance)
                     -> Vec<usize> {
    // Delete those events from the candidate:
    for timeslot_index in 0..candidate.num_timeslots() {
        for room_index in 0..candidate.num_rooms() {
            let event_index = candidate.get_allocation(timeslot_index, room_index)
                .expect("Invalid allocation attempted!")
                .event_index();
            // If the allocated event is in one of our tasks:
            if let Some(task) = tasks.iter().find(|task| task.event_index == event_index) {
                // Remove event.
                candidate.allocate_event(timeslot_index, room_index, None, instance);
                // Adjust the event in the instance to compensate.
                let timeslot = *instance.timeslot(timeslot_index).unwrap();
                let event = instance.event_mut(event_index).unwrap();
                match task.to_change {
                    ToChange::Room => {
                        unimplemented!();
                        // TODO(zac): I think that to make this trivial, we are going to have
                        // to change the way we handle valid rooms. (ie. events with no invalid
                        // rooms are going to have to be given every room as valid rooms at the
                        // start.
                    }
                    ToChange::Timeslot => {
                        // Add the timeslot to the list of banned timeslots.
                        event.add_banned_timeslot(timeslot);
                    }
                }
            }
        }
    }
    // Return a vector of the event indecies we need to reallocate.
    tasks.iter().map(|to_change| to_change.event_index).collect()
}

/// Perform the specified reallocate tasks on the CandidateSolution of the Instance.
pub fn reallocate_events(tasks: Vec<ReallocateTask>,
                         candidate: &mut CandidateSolution,
                         instance: &mut Instance) {
    // In order to _RE_allocate events, first we must:
    let events = unallocate_events(tasks, candidate, instance);

    for event_index in events {
        println!("{}", event_index);
    }
}
