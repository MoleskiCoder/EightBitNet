; ============================================================================
; 6809_sudoku - Sudoku solver in Motorola 6809 assembler (lwtools: lwasm)
;
; A port of the 6502 version to the 6809. Recursive backtracking solver, same
; algorithm and same console output.
;
; Console I/O matches the 6502 build (write a byte to CONOUT to print it).
; ROM at $F000-$FFFF, 6809 vectors at $FFF0-$FFFF, working RAM from $0200.
; ============================================================================

; ---- constants -------------------------------------------------------------

UNASSIGNED  equ 0
BOARD_SIZE  equ 9
CELL_COUNT  equ BOARD_SIZE*BOARD_SIZE   ; 81
STACKTOP    equ $4000                   ; hardware stack top (RAM $0200-$3FFF)

; ---- working RAM (uninitialised) -------------------------------------------
puzzle      equ   $0200                 ; live 9x9 board in RAM ($0200..$0250)

; ---- code ------------------------------------------------------------------
            org   $F000
            include "io.inc"

; ----------------------------------------------------------------------------
; reset - entry point
; ----------------------------------------------------------------------------
reset:
            orcc  #$50                  ; mask IRQ and FIRQ
            lds   #STACKTOP             ; set up the hardware stack
	    jsr   io_initialise_acia

            ; copy the clues from ROM into writable RAM
            ldx   #puzzle_init
            ldy   #puzzle
            ldb   #CELL_COUNT
rinit:
            lda   ,x+
            sta   ,y+
            decb
            bne   rinit

            jsr   io_outstr
            fcc   "Solving puzzle: "
            fcb   0

            clrb                        ; n = 0
            jsr   solve
            tsta
            bne   rfail

            jsr   io_outstr
            fcc   "pass"
            fcb   0
            jsr   print_board
            bra   rdone
rfail:
            jsr   io_outstr
            fcc   "fail"
            fcb   0
rdone:
            lda   #$0a                  ; trailing newline
            jsr   io_outchr
halt:
            bra   halt

; ----------------------------------------------------------------------------
; Inlined availability checks (macros). The row/column/box base pointers are
; precomputed once per cell and held on the stack (see solve), so each check
; is a single "ldx n,s" plus the nine comparisons. A = candidate digit.
; A conflict branches to _loop_continue; no conflict falls through.  The row
; check keeps a local 'used@' trampoline (its early compares sit >127 bytes
; from _loop_continue); column and box branch to _loop_continue directly.
;     0,s = row pointer   2,s = column pointer   4,s = box pointer
; ----------------------------------------------------------------------------
is_used_in_row macro
            ldx   ,s                    ; puzzle + row start
            cmpa  ,x
            beq   used@
            cmpa  1,x
            beq   used@
            cmpa  2,x
            beq   used@
            cmpa  3,x
            beq   used@
            cmpa  4,x
            beq   used@
            cmpa  5,x
            beq   used@
            cmpa  6,x
            beq   used@
            cmpa  7,x
            beq   used@
            cmpa  8,x
            beq   used@
            bra   pass@
used@       bra   _loop_continue   ; in range: _loop_continue is < 127 bytes ahead
pass@
            endm

is_used_in_column macro
            ldx   2,s                   ; puzzle + column
            cmpa  ,x
            beq   _loop_continue
            cmpa  (BOARD_SIZE*1),x
            beq   _loop_continue
            cmpa  (BOARD_SIZE*2),x
            beq   _loop_continue
            cmpa  (BOARD_SIZE*3),x
            beq   _loop_continue
            cmpa  (BOARD_SIZE*4),x
            beq   _loop_continue
            cmpa  (BOARD_SIZE*5),x
            beq   _loop_continue
            cmpa  (BOARD_SIZE*6),x
            beq   _loop_continue
            cmpa  (BOARD_SIZE*7),x
            beq   _loop_continue
            cmpa  (BOARD_SIZE*8),x
            beq   _loop_continue
            endm

is_used_in_box macro
            ldx   4,s                   ; puzzle + box start
            cmpa  ,x
            beq   _loop_continue
            cmpa  1,x
            beq   _loop_continue
            cmpa  2,x
            beq   _loop_continue
            cmpa  BOARD_SIZE,x
            beq   _loop_continue
            cmpa  BOARD_SIZE+1,x
            beq   _loop_continue
            cmpa  BOARD_SIZE+2,x
            beq   _loop_continue
            cmpa  (BOARD_SIZE*2),x
            beq   _loop_continue
            cmpa  (BOARD_SIZE*2)+1,x
            beq   _loop_continue
            cmpa  (BOARD_SIZE*2)+2,x
            beq   _loop_continue
            endm

