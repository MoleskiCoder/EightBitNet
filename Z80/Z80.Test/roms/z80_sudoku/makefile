# Makefile for the Z80 (CP/M) sudoku port - sjasmplus
#
# sjasmplus writes sudoku.com via the SAVEBIN directive at the end of the
# source.  Run the resulting .COM under CP/M (or a CP/M emulator).

ASM = sjasmplus

all: sudoku.com

sudoku.com: sudoku.asm
	$(ASM) sudoku.asm

.PHONY: clean
clean:
	rm -fv *.com *.hex *.lst *.map *.sym
