mod ectt;
pub mod event;
pub mod instance;
pub mod room;
pub mod timeslot;
pub mod constraints;

// export the following modules out of this module.
pub use data::event::*;
pub use data::instance::*;
pub use data::room::*;
pub use data::timeslot::*;
pub use data::constraints::*;

pub fn load(path: &str) -> TimetableDataResult {
    let loader = ectt::Loader::new(path);
    loader.load()
}
