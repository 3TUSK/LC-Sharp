# Project Report
LC-Sharp is our project to build a toolset for the Little Computer 3, an Instruction Set Architecture developed for Computer Sciece education. According to our base goals, this toolset will consist of a parser, assembler, and emulator. We have already written high-level plans for the implementation. However, prior to investing our time towards programming, we researched several existing projects with goals similar to ours.

For our research, we used GitHub search since GitHub is where most open-source LC-3 projects are likely to be available. A search for the keyword "lc3" returned several results. A sample of the found projects were written in Verilog or other hardware description language - not very relevant to our software development. Almost all others were written in C or C++. Some of them were incomplete, undocumented, or otherwise not fit for public use. However, we still found a few that were sufficiently developed and comprehensive. We evaluate them below.

# Research

It should not be a surprise that there has been several LC-3 toolkits available online as free and/or open-source software. As such, we may conclude some useful tips on how our project should be structured and programmed.

## Case Study: `complx`

[`complex`](https://github.com/TricksterGuy/complx) is a comprehensive, modular LC-3 toolkit developed by Georgia Tech students and/or faculties. Per its description, it includes LC-3 assembler, simulator (with debugger), graphical editor and an auto-grader for academic usage. The core part of this project is an embedded library called `liblc3`.

### `liblc3`

`liblc3` is a C++ library that "implements" LC-3 as a software-level state machine and provides related facilities (parser, assmebler, runner and debugger). 
 
The parser consists with two headers (`lc3_parser` and `lc3_symbol`), where the former is responsible for tokenization and validation of input lines, and the latter is in charge of "symbol table" (i.e. all labels defined in given input).  
The assembler is entirely written in the header `lc3_assemble`. The entry point is method named `lc3_assemble` which uses conventional two-passes strategy to assemble a source file - first pass is for validating and building symbol/label table, and the second pass is the real assemble step. Each line is passed to `lc3_assemble_one` which in turn uses a giant `switch` to determine the instruction type and properly assembles the line into an instruction.   
The runner splits into three parts: `lc3_runner`, `lc3_os` and `lc3_execute`. Header `lc3_runner` is the entry point where an instance of the state machine (`lc3_state`) is created and pass through all methods before it enters halted state. `lc3_os` holds a pre-defined unsignd short array which contains all necessary data (for example, built-in trap routines implementation according to LC-3 ISA). `lc3_execute` is the place where manipulations to state machine happen; instead of simulating actual control signals, the `lc3_execute` method extracts data from assembled instruction and directly manipulates the current state of `lc3_state` instance, for instance, the `ADD` instruction is handled as directly assigning new value to specified register. Similar to `lc3_assemble`, there is also a giant `switch` to determine the type of instruction and takes correct procedures accordingly.   
Due to the nature of debugging, the debugging facilities in `liblc3` is coupled with `lc3_state` and `lc3_runner`; specifically, `lc3_state` holds all references to breakpoints, watchers, comments, etc., and `lc3_runner` is responsible for calling exposed hooks during the lifecycle of the LC-3 state machine. There is a dedicated `lc3_debug` header that exposes hooks for adding and/or removing breakpoints/data watchers/etc..

Overall, `liblc3` provides a fully functional tool set for assembling, running and testing code written in LC-3 assembly. The implementation is on high-level and is based on a high-level state machine, which means that there are no data flow or control signals. The code style is rather C-like and largely procedure-oriented (especially the use of switch table, which makes them look like "god-method"), despite the fact that C++ supports object-oriented paradigm. 

### `complx`

`complx` is the LC-3 simulator in the complx LC-3 toolkit with a graphical user interface. It is built on top of `liblc3`, and it uses wxWidgets for the GUI part.

### `lc3edit`

`lc3edit` is a graphical editor for editing LC-3 assembly source file, based on wxScintilla and wxFlatNotebook.

### `pylc3`

`pylc3` is a Python binding of `liblc3`, which enables the use of Python rather than C++ to interact with `liblc3`. As its name suggests, it is essentially a thin wrapper around the LC-3 state machine from `liblc3` that exposes all public methods to Python program. 
Currently used by `complx` for its own auto-grader (`pylc3/unittest`) and `comp` (`pylc3/cli`), the command-line interface (CLI) counterpart of graphical `complx`.

## Case Study: `lc3tools`

[`lc3tools`](https://github.com/chiragsakhuja/lc3tools) is another comprehensive LC-3 toolkit developed by students and/or faculties from University of Texas at Austin. According its README, it includes LC-3 assembler, simulator and a grader for academic purpose. Structually, it is split into two part: "backend" (i.e. the LC-3 implementation) and the "frontend" (i.e. user interface, grader, etc.). Unit tests are not discussed because it is not our focus - we are not maintaining other people's project, after all.

### `lc3tools` backend

Similar to `liblc3` used by `complx`, the backend of `lc3tools` is also an LC-3 implemention based on a high-level state machine. All instructions are implemented as direct manipulations to state machine rather than simulation of actual data flows. There is one key differnece - `lc3tools` uses object-oriented design, and it abstracts all instructions as an interface `IInstruction` in which a method `execute` exposes the state machine for manipulation (see also dependency injection).  
Another key difference is the memory model - in `liblc3` there is a `unsigned short[65536]` in the state machine; while in `lc3tools`, there is a vector of `MemEntry` which essentially wraps a 16-bit integer and its location. The former is more compact but wasteful for most of times; the latter approach can save memory footprint, but is a bit hard to access because one would have to iterate over all entries to get content at a specific memory address.  
Other than that, there are not much differences between `liblc3` and `lc3tools` backend; both of them have tokenizers and use two-passes strategy for assembling source files.  

### `lc3tools` frontend

The frontend `lc3tools` contains a graphical user interface and a command-line interface for simulator, as well as a grader. Both grader and CLI are straightforward; the GUI part uses Vue.js and Electron framework.

## Conclusion

 - We may want to avoid re-inventing the wheel because both `liblc3` and `lc3tools` are on high-level to a certain extent.
 - We may want to consider a more object-oriented approach.
   - For one, the assembler and runner can break down to smaller, unit-test-friendly pieces, which should also bring additional flexibilities, for example defining the instruction `1101` or defining new pseudo-instructions.
   - For two, one of our choice is to simulate the real data flow and control signals, which object-oriented design actually fits better (each component can be modeled as object, and thus data flows are interactions between objects).
 - Two-pass assembling seems to be our first choice.
 - We have to find a balance point between "alloacting 65536 addresses from the beginning of the time" and "alloacting new addresses when requested and query it by iterating over all allocated addresses".
 - We may also want a modular project like `complx` so that one can reuse a portion of our project instead of depending on the whole thing.

# Our Goals
We intend to take a unique approach towards implementing the LC-3 emulator. Internally, our project will closely recreate the structure of the LC-3 FSM in code by simulating the use of control signals. That is meant to differentiate the project from others, which focus more on recreating just the visible behavior of the FSM.
