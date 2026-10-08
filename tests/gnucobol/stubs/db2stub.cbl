      ******************************************************************
      * STUB for Db2: stands in for one EXEC SQL statement.            *
      * It does not run SQL. It sets SQLCODE to the value the TEST     *
      * CASE gave it (external field DB2STUB-SQLCODE, set by the       *
      * driver). That value is an assumption of the test, not          *
      * something the program or Db2 produced.                         *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DB2STUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DB2STUB-SQLCODE             PIC S9(9) COMP-5 IS EXTERNAL.
       01  DSP-SQLCODE                 PIC -(9)9.
       LINKAGE SECTION.
           COPY SQLCA.
       PROCEDURE DIVISION USING SQLCA.
           MOVE DB2STUB-SQLCODE TO SQLCODE
           MOVE DB2STUB-SQLCODE TO DSP-SQLCODE
           DISPLAY 'STUB DB2STUB sets SQLCODE=' DSP-SQLCODE
           GOBACK.
