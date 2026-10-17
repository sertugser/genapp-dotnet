      ******************************************************************
      * Driver for LGIPDB01 (called directly) with a made-up SQLCODE.  *
      * Plays CICS and Db2 for the SQL-only run in runs/lgipdb01.md,   *
      * preconditions of tests/predictions/05-motor-inquire.md:        *
      *   B1  the program gets the address of one 32500-byte buffer.   *
      *       EIBCALEN is only the length the caller reports; the      *
      *       buffer beyond EIBCALEN is spaces before the call.        *
      *   B2  SQLCODE starts at 0 (SQLCA copybook has no VALUE).       *
      *   B3  numeric MOVE truncation is on (GnuCOBOL default).        *
      *   B4  the driver decides the SQLCODE of every EXEC SQL         *
      *       statement (DB2STUB-SQLCODE, see stubs/db2stub.cbl).      *
      * The return code is 77 before the call (LGIPDB01 never makes    *
      * 77; it makes 99 for an unknown request code).                  *
      * Usage: drv-lgipdb01 <REQUEST-ID> <SQLCODE> [EIBCALEN]          *
      *   e.g. drv-lgipdb01 01IMOT -904                                *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DRVIPDB1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EIBCALEN                    PIC S9(4) COMP-5 IS EXTERNAL.
       01  EIBTRNID                    PIC X(4)  IS EXTERNAL.
       01  EIBTRMID                    PIC X(4)  IS EXTERNAL.
       01  EIBTASKN                    PIC S9(7) COMP-3 IS EXTERNAL.
       01  ABEND-FLAG                  PIC X     IS EXTERNAL.
       01  DB2STUB-SQLCODE             PIC S9(9) COMP-5 IS EXTERNAL.

       01  DRV-ARGS                    PIC X(60) VALUE SPACES.
       01  DRV-REQID                   PIC X(6)  VALUE SPACES.
       01  DRV-SQLTXT                  PIC X(12) VALUE SPACES.
       01  DRV-LENTXT                  PIC X(12) VALUE SPACES.
       01  DRV-LEN                     PIC S9(9) COMP-5 VALUE 32500.
       01  DSP-LEN                     PIC -(5)9.
       01  DSP-SQL                     PIC -(9)9.

      * The one 32500-byte buffer (B1)
       01  DRV-BUFFER.
             Copy LGCMAREA.

       PROCEDURE DIVISION.
           ACCEPT DRV-ARGS FROM COMMAND-LINE
           UNSTRING DRV-ARGS DELIMITED BY ALL SPACES
               INTO DRV-REQID DRV-SQLTXT DRV-LENTXT
           IF DRV-REQID = SPACES OR DRV-SQLTXT = SPACES
               DISPLAY 'DRIVER usage: drv-lgipdb01 <REQUEST-ID> '
                       '<SQLCODE> [EIBCALEN]'
               MOVE 8 TO RETURN-CODE
               STOP RUN
           END-IF
           MOVE FUNCTION NUMVAL(DRV-SQLTXT) TO DB2STUB-SQLCODE
           IF DRV-LENTXT NOT = SPACES
               MOVE FUNCTION NUMVAL(DRV-LENTXT) TO DRV-LEN
           END-IF
           MOVE 'CGPL' TO EIBTRNID
           MOVE 'T001' TO EIBTRMID
           MOVE 1      TO EIBTASKN
           MOVE 'N'    TO ABEND-FLAG

      * Build the caller's data, then blank everything after EIBCALEN
           MOVE SPACES TO DRV-BUFFER
           MOVE DRV-REQID TO CA-REQUEST-ID
           MOVE 77        TO CA-RETURN-CODE
           MOVE 2         TO CA-CUSTOMER-NUM
           MOVE 1         TO CA-POLICY-NUM
           MOVE DRV-LEN TO EIBCALEN
           IF DRV-LEN < 32500
               MOVE SPACES TO DRV-BUFFER(DRV-LEN + 1:)
           END-IF

           MOVE EIBCALEN TO DSP-LEN
           MOVE DB2STUB-SQLCODE TO DSP-SQL
           DISPLAY 'DRIVER request=' DRV-REQID ' SQLCODE given=' DSP-SQL
                   ' EIBCALEN=' DSP-LEN
           DISPLAY 'BEFORE bytes 1-28 =[' DRV-BUFFER(1:28) ']'

           CALL 'LGIPDB01' USING DRV-BUFFER

           DISPLAY 'AFTER  ABEND-FLAG=[' ABEND-FLAG ']'
           DISPLAY 'AFTER  bytes 1-6   REQUEST-ID  =[' DRV-BUFFER(1:6)
                   ']'
           DISPLAY 'AFTER  bytes 7-8   RETURN-CODE =[' DRV-BUFFER(7:2)
                   ']'
           DISPLAY 'AFTER  bytes 9-18  CUSTOMER-NUM=[' DRV-BUFFER(9:10)
                   ']'
           DISPLAY 'AFTER  bytes 19-28 POLICY-NUM  =[' DRV-BUFFER(19:10)
                   ']'
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
           STOP RUN.
