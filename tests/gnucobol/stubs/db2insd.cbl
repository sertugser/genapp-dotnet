      ******************************************************************
      * STUB for Db2: stands in for the EXEC SQL statement             *
      *   INSERT INTO CUSTOMER (CUSTOMERNUMBER, FIRSTNAME, ...,        *
      *     EMAILADDRESS) VALUES (DEFAULT, :nine host variables)       *
      * in LGACDB01 (DEFAULT path, when the counter failed).           *
      * Shows the nine host variables. Db2 state, decided by the test  *
      * case (precondition B4, B6):                                    *
      *   - FORCE-INSD not 0: Db2 answers with that SQLCODE.           *
      *   - otherwise SQLCODE = 0 and the identity column gets its     *
      *     next value: IDENT-NEXT, set by the driver to 1000001       *
      *     (B6: no earlier DEFAULT insert; START WITH 1000001,        *
      *     base/cntl/db2cre.jcl:111-112). The value is kept in        *
      *     IDENT-LOCAL for DB2IDENT (IDENTITY_VAL_LOCAL).             *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DB2INSD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FORCE-INSD                  PIC S9(9) COMP-5 IS EXTERNAL.
       01  IDENT-NEXT                  PIC S9(9) COMP-5 IS EXTERNAL.
       01  IDENT-LOCAL                 PIC S9(9) COMP-5 IS EXTERNAL.
       01  DSP-SQLCODE                 PIC -(9)9.
       01  DSP-NUM                     PIC +9(9).
       LINKAGE SECTION.
           COPY SQLCA.
       01  LK-FIRSTNAME                PIC X(10).
       01  LK-LASTNAME                 PIC X(20).
       01  LK-DATEOFBIRTH              PIC X(10).
       01  LK-HOUSENAME                PIC X(20).
       01  LK-HOUSENUMBER              PIC X(4).
       01  LK-POSTCODE                 PIC X(8).
       01  LK-PHONEMOBILE              PIC X(20).
       01  LK-PHONEHOME                PIC X(20).
       01  LK-EMAILADDRESS             PIC X(100).
       PROCEDURE DIVISION USING SQLCA LK-FIRSTNAME LK-LASTNAME
                 LK-DATEOFBIRTH LK-HOUSENAME LK-HOUSENUMBER LK-POSTCODE
                 LK-PHONEMOBILE LK-PHONEHOME LK-EMAILADDRESS.
           DISPLAY 'STUB DB2INSD CUSTOMERNUMBER=DEFAULT'
           DISPLAY 'STUB DB2INSD FIRSTNAME=[' LK-FIRSTNAME ']'
           DISPLAY 'STUB DB2INSD LASTNAME=[' LK-LASTNAME ']'
           DISPLAY 'STUB DB2INSD DATEOFBIRTH=[' LK-DATEOFBIRTH ']'
           DISPLAY 'STUB DB2INSD HOUSENAME=[' LK-HOUSENAME ']'
           DISPLAY 'STUB DB2INSD HOUSENUMBER=[' LK-HOUSENUMBER ']'
           DISPLAY 'STUB DB2INSD POSTCODE=[' LK-POSTCODE ']'
           DISPLAY 'STUB DB2INSD PHONEMOBILE=[' LK-PHONEMOBILE ']'
           DISPLAY 'STUB DB2INSD PHONEHOME=[' LK-PHONEHOME ']'
           DISPLAY 'STUB DB2INSD EMAILADDRESS=[' LK-EMAILADDRESS ']'
           IF FORCE-INSD NOT = 0
               MOVE FORCE-INSD TO SQLCODE
           ELSE
               MOVE 0 TO SQLCODE
               MOVE IDENT-NEXT TO IDENT-LOCAL
               MOVE IDENT-LOCAL TO DSP-NUM
               DISPLAY 'STUB DB2INSD identity assigned=' DSP-NUM
           END-IF
           MOVE SQLCODE TO DSP-SQLCODE
           DISPLAY 'STUB DB2INSD answers SQLCODE=' DSP-SQLCODE
           GOBACK.
