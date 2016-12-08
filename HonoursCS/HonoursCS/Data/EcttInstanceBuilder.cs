using System;
using System.Collections.Generic;
using System.IO;

namespace HonoursCS.Data
{
    public sealed class EcttInstanceBuilder : IDisposable
    {
        private readonly Instance m_instance;
        private readonly StreamReader m_file;
        private uint m_numCourses;
        private uint m_numRooms;
        private uint m_numCurricula;
        private uint m_numUnavailableConstraints;
        private uint m_numRoomConstraints;

        public EcttInstanceBuilder(string filename)
        {
            m_file = new StreamReader(File.OpenRead(filename));
            m_instance = new Instance();
        }

        public Instance Build()
        {
            ReadHeader();
            ReadCourses();
            ReadRooms();
            ReadCurricula();
            ReadTimeslotConstraints();
            ReadRoomConstraints();
            return m_instance;
        }

        private void ReadHeader()
        {
            ReadName();
            ReadNumCourses();
            ReadNumRooms();
            ReadTimeslots();
            ReadNumCurricula();
            ReadMinMaxDailyLectures();
            ReadNumUnavailableConstraints();
            ReadNumRoomConstraints();
        }

        private void ReadName()
        {
            string[] strings = NextLine();
            // Name: <name>
            m_instance.Name = strings[1];
        }

        private void ReadNumCourses()
        {
            string[] strings = NextLine();
            // Courses: <num_courses>
            m_numCourses = uint.Parse(strings[1]);
        }

        private void ReadNumRooms()
        {
            string[] strings = NextLine();
            // Rooms: <rooms>
            m_numRooms = uint.Parse(strings[1]);
        }

        private void ReadTimeslots()
        {
            m_instance.Days = ReadDays();
            m_instance.PeriodsPerDay = ReadPeriods();
        }

        private uint ReadDays()
        {
            string[] strings = NextLine();
            // Days: <days>
            return uint.Parse(strings[1]);
        }

        private uint ReadPeriods()
        {
            string[] strings = NextLine();
            // Periods_Per_Day: <periods>
            return uint.Parse(strings[1]);
        }

        private void ReadNumCurricula()
        {
            string[] strings = NextLine();
            m_numCurricula = uint.Parse(strings[1]);
        }

        private void ReadMinMaxDailyLectures()
        {
            string[] strings = NextLine();
            uint min = uint.Parse(strings[1]);
            uint max = uint.Parse(strings[2]);
            m_instance.DailyLectures = Tuple.Create(min, max);
        }

        private void ReadNumUnavailableConstraints()
        {
            string[] strings = NextLine();
            m_numUnavailableConstraints = uint.Parse(strings[1]);
        }

        private void ReadNumRoomConstraints()
        {
            string[] strings = NextLine();
            m_numRoomConstraints = uint.Parse(strings[1]);
        }

        private void ReadCourses()
        {
            // Skip blank space.
            NextLine();
            // Skip Courses:"
            NextLine();
            for (int i = 0; i < m_numCourses; i++)
            {
                ReadCourse();
            }
        }

        private void ReadCourse()
        {
            string[] strings = NextLine();
            string courseid = strings[0];
            string teacherid = strings[1];
            // Check to see if we know this teacher:
            if (!m_instance.Teachers.ContainsKey(teacherid))
            {
                m_instance.Teachers.Add(teacherid, new Teacher(teacherid));
            }
            // Add this course to the list of course ids.
            m_instance.CourseIDs.Add(courseid);
            uint numLectures = uint.Parse(strings[2]);
            uint minDays = uint.Parse(strings[3]);
            uint numStudents = uint.Parse(strings[4]);
            // TODO(zac): How to use the doubleLectures feature?
            bool doubleLectures = uint.Parse(strings[5]) != 0;
            for (int i = 0; i < numLectures; i++)
            {
                m_instance.Events.Add(new Event(courseid, teacherid, numStudents, minDays, i));
            }
        }

        private void ReadRooms()
        {
            // Skip whitespace
            NextLine();
            // Skip "ROOMS:"
            NextLine();

            for (int i = 0; i < m_numRooms; i++)
            {
                ReadRoom();
            }
        }

        private void ReadRoom()
        {
            string[] strings = NextLine();

            string roomID = strings[0];
            uint capacity = uint.Parse(strings[1]);
            string buildingID = strings[2];
            m_instance.Rooms.Add(new Room(roomID, buildingID, capacity));
        }

