using HonoursCS.Data;
using HonoursCS.Util;
using System;
using System.Collections.Generic;

namespace HonoursCS
{
    /// <summary>
    /// The class that handles all allocation.
    /// </summary>
    public class AllocateStrategy
    {
        /// <summary>
        /// Our reference to the logger.
        /// </summary>
        private readonly Logger m_logger;

        /// <summary>
        /// Whether we want to log items within the algorithm.
        /// </summary>
        private readonly bool m_logEverything;

        /// <summary>
        /// A reference to the instance we are trying to solve.
        /// </summary>
        private readonly Instance m_instance;

        /// <summary>
        /// The number of generations our memetic strategy will run for.
        /// </summary>
        private readonly int m_generations;

        /// <summary>
        /// The number of candidates in a generation.
        /// </summary>
        private readonly int m_candidateSize;

        /// <summary>
        /// The number of items selected for use in tournament selection.
        /// </summary>
        private readonly int m_tournamentSize;

        /// <summary>
        /// The percentage of the population that are kept for the next generation.
        /// </summary>
        private readonly float m_elitePercentage;

        /// <summary>
        /// The percentage of generated candidates that are mutated each generation.
        /// </summary>
        private readonly float m_mutationWeight;

        /// <summary>
        /// The randomizer for this strategy.
        /// </summary>
        private readonly Random m_random;

        /// <summary>
        /// Whether or not this should be a mememtic algorithm.
        /// </summary>
        private readonly bool m_doMemeticStep;

        /// <summary>
        /// Construct a Memetic strategy from the given parameters.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="instance"></param>
        /// <param name="generations"></param>
        /// <param name="candidateSize"></param>
        /// <param name="tournamentPercentage"></param>
        /// <param name="elitePercentage"></param>
        /// <param name="mutationWeight"></param>
        /// <param name="seed"></param>
        public AllocateStrategy(Logger logger,
                       bool logEverything,
                       Instance instance,
                       int generations,
                       int candidateSize,
                       float tournamentPercentage,
                       float elitePercentage,
                       float mutationWeight,
                       bool doMemeticStep,
                       int seed)
        {
            m_logger = logger;
            m_logEverything = logEverything;
            m_instance = instance;
            m_generations = generations;
            m_candidateSize = candidateSize;
            m_tournamentSize = (int)(candidateSize * tournamentPercentage);
            m_elitePercentage = elitePercentage;
            m_mutationWeight = mutationWeight;
            m_doMemeticStep = doMemeticStep;
            m_random = new Random(seed);
            //            m_logger.WriteLine($"Created MemeticStrategy for {instance.Name} with " +
            //                $"numGenerations={generations}, candidateSize={candidateSize}, " +
            //                $"tournamentPercentage={tournamentPercentage}, elitePercentage={elitePercentage}, " +
            //                $"mutationWeight={mutationWeight}, doMemeticStep={doMemeticStep}, seed={seed}");
        }

        /// <summary>
        /// Attempt to allocate all events in the instance using a memetic algorithm.
        /// </summary>
        /// <returns></returns>
        public List<Candidate> MemeticAllocate()
        {
            List<Candidate> candidates = GenerateRandomCandidates(m_candidateSize);
            return MemeticAllocate(candidates);
        }

        /// <summary>
        /// Attempts to fix the candidate, using a memetic algorithm.
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        public Candidate MemeticFix(Candidate candidate)
        {
            // No point in hanging around if the candidate is already fine.
            if (candidate.TotalUnallocated == 0) return candidate;
            // m_logger.WriteLine($"->Memetic Fixing a candidate with {candidate.TotalUnallocated} unallocated events...");

            Candidate result = null;
            var time = Timer.Time(() =>
            {
                // Construct the candidates out of greedily allocated clones of the
                // specified candidate.
                List<Candidate> candidates = new List<Candidate>(m_candidateSize);
                for (int i = 0; i < m_candidateSize; i++)
                {
                    Candidate clone = new Candidate(candidate);
                    GreedyAllocate(clone);
                    candidates.Add(clone);
                }
                result = MemeticAllocate(candidates)[0];
            });
            var num_differences = candidate.CompareDifferencesWith(result);
            //  m_logger.WriteLine($"\t->Memetic Fix Complete: t={time} V(w)={result.WeightedViolations} V(h)={result.HardViolations} V(s)={result.SoftViolations} N(de)={num_differences}.");
            return result;
        }

        /// <summary>
        /// Attempts to fix the candidate using a greedy allocation method.
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        public Candidate GreedyFix(Candidate candidate)
        {
            // No point in hanging around if the candidate is already fine.
            if (candidate.TotalUnallocated == 0) return candidate;
            // m_logger.WriteLine($"->Greedy Fixing a candidate with {candidate.TotalUnallocated} unallocated events...");
            Candidate result = null;
            var time = Timer.Time(() =>
            {
                result = new Candidate(candidate);
                GreedyAllocate(result);
            });
            var num_differences = candidate.CompareDifferencesWith(result);
            // m_logger.WriteLine($"\t->Greedy Fix Complete: t={time} V(w)={result.WeightedViolations} V(h)={result.HardViolations} V(s)={result.SoftViolations} N(de)={num_differences}.");
            return result;
        }

