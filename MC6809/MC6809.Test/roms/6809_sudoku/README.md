# 6809_sudoku

Sudoku solver in Motorola 6809 assembler, built with
[lwtools](http://www.lwtools.ca/) (`lwasm` + `lwlink`).

Ported from the 6502 version (`6502_sudoku`). It uses the same recursive
backtracking algorithm and produces identical console output, rewritten for the
6809.

## Building

With lwtools installed (lwasm/lwlink on the PATH):

    make

This produces `sudoku.bin`, a 4 KiB raw ROM image spanning `$F000-$FFFF` with
the 6809 reset vector at `$FFFE`. A listing (`sudoku.lst`) and link map
(`sudoku.map`) are produced too.

By hand:

    lwasm  --format=obj --output=sudoku.o --list=sudoku.lst sudoku.asm
    lwlink --format=raw --script=sudoku.link --map=sudoku.map --output=sudoku.bin sudoku.o

## Running

The program expects a memory-mapped console matching the 6502 build: writing a
byte to `CONOUT` (`$E001`) emits a character. Map the 4 KiB image at `$F000`,
take reset from `$FFFE`, and provide a CONOUT sink. On start it prints:

    Solving puzzle: pass

followed by the solved grid.

## Notes on the port

The 6809 has two index registers, two stack pointers and 16-bit pointers, so
several 6502 workarounds disappear:

- The software data stack (`stack.inc`) is gone. The cell index `n` is passed
  to `solve` in `B`, and recursion uses the hardware `S` stack directly.
- `is_available` and its row/column/box helpers return their result in the `Z`
  condition code and preserve `A` (the digit) and `B` (the cell index).
- The board lives in RAM at `$0200`. The clues are copied there from a ROM
  table (`puzzle_init`) at reset, so the image is ROM-clean.

Memory map:

    $0200-$3FFF  RAM (board + system stack, growing down from $4000)
    $F000-$FFEF  code + read-only tables
    $FFF0-$FFFF  6809 vectors (reset at $FFFE)
