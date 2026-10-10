      ******************************************************************
      * Driver for LGICUS01 (and LGICDB01 called directly).            *
      * Plays CICS and Db2 for the 02-xx predictions                   *
      * (tests/predictions/02-customer-inquire.md), preconditions:     *
      *   B1  the program always gets the address of one 32500-byte    *
      *       buffer. EIBCALEN is only the length the caller reports.  *
      *       Buffer bytes beyond EIBCALEN are spaces before the call. *
      *   B2  SQLCODE starts at 0 (SQLCA copybook has no VALUE; this   *
      *       is checked by case D0209).                               *
      *   B3  numeric MOVE truncation is on (GnuCOBOL default).        *
      *   B4  the driver sets the Db2 state: DB2FORCE-SQLCODE.         *
      * Usage: drv-lgicus01 <CASE>   (cases are listed in EVALUATE)    *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DRVICUS1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EIBCALEN                    PIC S9(4) COMP-5 IS EXTERNAL.
       01  EIBTRNID                    PIC X(4)  IS EXTERNAL.
       01  EIBTRMID                    PIC X(4)  IS EXTERNAL.
       01  EIBTASKN                    PIC S9(7) COMP-3 IS EXTERNAL.
       01  DB2FORCE-SQLCODE            PIC S9(9) COMP-5 IS EXTERNAL.

       01  DRV-CASE                    PIC X(8) VALUE SPACES.
       01  DRV-PROGRAM                 PIC X(8) VALUE 'LGICUS01'.
       01  DRV-REQID                   PIC X(6) VALUE '01ICUS'.
       01  DRV-CUSTNUM                 PIC 9(10) VALUE 1.
       01  DRV-CUSTNUM-BLANK           PIC X VALUE 'N'.
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
           MOVE 0      TO DB2FORCE-SQLCODE
           EVALUATE DRV-CASE
      *      --- chain: driver -> LGICUS01 -> LGICDB01 ---
             WHEN 'C0201'
               CONTINUE
             WHEN 'C0202'
               MOVE 0 TO DRV-CUSTNUM
             WHEN 'C0203'
               MOVE -913 TO DB2FORCE-SQLCODE
             WHEN 'C0204'
               MOVE -204 TO DB2FORCE-SQLCODE
             WHEN 'C0205'
               MOVE 0 TO DRV-LEN
             WHEN 'C0206A'
               MOVE 89 TO DRV-LEN
             WHEN 'C0206B'
               MOVE 8 TO DRV-LEN
             WHEN 'C0207A'
               MOVE 7 TO DRV-LEN
             WHEN 'C0207B'
               MOVE 1 TO DRV-LEN
             WHEN 'C0208'
               MOVE 90 TO DRV-LEN
             WHEN 'C0212'
               MOVE '01AEND' TO DRV-REQID
             WHEN 'C0213'
               MOVE 1000000001 TO DRV-CUSTNUM
             WHEN 'C0214'
               MOVE 'Y' TO DRV-CUSTNUM-BLANK
      *      --- LGICDB01 called directly ---
             WHEN 'D0209'
               MOVE 'LGICDB01' TO DRV-PROGRAM
               MOVE 0 TO DRV-LEN
             WHEN 'D0210A'
               MOVE 'LGICDB01' TO DRV-PROGRAM
               MOVE 89 TO DRV-LEN
             WHEN 'D0210B'
               MOVE 'LGICDB01' TO DRV-PROGRAM
               MOVE 8 TO DRV-LEN
             WHEN 'D0211'
               MOVE 'LGICDB01' TO DRV-PROGRAM
               MOVE 233 TO DRV-LEN
             WHEN 'D0217'
               MOVE 'LGICDB01' TO DRV-PROGRAM
               MOVE 90 TO DRV-LEN
               MOVE -204 TO DB2FORCE-SQLCODE
             WHEN OTHER
               DISPLAY 'DRIVER unknown case [' DRV-CASE ']'
               MOVE 8 TO RETURN-CODE
               STOP RUN
           END-EVALUATE

      * Build the caller's data, then blank everything after EIBCALEN
           MOVE SPACES TO DRV-BUFFER
           MOVE DRV-REQID TO CA-REQUEST-ID
           MOVE 99        TO CA-RETURN-CODE
           IF DRV-CUSTNUM-BLANK = 'Y'
               MOVE SPACES TO DRV-BUFFER(9:10)
           ELSE
               MOVE DRV-CUSTNUM TO CA-CUSTOMER-NUM
           END-IF
           MOVE 777 TO CA-NUM-POLICIES
           MOVE DRV-LEN TO EIBCALEN
           IF DRV-LEN < 32500
               MOVE SPACES TO DRV-BUFFER(DRV-LEN + 1:)
           END-IF

           MOVE EIBCALEN TO DSP-LEN
           MOVE DB2FORCE-SQLCODE TO DSP-FORCE
           DISPLAY 'DRIVER case=' DRV-CASE ' program=' DRV-PROGRAM
                   ' EIBCALEN=' DSP-LEN ' forced SQLCODE=' DSP-FORCE
           DISPLAY 'BEFORE bytes 7-8      =[' DRV-BUFFER(7:2) ']'
           DISPLAY 'BEFORE bytes 91-93    =[' DRV-BUFFER(91:3) ']'

           CALL DRV-PROGRAM USING DRV-BUFFER

           DISPLAY 'AFTER  bytes 1-6   REQUEST-ID  =[' DRV-BUFFER(1:6)
                   ']'
           DISPLAY 'AFTER  bytes 7-8   RETURN-CODE =[' DRV-BUFFER(7:2)
                   ']'
           DISPLAY 'AFTER  bytes 9-18  CUSTOMER-NUM=[' DRV-BUFFER(9:10)
                   ']'
           DISPLAY 'AFTER  bytes 19-28 FIRST-NAME  =[' DRV-BUFFER(19:10)
                   ']'
           DISPLAY 'AFTER  bytes 29-48 LAST-NAME   =[' DRV-BUFFER(29:20)
                   ']'
           DISPLAY 'AFTER  bytes 49-58 DOB         =[' DRV-BUFFER(49:10)
                   ']'
           DISPLAY 'AFTER  bytes 59-78 HOUSE-NAME  =[' DRV-BUFFER(59:20)
                   ']'
           DISPLAY 'AFTER  bytes 79-82 HOUSE-NUM   =[' DRV-BUFFER(79:4)
                   ']'
           DISPLAY 'AFTER  bytes 83-90 POSTCODE    =[' DRV-BUFFER(83:8)
                   ']'
           DISPLAY 'AFTER  bytes 91-93 NUM-POLICIES=[' DRV-BUFFER(91:3)
                   ']'
           DISPLAY 'AFTER  bytes 94-113 PHONE-MOBILE=['
                   DRV-BUFFER(94:20) ']'
           DISPLAY 'AFTER  bytes 114-133 PHONE-HOME =['
                   DRV-BUFFER(114:20) ']'
           DISPLAY 'AFTER  bytes 134-233 EMAIL      =['
                   DRV-BUFFER(134:100) ']'
           IF DRV-BUFFER(234:32267) = SPACES
               DISPLAY 'AFTER  bytes 234-32500 POLICY-DATA = all spaces'
           ELSE
               DISPLAY 'AFTER  bytes 234-32500 POLICY-DATA = CHANGED'
           END-IF
           STOP RUN.
