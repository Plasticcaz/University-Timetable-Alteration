# University Timetable Alteration #

### What is this? ###

This is a repository to house work done for my Honours dissertation on the University Timetabling problem.

### How do I get set up? ###

The project is written in C#, and developed using Visual Studio 2015 Community Edition on Windows. You can find the visual studio project in HonoursCS.

The original attempt was to program this project in a relatively new language called Rust, and you can find the old rust project in the "old_rust_code" folder.
I moved on to use C# instead, because of more advanced tooling, and less strict rules (due to C# using Garbage Collection). This allowed me to write code at a
much faster rate.

NOTE: Please be aware that the TestApp project currently assumes that it's working directory contains the test_data folder. You may need to manually change the working directory of TestApp to the folder "test_data" is in. "output.log" will also be placed in the working directory as the program runs.