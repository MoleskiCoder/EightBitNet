# z80_sudoku

Sudoku solver in Z80 assembler, assembled with
[sjasmplus](https://github.com/z00m128/sjasmplus) and packaged as a CP/M `.COM`.

A port of the 6502 version (`6502_sudoku`). It uses the same recursive
backtracking algorithm with the per-candidate row/column/box scan. (On 8-bit
CPUs the naive scan beats bitmask/MRV variants - that was measured on the
6502 - so this port keeps the scan.)

## Building

    make            # or: sjasmplus sudoku.asm

Produces `sudoku.com` (CP/M binary) and `sudoku.hex` (Intel HEX, load address $0100) via the `SAVEBIN`/`SAVEHEX` directives.

## Running

It's a standard CP/M transient program (`ORG $0100`). Run it under CP/M or a
CP/M emulator; it prints via BDOS function 2 and returns via warm boot:

    Solving puzzle: pass

followed by the solved grid.

## Notes

- Console output: CP/M BDOS function 2 (`LD E,char / LD C,2 / CALL 5`).
- The board and tables live in the TPA (RAM), so the puzzle is mutated in place.
- `n` (the cell index) is passed to `solve` in `C`; recursion uses the hardware
  stack. `is_available` returns its result in the carry flag and preserves
  `A`, `B`, `C`.
- The row, column and box scans are fully unrolled (as in the 6502/6809 ports).
