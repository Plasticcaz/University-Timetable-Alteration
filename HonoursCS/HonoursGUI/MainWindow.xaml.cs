using HonoursCS;
using HonoursCS.Data;
using HonoursCS.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;

namespace HonoursGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The instance selected.
        /// </summary>
        private readonly Instance m_instance;

        /// <summary>
        /// The Candidate that has been generated.
        /// </summary>
        private Candidate m_candidate;

        /// <summary>
        /// The Allocation Strategy object.
        /// </summary>
        private AllocateStrategy m_strategy;

        /// <summary>
        /// The logger for the gui.
        /// </summary>
        private Logger m_logger;

        public MainWindow()
        {
            InitializeComponent();
            // Initialize the Instance.
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".ectt";
            dialog.Multiselect = false;
            bool? result = dialog.ShowDialog();
            if (result == true)
                m_instance = new EcttInstanceBuilder(dialog.FileName).Build();
            var now = DateTime.Now;
            string mode;
#if DEBUG
            mode = "Debug";
#else
            mode = "Release";
#endif
            m_logger = new Logger($"logs/gui{now.Year}-{now.Month}-{now.Day}-{now.Hour}-{now.Minute}-{now.Second}-{mode}.log");
        }

        private delegate List<Candidate> AllocateDelegate();

        /// <summary>
        /// Called when the user presses the "Generate" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GenerateCandidate_Click(object sender, RoutedEventArgs e)
        {
            // TODO(zac): Maybe allow the user to specify the parameters.
            const int NUM_GENERATIONS = 100;
            const int NUM_CANDIDATES = 10;
            const float TOURNAMENT_PERCENTAGE = 0.95f;
            const float ELITE_PERCENTAGE = 0.75f;
            const float MUTATION_RATE = 0.30f;
            int seed = new Random().Next();
            m_strategy = new AllocateStrategy(m_logger, false, m_instance, NUM_GENERATIONS, NUM_CANDIDATES, TOURNAMENT_PERCENTAGE, ELITE_PERCENTAGE, MUTATION_RATE, true, seed);
            AllocateDelegate allocate = m_strategy.MemeticAllocate;
            m_output.Text = "Generating candidates...";
            m_logger.WriteLine(m_output.Text);
            allocate.BeginInvoke((asyncResult) =>
            {
                if (asyncResult.IsCompleted)
                {
                    List<Candidate> candidates = allocate.EndInvoke(asyncResult);
                    candidates.Sort((a, b) => a.WeightedViolations.CompareTo(b.WeightedViolations));
                    m_candidate = candidates[0];
                    m_candidate.ReEvaluateConstraints();
                    Refresh();
                }
            }, null);
        }

        /// <summary>
        /// Makes sure the current state of the candidate is being visualized.
        /// </summary>
        private void Refresh()
        {
            Dispatcher.Invoke(() =>
            {
                m_dataGrid.ItemsSource = null;
                m_dataGrid.ItemsSource = m_candidate.Allocations().GetInternalData();
                m_output.Text = $"V(weighted): {m_candidate.WeightedViolations} N_unallocated: {m_candidate.TotalUnallocated}";
                m_logger.WriteLine($"Current State: {m_output.Text}");
            });
        }

        private void BanTimeslot_Click(object sender, RoutedEventArgs e)
        {
            if (m_dataGrid.SelectedItem == null) return;
            Allocation allocation = (Allocation)m_dataGrid.SelectedItem;
            if (allocation.Event == null) return;
            m_logger.WriteLine($"Banning a timeslot for {allocation.Event.CourseID} at timeslot t={allocation.TimeslotIndex} r={allocation.RoomIndex}.");
            // Remove allocation from event, and update
            new Action(() =>
            {
                m_candidate.BanTimeslotForCourse(allocation);
                Refresh();
            }).BeginInvoke(null, null);
        }

        /// <summary>
        /// Called when the "Memetic Fix" button is pressed.
        ///
        /// Calls MemeticStrategy.MemeticFix in an attempt to perform reallocation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MemeticFix_Click(object sender, RoutedEventArgs e)
        {
            if (m_strategy != null && m_candidate != null)
            {
                m_output.Text = "Reallocating...";
                m_logger.WriteLine(m_output.Text);
                new Action(() =>
                {
                    var temp = m_strategy.MemeticFix(m_candidate);
                    var diff = m_candidate.CompareDifferencesWith(temp);
                    m_candidate = temp;
                    m_logger.WriteLine($"Number of displaced events: {diff}");

                    Refresh();
                }).BeginInvoke(null, null);
            }
        }

        /// <summary>
        /// Called when the "Greedy Fix" button is pressed.
        ///
        /// Calls MemeticStrategy.GreedyFix in an attempt to perform reallocation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GreedyFix_Click(object sender, RoutedEventArgs e)
        {
            if (m_strategy != null && m_candidate != null)
            {
                m_output.Text = "Reallocating...";
                m_logger.WriteLine(m_output.Text);
                new Action(() =>
                {
                    var temp = m_strategy.GreedyFix(m_candidate);
                    var diff = m_candidate.CompareDifferencesWith(temp);
                    m_candidate = temp;
                    m_logger.WriteLine($"Number of displaced events: {diff}");
                    Refresh();
                }).BeginInvoke(null, null);
            }
        }

        /// <summary>
        /// Called when Ban Room is clicked.
        ///
        /// Bans the event in the currently selected allocation, and unallocates
        /// it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BanRoom_Click(object sender, RoutedEventArgs e)
        {
            if (m_dataGrid.SelectedItem == null) return;
            Allocation allocation = (Allocation)m_dataGrid.SelectedItem;
            if (allocation.Event == null) return;
            m_logger.WriteLine($"Banning a room for {allocation.Event.CourseID} at timeslot t={allocation.TimeslotIndex} r={allocation.RoomIndex}.");
            new Action(() =>
            {
                m_candidate.BanRoomForCourse(allocation);
                Refresh();
            }).BeginInvoke(null, null);
        }

        private void BanFridays_Click(object sender, RoutedEventArgs e)
        {
            const uint DAY = 4;
            // TODO(zac): Allow the user to specify the day.
            m_logger.WriteLine($"Banning day {DAY} for all events.");
            Action a = () =>
            {
                m_candidate.BanDayForAll(DAY);
                Refresh();
            };
            a.BeginInvoke(null, null);
        }

        private void m_dataGrid_SelectedCellsChanged(object sender, System.Windows.Controls.SelectedCellsChangedEventArgs e)
        {
            if (m_dataGrid.SelectedItem == null) return;
            var allocation = (Allocation)m_dataGrid.CurrentItem;
            m_infoBox.Text = $"Violated:\nCurriculum: {allocation.IsViolated(ConstraintType.CurriculumConstraint)}\nRoomCapacity: {allocation.IsViolated(ConstraintType.RoomCapacityConstraint)}\n" +
                             $"Room: {allocation.IsViolated(ConstraintType.RoomConstraint)}\nTeacher: {allocation.IsViolated(ConstraintType.TeacherConstraint)}\n" +
                             $"Timeslot: {allocation.IsViolated(ConstraintType.TimeslotConstraint)}";
        }

        private void BanRoomAll_Click(object sender, RoutedEventArgs e)
        {
            if (m_dataGrid.SelectedItem == null) return;
            var allocation = (Allocation)m_dataGrid.CurrentItem;
            m_logger.WriteLine($"Banning room {allocation.RoomIndex} for all events.");
            Action a = () =>
            {
                m_candidate.BanRoomForAll(allocation.RoomIndex);
                Refresh();
            };
            a.BeginInvoke(null, null);
        }

        private void BanTimeslotAll(object sender, RoutedEventArgs e)
        {
            if (m_dataGrid.SelectedItem == null) return;
            var allocation = (Allocation)m_dataGrid.CurrentItem;
            m_logger.WriteLine($"Banning Timeslot {allocation.TimeslotIndex} for all events.");
            Action a = () =>
            {
                m_candidate.BanTimeslotForAll(allocation.TimeslotIndex);
                Refresh();
            };
            a.BeginInvoke(null, null);
        }
    }
}