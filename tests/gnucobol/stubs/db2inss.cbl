      ******************************************************************
      * STUB for Db2: stands in for the EXEC SQL statement             *
      *   INSERT INTO CUSTOMER_SECURE (customerNumber, customerPass,   *
      *     state_indicator, pass_changes)                             *
      *   VALUES (:DB2-CUSTOMERNUM-INT, :D2-CUSTSECR-PASS,             *
      *           :D2-CUSTSECR-STATE, :DB2-CUSTOMERCNT-INT)            *
      * in LGACDB02. Shows the four host variables. Db2 state, decided *
      * by the test case (precondition B4):                            *
      *   - FORCE-INSS not 0: Db2 answers with that SQLCODE.           *
      *   - otherwise: every customer 1-10 has a CUSTOMER_SECURE row   *
      *     (base/cntl/db2cre.jcl:454-760), so the primary key is      *
      *     violated and SQLCODE = -803. Any other number: 0.          *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DB2INSS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FORCE-INSS                  PIC S9(9) COMP-5 IS EXTERNAL.
       01  DSP-SQLCODE                 PIC -(9)9.
       01  DSP-NUM                     PIC +9(9).
       01  DSP-CNT                     PIC +9(9).
       LINKAGE SECTION.
           COPY SQLCA.
       01  LK-CUSTOMERNUMBER           PIC S9(9) COMP.
       01  LK-CUSTOMERPASS             PIC X(32).
       01  LK-STATE-INDICATOR          PIC X.
       01  LK-PASS-CHANGES             PIC S9(9) COMP.
       PROCEDURE DIVISION USING SQLCA LK-CUSTOMERNUMBER LK-CUSTOMERPASS
                 LK-STATE-INDICATOR LK-PASS-CHANGES.
           MOVE LK-CUSTOMERNUMBER TO DSP-NUM
           MOVE LK-PASS-CHANGES TO DSP-CNT
           DISPLAY 'STUB DB2INSS customerNumber=' DSP-NUM
           DISPLAY 'STUB DB2INSS customerPass=[' LK-CUSTOMERPASS ']'
           DISPLAY 'STUB DB2INSS state_indicator=['
                   LK-STATE-INDICATOR ']'
           DISPLAY 'STUB DB2INSS pass_changes=' DSP-CNT
           IF FORCE-INSS NOT = 0
               MOVE FORCE-INSS TO SQLCODE
           ELSE
               IF LK-CUSTOMERNUMBER >= 1 AND LK-CUSTOMERNUMBER <= 10
                   MOVE -803 TO SQLCODE
               ELSE
                   MOVE 0 TO SQLCODE
               END-IF
           END-IF
           MOVE SQLCODE TO DSP-SQLCODE
           DISPLAY 'STUB DB2INSS answers SQLCODE=' DSP-SQLCODE
           GOBACK.