is_available macro
            is_used_in_row
            is_used_in_column
            is_used_in_box
            endm

; ----------------------------------------------------------------------------
; solve - recursive backtracking
;   entry: B = cell index n (0..81)
;   exit : A = 0 on success, A <> 0 on failure
;
; For each empty cell the row/column/box base pointers are computed ONCE and
; held on the stack; they do not depend on the candidate digit, so the inner
; digit loop avoids recomputing them. Per-cell stack frame:
;     0,s  row pointer    (puzzle + row start)
;     2,s  column pointer (puzzle + column)
;     4,s  box pointer    (puzzle + box start)
;     6,s  n              (cell index)
; ----------------------------------------------------------------------------
solve:
            cmpb  #CELL_COUNT
            bne   _not_finished
            clra
            rts				; success!

_not_finished:
            ldx   #puzzle
            lda   b,x                   ; A = puzzle[n]
            beq   sv_empty              ; empty cell -> try to fill it
            incb                        ; already assigned -> skip
            bra   solve                 ; tail recursion

sv_empty:
            pshs  b                     ; 0,s = n
            ldx   #table_move2box_start
            ldb   b,x                   ; B = box start  (B still = n)
            ldx   #puzzle
            abx
            pshs  x                     ; push box pointer
            ldb   2,s                   ; B = n
            ldx   #table_move2x
            ldb   b,x                   ; B = column
            ldx   #puzzle
            abx
            pshs  x                     ; push column pointer
            ldb   4,s                   ; B = n
            ldx   #table_move2row_start
            ldb   b,x                   ; B = row start
            ldx   #puzzle
            abx
            pshs  x                     ; push row pointer
            lda   #1                    ; first candidate digit

_loop:
            is_available                ; A = digit; conflict -> _loop_continue

            ; available: place the digit and recurse
            ldb   6,s                   ; B = n
            ldx   #puzzle
            sta   b,x                   ; puzzle[n] = digit
            pshs  a                     ; 0,s = digit ; n now at 7,s
            ldb   7,s                   ; B = n
            incb                        ; recurse on n+1

            jsr   solve

            tsta
            beq   _return_true

            puls  a                     ; A = digit ; frame restored
            ; fall through to try the next digit
_loop_continue:
            inca
            cmpa  #BOARD_SIZE+1
            lbne  _loop                ; _loop is > 127 bytes back

			               ; failure, unmake & try again
            ldb   6,s
            ldx   #puzzle
            clr   b,x                   ; puzzle[n] = UNASSIGNED

_return_false:
            leas  7,s                   ; drop the per-cell frame (row,col,box,n)
            lda   #1                    ; non-zero -> failure
            rts

_return_true:
            leas  8,s                   ; drop saved digit + per-cell frame
            clra                        ; success
            rts

; ----------------------------------------------------------------------------
; Board printing
; ----------------------------------------------------------------------------
print_board_element:                    ; entry: B = cell index
            lda   #$20                  ; space
            jsr   io_outchr
            ldx   #puzzle
            lda   b,x                   ; A = puzzle[index]
            beq   pbe_blank
            adda  #$30                  ; digit -> ASCII
            jsr   io_outchr
            bra   pbe_done
pbe_blank:
            lda   #$2d                  ; '-'
            jsr   io_outchr
pbe_done:
            lda   #$20                  ; space
            jsr   io_outchr
            rts

print_box_break_vertical:
            lda   #$7c                  ; '|'
            jsr   io_outchr
            rts

print_box_break_horizontal:
            jsr   io_outstr
            fcc   " --------+---------+--------"
            fcb   0
            rts

print_newline:
            lda   #$0d                  ; CR
            jsr   io_outchr
            lda   #$0a                  ; LF
            jsr   io_outchr
            rts

print_board:
            jsr   print_newline
            jsr   print_newline
            jsr   print_box_break_horizontal
            jsr   print_newline

            clrb                        ; index = 0
pb_loop:
            jsr   print_board_element   ; B = index
            incb                        ; advance to next index

            ldx   #table_move2box_y
            lda   b,x
            bne   pb_boxh
            ldx   #table_move2x
            lda   b,x
            bne   pb_boxh
            jsr   print_newline
            jsr   print_box_break_horizontal
pb_boxh:
            ldx   #table_move2x
            lda   b,x
            bne   pb_newl
            jsr   print_newline
            bra   pb_cont
pb_newl:
            ldx   #table_move2box_x
            lda   b,x
            bne   pb_cont
            jsr   print_box_break_vertical
