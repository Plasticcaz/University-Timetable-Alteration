/// a macro to keep track of the time it took to execute a statement or group of statements.
macro_rules! time {
    ( $( $statement:stmt;)* ) => {
        {
            let now = std::time::Instant::now();
            $(
                $statement;
            )*
            now.elapsed()
        }
    };
}
