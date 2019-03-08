;------------------------------------------------------------------
			.ORIG x0400
			.TRAP GETC
START		ST		R7, SaveR7
			JSR		SaveReg
		
Input		JSR		ReadChar
			ADD		R2, R0, #0		; Move char to R2 for writing
		
			JSR		RestoreReg
			LD		R7, SaveR7
			RET						; JMP R7 terminates
									; the TRAP routine
SaveR7		.FILL	x0000

ReadChar	LDI		R3, KBSR
			BRzp	ReadChar
			LDI		R0, KBDR
			RET
KBSR		.FILL	xFE00
KBDR		.FILL	xFE02

SaveReg		ST		R1, SaveR1
			ST		R2, SaveR2
			ST		R3, SaveR3
			ST		R4, SaveR4
			ST		R5, SaveR5
			ST		R6, SaveR6
			RET

RestoreReg	LD		R1, SaveR1
			LD		R2, SaveR2
			LD		R3, SaveR3
			LD		R4, SaveR4
			LD		R5, SaveR5
			LD		R6, SaveR6
			RET			
SaveR1		.FILL	x0000
SaveR2		.FILL	x0000
SaveR3		.FILL	x0000
SaveR4		.FILL	x0000
SaveR5		.FILL	x0000
SaveR6		.FILL	x0000
			.END
;------------------------------------------------------------------
			.ORIG	x0430		; System call starting address
			.TRAP	OUT
			ST		R1, SaveR1	; R1 will be used to poll the DSR
								; hardware
								
; Write the character
TryWrite	LDI		R1, DSR		; Get status
			BRzp	TryWrite	; Bit 15 on says display is ready
WriteIt		STI		R0, DDR		; Write character

; Return from TRAP
Return		LD		R1, SaveR1	; Restore registers
			RET					; Return from trap (JMP R7, actually)
DSR			.FILL	xFE04		; Address of display status register
DDR			.FILL	xFE06		; Address of display data register
SaveR1		.BLKW	1
			.END

;------------------------------------------------------------------
			
; This service routine writes a NULL-terminated string to the console.
; It services the PUTS service call (TRAP x22).
; Inputs: R0 is a pointer to the string to print.
			.ORIG	x0450		; Where this TSR resides
			.TRAP	PUTS
			ST		R7, SaveR7	; Save R7 for later return
			ST		R0, SaveR0	; Save other registers that
			ST		R1, SaveR1	; are needed by this routine
			ST		R3, SaveR3  ;
			
;Loop through each character in the array
Loop		LDR		R1, R0, #0	; Retrieve the character(s)
			BRz		Return		; If it is 0, done
L2			LDI		R3, DSR
			BRzp	L2
			STI		R1, DDR		; Write the character
			ADD		R0, R0, #1	; Increment pointer
			BRnzp Loop			; Do it all over again
			
; Return from the request for service call
Return		LD		R3, SaveR3
			LD		R1, SaveR1
			LD		R0, SaveR0
			LD		R7, SaveR7
			RET
			
; Register locations
DSR			.FILL	xFE04
DDR			.FILL	xFE06
SaveR0		.FILL	x0000
SaveR1		.FILL	x0000
SaveR3		.FILL	x0000
SaveR7		.FILL	x0000
			.END

;------------------------------------------------------------------
			
; Service Routine for Keyboard Input
			.ORIG x04A0
			.TRAP IN
START		ST		R7, SaveR7
			JSR		SaveReg
			LD		R2, Newline
			JSR		WriteChar
			LEA		R1, Prompt
		
Loop		LDR		R2, R1, #0
			BRz		Input
			JSR		WriteChar
			ADD		R1, R1, #1
			BRnzp	Loop
		
Input		JSR		ReadChar
			ADD		R2, R0, #0		; Move char to R2 for writing
			BRnzp	Loop			; Echo to monitor
		
			LD		R2, Newline
			JSR		WriteChar
			JSR		RestoreReg
			LD		R7, SaveR7
			RET						; JMP R7 terminates
									; the TRAP routine
SaveR7		.FILL	x0000
Newline		.FILL	x000A
Prompt		.STRINGZ	"Input a character>"

WriteChar	LDI		R3, DSR
			BRzp	WriteChar
			STI		R2, DDR
			RET						; JMP R7 terminates subroutine
DSR			.FILL	xFE04
DDR			.FILL	xFE06

ReadChar	LDI		R3, KBSR
			BRzp	ReadChar
			LDI		R0, KBDR
			RET
KBSR		.FILL	xFE00
KBDR		.FILL	xFE02

SaveReg		ST		R1, SaveR1
			ST		R2, SaveR1
			ST		R3, SaveR1
			ST		R4, SaveR1
			ST		R5, SaveR1
			ST		R6, SaveR1
			RET

RestoreReg	LD		R1, SaveR1
			LD		R2, SaveR1
			LD		R3, SaveR1
			LD		R4, SaveR1
			LD		R5, SaveR1
			LD		R6, SaveR1
			RET			
SaveR1		.FILL	x0000
SaveR2		.FILL	x0000
SaveR3		.FILL	x0000
SaveR4		.FILL	x0000
SaveR5		.FILL	x0000
SaveR6		.FILL	x0000
			.END

;------------------------------------------------------------------
			
.ORIG xFD70			; Where this routine resides
.TRAP HALT
ST R7, SaveR7
ST R1, SaveR1		; R1: a temp for MC register
ST R0, SaveRO		; R0 is used as working space

; print message that machine is halting

LD R0, ASCIINewLine
OUT
LEA R0, Message
PUTS
LD R0, ASCIINewLine
OUT

; clear bit 15 at xFFFE to stop the machine
LDI R1, MCR			; Load MC register into R1
LD R0, MASK			; R0 = X7FFF
AND R0, R1, R0		; Mask to clear the top bit
STI R0, MCR			; Store R0 into MC register

; return from HALT routine.
; (how can this routine return if the machine is halted above?

LD R1, SaveR1 			; Restore registers
LD R0, SaveRO
LD R7, SaveR7
RET					; JMP R7, actually

; Some constants

ASCIINewLine .FILL xOOOA
SaveRO .BLKW 1
SaveR1 .BLKW 1
SaveR7 .BLKW 1
Message .STRINGZ "Halting the machine."
MCR .FILL xFFFE ; Address of MCR
MASK .FILL X7FFF ; Mask to clear the top bit
.END