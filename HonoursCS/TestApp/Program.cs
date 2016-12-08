using HonoursCS;
using HonoursCS.Data;
using HonoursCS.Util;
using System;

namespace TestApp
{
    internal enum BanOp
    {
        BanRoomForAll,
        BanTimeslotForAll,
        BanDayForAll,
    }

    /// <summary>
    /// A Command-Line program that runs automated tests on the data.
    /// </summary>
    public static class Program
    {
        // NOTE(zac): We want to be able to tell the difference between which release modes we are in.
#if DEBUG
        private const string mode = "Debug";
#else
        private const string mode = "Release";
#endif

        public static void Main(string[] args)
        {
            const int numGenerations = 15000;
            const int numCandidates = 150;

            float[] mutationWeights = { 0.25f };
            float[] tournamentPercentages = { 0.5f };
            float[] elitePercentages = {0.5f};
            bool[] doMemeticSteps = {true, false};
            int seed = new Random().Next();

            string[] test_data = new[]
            {
                "test_data/comp01.ectt",
                "test_data/comp02.ectt",
                "test_data/comp03.ectt",
                "test_data/comp04.ectt",
                "test_data/comp05.ectt",
                "test_data/comp06.ectt",
                /*
                "test_data/comp07.ectt",
                "test_data/comp08.ectt",
                "test_data/comp09.ectt",
                "test_data/comp10.ectt",
                "test_data/comp11.ectt",
                "test_data/comp12.ectt",
                "test_data/comp13.ectt",
                "test_data/comp14.ectt",
                "test_data/comp15.ectt",
                "test_data/comp16.ectt",
                "test_data/comp17.ectt",
                "test_data/comp18.ectt",
                "test_data/comp19.ectt",
                "test_data/comp20.ectt",
                "test_data/comp21.ectt",
                "test_data/Udine1.ectt",
                "test_data/Udine2.ectt",
                "test_data/Udine3.ectt",
                "test_data/Udine4.ectt",
                "test_data/Udine5.ectt",
                "test_data/Udine6.ectt",
                "test_data/Udine7.ectt",
                "test_data/Udine8.ectt",
                "test_data/Udine9.ectt"
                */
            };
#if false
            RunNormalExperiments(test_data, seed, numGenerations, numCandidates, mutationWeights, tournamentPercentages, elitePercentages, doMemeticSteps);
#else
            IAsyncResult[] results = new IAsyncResult[2];
            RunAllBanExperimentsDel del = RunAllBanExperiments;
            results[0] = del.BeginInvoke(test_data, seed, numGenerations, numCandidates, mutationWeights, tournamentPercentages, elitePercentages, doMemeticSteps, null, null);
            System.Threading.Thread.Sleep(2000); // Wait for 2 seconds in order to ensure that we generate different files.
            results[1] = del.BeginInvoke(test_data, seed, numGenerations, numCandidates, mutationWeights, tournamentPercentages, elitePercentages, doMemeticSteps, null, null);
            del.EndInvoke(results[0]);
            del.EndInvoke(results[1]);

#endif
        }

        delegate void RunAllBanExperimentsDel(string[] test_data, int seed, int numGenerations, int numCandidates, float[] mutationWeights, float[] tournamentPercentages, float[] elitePercentages, bool[] doMemeticSteps);

        private static void RunAllBanExperiments(string[] test_data, int seed, int numGenerations, int numCandidates, float[] mutationWeights, float[] tournamentPercentages, float[] elitePercentages, bool[] doMemeticSteps)
        {
            RunBanExperiments(BanOp.BanDayForAll, test_data, seed, numGenerations, numCandidates, mutationWeights, tournamentPercentages, elitePercentages, doMemeticSteps);
            RunBanExperiments(BanOp.BanRoomForAll, test_data, seed, numGenerations, numCandidates, mutationWeights, tournamentPercentages, elitePercentages, doMemeticSteps);
            RunBanExperiments(BanOp.BanTimeslotForAll, test_data, seed, numGenerations, numCandidates, mutationWeights, tournamentPercentages, elitePercentages, doMemeticSteps);
        }

