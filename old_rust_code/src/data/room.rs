pub type BuildingID = String;
pub type RoomID = String;

#[derive(PartialEq)]
pub struct Room {
    id: RoomID,
    capacity: usize,
    // NOTE(zac): I'm not sure I need this field, but I'll keep it here for now, just in case.
    _building: BuildingID,
}

impl Room {
    pub fn new(id: RoomID, capacity: usize, building: BuildingID) -> Self {
        Room {
            id: id,
            capacity: capacity,
            _building: building,
        }
    }

    pub fn id(&self) -> &str {
        &self.id
    }

    pub fn capacity(&self) -> usize {
        self.capacity
    }
}
