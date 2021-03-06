# Research
We researched existing LC-3 programs through GitHub to determine a base for our own work.

- https://justinmeiners.github.io/lc3-vm/
  - This is a detailed explanation of a minimal LC-3 emulator written in 250 lines of C. It links to two LC-3 demo games.
- https://github.com/zzh1996/lc3asm/blob/master/lc3asm.cpp#L63
  - A very simple C++ assembler that parses with regex and implements instructions through the god-class antipattern
- https://github.com/edga/lc3
  - An advanced LC-3 toolset that includes a tiny operating system with functioning interrupts.
  - http://www.cs.utexas.edu/users/fussell/courses/cs310h/simulator/lc3db/index.html#download
- https://github.com/TricksterGuy/complx
  - A comprehensive ~~(and of course complex)~~ LC-3 toolset, including assembler, simulator, debugging facilities, GUI editor and even an auto-grader...
  - Supports all instructions (including interrupts/`RTI`), according to its README
  - PyLC3 binding support
- https://github.com/chiragsakhuja/lc3tools
  - Another LC-3 toolset<!-- from U Teaxs -->, including assembler, simulator and garder.
  - Cross-platform, supports interrupts (according to its README)