        private void ReadCurricula()
        {
            // Skip whitespace
            NextLine();
            // Skip CURRICULA:
            NextLine();

            for (int i = 0; i < m_numCurricula; i++)
            {
                ReadCurriculum();
            }
        }

        private void ReadCurriculum()
        {
            string[] strings = NextLine();

            string curriculumID = strings[0];
            int numCourses = int.Parse(strings[1]);
            for (int i = 0; i < numCourses; i++)
            {
                string courseID = strings[2 + i];
                for (int j = 0; j < m_instance.Events.Count; j++)
                {
                    Event e = m_instance.Events[j];
                    if (e.CourseID.Equals(courseID))
                    {
                        e.CurriculumID = curriculumID;
                    }
                }
            }
        }

        private void ReadTimeslotConstraints()
        {
            // Skip the whitespace
            NextLine();
            // Skip "UNAVAILABILITY CONSTRAINTS"
            NextLine();

            var courseDayPeriods = new List<Tuple<string, Timeslot>>((int)m_numUnavailableConstraints);
            for (int i = 0; i < courseDayPeriods.Capacity; i++)
            {
                courseDayPeriods.Add(ReadTimeslotConstraint());
            }
            // Group together by courseID.
            courseDayPeriods.Sort((a, b) => String.CompareOrdinal(a.Item1, b.Item1));

            while (courseDayPeriods.Count != 0)
            {
                string currentCourseID = courseDayPeriods[0].Item1;
                var bannedTimeslots = new List<uint>();
                while (courseDayPeriods.Count != 0 &&
                    courseDayPeriods[0] != null &&
                    courseDayPeriods[0].Item1.Equals(currentCourseID))
                {
                    var tuple = courseDayPeriods[0];
                    courseDayPeriods.RemoveAt(0);
                    bannedTimeslots.Add(m_instance.TimeslotIndex(tuple.Item2));
                }

                for (int i = 0; i < m_instance.Events.Count; i++)
                {
                    Event e = m_instance.Events[i];
                    if (e.CourseID.Equals(currentCourseID))
                    {
                        e.BannedTimeslotIndices = bannedTimeslots;
                    }
                }
            }
        }

        private Tuple<string, Timeslot> ReadTimeslotConstraint()
        {
            string[] strings = NextLine();
            return Tuple.Create(strings[0], new Timeslot(uint.Parse(strings[1]), uint.Parse(strings[2])));
        }

        private void ReadRoomConstraints()
        {
            // skip whitespace
            NextLine();
            // Skip "ROOM_CONSTRAINTS"
            NextLine();

            var courseRooms = new List<Tuple<string, string>>((int)m_numRoomConstraints);
            for (int i = 0; i < courseRooms.Capacity; i++)
            {
                var tuple = ReadRoomConstraint();
                courseRooms.Add(tuple);
            }
            // Group by course id.
            courseRooms.Sort((a, b) => String.CompareOrdinal(a.Item1, b.Item1));

            while (courseRooms.Count != 0)
            {
                string currentCourseID = courseRooms[0].Item1;
                var validRoomIndecies = new List<uint>();

                while (courseRooms.Count != 0 &&
                    courseRooms[0] != null &&
                    courseRooms[0].Item1.Equals(currentCourseID))
                {
                    var tuple = courseRooms[0];
                    courseRooms.RemoveAt(0);
                    uint roomIndex = m_instance.IndexOfRoom(tuple.Item2);
                    validRoomIndecies.Add(roomIndex);
                }

                for (int i = 0; i < m_instance.Events.Count; i++)
                {
                    Event e = m_instance.Events[i];
                    if (e.CourseID.Equals(currentCourseID))
                    {
                        e.ValidRoomIndices = validRoomIndecies;
                    }
                }
            }
        }

        private Tuple<string, string> ReadRoomConstraint()
        {
            string[] strings = NextLine();
            return Tuple.Create(strings[0], strings[1]);
        }

        /// <summary>
        /// Return the next line, split by whitespace.
        /// </summary>
        /// <returns></returns>
        private string[] NextLine()
        {
            string line = m_file.ReadLine();
            return line.Split();
        }

        public void Dispose()
        {
            m_file.Dispose();
        }
    }
}