      ******************************************************************
      * STUB for Db2: stands in for the EXEC SQL statement             *
      *   INSERT INTO CUSTOMER (CUSTOMERNUMBER, FIRSTNAME, LASTNAME,   *
      *     DATEOFBIRTH, HOUSENAME, HOUSENUMBER, POSTCODE,             *
      *     PHONEMOBILE, PHONEHOME, EMAILADDRESS)                      *
      *   VALUES (:ten host variables, same order)                     *
      * in LGACDB01 (counter number path). The stub shows every host   *
      * variable it received, so the test can check what the COBOL     *
      * program passed to the INSERT. The Db2 state is decided by the  *
      * test case (ADR 0004, prediction precondition B4):              *
      *   - FORCE-INSC not 0: Db2 answers with that SQLCODE.           *
      *   - otherwise: customer numbers 1-10 exist (base/cntl/         *
      *     db2cre.jcl:437-760), so the primary key is violated and    *
      *     SQLCODE = -803. Any other number: SQLCODE = 0.             *
      * The column list is mapped to the host variables by position    *
      * HERE, by hand (the converter checked the order).               *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DB2INSC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FORCE-INSC                  PIC S9(9) COMP-5 IS EXTERNAL.
       01  DSP-SQLCODE                 PIC -(9)9.
       01  DSP-NUM                     PIC +9(9).
       LINKAGE SECTION.
           COPY SQLCA.
       01  LK-CUSTOMERNUMBER           PIC S9(9) COMP.
       01  LK-FIRSTNAME                PIC X(10).
       01  LK-LASTNAME                 PIC X(20).
       01  LK-DATEOFBIRTH              PIC X(10).
       01  LK-HOUSENAME                PIC X(20).
       01  LK-HOUSENUMBER              PIC X(4).
       01  LK-POSTCODE                 PIC X(8).
       01  LK-PHONEMOBILE              PIC X(20).
       01  LK-PHONEHOME                PIC X(20).
       01  LK-EMAILADDRESS             PIC X(100).
       PROCEDURE DIVISION USING SQLCA LK-CUSTOMERNUMBER LK-FIRSTNAME
                 LK-LASTNAME LK-DATEOFBIRTH LK-HOUSENAME LK-HOUSENUMBER
                 LK-POSTCODE LK-PHONEMOBILE LK-PHONEHOME
                 LK-EMAILADDRESS.
           MOVE LK-CUSTOMERNUMBER TO DSP-NUM
           DISPLAY 'STUB DB2INSC CUSTOMERNUMBER=' DSP-NUM
           DISPLAY 'STUB DB2INSC FIRSTNAME=[' LK-FIRSTNAME ']'
           DISPLAY 'STUB DB2INSC LASTNAME=[' LK-LASTNAME ']'
           DISPLAY 'STUB DB2INSC DATEOFBIRTH=[' LK-DATEOFBIRTH ']'
           DISPLAY 'STUB DB2INSC HOUSENAME=[' LK-HOUSENAME ']'
           DISPLAY 'STUB DB2INSC HOUSENUMBER=[' LK-HOUSENUMBER ']'
           DISPLAY 'STUB DB2INSC POSTCODE=[' LK-POSTCODE ']'
           DISPLAY 'STUB DB2INSC PHONEMOBILE=[' LK-PHONEMOBILE ']'
           DISPLAY 'STUB DB2INSC PHONEHOME=[' LK-PHONEHOME ']'
           DISPLAY 'STUB DB2INSC EMAILADDRESS=[' LK-EMAILADDRESS ']'
           IF FORCE-INSC NOT = 0
               MOVE FORCE-INSC TO SQLCODE
           ELSE
               IF LK-CUSTOMERNUMBER >= 1 AND LK-CUSTOMERNUMBER <= 10
                   MOVE -803 TO SQLCODE
               ELSE
                   MOVE 0 TO SQLCODE
               END-IF
           END-IF
           MOVE SQLCODE TO DSP-SQLCODE
           DISPLAY 'STUB DB2INSC answers SQLCODE=' DSP-SQLCODE
           GOBACK.