        /// <summary>
        /// Run through a memetic algorithm on the list of intial candidates.
        /// </summary>
        /// <param name="candidates"></param>
        /// <returns></returns>
        private List<Candidate> MemeticAllocate(List<Candidate> candidates)
        {
            var time = Timer.Time(() =>
            {
                // Allocate both the candidates, and the children straight up to prevent
                // continual reallocation of Lists.
                List<Candidate> children = new List<Candidate>(m_candidateSize);
                uint generation = 0;
                while (generation < m_generations && candidates[0].WeightedViolations != 0)
                {
                    //    if (m_logEverything) m_logger.WriteLine($"\tStarting Generation#{generation}.");
                    // If the children contain anything, we want to clear it.
                    children.Clear();
                    // Take the best of the previous generation, and keep their genes.
                    ElitismSelection(candidates, children, m_elitePercentage);
                    // Fill up the rest of the populations with mutations and crossovers.
                    while (children.Count < m_candidateSize)
                    {
                        Candidate parent1 = TournamentSelection(candidates, m_tournamentSize);
                        Candidate parent2 = TournamentSelection(candidates, m_tournamentSize);
                        var result = Crossover(parent1, parent2);
                        Candidate child1 = result.Item1;
                        Candidate child2 = result.Item2;

                        // Mutate.
                        if (m_random.NextDouble() < m_mutationWeight)
                            Mutate(child1);
                        if (m_random.NextDouble() < m_mutationWeight)
                            Mutate(child2);

                        // Try and improve the children before you add them.
                        if (m_doMemeticStep)
                        {
                            MemeticStep(child1);
                            MemeticStep(child2);
                        }

                        children.Add(child1);
                        children.Add(child2);
                    }
                    children.Sort((a, b) => a.WeightedViolations.CompareTo(b.WeightedViolations));
                    // if (m_logEverything) m_logger.WriteLine($"\tBest Candidate of generation has V(w)={candidates[0].WeightedViolations}");
                    // Swap the candidates and the children.
                    List<Candidate> temp = candidates;
                    candidates = children;
                    children = temp;

                    generation += 1;
                }
            });
            // m_logger.WriteLine($"\t->Finished Memetic Allocation: {time}.");
            return candidates;
        }

        /// <summary>
        /// Swaps a random number of events.
        /// </summary>
        /// <param name="candidate"></param>
        private void Mutate(Candidate candidate)
        {
            var deallocateList = new List<Event>(2);
            int length = candidate.Allocations().GetInternalData().Length;
            int lowerLimit = (int)(length * 0.20);
            int upperLimit = (int)(length * 0.80);
            for (int i = 0; i < m_random.Next(lowerLimit, upperLimit); i++)
            {
                deallocateList.Clear();
                var t1 = (uint)m_random.Next((int)m_instance.NumTimeslots - 1);
                var t2 = (uint)m_random.Next((int)m_instance.NumTimeslots - 1);
                var r1 = (uint)m_random.Next(m_instance.Rooms.Count - 1);
                var r2 = (uint)m_random.Next(m_instance.Rooms.Count - 1);

                var e1 = candidate.AllocationAt(t1, r1).Event;
                var e2 = candidate.AllocationAt(t2, r2).Event;
                deallocateList.Add(e1);
                deallocateList.Add(e2);
                candidate.DeallocateEvents(deallocateList);
                candidate.AllocateEvent(t1, r1, e2);
                candidate.AllocateEvent(t2, r2, e1);
            }
            candidate.ReEvaluateConstraints();
        }

        /// <summary>
        /// Greedily try to improve the population.
        /// </summary>
        /// <param name="candidate"></param>
        private void MemeticStep(Candidate candidate)
        {
            for (uint t = 0; t < m_instance.NumTimeslots; t++)
            {
                for (uint r = 0; r < m_instance.Rooms.Count; r++)
                {
                    if (candidate.AllocationAt(t, r).SoftViolations != 0 || candidate.AllocationAt(t, r).HardViolations != 0)
                        candidate.DeallocateAt(t, r);
                }
            }
            // Greedily attempt to allocate the unallocated events.
            GreedyAllocate(candidate);
            candidate.ReEvaluateConstraints();
        }

        /// <summary>
        /// Creates and returns children of the parents.
        /// </summary>
        /// <param name="parent1"></param>
        /// <param name="parent2"></param>
        /// <returns></returns>
        private Tuple<Candidate, Candidate> Crossover(Candidate parent1, Candidate parent2)
        {
            int n = m_instance.Events.Count / 2;
            // Clone the parents to get some children.
            var child1 = new Candidate(parent1);
            var child2 = new Candidate(parent2);
            // Modify the children with aspects of the other parent.
            Cross(child1, parent2, n);
            Cross(child2, parent1, n);
            return Tuple.Create(child1, child2);
        }

