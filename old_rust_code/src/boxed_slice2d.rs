use std::default::Default;
use std::ops::{Index, IndexMut};

#[derive(Clone)]
pub struct BoxedSlice2D<T: Default> {
    data: Box<[T]>,
    width: usize,
    height: usize,
}

impl<T: Default> BoxedSlice2D<T> {
    pub fn new(width: usize, height: usize) -> Self {
        let capacity = width * height;
        let mut data = Vec::with_capacity(capacity);
        for _ in 0..capacity {
            data.push(Default::default());
        }

        BoxedSlice2D {
            data: data.into_boxed_slice(),
            width: width,
            height: height,
        }
    }

    pub fn width(&self) -> usize {
        self.width
    }

    pub fn height(&self) -> usize {
        self.height
    }
}

impl<T: Default> Index<(usize, usize)> for BoxedSlice2D<T> {
    type Output = T;
    fn index(&self, index: (usize, usize)) -> &T {
        let x = index.0;
        let y = index.1;
        &self.data[self.width * y + x]
    }
}

impl<T: Default> IndexMut<(usize, usize)> for BoxedSlice2D<T> {
    fn index_mut(&mut self, index: (usize, usize)) -> &mut T {
        let x = index.0;
        let y = index.1;
        &mut self.data[self.width * y + x]
    }
}
