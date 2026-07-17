; ============================================================================
; z80_sudoku - Sudoku solver in Z80 assembler (sjasmplus), CP/M .COM
;
; A port of the 6502 version.  Same recursive backtracking algorithm with the
; per-candidate row/column/box scan (the naive scan is the right choice on an
; 8-bit CPU - bitmask/MRV variants were measured slower on the 6502).
;
; Console output uses CP/M BDOS function 2; the program returns via warm boot.
; ============================================================================

        DEVICE NOSLOT64K

BDOS    equ 0x0005
CONOUT  equ 2

        ORG 0x0100

start:
        ld   sp, stacktop

        ld   hl, msg_solving
        call print_string

        ld   c, 0               ; n = 0
        call solve
        or   a
        jr   nz, sv_fail

        ld   hl, msg_pass
        call print_string
        call print_board
        jr   sv_done
sv_fail:
        ld   hl, msg_fail
        call print_string
sv_done:
        ld   a, 0x0a            ; trailing newline
        call conout
        jp   0                  ; warm boot back to CP/M

; ----------------------------------------------------------------------------
; conout - print the character in A via BDOS function 2.  Preserves all regs.
; ----------------------------------------------------------------------------
conout:
        push af
        push bc
        push de
        push hl
        ld   e, a
        ld   c, CONOUT
        call BDOS
        pop  hl
        pop  de
        pop  bc
        pop  af
        ret

; print_string - print the nul-terminated string at HL.
print_string:
        push af
ps_loop:
        ld   a, (hl)
        or   a
        jr   z, ps_done
        call conout
        inc  hl
        jr   ps_loop
ps_done:
        pop  af
        ret

; ----------------------------------------------------------------------------
; solve - recursive backtracking
;   entry: C = cell index n (0..81)
;   exit : A = 0 on success, A <> 0 on failure
; ----------------------------------------------------------------------------
solve:
        ld   a, c
        cp   81
        jr   nz, sv_go
        xor  a                  ; reached the end -> solved
        ret
sv_go:
        ld   hl, puzzle
        ld   b, 0
        add  hl, bc             ; HL = puzzle + n
        ld   a, (hl)
        or   a
        jr   z, sv_empty
        inc  c                  ; already assigned -> skip
        jp   solve              ; tail recursion
sv_empty:
        ld   b, 1               ; first candidate digit
sv_loop:
        ld   a, b
        call is_available       ; CY = 1 if the digit is already used
        jr   c, sv_next
        ld   a, b
        ld   hl, puzzle
        ld   d, 0
        ld   e, c
        add  hl, de             ; HL = puzzle + n
        ld   (hl), a            ; tentative assignment
        push bc                 ; save digit (B) and n (C)
        inc  c
        call solve
        pop  bc
        or   a
        jr   z, sv_success
sv_next:
        inc  b
        ld   a, b
        cp   10
        jp   nz, sv_loop
        ld   hl, puzzle         ; every digit failed -> unmake and backtrack
        ld   d, 0
        ld   e, c
        add  hl, de
        ld   (hl), 0
        ld   a, 1               ; non-zero -> failure
        ret
sv_success:
        xor  a
        ret

; ----------------------------------------------------------------------------
; is_available - may digit A be placed in cell C (n)?
;   entry: A = digit, C = n
;   exit : CY = 1 if the digit is already used (not available), CY = 0 if free
;   preserves A, B, C.
; ----------------------------------------------------------------------------
is_available:
        push bc
        ; --- row (9 contiguous cells from the row start) ---
        ld   hl, table_move2row_start
        ld   b, 0
        add  hl, bc
        ld   e, (hl)
        ld   hl, puzzle
        ld   d, 0
        add  hl, de             ; HL = puzzle + row start
        cp   (hl)
        jr   z, ia_r
        inc  hl
        cp   (hl)
        jr   z, ia_r
        inc  hl
        cp   (hl)
        jr   z, ia_r
        inc  hl
        cp   (hl)
        jr   z, ia_r
        inc  hl
        cp   (hl)
        jr   z, ia_r
        inc  hl
        cp   (hl)
        jr   z, ia_r
        inc  hl
        cp   (hl)
        jr   z, ia_r
        inc  hl
        cp   (hl)
        jr   z, ia_r
        inc  hl
        cp   (hl)
        jr   z, ia_r
        jr   ia_row_ok          ; no conflict in row
