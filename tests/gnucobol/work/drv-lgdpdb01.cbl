      ******************************************************************
      * Driver for LGDPDB01 under GnuCOBOL.                            *
      * Plays the role of CICS and Db2: sets the EIB fields, builds    *
      * the COMMAREA, decides what SQLCODE the (stubbed) Db2 returns,  *
      * CALLs the program and shows the return code.                   *
      * Usage: drv-lgdpdb01 DELOK | DEL100 | DELFAIL | BADID | SHORT   *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DRVDPDB1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EIBCALEN                    PIC S9(4) COMP-5 IS EXTERNAL.
       01  EIBTRNID                    PIC X(4)  IS EXTERNAL.
       01  EIBTRMID                    PIC X(4)  IS EXTERNAL.
       01  EIBTASKN                    PIC S9(7) COMP-3 IS EXTERNAL.
       01  DB2STUB-SQLCODE             PIC S9(9) COMP-5 IS EXTERNAL.

       01  DRV-CASE                    PIC X(8) VALUE SPACES.
       01  DRV-PROGRAM                 PIC X(8) VALUE 'LGDPDB01'.

       01  DRV-COMMAREA.
             Copy LGCMAREA.
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
           MOVE 0      TO DB2STUB-SQLCODE
           IF DRV-CASE = 'SHORT'
               MOVE 10 TO EIBCALEN
               MOVE '01DMOT' TO DRV-SHORT-ID
               MOVE 99       TO DRV-SHORT-RC
               MOVE '00'     TO DRV-SHORT-REST
               DISPLAY 'DRIVER case=SHORT EIBCALEN=' EIBCALEN
                       ' return code before=' DRV-SHORT-RC
               CALL DRV-PROGRAM USING DRV-SHORT-CA
               DISPLAY 'DRIVER return code after =' DRV-SHORT-RC
           ELSE
               MOVE LENGTH OF DRV-COMMAREA TO EIBCALEN
               MOVE SPACES   TO DRV-COMMAREA
               MOVE '01DMOT' TO CA-REQUEST-ID
               MOVE 99       TO CA-RETURN-CODE
               MOVE 1        TO CA-CUSTOMER-NUM
               MOVE 1        TO CA-POLICY-NUM
               EVALUATE DRV-CASE
                 WHEN 'DELOK'
                   CONTINUE
                 WHEN 'DEL100'
                   MOVE 100 TO DB2STUB-SQLCODE
                 WHEN 'DELFAIL'
                   MOVE -911 TO DB2STUB-SQLCODE
                 WHEN 'BADID'
                   MOVE '01XXXX' TO CA-REQUEST-ID
                 WHEN OTHER
                   DISPLAY 'DRIVER usage: drv-lgdpdb01 DELOK | DEL100'
                           ' | DELFAIL | BADID | SHORT'
                   MOVE 8 TO RETURN-CODE
                   STOP RUN
               END-EVALUATE
               DISPLAY 'DRIVER case=' DRV-CASE ' EIBCALEN=' EIBCALEN
                       ' stub SQLCODE=' DB2STUB-SQLCODE
                       ' return code before=' CA-RETURN-CODE
               CALL DRV-PROGRAM USING DRV-COMMAREA
               DISPLAY 'DRIVER return code after =' CA-RETURN-CODE
           END-IF
           STOP RUN.
