      ******************************************************************
      * STUB for Db2: stands in for the EXEC SQL statement             *
      *   SET :DB2-CUSTOMERNUM-INT = IDENTITY_VAL_LOCAL()              *
      * in LGACDB01.                                                   *
      *   - FORCE-IDENT not 0: Db2 answers with that SQLCODE and the   *
      *     host variable is not touched.                              *
      *   - otherwise the host variable gets the identity value of     *
      *     the last DEFAULT insert (IDENT-LOCAL, set by DB2INSD),     *
      *     SQLCODE = 0.                                               *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DB2IDENT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FORCE-IDENT                 PIC S9(9) COMP-5 IS EXTERNAL.
       01  IDENT-LOCAL                 PIC S9(9) COMP-5 IS EXTERNAL.
       01  DSP-SQLCODE                 PIC -(9)9.
       01  DSP-NUM                     PIC +9(9).
       LINKAGE SECTION.
           COPY SQLCA.
       01  LK-NUM                      PIC S9(9) COMP.
       PROCEDURE DIVISION USING SQLCA LK-NUM.
           IF FORCE-IDENT NOT = 0
               MOVE FORCE-IDENT TO SQLCODE
           ELSE
               MOVE IDENT-LOCAL TO LK-NUM
               MOVE 0 TO SQLCODE
               MOVE LK-NUM TO DSP-NUM
               DISPLAY 'STUB DB2IDENT identity value=' DSP-NUM
           END-IF
           MOVE SQLCODE TO DSP-SQLCODE
           DISPLAY 'STUB DB2IDENT answers SQLCODE=' DSP-SQLCODE
           GOBACK.
