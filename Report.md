# Project Report
LC-Sharp is our project to build a toolset for the Little Computer 3, an Instruction Set Architecture developed for Computer Sciece education. According to our base goals, this toolset will consist of a parser, assembler, and emulator. We have already written high-level plans for the implementation. However, prior to investing our time towards programming, we researched several existing projects with goals similar to ours.

For our research, we used GitHub search since GitHub is where most open-source LC-3 projects are likely to be available. A search for the keyword "lc3" returned several results. A sample of the found projects were written in Verilog or other hardware description language - not very relevant to our software development. Almost all others were written in C or C++. Some of them were incomplete, undocumented, or otherwise not fit for public use. However, we still found a few that were sufficiently developed and comprehensive. We evaluate them below.

# Research

It should not be a surprise that there has been several LC-3 toolkits available online; many of them are free and/or open-source software. As such, we may conclude some useful tips on how our project are structured and programmed.

## Case Study: `complx`

[`complex`](https://github.com/TricksterGuy/complx) is a comprehensive, modular LC-3 toolkit developed by Georgia Tech students and/or faculties. Per its description, it includes LC-3 assembler, simulator (with debugger), graphical editor and an auto-grader for academic usage. The core part of this project is an embedded library called `liblc3`.

### Overview of `liblc3`

`liblc3` is a C++ library that "implements" LC-3 as a software-level state machine and provides related facilities (parser, assmebler, runner and debugger). 

### Overview of `complx`

`complx` is the LC-3 simulator in the complx LC-3 toolkit.

### Overview of `lc3edit`

`lc3edit` is a graphical editor for editing LC-3 assembly source file, based on wxScintilla and wxFlatNotebook.

### `pylc3`

`pylc3` is a Python binding of `liblc3`, which enables the use of Python rather than C++ to access `liblc3`.

# Our Goals
We intend to take a unique approach towards implementing the LC-3 emulator. Internally, our project will closely recreate the structure of the LC-3 FSM in code by simulating the use of control signals. That is meant to differentiate the project from others, which focus more on recreating just the visible behavior of the FSM.
