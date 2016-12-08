using System;
using System.Collections.Concurrent;
using System.IO;

namespace HonoursCS.Util
{
    /// <summary>
    /// A logger that writes to a log file on a separate thread.
    /// </summary>
    public sealed class Logger
    {
        /// <summary>
        /// The filestream we are writing to.
        /// </summary>
        private readonly StreamWriter m_fs;

        /// <summary>
        /// The queue of messages to log.
        /// </summary>
        private readonly BlockingCollection<string> m_queue;

        /// <summary>
        /// Create a logger that creates a new file with the specified name, and writes
        /// all input to it.
        /// </summary>
        /// <param name="filename"></param>
        public Logger(string filename)
        {
            m_queue = new BlockingCollection<string>();
            if (File.Exists(filename) && filename.EndsWith(".log"))
                File.Delete(filename);
            m_fs = new StreamWriter(File.OpenWrite(filename));
            // NOTE(zac): We want to make sure everything gets printed to
            // file, even if program crashes.
            m_fs.AutoFlush = true;
            Action logThread = Writer;
            logThread.BeginInvoke(null, null);
        }

        /// <summary>
        /// This method is invoked upon Logger creation, and performs all the
        /// actual writing.
        /// </summary>
        private void Writer()
        {
            while (true)
            {
                string toWrite = m_queue.Take();
                // Write to the file.
                m_fs.Write(toWrite);
                // Write to console as well.
                Console.Write(toWrite);
            }
        }

        /// <summary>
        /// Write something to the log file asyncronously, flushing the buffer after every string.
        /// (That way, if the program crashes, or the user closes the program prematurely, the log
        /// should still exist.)
        /// </summary>
        /// <param name="value"></param>
        public void Write<T>(T value)
        {
            m_queue.Add($"{value}");
        }

        /// <summary>
        /// Write something to the log file asyncronously, flushing the buffer after every string.
        /// (That way, if the program crashes, or the user closes the program prematurely, the log
        /// should still exist.)
        /// </summary>
        /// <param name="value"></param>
        public void WriteLine<T>(T value)
        {
            m_queue.Add($"{value}\n");
        }
    }
}