ia_r:   jp   ia_used            ; (trampoline: ia_used is > 127 bytes away)
ia_row_ok:
        ; --- column (9 cells, stride 9) ---
        ld   hl, table_move2x
        ld   b, 0
        add  hl, bc
        ld   e, (hl)
        ld   hl, puzzle
        ld   d, 0
        add  hl, de             ; HL = puzzle + column
        ld   de, 9
        cp   (hl)
        jr   z, ia_c
        add  hl, de
        cp   (hl)
        jr   z, ia_c
        add  hl, de
        cp   (hl)
        jr   z, ia_c
        add  hl, de
        cp   (hl)
        jr   z, ia_c
        add  hl, de
        cp   (hl)
        jr   z, ia_c
        add  hl, de
        cp   (hl)
        jr   z, ia_c
        add  hl, de
        cp   (hl)
        jr   z, ia_c
        add  hl, de
        cp   (hl)
        jr   z, ia_c
        add  hl, de
        cp   (hl)
        jr   z, ia_c
        jr   ia_col_ok          ; no conflict in column
ia_c:   jp   ia_used
ia_col_ok:
        ; --- box (offsets 0,1,2,9,10,11,18,19,20); ia_used now in jr range ---
        ld   hl, table_move2box_start
        ld   b, 0
        add  hl, bc
        ld   e, (hl)
        ld   hl, puzzle
        ld   d, 0
        add  hl, de             ; HL = puzzle + box start
        cp   (hl)
        jr   z, ia_used
        inc  hl
        cp   (hl)
        jr   z, ia_used
        inc  hl
        cp   (hl)
        jr   z, ia_used
        ld   de, 7
        add  hl, de
        cp   (hl)
        jr   z, ia_used
        inc  hl
        cp   (hl)
        jr   z, ia_used
        inc  hl
        cp   (hl)
        jr   z, ia_used
        ld   de, 7
        add  hl, de
        cp   (hl)
        jr   z, ia_used
        inc  hl
        cp   (hl)
        jr   z, ia_used
        inc  hl
        cp   (hl)
        jr   z, ia_used
        pop  bc                 ; available
        or   a                  ; CY = 0
        ret
ia_used:
        pop  bc
        scf                     ; CY = 1
        ret

; ----------------------------------------------------------------------------
; Board printing
; ----------------------------------------------------------------------------
print_element:                  ; entry: A = cell index
        push hl
        push bc
        ld   c, a               ; C = index
        ld   a, 0x20            ; space
        call conout
        ld   hl, puzzle
        ld   b, 0
        add  hl, bc             ; HL = puzzle + index
        ld   a, (hl)
        or   a
        jr   z, pe_blank
        add  a, 0x30            ; digit -> ASCII
        call conout
        jr   pe_fin
pe_blank:
        ld   a, 0x2d            ; '-'
        call conout
pe_fin:
        ld   a, 0x20            ; space
        call conout
        pop  bc
        pop  hl
        ret

print_box_break_vertical:
        ld   a, 0x7c            ; '|'
        call conout
        ret

print_box_break_horizontal:
        ld   hl, msg_hbreak
        call print_string
        ret

print_newline:
        ld   a, 0x0d
        call conout
        ld   a, 0x0a
        call conout
        ret

print_board:
        call print_newline
        call print_newline
        call print_box_break_horizontal
        call print_newline
        ld   c, 0               ; index
pb_loop:
        ld   a, c
        call print_element
        inc  c                  ; advance to next index
        ; horizontal box break at the top of each band of three rows
        ld   hl, table_move2box_y
        ld   b, 0
        add  hl, bc
        ld   a, (hl)
        or   a
        jr   nz, pb_boxh
        ld   hl, table_move2x
        ld   b, 0
        add  hl, bc
        ld   a, (hl)
        or   a
        jr   nz, pb_boxh
        call print_newline
        call print_box_break_horizontal
pb_boxh:
        ld   hl, table_move2x
        ld   b, 0
        add  hl, bc
        ld   a, (hl)
        or   a
        jr   nz, pb_newl
        call print_newline
        jr   pb_cont
pb_newl:
        ld   hl, table_move2box_x
        ld   b, 0
        add  hl, bc
        ld   a, (hl)
        or   a
        jr   nz, pb_cont
        call print_box_break_vertical
