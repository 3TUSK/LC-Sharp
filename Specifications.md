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
- `Assembler`: Current implementation of the LC-3 assembler. Converts assembly string code to short integer data, or shortcode.

# GUI
```
lcs -g {program_file} [input_file] [output_file]
```
- Instructions
  - A scrollable view of the LC-3's main memory. The current instruction about to be executed is highlighted.
  - When an instruction is fetched, the view snaps to center on the instruction and highlights it. Exception: TRAP subroutines do not affect this view during execution.
  - When scrolled, we dynamically regenerate the labels.
  - Updates on every instruction
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
  - Set PC: A text view allowing the user to set the PC
  - Write Instruction: A text view allowing the user to assemble an instruction directly into memory.
- Output
  - A read-only text view containing the program's output.
  - The DSR is checked during the main loop and any output is read from DDR and sent to this view.
- Input
  - A text view containing the program's input.
  - The KBSR is checked during the main loop and any input is read from this view and sent to the KBDR.
# CLI
```
lcs -c {program_file} [input_file] [output_file]
```
- In CLI mode, the emulator runs the entire program with no GUI.
- Input
  - If we have an output file, then we read all input from it.
    - If the input file ends before the program is done taking input, then we throw an error.
  - Otherwise, we get it through `ReadKey(true)`, which does not echo key presses (since the program should already do that).
- Output
  - If we have an output file, then we write all output to it.
  - Otherwise, output is printed to console
# Macros
- Custom TRAP Subroutines
# Compiling
- Install Visual Studio Community 2017
- We require a modified GUI library available at [INeedAUniqueUsername/gui.cs](https://github.com/INeedAUniqueUsername/gui.cs)
