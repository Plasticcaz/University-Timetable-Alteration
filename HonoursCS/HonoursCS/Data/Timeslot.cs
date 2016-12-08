namespace HonoursCS.Data
{
    /// <summary>
    /// A data structure to hold all information to do with a timeslot.
    /// </summary>
    public struct Timeslot
    {
        /// <summary>
        /// The day of the timeslot.
        /// </summary>
        public uint Day { get; set; }

        /// <summary>
        /// The period on the specified day.
        /// </summary>
        public uint Period { get; set; }

        /// <summary>
        /// Construct a timeslot with the specified day and period.
        /// </summary>
        /// <param name="day"></param>
        /// <param name="period"></param>
        public Timeslot(uint day, uint period)
        {
            Day = day;
            Period = period;
        }

        /// <summary>
        /// Checks to see if two timeslots are equivalent.
        /// It is probably more efficient to compare timeslot indices,
        /// rather than a timeslot data structure in hot code.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool Equals(Timeslot t)
        {
            return Day == t.Day && Period == t.Period;
        }
    }
}