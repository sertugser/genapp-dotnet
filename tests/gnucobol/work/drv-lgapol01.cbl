      ******************************************************************
      * Driver for LGAPOL01 under GnuCOBOL.                            *
      * Plays the role of CICS: sets the EIB fields, builds the        *
      * COMMAREA, CALLs the program and shows the return code.         *
      * Usage: drv-lgapol01 SHORT | OK | NONE                          *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DRVPOL01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EIBCALEN                    PIC S9(4) COMP-5 IS EXTERNAL.
       01  EIBTRNID                    PIC X(4)  IS EXTERNAL.
       01  EIBTRMID                    PIC X(4)  IS EXTERNAL.
       01  EIBTASKN                    PIC S9(7) COMP-3 IS EXTERNAL.

       01  DRV-CASE                    PIC X(8) VALUE SPACES.
       01  DRV-PROGRAM                 PIC X(8) VALUE 'LGAPOL01'.

      * Full-size COMMAREA (32500 bytes) for the sufficient-length run
       01  DRV-COMMAREA.
             Copy LGCMAREA.
      * Truncated COMMAREA (10 bytes) for the too-short run: request id
      * (6) + return code (2) + 2 bytes of customer number
       01  DRV-SHORT-CA.
           03 DRV-SHORT-ID             PIC X(6).
           03 DRV-SHORT-RC             PIC 9(2).
           03 DRV-SHORT-REST           PIC X(2).

       PROCEDURE DIVISION.
           ACCEPT DRV-CASE FROM COMMAND-LINE
           MOVE FUNCTION UPPER-CASE(DRV-CASE) TO DRV-CASE
           MOVE 'CGPL' TO EIBTRNID
           MOVE 'T001' TO EIBTRMID
           MOVE 1      TO EIBTASKN
           EVALUATE DRV-CASE
             WHEN 'SHORT'
               MOVE 10 TO EIBCALEN
               MOVE '01AMOT' TO DRV-SHORT-ID
               MOVE 99       TO DRV-SHORT-RC
               MOVE '00'     TO DRV-SHORT-REST
               DISPLAY 'DRIVER case=SHORT EIBCALEN=' EIBCALEN
                       ' return code before=' DRV-SHORT-RC
               CALL DRV-PROGRAM USING DRV-SHORT-CA
               DISPLAY 'DRIVER return code after =' DRV-SHORT-RC
             WHEN 'OK'
               MOVE LENGTH OF DRV-COMMAREA TO EIBCALEN
               MOVE SPACES   TO DRV-COMMAREA
               MOVE '01AMOT' TO CA-REQUEST-ID
               MOVE 99       TO CA-RETURN-CODE
               MOVE 1        TO CA-CUSTOMER-NUM
               MOVE 1        TO CA-POLICY-NUM
               DISPLAY 'DRIVER case=OK EIBCALEN=' EIBCALEN
                       ' return code before=' CA-RETURN-CODE
               CALL DRV-PROGRAM USING DRV-COMMAREA
               DISPLAY 'DRIVER return code after =' CA-RETURN-CODE
             WHEN 'NONE'
               MOVE 0 TO EIBCALEN
               MOVE SPACES   TO DRV-COMMAREA
               MOVE 99       TO CA-RETURN-CODE
               DISPLAY 'DRIVER case=NONE EIBCALEN=' EIBCALEN
                       ' return code before=' CA-RETURN-CODE
               CALL DRV-PROGRAM USING DRV-COMMAREA
               DISPLAY 'DRIVER return code after =' CA-RETURN-CODE
             WHEN OTHER
               DISPLAY 'DRIVER usage: drv-lgapol01 SHORT | OK | NONE'
               MOVE 8 TO RETURN-CODE
           END-EVALUATE
           STOP RUN.
