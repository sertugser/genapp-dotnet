      ******************************************************************
      * STUB for Db2: stands in for the motor SELECT of LGIPDB01:      *
      *   SELECT ISSUEDATE, EXPIRYDATE, LASTCHANGED, BROKERID,         *
      *          BROKERSREFERENCE, PAYMENT, MAKE, MODEL, VALUE,        *
      *          REGNUMBER, COLOUR, CC, YEAROFMANUFACTURE, PREMIUM,    *
      *          ACCIDENTS                                             *
      *   INTO   (fifteen host variables; BROKERID, BROKERSREFERENCE   *
      *           and PAYMENT each followed by an indicator variable)  *
      *   FROM POLICY, MOTOR                                           *
      *   WHERE POLICY.POLICYNUMBER = MOTOR.POLICYNUMBER               *
      *     AND POLICY.CUSTOMERNUMBER = :customer                      *
      *     AND POLICY.POLICYNUMBER = :policy                          *
      * The test case decides the Db2 state (ADR 0004, prediction      *
      * precondition B4):                                              *
      *   - DB2FORCE-SQLCODE not 0: Db2 answers with that SQLCODE and  *
      *     does not touch the host variables.                         *
      *   - DB2FORCE-NULL 'B' / 'P': the BROKERID / PAYMENT column of  *
      *     the row is NULL: the indicator is -1 and the host variable *
      *     is not touched (the Db2 rule).                             *
      *   - otherwise the POLICY and MOTOR tables below are searched   *
      *     and joined here, not by Db2.                               *
      * The tables hold only the rows the 05-xx cases need, with the   *
      * values of base/cntl/db2cre.jcl:                                *
      *   POLICY 1 (customer 2), 3 (customer 5), 4 (customer 8, type   *
      *   E); MOTOR 1 and 3. Policy 4 has no MOTOR row; policy 999     *
      *   does not exist.                                              *
      * The SELECT list is mapped to the INTO list by position HERE,   *
      * by hand. convert-lgipol01.py checks the SELECT text against    *
      * this order. Db2 is not asked to check the mapping.             *
      * DATE and TIMESTAMP values are the JCL literals; the format Db2 *
      * would return is not known (predictions 05-01, 05-02: belirsiz).*
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DB2MOTOR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DB2FORCE-SQLCODE            PIC S9(9) COMP-5 IS EXTERNAL.
       01  DB2FORCE-NULL               PIC X IS EXTERNAL.
       01  DSP-SQLCODE                 PIC -(9)9.
       01  IX                          PIC S9(4) COMP-5.
       01  MATCH-POL                   PIC S9(4) COMP-5.
       01  MATCH-MOT                   PIC S9(4) COMP-5.
      * POLICY table, three rows. Column types as in db2cre.jcl:161-178.
       01  POLICY-TABLE.
           03 POLICY-ROW OCCURS 3 TIMES.
              05 POL-POLICYNUMBER      PIC S9(9) COMP.
              05 POL-CUSTOMERNUMBER    PIC S9(9) COMP.
              05 POL-ISSUEDATE         PIC X(10).
              05 POL-EXPIRYDATE        PIC X(10).
              05 POL-POLICYTYPE        PIC X.
              05 POL-LASTCHANGED       PIC X(26).
              05 POL-BROKERID          PIC S9(9) COMP.
              05 POL-BROKERSREF        PIC X(10).
              05 POL-PAYMENT           PIC S9(9) COMP.
      * MOTOR table, two rows. Column types as in db2cre.jcl:275-290.
       01  MOTOR-TABLE.
           03 MOTOR-ROW OCCURS 2 TIMES.
              05 MOT-POLICYNUMBER      PIC S9(9) COMP.
              05 MOT-MAKE              PIC X(15).
              05 MOT-MODEL             PIC X(15).
              05 MOT-VALUE             PIC S9(9) COMP.
              05 MOT-REGNUMBER         PIC X(7).
              05 MOT-COLOUR            PIC X(8).
              05 MOT-CC                PIC S9(4) COMP.
              05 MOT-YEAROFMANUFACTURE PIC X(10).
              05 MOT-PREMIUM           PIC S9(9) COMP.
              05 MOT-ACCIDENTS         PIC S9(9) COMP.
       LINKAGE SECTION.
           COPY SQLCA.
       01  LK-CUSTOMERNUMBER           PIC S9(9) COMP.
       01  LK-POLICYNUMBER             PIC S9(9) COMP.
       01  LK-ISSUEDATE                PIC X(10).
       01  LK-EXPIRYDATE               PIC X(10).
       01  LK-LASTCHANGED              PIC X(26).
       01  LK-BROKERID                 PIC S9(9) COMP.
       01  LK-IND-BROKERID             PIC S9(4) COMP.
       01  LK-BROKERSREF               PIC X(10).
       01  LK-IND-BROKERSREF           PIC S9(4) COMP.
       01  LK-PAYMENT                  PIC S9(9) COMP.
       01  LK-IND-PAYMENT              PIC S9(4) COMP.
       01  LK-MAKE                     PIC X(15).
       01  LK-MODEL                    PIC X(15).
       01  LK-VALUE                    PIC S9(9) COMP.
       01  LK-REGNUMBER                PIC X(7).
       01  LK-COLOUR                   PIC X(8).
       01  LK-CC                       PIC S9(4) COMP.
       01  LK-YEAROFMANUFACTURE        PIC X(10).
       01  LK-PREMIUM                  PIC S9(9) COMP.
       01  LK-ACCIDENTS                PIC S9(9) COMP.
       PROCEDURE DIVISION USING SQLCA LK-CUSTOMERNUMBER LK-POLICYNUMBER
                 LK-ISSUEDATE LK-EXPIRYDATE LK-LASTCHANGED
                 LK-BROKERID LK-IND-BROKERID
                 LK-BROKERSREF LK-IND-BROKERSREF
                 LK-PAYMENT LK-IND-PAYMENT
                 LK-MAKE LK-MODEL LK-VALUE LK-REGNUMBER LK-COLOUR
                 LK-CC LK-YEAROFMANUFACTURE LK-PREMIUM LK-ACCIDENTS.
           PERFORM LOAD-TABLES
           IF DB2FORCE-SQLCODE NOT = 0
               MOVE DB2FORCE-SQLCODE TO SQLCODE
           ELSE
               PERFORM JOIN-ROWS
               IF MATCH-POL > 0 AND MATCH-MOT > 0
                   PERFORM RETURN-ROW
                   MOVE 0 TO SQLCODE
               ELSE
                   MOVE 100 TO SQLCODE
               END-IF
           END-IF
           MOVE SQLCODE TO DSP-SQLCODE
           DISPLAY 'STUB DB2MOTOR answers SQLCODE=' DSP-SQLCODE
           GOBACK.

      * POLICY.POLICYNUMBER = MOTOR.POLICYNUMBER
      * AND POLICY.CUSTOMERNUMBER = :customer
      * AND POLICY.POLICYNUMBER = :policy
       JOIN-ROWS.
           MOVE 0 TO MATCH-POL MATCH-MOT
           PERFORM VARYING IX FROM 1 BY 1 UNTIL IX > 3
               IF POL-POLICYNUMBER(IX) = LK-POLICYNUMBER
                  AND POL-CUSTOMERNUMBER(IX) = LK-CUSTOMERNUMBER
                   MOVE IX TO MATCH-POL
               END-IF
           END-PERFORM
           IF MATCH-POL > 0
               PERFORM VARYING IX FROM 1 BY 1 UNTIL IX > 2
                   IF MOT-POLICYNUMBER(IX) = POL-POLICYNUMBER(MATCH-POL)
                       MOVE IX TO MATCH-MOT
                   END-IF
               END-PERFORM
           END-IF.

       RETURN-ROW.
           MOVE POL-ISSUEDATE(MATCH-POL)     TO LK-ISSUEDATE
           MOVE POL-EXPIRYDATE(MATCH-POL)    TO LK-EXPIRYDATE
           MOVE POL-LASTCHANGED(MATCH-POL)   TO LK-LASTCHANGED
           IF DB2FORCE-NULL = 'B'
               MOVE -1 TO LK-IND-BROKERID
           ELSE
               MOVE 0 TO LK-IND-BROKERID
               MOVE POL-BROKERID(MATCH-POL) TO LK-BROKERID
           END-IF
           MOVE 0 TO LK-IND-BROKERSREF
           MOVE POL-BROKERSREF(MATCH-POL)    TO LK-BROKERSREF
           IF DB2FORCE-NULL = 'P'
               MOVE -1 TO LK-IND-PAYMENT
           ELSE
               MOVE 0 TO LK-IND-PAYMENT
               MOVE POL-PAYMENT(MATCH-POL) TO LK-PAYMENT
           END-IF
           MOVE MOT-MAKE(MATCH-MOT)              TO LK-MAKE
           MOVE MOT-MODEL(MATCH-MOT)             TO LK-MODEL
           MOVE MOT-VALUE(MATCH-MOT)             TO LK-VALUE
           MOVE MOT-REGNUMBER(MATCH-MOT)         TO LK-REGNUMBER
           MOVE MOT-COLOUR(MATCH-MOT)            TO LK-COLOUR
           MOVE MOT-CC(MATCH-MOT)                TO LK-CC
           MOVE MOT-YEAROFMANUFACTURE(MATCH-MOT) TO LK-YEAROFMANUFACTURE
           MOVE MOT-PREMIUM(MATCH-MOT)           TO LK-PREMIUM
           MOVE MOT-ACCIDENTS(MATCH-MOT)         TO LK-ACCIDENTS.

      * Values of base/cntl/db2cre.jcl, line numbers in the comments.
       LOAD-TABLES.
      *    POLICY 1: db2cre.jcl:945-954
           MOVE 1            TO POL-POLICYNUMBER(1)
           MOVE 2            TO POL-CUSTOMERNUMBER(1)
           MOVE '2011-05-11' TO POL-ISSUEDATE(1)
           MOVE '2012-05-10' TO POL-EXPIRYDATE(1)
           MOVE 'M'          TO POL-POLICYTYPE(1)
           MOVE '2011-05-11 17:12:17' TO POL-LASTCHANGED(1)
           MOVE 0            TO POL-BROKERID(1)
           MOVE SPACES       TO POL-BROKERSREF(1)
           MOVE 0            TO POL-PAYMENT(1)
      *    POLICY 3: db2cre.jcl:969-978
           MOVE 3            TO POL-POLICYNUMBER(2)
           MOVE 5            TO POL-CUSTOMERNUMBER(2)
           MOVE '2011-09-08' TO POL-ISSUEDATE(2)
           MOVE '2012-09-07' TO POL-EXPIRYDATE(2)
           MOVE 'M'          TO POL-POLICYTYPE(2)
           MOVE '2011-09-08 13:14:45' TO POL-LASTCHANGED(2)
           MOVE 0            TO POL-BROKERID(2)
           MOVE SPACES       TO POL-BROKERSREF(2)
           MOVE 0            TO POL-PAYMENT(2)
      *    POLICY 4: db2cre.jcl:849-858 (endowment, no MOTOR row)
           MOVE 4            TO POL-POLICYNUMBER(3)
           MOVE 8            TO POL-CUSTOMERNUMBER(3)
           MOVE '2011-08-05' TO POL-ISSUEDATE(3)
           MOVE '2012-08-04' TO POL-EXPIRYDATE(3)
           MOVE 'E'          TO POL-POLICYTYPE(3)
           MOVE '2011-08-05 10:45:19' TO POL-LASTCHANGED(3)
           MOVE 0            TO POL-BROKERID(3)
           MOVE SPACES       TO POL-BROKERSREF(3)
           MOVE 0            TO POL-PAYMENT(3)
      *    MOTOR 1: db2cre.jcl:1105-1114
           MOVE 1            TO MOT-POLICYNUMBER(1)
           MOVE 'FORD'       TO MOT-MAKE(1)
           MOVE 'KA'         TO MOT-MODEL(1)
           MOVE 8500         TO MOT-VALUE(1)
           MOVE 'LL60LOO'    TO MOT-REGNUMBER(1)
           MOVE 'ORANGE'     TO MOT-COLOUR(1)
           MOVE 1000         TO MOT-CC(1)
           MOVE '1999-07-19' TO MOT-YEAROFMANUFACTURE(1)
           MOVE 450          TO MOT-PREMIUM(1)
           MOVE 0            TO MOT-ACCIDENTS(1)
      *    MOTOR 3: db2cre.jcl:1128-1137
           MOVE 3            TO MOT-POLICYNUMBER(2)
           MOVE 'DENNIS'     TO MOT-MAKE(2)
           MOVE 'ENGINE'     TO MOT-MODEL(2)
           MOVE 23500        TO MOT-VALUE(2)
           MOVE 'FIRE1'      TO MOT-REGNUMBER(2)
           MOVE 'RED'        TO MOT-COLOUR(2)
           MOVE 5000         TO MOT-CC(2)
           MOVE '1973-05-13' TO MOT-YEAROFMANUFACTURE(2)
           MOVE 2850         TO MOT-PREMIUM(2)
           MOVE 12           TO MOT-ACCIDENTS(2).
