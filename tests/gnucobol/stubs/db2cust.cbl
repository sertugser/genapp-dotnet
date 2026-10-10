      ******************************************************************
      * STUB for Db2: stands in for the one EXEC SQL statement of      *
      *   SELECT FIRSTNAME, LASTNAME, DATEOFBIRTH, HOUSENAME,          *
      *          HOUSENUMBER, POSTCODE, PHONEMOBILE, PHONEHOME,        *
      *          EMAILADDRESS                                          *
      *   INTO   (nine host variables, same order)                     *
      *   FROM CUSTOMER WHERE CUSTOMERNUMBER = :host-variable          *
      * in LGICDB01. The test case decides the Db2 state (ADR 0004,    *
      * prediction precondition B4):                                   *
      *   - DB2FORCE-SQLCODE not 0: Db2 answers with that SQLCODE and  *
      *     does not touch the host variables.                         *
      *   - otherwise the CUSTOMER table below is searched.            *
      * The table holds customer 1 only, with the values of            *
      * base/cntl/db2cre.jcl:437-446. Other customers are not loaded   *
      * because no test case needs them: "not found" is SQLCODE 100.   *
      * The SELECT list is mapped to the INTO list by position HERE,   *
      * by hand. Db2 is not asked to check this mapping.               *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DB2CUST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DB2FORCE-SQLCODE            PIC S9(9) COMP-5 IS EXTERNAL.
       01  DSP-SQLCODE                 PIC -(9)9.
      * CUSTOMER table, one row. Column types as in db2cre.jcl:113-121.
       01  CUSTOMER-ROW-1.
           03 ROW-CUSTOMERNUMBER       PIC S9(9) COMP VALUE 1.
           03 ROW-FIRSTNAME            PIC X(10)  VALUE 'Andrew'.
           03 ROW-LASTNAME             PIC X(20)  VALUE 'Pandy'.
           03 ROW-DATEOFBIRTH          PIC X(10)  VALUE '1950-07-11'.
           03 ROW-HOUSENAME            PIC X(20)  VALUE ' '.
           03 ROW-HOUSENUMBER          PIC X(4)   VALUE '34'.
           03 ROW-POSTCODE             PIC X(8)   VALUE 'PI101OO'.
           03 ROW-PHONEHOME            PIC X(20)  VALUE '01962 811234'.
           03 ROW-PHONEMOBILE          PIC X(20)  VALUE '07799 123456'.
           03 ROW-EMAILADDRESS         PIC X(100)
                                       VALUE 'A.Pandy@beebhouse.com'.
       LINKAGE SECTION.
           COPY SQLCA.
       01  LK-CUSTOMERNUMBER           PIC S9(9) COMP.
       01  LK-INTO-1                   PIC X(10).
       01  LK-INTO-2                   PIC X(20).
       01  LK-INTO-3                   PIC X(10).
       01  LK-INTO-4                   PIC X(20).
       01  LK-INTO-5                   PIC X(4).
       01  LK-INTO-6                   PIC X(8).
       01  LK-INTO-7                   PIC X(20).
       01  LK-INTO-8                   PIC X(20).
       01  LK-INTO-9                   PIC X(100).
       PROCEDURE DIVISION USING SQLCA LK-CUSTOMERNUMBER
                 LK-INTO-1 LK-INTO-2 LK-INTO-3 LK-INTO-4 LK-INTO-5
                 LK-INTO-6 LK-INTO-7 LK-INTO-8 LK-INTO-9.
           IF DB2FORCE-SQLCODE NOT = 0
               MOVE DB2FORCE-SQLCODE TO SQLCODE
           ELSE
               IF LK-CUSTOMERNUMBER = ROW-CUSTOMERNUMBER
                   MOVE ROW-FIRSTNAME    TO LK-INTO-1
                   MOVE ROW-LASTNAME     TO LK-INTO-2
                   MOVE ROW-DATEOFBIRTH  TO LK-INTO-3
                   MOVE ROW-HOUSENAME    TO LK-INTO-4
                   MOVE ROW-HOUSENUMBER  TO LK-INTO-5
                   MOVE ROW-POSTCODE     TO LK-INTO-6
                   MOVE ROW-PHONEMOBILE  TO LK-INTO-7
                   MOVE ROW-PHONEHOME    TO LK-INTO-8
                   MOVE ROW-EMAILADDRESS TO LK-INTO-9
                   MOVE 0 TO SQLCODE
               ELSE
                   MOVE 100 TO SQLCODE
               END-IF
           END-IF
           MOVE SQLCODE TO DSP-SQLCODE
           DISPLAY 'STUB DB2CUST answers SQLCODE=' DSP-SQLCODE
           GOBACK.
