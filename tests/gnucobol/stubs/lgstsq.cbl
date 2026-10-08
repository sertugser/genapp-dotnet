      ******************************************************************
      * STUB for LGSTSQ (writes messages to the TSQ/TDQ).              *
      * Stands in for EXEC CICS LINK PROGRAM('LGSTSQ'). The message    *
      * is shown on the screen instead of being queued. The caller     *
      * passes the message length as the second parameter (the LENGTH  *
      * option of the original LINK).                                  *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGSTSQ.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-MESSAGE                  PIC X(200).
       01  LK-LENGTH                   PIC 9(9) COMP-5.
       PROCEDURE DIVISION USING LK-MESSAGE LK-LENGTH.
           DISPLAY 'STUB LGSTSQ length=' LK-LENGTH
                   ' message=[' LK-MESSAGE(1:LK-LENGTH) ']'
           GOBACK.