        /// <summary>
        /// Used internally by Crossover. You probably want to use that method instead.
        ///
        /// Modify the child candidate with aspects of the parent candidate.
        /// Presumably, the child is a clone of another candidate, and the parent
        /// is the "second" parent of the child.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <param name="n"></param>
        private void Cross(Candidate child, Candidate parent, int n)
        {
            for (int i = 0; i < n; i++)
            {
                uint t = (uint)m_random.Next((int)m_instance.NumTimeslots - 1);
                uint r = (uint)m_random.Next((int)m_instance.Rooms.Count - 1);
                Event @event = parent.AllocationAt(t, r).Event;
                // Deallocate the event if it exists.
                if (@event != null)
                    child.DeallocateEvent(@event);
                // Allocate the event in that position.
                child.AllocateEvent(t, r, @event);
            }
            // Greedily allocate any displaced events.
            GreedyAllocate(child);
            child.ReEvaluateConstraints();
        }

        /// <summary>
        /// Select a candidate from the list of candidates through a tournament with the
        /// specified tournamentSize;
        /// </summary>
        /// <param name="candidates"></param>
        /// <param name="tournamentSize"></param>
        /// <returns></returns>
        private Candidate TournamentSelection(List<Candidate> candidates, int tournamentSize)
        {
            Candidate best = null;
            for (int i = 0; i < tournamentSize; i++)
            {
                Candidate temp = RandomUtil.Choose(candidates, m_random);
                if (best == null || best.WeightedViolations > temp.WeightedViolations)
                {
                    best = temp;
                }
            }
            return best;
        }

        /// <summary>
        /// Adds the best 'n' candidates from the candidates, to the children.
        /// </summary>
        /// <param name="candidates"></param>
        /// <param name="children"></param>
        /// <param name="n"></param>
        private static void ElitismSelection(List<Candidate> candidates, List<Candidate> children, float percentage)
        {
            int n = (int)(candidates.Count * percentage);
            candidates.Sort((a, b) => a.WeightedViolations.CompareTo(b.WeightedViolations));
            for (int i = 0; i < n; i++)
            {
                children.Add(candidates[i]);
            }
        }

        /// <summary>
        /// Generate a list of 'n' candidates.
        /// </summary>
        /// <returns></returns>
        private List<Candidate> GenerateRandomCandidates(int n)
        {
            List<Candidate> candidates = new List<Candidate>(n);

            while (candidates.Count < n)
            {
                Candidate candidate = new Candidate(m_instance);
                GreedyAllocate(candidate);
                candidate.ReEvaluateConstraints();
                if (candidate.TotalUnallocated == 0)
                {
                    candidates.Add(candidate);
                }
            }
            candidates.Sort((a, b) => a.WeightedViolations.CompareTo(b.WeightedViolations));
            return candidates;
        }

        /// <summary>
        /// Greedily attempt to allocate all events in the unallocated queue of a candidate.
        /// </summary>
        /// <param name="candidate"></param
        private void GreedyAllocate(Candidate candidate)
        {
            // NOTE(zac): This algorithm is adapted from 'University Course Timetabling with Genetic Algorithm: A Laboratory Excercises Case Study'
            // Though it has been changed a bit.
            candidate.SortUnallocatedForGreedy();
            while (candidate.TotalUnallocated != 0)
            {
                // get an event from the unallocated queue:
                Event toAllocate = candidate.NextUnallocated();
                bool isAllocated = false;

                // Create a valid Room index
                List<uint> roomIndices = candidate.GetValidRooms(toAllocate);

                while (roomIndices.Count > 0 && !isAllocated)
                {
                    // Remove a random room index from the list:
                    uint roomIndex = RandomUtil.ChooseRemove(roomIndices, m_random);

                    List<uint> timeslotIndices = candidate.GetValidTimeslots(toAllocate);

                    while (timeslotIndices.Count > 0 && !isAllocated)
                    {
                        // Remove a random timeslot from the list:
                        uint timeslotIndex = RandomUtil.ChooseRemove(timeslotIndices, m_random);

                        // Is this timeslot occupied?
                        if (candidate.AllocationAt(timeslotIndex, roomIndex).IsEmpty)
                        {
                            candidate.AllocateEvent(timeslotIndex, roomIndex, toAllocate);
                            isAllocated = true;
                        }
                    }
                }
                // Check to see if we still haven't allocated the event.
                if (!isAllocated)
                {
                    // Pick a random timeslot/room, and plonk it there. Hopefully we can find a spot for
                    // any displaced events.
                    var validTimeslots = m_instance.TimeslotIndices;
                    var validRooms = m_instance.RoomIndices;
                    uint t = RandomUtil.Choose(validTimeslots, m_random);
                    uint r = RandomUtil.Choose(validRooms, m_random);
                    candidate.AllocateEvent(t, r, toAllocate);
                    candidate.SortUnallocatedForGreedy();
                }
            }
        }
    }
}