pb_cont:
            cmpb  #CELL_COUNT
            bne   pb_loop
            rts

; ----------------------------------------------------------------------------
; Read-only data
; ----------------------------------------------------------------------------

puzzle_init:
            fcb   8,0,0,0,0,0,0,0,0
            fcb   0,0,3,6,0,0,0,0,0
            fcb   0,7,0,0,9,0,2,0,0
            fcb   0,5,0,0,0,7,0,0,0
            fcb   0,0,0,0,4,5,7,0,0
            fcb   0,0,0,1,0,0,0,3,0
            fcb   0,0,1,0,0,0,0,6,8
            fcb   0,0,8,5,0,0,0,1,0
            fcb   0,9,0,0,0,0,4,0,0

table_move2x:
            fcb   0,1,2,3,4,5,6,7,8
            fcb   0,1,2,3,4,5,6,7,8
            fcb   0,1,2,3,4,5,6,7,8
            fcb   0,1,2,3,4,5,6,7,8
            fcb   0,1,2,3,4,5,6,7,8
            fcb   0,1,2,3,4,5,6,7,8
            fcb   0,1,2,3,4,5,6,7,8
            fcb   0,1,2,3,4,5,6,7,8
            fcb   0,1,2,3,4,5,6,7,8
            fcb   0

table_move2y:
            fcb   0,0,0,0,0,0,0,0,0
            fcb   1,1,1,1,1,1,1,1,1
            fcb   2,2,2,2,2,2,2,2,2
            fcb   3,3,3,3,3,3,3,3,3
            fcb   4,4,4,4,4,4,4,4,4
            fcb   5,5,5,5,5,5,5,5,5
            fcb   6,6,6,6,6,6,6,6,6
            fcb   7,7,7,7,7,7,7,7,7
            fcb   8,8,8,8,8,8,8,8,8

table_move2box_x:
            fcb   0,1,2,0,1,2,0,1,2
            fcb   0,1,2,0,1,2,0,1,2
            fcb   0,1,2,0,1,2,0,1,2
            fcb   0,1,2,0,1,2,0,1,2
            fcb   0,1,2,0,1,2,0,1,2
            fcb   0,1,2,0,1,2,0,1,2
            fcb   0,1,2,0,1,2,0,1,2
            fcb   0,1,2,0,1,2,0,1,2
            fcb   0,1,2,0,1,2,0,1,2
            fcb   0

table_move2box_y:
            fcb   0,0,0,0,0,0,0,0,0
            fcb   1,1,1,1,1,1,1,1,1
            fcb   2,2,2,2,2,2,2,2,2
            fcb   0,0,0,0,0,0,0,0,0
            fcb   1,1,1,1,1,1,1,1,1
            fcb   2,2,2,2,2,2,2,2,2
            fcb   0,0,0,0,0,0,0,0,0
            fcb   1,1,1,1,1,1,1,1,1
            fcb   2,2,2,2,2,2,2,2,2
            fcb   0

table_move2row_start:
            fcb   0,0,0,0,0,0,0,0,0
            fcb   9,9,9,9,9,9,9,9,9
            fcb   18,18,18,18,18,18,18,18,18
            fcb   27,27,27,27,27,27,27,27,27
            fcb   36,36,36,36,36,36,36,36,36
            fcb   45,45,45,45,45,45,45,45,45
            fcb   54,54,54,54,54,54,54,54,54
            fcb   63,63,63,63,63,63,63,63,63
            fcb   72,72,72,72,72,72,72,72,72

table_move2box_start:
            fcb   0,0,0,3,3,3,6,6,6
            fcb   0,0,0,3,3,3,6,6,6
            fcb   0,0,0,3,3,3,6,6,6
            fcb   27,27,27,30,30,30,33,33,33
            fcb   27,27,27,30,30,30,33,33,33
            fcb   27,27,27,30,30,30,33,33,33
            fcb   54,54,54,57,57,57,60,60,60
            fcb   54,54,54,57,57,57,60,60,60
            fcb   54,54,54,57,57,57,60,60,60
            
	    zmb   $FFF0-*           ; pad ROM up to the vector table
            
            fdb   halt          ; $FFF0 reserved
            fdb   halt          ; $FFF2 SWI3
            fdb   halt          ; $FFF4 SWI2
            fdb   halt          ; $FFF6 FIRQ
            fdb   halt          ; $FFF8 IRQ
            fdb   halt          ; $FFFA SWI
            fdb   halt          ; $FFFC NMI
            fdb   reset         ; $FFFE RESET