# Makefile for the 6809 sudoku port (lwtools)
#
# lwlink's "raw" target only honours a single load address and concatenates
# sections, so it cannot zero-fill the gap up to the 6809 vector table at
# $FFF0. We therefore assemble the fixed ROM directly with lwasm in absolute
# (org) mode, which produces a gap-filled 4 KiB image with vectors at $FFFE.
#
# Outputs:
#   sudoku.bin - flat 4 KiB raw ROM image (load at $F000)
#   sudoku.hex - Intel HEX of the same image (addresses embedded)
#   sudoku.lst - listing (address per source line)
#   sudoku.map - symbol -> address map (for debuggers / monitors)

LWASM = lwasm

all: sudoku.bin sudoku.hex

sudoku.bin: sudoku.asm io.inc
	$(LWASM) --format=raw  --output=sudoku.bin --list=sudoku.lst --map=sudoku.map sudoku.asm

sudoku.hex: sudoku.asm io.inc
	$(LWASM) --format=ihex --output=sudoku.hex sudoku.asm

.PHONY: clean
clean:
	rm -fv *.bin *.hex *.lst *.map *.o
