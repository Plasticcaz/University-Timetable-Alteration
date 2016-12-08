use std;

/// Construct a vector of items that contain every number in the range.
pub fn vec_from_range(range: std::ops::Range<usize>) -> Vec<usize> {
    let capacity = range.end - range.start;
    let mut v = Vec::with_capacity(capacity);
    for i in range {
        v.push(i);
    }
    v
}