pb_cont:
        ld   a, c
        cp   81
        jp   nz, pb_loop
        ret

; ----------------------------------------------------------------------------
; Strings (nul-terminated)
; ----------------------------------------------------------------------------
msg_solving: db "Solving puzzle: ", 0
msg_pass:    db "pass", 0
msg_fail:    db "fail", 0
msg_hbreak:  db " --------+---------+--------", 0

; ----------------------------------------------------------------------------
; The puzzle (clues).  Lives in the TPA (RAM) so it is mutated in place.
; "World's hardest sudoku".
; ----------------------------------------------------------------------------
puzzle:
        db 8,0,0,0,0,0,0,0,0
        db 0,0,3,6,0,0,0,0,0
        db 0,7,0,0,9,0,2,0,0
        db 0,5,0,0,0,7,0,0,0
        db 0,0,0,0,4,5,7,0,0
        db 0,0,0,1,0,0,0,3,0
        db 0,0,1,0,0,0,0,6,8
        db 0,0,8,5,0,0,0,1,0
        db 0,9,0,0,0,0,4,0,0

; ----------------------------------------------------------------------------
; Lookup tables.  The three used while printing get a trailing sentinel so the
; index-81 read at the end of the board loop is defined (matches the 6502).
; ----------------------------------------------------------------------------
table_move2x:
        db 0,1,2,3,4,5,6,7,8
        db 0,1,2,3,4,5,6,7,8
        db 0,1,2,3,4,5,6,7,8
        db 0,1,2,3,4,5,6,7,8
        db 0,1,2,3,4,5,6,7,8
        db 0,1,2,3,4,5,6,7,8
        db 0,1,2,3,4,5,6,7,8
        db 0,1,2,3,4,5,6,7,8
        db 0,1,2,3,4,5,6,7,8
        db 0

table_move2box_x:
        db 0,1,2,0,1,2,0,1,2
        db 0,1,2,0,1,2,0,1,2
        db 0,1,2,0,1,2,0,1,2
        db 0,1,2,0,1,2,0,1,2
        db 0,1,2,0,1,2,0,1,2
        db 0,1,2,0,1,2,0,1,2
        db 0,1,2,0,1,2,0,1,2
        db 0,1,2,0,1,2,0,1,2
        db 0,1,2,0,1,2,0,1,2
        db 0

table_move2box_y:
        db 0,0,0,0,0,0,0,0,0
        db 1,1,1,1,1,1,1,1,1
        db 2,2,2,2,2,2,2,2,2
        db 0,0,0,0,0,0,0,0,0
        db 1,1,1,1,1,1,1,1,1
        db 2,2,2,2,2,2,2,2,2
        db 0,0,0,0,0,0,0,0,0
        db 1,1,1,1,1,1,1,1,1
        db 2,2,2,2,2,2,2,2,2
        db 0

table_move2row_start:
        db 0,0,0,0,0,0,0,0,0
        db 9,9,9,9,9,9,9,9,9
        db 18,18,18,18,18,18,18,18,18
        db 27,27,27,27,27,27,27,27,27
        db 36,36,36,36,36,36,36,36,36
        db 45,45,45,45,45,45,45,45,45
        db 54,54,54,54,54,54,54,54,54
        db 63,63,63,63,63,63,63,63,63
        db 72,72,72,72,72,72,72,72,72

table_move2box_start:
        db 0,0,0,3,3,3,6,6,6
        db 0,0,0,3,3,3,6,6,6
        db 0,0,0,3,3,3,6,6,6
        db 27,27,27,30,30,30,33,33,33
        db 27,27,27,30,30,30,33,33,33
        db 27,27,27,30,30,30,33,33,33
        db 54,54,54,57,57,57,60,60,60
        db 54,54,54,57,57,57,60,60,60
        db 54,54,54,57,57,57,60,60,60

prog_end:                       ; end of code + data (this is the saved image)

; ----------------------------------------------------------------------------
; Stack: grows down from stacktop, which sits in TPA RAM just past the image
; (not part of the saved binary).  Plenty for ~60 levels of recursion.
; ----------------------------------------------------------------------------
        ds 512
stacktop:

        SAVEBIN "sudoku.com", 0x0100, prog_end-0x0100
        SAVEHEX "sudoku.hex", 0x0100, prog_end-0x0100
