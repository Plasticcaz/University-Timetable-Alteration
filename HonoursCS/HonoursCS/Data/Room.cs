namespace HonoursCS.Data
{
    /// <summary>
    /// A class to hold all data on a room.
    /// </summary>
    public sealed class Room
    {
        /// <summary>
        /// The room id.
        /// </summary>
        public string RoomID { get; private set; }

        /// <summary>
        /// The building id.
        /// </summary>
        public string BuildingID { get; private set; }

        /// <summary>
        /// The capacity of the room.
        /// </summary>
        public uint Capacity { get; private set; }

        /// <summary>
        /// Construct a new room objecte with the specified room id, building id, and capacity.
        /// </summary>
        /// <param name="roomid"></param>
        /// <param name="buildingid"></param>
        /// <param name="capacity"></param>
        public Room(string roomid, string buildingid, uint capacity)
        {
            RoomID = roomid;
            BuildingID = buildingid;
            Capacity = capacity;
        }
    }
}