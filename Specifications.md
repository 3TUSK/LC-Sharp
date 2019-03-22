# Classes
- `LC3`: The main module representing the LC3 FSM. Functionality is split into smaller modules according to the von Neumann model.
  - `control`: An instance of the Control module
  - `memory`: An instance of the Memory module
  - `processing`: An instance of the Processing module
  - `Fetch()`: Loads the current instruction into IR and increments the PC.
  - `Execute()`: Executes the instruction code stored in IR by setting the control signals in `control`, `memory`, and `processing`.
- `Control`: Contains the PC and IR
  - `lc3`: A reference to the parent.
  - `pc`: A `ushort` for the PC.
  - `ir`: A `ushort` for the IR.
  - `ldPC()`: Sets the `pc` from `pcmuxout` in the parent's `processing`
  - `gatePC()`: Sets the parent's `bus` to the PC
  - `ldIR()`: Sets the `ir` from the parent's `bus`
- `Processing`: Contains the ALU, address adder, Muxes, registers, and condition codes
- `Memory`: Contains the data of the LC-3's main memory
  - `mar`: A `ushort` for the MAR.
  - `mdr`: A `ushort` for the MDR.
  - `mem`: A `Dictionary<ushort, ushort>` that maps addresses to data.
  - `ldMAR()`: Sets `mar` to the `bus`.
  - `ldMDR()`: Sets `mdr` to the `bus`.
  - `gateMAR()`: Sets the `bus` to `mar`.
  - `gateMDR()`: Sets the `bus` to `mdr`.
  - `memEnR()`: Writes the data of `mem` at the address in `mar` to `mdr`.
  - `memEnW()`: Writes the data of `mdr` to `mem` at the address in `mar`
  - `Read(ushort mar)`: Used by the handler to directly read from memory
  - `Write(ushort mar, ushort mdr)`: Used by the handler to directly write to memory
- `Assembler`: Alex Chen's implementation of an LC-3 assembler. Converts assembly string code to short integer data, or shortcode.
  - `List<InstructionPass> secondPass`: A list of instructions that require a second pass.
  - `Dictionary<string, short> labels`: Maps labels to memory locations
  - `Dictionary<short, string> labelsReverse`: Maps memory locations to labels
  - `Dictionary<string, short> trapLookup`: Maps a TRAP instruction name to its starting location.
  - `HashSet<short> nonInstruction`: Tracks the memory locations used for pure data for debug purpses.
  - `OpLookup ops`: A lookup table for Ops by name and by code (TRAP subroutines use their full code for lookup since they have the same opcode).
  - `short trapVectorIndex`: The index on the TRAP vector table where the starting location of the next declared TRAP instruction will be stored.
  - `AssembleLines(params string[] lines)`: Assembles two passes for an array of lines at the current PC, clearing any instructions previously queued for second passing.
  - `FirstPass()`: Reads through the string code, handling instructions, Directives, and labels. If an instruction needs a second pass, we store it (with contextual `pc` and line `index` info) for second passing later.
  - `SecondPass()`: Handles all instructions stored for second pass, loading the instruction's `pc` and line `index` context info and then assembling it.
  - `Directive(string directive)`: Handles a named directive.
  - `.BLKW`: Skips the `pc` assembly context over a segment of memory locations.
  - `.BREAK`: Records a breakpoint; the GUI will pause execution when the PC reaches this value
  - `.END`: Ends the first pass for the current file, calling the second pass on instructions handled so far and clearing labels.
  - `.FILL`: Sets the data at the current memory location to the given value.
  - `.SCOPE`: Creates a new scope with the given name. Clears the previous scope by calling second pass on instructions handled so far and then appending its name to the labels passed so far.
  - `.STRINGZ`: Places the characters of the given string into a segment of memory locations the last of which will be set to `0`.
  - `.TRAP`: Declares a new TRAP subroutine with the given name, adding the current memory location to the TRAP vector table and adding a new Trap object to the `ops` table. Also creates a new scope with the given name.
# Command-line arguments

## GUI Mode
```
lcs gui {program_file} [input_file] [output_file] - launches LC-Sharp in GUI mode
```
- Instructions
  - A scrollable view of the LC-3's main memory.
  - The current instruction about to be executed is highlighted.
  - When an instruction is fetched, the view snaps to center on the instruction and highlights it. Note that for abstraction purposes, TRAP subroutines do not affect this view during execution.
  - When scrolled, we dynamically regenerate the labels.
  - Updates on every instruction
  - Locations containing breakpoints are prefixed with `*`
- Registers
  - A state listing of the main registers, the condition codes, the PC, and the IR.
  - Updates on every instruction
- Labels
  - A listing of named memory addresses and their data
- Status
  - `Idle`: The program is paused
  - `Run All`: The program is currently running until halted.
  - `Run Step Once`: The program is currently running a single instruction.
  - `Run Step Over`: The program is currently running through a single subroutine call and will pause once the subroutine returns.
  - `Waiting`: The program is waiting to receive input.
  - `Halt`: The program is halted normally.
  - `Error`: The program is halted due to a bug detected by the emulator.
- Buttons
  - Open Program: Opens a program from its source file
  - Save Program: Saves the source of the program to a file
  - Restart: Sets all registers to their default values.
  - Run All: Executes instructions until the program is halted. Waits for input if needed.
  - Run Step Once: Executes the current instruction. Waits for input if needed.
  - Run Step Over: Stores the current address of the PC (one index ahead) and executes instructions until that point is reached. Waits for input if needed.
- Debug
  - Set Scroll: A text view allowing the user to set the current scroll position
  - Set PC: A text view allowing the user to set the PC and reset the FSM's status if it was halted for any reason.
  - Assemble Debug Code: A text view allowing the user to assemble instructions directly into memory.
- Output
  - A read-only text view containing the program's output.
  - The DSR is checked during the main loop and any output is read from DDR and sent to this view.
- Input
  - A text view containing the program's input.
  - The KBSR is checked during the main loop and any input is read from this view and sent to the KBDR.

## CLI
```
lcs cli {program_file} [input_file] [output_file] - launches LC-Sharp in headless (command-line) mode
```
- In CLI mode, the emulator runs the entire program with no GUI.
- Input
  - If we have an input file, then we read all input from it. If it ends before the program is done taking input, then we throw an error.
  - Otherwise, we get it through `ReadKey(true)`, which does not echo key presses (since the program should already do that).
- Output
  - If we have an output file, then we make sure that the program's output matches the file contents.
  - Output is printed to console

# Macros
- Custom TRAP Subroutines support - `.TRAP`

# Compiling (TODO: We need a minimum compilation guide)
- Install Visual Studio Community 2017
- We require a modified GUI library available at [INeedAUniqueUsername/gui.cs](https://github.com/INeedAUniqueUsername/gui.cs)
- Open `LC-Sharp.sln`

# Unicode support
  - In LC-Sharp, the upper nibble (`[15:8]`) of Display Data Register (DDR, `xFE06`) is also reserved. That said, character written in the whole DDR will be displayed on the screen.
    - In the original LC-3 ISA, only the lower nibble (`[7:0]`) has defined behavior; the behavior for `[15:8]` is undefined.
    - This change is backward compatible, since the first unicode plane is "compatible" with ASCII.
    - This effectively expands the range of support characters from ASCII to first 256 unicode planes (i.e. 0x0000 to 0xFFFF).
    - No plan for further unicode support due to word size of 16 bit. 