        private static void RunNormalExperiments(string[] test_data, int seed, int numGenerations, int numCandidates, float[] mutationWeights, float[] tournamentPercentages, float[] elitePercentages, bool[] doMemeticSteps)
        {
            DateTime now = DateTime.Now;
            Logger normalLogger = new Logger($"logs/{now.Year}-{now.Month}-{now.Day}-{now.Hour}-{now.Minute}-{now.Second}-{mode}.csv");
            normalLogger.WriteLine("Instance,Rooms,Days,PeriodsPerDay,Teachers,Seed,Generations,Candidate Size,Tournament %,Elite %,Mutation Rate,v(W),v(H),V(S),doMemeticStep,Time Taken");

            foreach (string filename in test_data)
            {
                Instance instance = new EcttInstanceBuilder(filename).Build();
                foreach (var tournamentPercentage in tournamentPercentages)
                    foreach (var elitePercentage in elitePercentages)
                        foreach (var mutationWeight in mutationWeights)
                            foreach (var doMemeticStep in doMemeticSteps)
                            {
                                AllocateStrategy strategy = new AllocateStrategy(null, false, instance, numGenerations, numCandidates, tournamentPercentage, elitePercentage, mutationWeight, doMemeticStep, seed);
                                Candidate topSolution = null;
                                var time = Timer.Time(new Action(() =>
                                {
                                    topSolution = strategy.MemeticAllocate()[0];
                                }));
                                normalLogger.WriteLine($"{filename},{instance.Rooms.Count},{instance.Days},{instance.PeriodsPerDay},{instance.Teachers.Count},{seed},{numGenerations},{numCandidates},{tournamentPercentage},{elitePercentage},{mutationWeight},{topSolution.WeightedViolations},{topSolution.HardViolations},{topSolution.SoftViolations},{doMemeticStep},{time}");
                            }
            }
        }

