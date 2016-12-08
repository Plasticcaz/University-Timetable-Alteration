use rand::*;
use rand::distributions::IndependentSample;
use rand::distributions::range::Range;

use candidate::*;
use allocation::*;
use data::instance::*;
use data::timeslot::*;
use util;


/// An implementation of a genetic algorithm as an implementation of an
/// allocation strategy.
pub struct GeneticStrategy {
    /// The number of generations to simulate.
    generations: usize,
    /// The number of candidates per each generation.
    candidates_size: usize,
    /// Tournament size.
    tournament_size: usize,
    elite_number: usize,
    /// The weight of calling the mutation operator. 1 in mutation_weight chance.
    mutation_weight: u32,

    /// The Rng implementation we will use.
    rng: StdRng,
}

impl GeneticStrategy {
    pub fn new(generations: usize,
               candidates_size: usize,
               tournament_size: usize,
               elite_number: usize,
               mutation_weight: u32,
               seed: [usize; 4])
               -> Self {
        use rand::SeedableRng;
        GeneticStrategy {
            generations: generations,
            candidates_size: candidates_size,
            tournament_size: tournament_size,
            elite_number: elite_number,
            mutation_weight: mutation_weight,
            rng: StdRng::from_seed(&seed),
        }
    }
}

impl AllocationStrategy for GeneticStrategy {
    fn allocate(&mut self, instance: &Instance) -> Box<[CandidateSolution]> {
        // --- Initialization phase:
        let mut candidates =
            generate_n_random_candidates(self.candidates_size, instance, &mut self.rng);
        println!("Finished generating initial population.");
        while self.generations > 0 {
            let mut children = Vec::with_capacity(self.candidates_size);
            // --- Elitism step. Get the n best candidates, and let them persist into the new
            // generation.
            elitism_selection(&candidates, &mut children, self.elite_number);

            // --- selection, crossover, and mutation
            while children.len() < self.candidates_size {
                let parent1 =
                    tournament_selection(self.tournament_size, &candidates, &mut self.rng);
                let parent2 =
                    tournament_selection(self.tournament_size, &candidates, &mut self.rng);
                let (mut child1, mut child2) =
                    crossover(&parent1, &parent2, &mut self.rng, instance);
                if self.rng.gen_weighted_bool(self.mutation_weight) {
                    mutate(&mut child1, &mut self.rng, instance);
                }
                if self.rng.gen_weighted_bool(self.mutation_weight) {
                    mutate(&mut child2, &mut self.rng, instance);
                }
                children.push(child1);
                children.push(child2);
            }
            // --- Make way for the next generation!
            candidates = children.into_boxed_slice();
            candidates.sort_by(|a, b| a.violations().cmp(&b.violations()));
            self.generations -= 1;

            // --- Early exit if solution is found.
            if candidates[0].violations() == 0 {
                println!("Found best solution, breaking out of loop early! generation: {}",
                         self.generations + 1);
                break;
            }
        }
        // --- Return the list of candidates, sorted by number of violations
        candidates
    }
}

/// Performs an elitism selection on a candidates. Selects the n best solutions, and places them
/// into the new candidates (next generation).
fn elitism_selection(candidates: &[CandidateSolution],
                     new_candidates: &mut Vec<CandidateSolution>,
                     n: usize) {
    for i in 0..n {
        new_candidates.push(candidates[i].clone());
    }
}

/// A tournament selection operator. NOTE: 'p' is the probability of selection for the best
/// solution. 'k' is the number of contestants to compete in the tournament
fn tournament_selection<'a, RNG: Rng>(k: usize,
                                      population: &'a [CandidateSolution],
                                      rng: &mut RNG)
                                      -> &'a CandidateSolution {
    assert!(k != 0);
    let mut best: Option<&CandidateSolution> = None;
    for _ in 0..k {
        let temp = rng.choose(population).unwrap();
        if best.is_none() || temp.violations() < best.unwrap().violations() {
            best = Some(temp);
        }
    }
    best.take().unwrap()
}

