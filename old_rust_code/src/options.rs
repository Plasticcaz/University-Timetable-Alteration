use toml::*;

use allocation::*;
use genetic::*;

pub struct Options {
    pub strategy: Box<AllocationStrategy>,
}

/// Attempt to load an options file
pub fn load_options(path: &str) -> Result<Options, String> {
    let table = match load_toml_table(path) {
        Some(table) => table,
        None => return Err(format!("Failed to load a toml file from {}.", path)),
    };

    let strategy_id = if let Some(id) = table.lookup("strategy.name") {
        id.as_str()
    } else {
        return Err(format!("strategy.name not specified in {}", path));
    };

    let strategy = match strategy_id {
        Some("genetic") => load_genetic_strategy(&table),
        _ => return Err("Unrecognized strategy specified.".to_owned()),
    };

    Ok(Options { strategy: strategy })
}

/// Load the Toml file, and return a table of all the entries in it.
fn load_toml_table(path: &str) -> Option<Value> {
    use std::fs::File;
    use std::io::Read;
    let mut file = match File::open(path) {
        Ok(file) => file,
        Err(_) => return None,
    };
    let file_length = file.metadata().expect("options.toml: File has no metadata!").len() as usize;
    let mut contents: Vec<u8> = Vec::with_capacity(file_length + 1);
    file.read_to_end(&mut contents).expect("options.toml: Failed to read exact amount of bytes.");
    let contents = String::from_utf8(contents)
        .expect("options.toml: Could not build a string from bytes.");
    let mut parser = Parser::new(&contents);
    Some(Value::Table(parser.parse().unwrap()))
}

fn load_genetic_strategy(table: &Value) -> Box<AllocationStrategy> {
    let generations = match table.lookup("genetic.generations") {
        Some(&Value::Integer(value)) => value as usize,
        _ => 6, // Default Value
    };
    let tournament_size = match table.lookup("genetic.tournament_size") {
        Some(&Value::Integer(value)) => value as usize,
        _ => 50, // Default Value
    };
    let elite_number = match table.lookup("genetic.elite_number") {
        Some(&Value::Integer(value)) => value as usize,
        _ => 2, // Default Value
    };
    let mutation_weight = match table.lookup("genetic.mutation_weight") {
        Some(&Value::Integer(value)) => value as u32,
        _ => 80, // Default Value
    };
    let candidates_size = match table.lookup("genetic.candidates_size") {
        Some(&Value::Integer(value)) => value as usize,
        _ => 100, // Default Value
    };
    // TODO(zac): Specification of seed in options.toml
    let seed = generate_random_seed();
    let strategy = GeneticStrategy::new(generations,
                                        candidates_size,
                                        tournament_size,
                                        elite_number,
                                        mutation_weight,
                                        seed);
    Box::new(strategy)
}

fn generate_random_seed() -> [usize; 4] {
    use rand::*;

    let mut rng = StdRng::new().expect("Failed to create a RNG to generate a seed.");

    let seed = rng.gen();
    print_seed(&seed);
    seed
}

fn print_seed(seed: &[usize; 4]) {
    print!("seed: [");
    for i in 0..seed.len() {
        print!("{}", seed[i]);
        if i != (seed.len() - 1) {
            print!(", ");
        }
    }
    println!("]");
}