        private static void RunBanExperiments(BanOp toBan, string[] test_data, int seed, int numGenerations, int numCandidates, float[] mutationWeights, float[] tournamentPercentages, float[] elitePercentages, bool[] doMemeticSteps)
        {
            DateTime now = DateTime.Now;
            Logger normalLogger = new Logger($"logs/Banning-{now.Year}-{now.Month}-{now.Day}-{now.Hour}-{now.Minute}-{now.Second}-{mode}.csv");
            normalLogger.WriteLine("BanType,BannedValue,FixMethod,DeallocatedByBan,Instance,Seed,Generations,CandidateSize,Tournament %,Elite %,Mutation Rate,doMemeticStep,v(W),v(H),V(S),EventsInBanned,DisplacedEvents,TimeTaken");
            foreach (string filename in test_data)
            {
                Instance instance = new EcttInstanceBuilder(filename).Build();
                if (toBan == BanOp.BanDayForAll)
                {
                    instance.Days += 1;
                }
                AllocateStrategy strategy = new AllocateStrategy(null, false, instance, numGenerations, numCandidates, mutationWeights[0], tournamentPercentages[0], elitePercentages[0], true, seed);
                Candidate initial = strategy.MemeticAllocate()[0];
                System.Console.Write("Initial Allocation finished.");

                string banType;
                uint bannedValue = 0;
                switch (toBan)
                {
                    case BanOp.BanRoomForAll:
                        initial.BanRoomForAll(bannedValue);
                        banType = "RoomBanned";
                        break;

                    case BanOp.BanTimeslotForAll:
                        initial.BanTimeslotForAll(bannedValue);
                        banType = "TimeslotBanned";
                        break;

                    case BanOp.BanDayForAll:
                        bannedValue = instance.Days - 1;
                        initial.BanDayForAll(bannedValue);
                        banType = "DayBanned";
                        break;

                    default:
                        banType = "";
                        throw new InvalidOperationException("This should be unreachable.");
                }
#if false
                // Perform and test greedy fix:
                {
                    const string fixMethod = "Greedy";
                    Candidate greedyFixed = null;
                    var time = Timer.Time(() =>
                    {
                        greedyFixed = strategy.GreedyFix(initial);
                    });
                    uint eventsInBanned = FindEventsInBanned(toBan, bannedValue, greedyFixed);
                    normalLogger.WriteLine($"{banType},{bannedValue},{fixMethod},{initial.TotalUnallocated},{filename},{seed},{numGenerations},{numCandidates},{0},{0},{0},{false},{greedyFixed.WeightedViolations},{greedyFixed.HardViolations},{greedyFixed.SoftViolations},{eventsInBanned},{initial.CompareDifferencesWith(greedyFixed)},{time}");
                }
#endif
                foreach (var tournamentPercentage in tournamentPercentages)
                    foreach (var elitePercentage in elitePercentages)
                        foreach (var mutationWeight in mutationWeights)
                            foreach (var doMemeticStep in doMemeticSteps)
                            {
                                AllocateStrategy fixStrategy = new AllocateStrategy(null, false, instance, numGenerations, numCandidates, tournamentPercentage, elitePercentage, mutationWeight, doMemeticStep, seed);
                                // Try the Genetic/Memetic fix.
#if false
                                {
                                    const string fixMethod = "Genetic/Memetic";

                                    Candidate fixedCandidate = null;
                                    var time = Timer.Time(() =>
                                    {
                                        fixedCandidate = fixStrategy.MemeticFix(initial);
                                    });
                                    uint eventsInBanned = FindEventsInBanned(toBan, bannedValue, fixedCandidate);
                                    normalLogger.WriteLine($"{banType},{bannedValue},{fixMethod},{initial.TotalUnallocated},{filename},{seed},{numGenerations},{numCandidates},{tournamentPercentage},{elitePercentage},{mutationWeight},{doMemeticStep},{fixedCandidate.WeightedViolations},{fixedCandidate.HardViolations},{fixedCandidate.SoftViolations},{eventsInBanned},{initial.CompareDifferencesWith(fixedCandidate)},{time}");
                                }
#endif
                                // Try just reallocating using the initial generation algorithm (Don't try and generate similar solutions to the banned one).
                                {
                                    const string fixMethod = "Start-over";

                                    Candidate fixedCandidate = null;
                                    var time = Timer.Time(() =>
                                    {
                                        fixedCandidate = fixStrategy.MemeticAllocate()[0];
                                    });
                                    uint eventsInBanned = FindEventsInBanned(toBan, bannedValue, fixedCandidate);
                                    normalLogger.WriteLine($"{banType},{bannedValue},{fixMethod},{initial.TotalUnallocated},{filename},{seed},{numGenerations},{numCandidates},{tournamentPercentage},{elitePercentage},{mutationWeight},{doMemeticStep},{fixedCandidate.WeightedViolations},{fixedCandidate.HardViolations},{fixedCandidate.SoftViolations},{eventsInBanned},{initial.CompareDifferencesWith(fixedCandidate)},{time}");
                                }

                            }
            }
        }

        private static uint FindEventsInBanned(BanOp toBan, uint bannedValue, Candidate candidate)
        {
            uint returnValue = 0;
            switch (toBan)
            {
                case BanOp.BanRoomForAll:
                    returnValue = candidate.CountEventsInRoom(bannedValue);
                    break;

                case BanOp.BanTimeslotForAll:
                    returnValue = candidate.CountEventsOnTimeslot(bannedValue);
                    break;

                case BanOp.BanDayForAll:
                    returnValue = candidate.CountEventsOnDay(bannedValue);
                    break;

                default:
                    throw new InvalidOperationException("Unreachable!");
            }
            return returnValue;
        }
    }
}