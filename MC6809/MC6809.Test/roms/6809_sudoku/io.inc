; io.inc - console I/O for the 6809 sudoku port
;
; Memory-mapped console, matching the 6502 build: writing a byte to 6850 ACIA
; emits a character.

UART        equ $A000
RECEV       equ UART+1
TRANS       equ UART+1
USTAT       equ UART
UCTRL       equ UART

ACIA_RESET  equ $03             ; master reset (CR1:CR0 = 11)
ACIA_CFG    equ $15             ; div-16, 8 data / no parity / 1 stop, no interrupts


io_initialise_acia:
            lda #ACIA_RESET    ; master reset must precede configuration
            sta UCTRL
            lda #ACIA_CFG      ; DIV16 CLOCK -> 7372800 / 4 / 16 = 115200 baud
            sta UCTRL
            rts

; io_waitacia - wait for the ACIA to become ready
io_waitacia:
            pshs a
io_wrwait:  lda  USTAT
            bita #2
            beq  io_wrwait
            puls a
            rts

; io_outchr - print the character held in A
io_outchr:
            bsr  io_waitacia        
            sta  TRANS           
            rts                  

; io_outstr - print the nul-terminated string that immediately follows the
;             "jsr io_outstr" instruction, then resume just past the nul.
;             (Inline-string trick, as used in the 6502 original.)
io_outstr:
            puls  x                 ; X = return address = first byte of string
ios_loop:
            lda   ,x+
            beq   ios_done
            bsr   io_outchr
            bra   ios_loop
ios_done:
            pshs  x                 ; resume after the terminating nul
            rts