/// Generate n candidates with a random allocation strategy.
fn generate_n_random_candidates<RNG: Rng>(n: usize,
                                          instance: &Instance,
                                          rng: &mut RNG)
                                          -> Box<[CandidateSolution]> {
    let mut candidates = Vec::with_capacity(n);
    for _ in 0..n {
        let mut candidate = CandidateSolution::new(instance);
        // NOTE(zac): This algorithm adapted from 'University Course Timetabling with Genetic Algorithm: A Laboratory Excercises Case Study'
        let mut events_indicies = util::vec_from_range(0..instance.events().len());

        // while E is not empty.
        while !events_indicies.is_empty() {
            // select random event e and remove it from E
            let (event, event_index) = {
                let index_index = rng.gen_range(0, events_indicies.len());
                let index = events_indicies.remove(index_index);
                (instance.event(index).expect("Unexpected event index!"), index)
            };

            // Find the set of valid timeslots for this event.
            let mut valid_timeslots: Vec<TimeSlot> = instance.timeslots()
                .iter()
                // Filter out any banned timeslots.
                .filter(|timeslot| !event.banned_timeslots().contains(timeslot))
                // Filter out any timeslots that will cause a teacher conflict.
                .filter(|timeslot| {
                    let timeslot_index = instance.to_timeslot_index(timeslot.day, timeslot.period);
                    for room_index in 0..instance.num_rooms() {
                        if let Some(allocation) = candidate.get_allocation(timeslot_index, room_index) {
                            let teacher = instance.event(allocation.event_index()).unwrap().teacher();
                            if teacher == event.teacher() {
                                return false; // don't include this.
                            }
                        }
                    }
                    // If we have gotten here, it's fine.
                    true
                })
                .map(|timeslot| *timeslot)
                .collect();

            if valid_timeslots.len() == 0 {
                // TODO(zac): Ideally we'd just throw this out and start again.
                valid_timeslots = instance.timeslots().to_owned();
            }

            let valid_rooms: Vec<usize> = if event.valid_rooms().is_some() {
                event.valid_rooms().unwrap().to_owned()
            } else {
                util::vec_from_range(0..instance.num_rooms())
            };

            'allocate: while !valid_timeslots.is_empty() {
                let timeslot_index = {
                    let timeslot_index = rng.gen_range(0, valid_timeslots.len());
                    let timeslot = valid_timeslots.remove(timeslot_index);
                    instance.to_timeslot_index(timeslot.day, timeslot.period)
                };
                let mut valid_rooms = valid_rooms.clone();
                while !valid_rooms.is_empty() {
                    let room_index = {
                        let room_index_index = rng.gen_range(0, valid_rooms.len());
                        let room_index = valid_rooms.remove(room_index_index);
                        room_index
                    };

                    if candidate.get_allocation(timeslot_index, room_index).is_none() {
                        candidate.allocate_event(timeslot_index,
                                                 room_index,
                                                 Some(event_index),
                                                 instance);
                        break 'allocate;
                        // NOTE: If we have made a successful allocation, no
                        // sense in continuing the loops.
                    }
                }
            }
        }
        candidates.push(candidate);
    }

    candidates.into_boxed_slice()
}

/// Creates children of the candidate solutions.
fn crossover<RNG: Rng>(a: &CandidateSolution,
                       b: &CandidateSolution,
                       rng: &mut RNG,
                       instance: &Instance)
                       -> (CandidateSolution, CandidateSolution) {
    let mut candidate_a = a.clone();
    let mut candidate_b = b.clone();
    let crossover_point = {
        let timeslot_range = Range::new(0, a.num_timeslots());
        let x = timeslot_range.ind_sample(rng);
        let room_range = Range::new(0, a.num_rooms());
        let y = room_range.ind_sample(rng);
        (x, y)
    };
    // This is a simple 1 point crossover operator.
    for x in 0..crossover_point.0 {
        for y in 0..crossover_point.1 {
            let event_index = a.get_allocation(x, y).map(|allocation| allocation.event_index());
            candidate_a.allocate_event(x, y, event_index, instance);
        }
    }
    for x in crossover_point.0..a.num_timeslots() {
        for y in crossover_point.1..a.num_rooms() {
            let event_index = a.get_allocation(x, y).map(|allocation| allocation.event_index());
            candidate_b.allocate_event(x, y, event_index, instance);
        }
    }
    (candidate_a, candidate_b)
}

/// Pick two random Allocation Slots and switch them "randomly". Not sure this is true mutation in
/// the generic genetic strategy sense, but I think this works for this case.
fn mutate<RNG: Rng>(candidate: &mut CandidateSolution, rng: &mut RNG, instance: &Instance) {
    let timeslot_range = Range::new(0, candidate.num_timeslots());
    let room_range = Range::new(0, candidate.num_rooms());

    let index1 = (timeslot_range.ind_sample(rng), room_range.ind_sample(rng));
    let index2 = (timeslot_range.ind_sample(rng), room_range.ind_sample(rng));
    let (event1, event2) = {
        let allocation1 = candidate.get_allocation_with_index(index1);
        let allocation2 = candidate.get_allocation_with_index(index2);

        let event1 = allocation1.map(|allocation| allocation.event_index());
        let event2 = allocation2.map(|allocation| allocation.event_index());
        (event1, event2)
    };
    // Swap places.
    candidate.allocate_event(index1.0, index1.1, event2, instance);
    candidate.allocate_event(index2.0, index2.1, event1, instance);
}
