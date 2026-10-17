      ******************************************************************
      * Driver for LGIPOL01 (and LGIPDB01 called directly).            *
      * Plays CICS and Db2 for the 05-xx predictions                   *
      * (tests/predictions/05-motor-inquire.md), preconditions:        *
      *   B1  the program always gets the address of one 32500-byte    *
      *       buffer. EIBCALEN is only the length the caller reports.  *
      *       Buffer bytes beyond EIBCALEN are spaces before the call. *
      *   B2  SQLCODE starts at 0 (SQLCA copybook has no VALUE; this   *
      *       is checked by case D0510).                               *
      *   B3  numeric MOVE truncation is on (GnuCOBOL default; checked *
      *       by case C0519).                                          *
      *   B4  the driver sets the Db2 state: DB2FORCE-SQLCODE and      *
      *       DB2FORCE-NULL.                                           *
      * The return code is 77 before the call (no program of the       *
      * chain produces 77; LGIPDB01 produces 99 for an unknown code).  *
      * Usage: drv-lgipol01 <CASE>   (cases are listed in EVALUATE)    *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DRVIPOL1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EIBCALEN                    PIC S9(4) COMP-5 IS EXTERNAL.
       01  EIBTRNID                    PIC X(4)  IS EXTERNAL.
       01  EIBTRMID                    PIC X(4)  IS EXTERNAL.
       01  EIBTASKN                    PIC S9(7) COMP-3 IS EXTERNAL.
       01  ABEND-FLAG                  PIC X     IS EXTERNAL.
       01  DB2FORCE-SQLCODE            PIC S9(9) COMP-5 IS EXTERNAL.
       01  DB2FORCE-NULL               PIC X     IS EXTERNAL.

       01  DRV-CASE                    PIC X(8) VALUE SPACES.
       01  DRV-PROGRAM                 PIC X(8) VALUE 'LGIPOL01'.
       01  DRV-REQID                   PIC X(6) VALUE '01IMOT'.
       01  DRV-CUSTNUM                 PIC 9(10) VALUE 2.
       01  DRV-POLNUM                  PIC 9(10) VALUE 1.
       01  DRV-CUSTNUM-BLANK           PIC X VALUE 'N'.
       01  DRV-POLNUM-BLANK            PIC X VALUE 'N'.
       01  DRV-LEN                     PIC S9(9) COMP-5 VALUE 32500.
       01  DSP-LEN                     PIC -(5)9.
       01  DSP-FORCE                   PIC -(9)9.

      * The one 32500-byte buffer (B1)
       01  DRV-BUFFER.
             Copy LGCMAREA.

       PROCEDURE DIVISION.
           ACCEPT DRV-CASE FROM COMMAND-LINE
           MOVE FUNCTION UPPER-CASE(DRV-CASE) TO DRV-CASE
           MOVE 'CGPL' TO EIBTRNID
           MOVE 'T001' TO EIBTRMID
           MOVE 1      TO EIBTASKN
           MOVE 'N'    TO ABEND-FLAG
           MOVE 0      TO DB2FORCE-SQLCODE
           MOVE 'N'    TO DB2FORCE-NULL
           EVALUATE DRV-CASE
      *      --- chain: driver -> LGIPOL01 -> LGIPDB01 ---
             WHEN 'C0501'
               CONTINUE
             WHEN 'C0502'
               MOVE 5 TO DRV-CUSTNUM
               MOVE 3 TO DRV-POLNUM
             WHEN 'C0503'
               MOVE 999 TO DRV-POLNUM
             WHEN 'C0504'
               MOVE 3 TO DRV-CUSTNUM
             WHEN 'C0505'
               MOVE 8 TO DRV-CUSTNUM
               MOVE 4 TO DRV-POLNUM
             WHEN 'C0506'
               MOVE -904 TO DB2FORCE-SQLCODE
             WHEN 'C0507'
               MOVE '01imot' TO DRV-REQID
             WHEN 'C0508'
               MOVE '01IXXX' TO DRV-REQID
             WHEN 'C0509'
               MOVE 0 TO DRV-LEN
             WHEN 'C0511A'
               MOVE 100 TO DRV-LEN
             WHEN 'C0511B'
               MOVE 28 TO DRV-LEN
             WHEN 'C0511C'
               MOVE 169 TO DRV-LEN
             WHEN 'C0516'
               MOVE 'B' TO DB2FORCE-NULL
             WHEN 'C0517'
               MOVE 'P' TO DB2FORCE-NULL
             WHEN 'C0519'
               MOVE 1000000002 TO DRV-CUSTNUM
             WHEN 'C0520A'
               MOVE 'Y' TO DRV-CUSTNUM-BLANK
             WHEN 'C0520B'
               MOVE 'Y' TO DRV-POLNUM-BLANK
      *      --- LGIPDB01 called directly ---
             WHEN 'D0510'
               MOVE 'LGIPDB01' TO DRV-PROGRAM
               MOVE 0 TO DRV-LEN
             WHEN 'D0512A'
               MOVE 'LGIPDB01' TO DRV-PROGRAM
               MOVE 169 TO DRV-LEN
             WHEN 'D0512B'
               MOVE 'LGIPDB01' TO DRV-PROGRAM
               MOVE 28 TO DRV-LEN
             WHEN 'D0513'
               MOVE 'LGIPDB01' TO DRV-PROGRAM
               MOVE 170 TO DRV-LEN
             WHEN 'D0514'
               MOVE 'LGIPDB01' TO DRV-PROGRAM
               MOVE 100 TO DRV-LEN
               MOVE 999 TO DRV-POLNUM
             WHEN 'D0515'
               MOVE 'LGIPDB01' TO DRV-PROGRAM
               MOVE 50 TO DRV-LEN
               MOVE -904 TO DB2FORCE-SQLCODE
             WHEN OTHER
               DISPLAY 'DRIVER unknown case [' DRV-CASE ']'
               MOVE 8 TO RETURN-CODE
               STOP RUN
           END-EVALUATE

      * Build the caller's data, then blank everything after EIBCALEN
           MOVE SPACES TO DRV-BUFFER
           MOVE DRV-REQID TO CA-REQUEST-ID
           MOVE 77        TO CA-RETURN-CODE
           IF DRV-CUSTNUM-BLANK = 'Y'
               MOVE SPACES TO DRV-BUFFER(9:10)
           ELSE
               MOVE DRV-CUSTNUM TO CA-CUSTOMER-NUM
           END-IF
           IF DRV-POLNUM-BLANK = 'Y'
               MOVE SPACES TO DRV-BUFFER(19:10)
           ELSE
               MOVE DRV-POLNUM TO CA-POLICY-NUM
           END-IF
           MOVE DRV-LEN TO EIBCALEN
           IF DRV-LEN < 32500
               MOVE SPACES TO DRV-BUFFER(DRV-LEN + 1:)
           END-IF

           MOVE EIBCALEN TO DSP-LEN
           MOVE DB2FORCE-SQLCODE TO DSP-FORCE
           DISPLAY 'DRIVER case=' DRV-CASE ' program=' DRV-PROGRAM
                   ' EIBCALEN=' DSP-LEN ' forced SQLCODE=' DSP-FORCE
                   ' forced NULL=' DB2FORCE-NULL
           DISPLAY 'BEFORE bytes 1-28 =[' DRV-BUFFER(1:28) ']'
           IF DRV-BUFFER(29:32472) = SPACES
               DISPLAY 'BEFORE bytes 29-32500 = all spaces'
           ELSE
               DISPLAY 'BEFORE bytes 29-32500 = CHANGED'
           END-IF

           CALL DRV-PROGRAM USING DRV-BUFFER

           DISPLAY 'AFTER  ABEND-FLAG=[' ABEND-FLAG ']'
           DISPLAY 'AFTER  bytes 1-6   REQUEST-ID  =[' DRV-BUFFER(1:6)
                   ']'
           DISPLAY 'AFTER  bytes 7-8   RETURN-CODE =[' DRV-BUFFER(7:2)
                   ']'
           DISPLAY 'AFTER  bytes 9-18  CUSTOMER-NUM=[' DRV-BUFFER(9:10)
                   ']'
           DISPLAY 'AFTER  bytes 19-28 POLICY-NUM  =[' DRV-BUFFER(19:10)
                   ']'
           DISPLAY 'AFTER  bytes 29-38 ISSUE-DATE  =[' DRV-BUFFER(29:10)
                   ']'
           DISPLAY 'AFTER  bytes 39-48 EXPIRY-DATE =[' DRV-BUFFER(39:10)
                   ']'
           DISPLAY 'AFTER  bytes 49-74 LASTCHANGED =[' DRV-BUFFER(49:26)
                   ']'
           DISPLAY 'AFTER  bytes 75-84 BROKERID    =[' DRV-BUFFER(75:10)
                   ']'
           DISPLAY 'AFTER  bytes 85-94 BROKERSREF  =[' DRV-BUFFER(85:10)
                   ']'
           DISPLAY 'AFTER  bytes 95-100 PAYMENT    =[' DRV-BUFFER(95:6)
                   ']'
           DISPLAY 'AFTER  bytes 101-115 M-MAKE    =['
                   DRV-BUFFER(101:15) ']'
           DISPLAY 'AFTER  bytes 116-130 M-MODEL   =['
                   DRV-BUFFER(116:15) ']'
           DISPLAY 'AFTER  bytes 131-136 M-VALUE   =['
                   DRV-BUFFER(131:6) ']'
           DISPLAY 'AFTER  bytes 137-143 M-REGNUMBER=['
                   DRV-BUFFER(137:7) ']'
           DISPLAY 'AFTER  bytes 144-151 M-COLOUR  =['
                   DRV-BUFFER(144:8) ']'
           DISPLAY 'AFTER  bytes 152-155 M-CC      =['
                   DRV-BUFFER(152:4) ']'
           DISPLAY 'AFTER  bytes 156-165 M-MANUFACTURED=['
                   DRV-BUFFER(156:10) ']'
           DISPLAY 'AFTER  bytes 166-171 M-PREMIUM =['
                   DRV-BUFFER(166:6) ']'
           DISPLAY 'AFTER  bytes 172-177 M-ACCIDENTS=['
                   DRV-BUFFER(172:6) ']'
           DISPLAY 'AFTER  bytes 178-182 FINAL-MARK=['
                   DRV-BUFFER(178:5) ']'
           IF DRV-BUFFER(183:32318) = SPACES
               DISPLAY 'AFTER  bytes 183-32500 = all spaces'
           ELSE
               DISPLAY 'AFTER  bytes 183-32500 = CHANGED'
           END-IF
           IF DRV-BUFFER(29:32472) = SPACES
               DISPLAY 'AFTER  bytes 29-32500 = all spaces'
           ELSE
               DISPLAY 'AFTER  bytes 29-32500 = CHANGED'
           END-IF
           IF DRV-BUFFER = SPACES
               DISPLAY 'AFTER  bytes 1-32500 = all spaces'
           ELSE
               DISPLAY 'AFTER  bytes 1-32500 = CHANGED'
           END-IF
           STOP RUN.
