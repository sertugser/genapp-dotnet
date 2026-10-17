      ******************************************************************
      *                                                                *
      * (C) Copyright IBM Corp. 2011, 2021                             *
      *                                                                *
      *                    Inquire Policy                              *
      *                                                                *
      *   To obtain full details of an individual policy:              *
      *     Endowment, House, Motor, Commercial                        *
      *                                                                *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGIPDB01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
      *
       DATA DIVISION.

       WORKING-STORAGE SECTION.

      *----------------------------------------------------------------*
      * GnuCOBOL: EIB fields CICS would normally fill in, and fields   *
      * of the test rig. EXTERNAL so the driver program can set them   *
      * before the CALL.                                               *
      *----------------------------------------------------------------*
       01  EIBCALEN                    PIC S9(4) COMP-5 IS EXTERNAL.
       01  EIBTRNID                    PIC X(4)  IS EXTERNAL.
       01  EIBTRMID                    PIC X(4)  IS EXTERNAL.
       01  EIBTASKN                    PIC S9(7) COMP-3 IS EXTERNAL.
       01  ABEND-FLAG                  PIC X     IS EXTERNAL.

        01 TSAREA.
          03  TSAREAPOSTCODE           Pic X(8).
          03  Filler                   Pic X Value Spaces.
          03  TSAREACUSTNUM            Pic 9(10).
          03  Filler                   Pic X Value Spaces.
          03  TSAREAPOLNUM             Pic 9(10).
          03  Filler                   Pic X Value Spaces.
      *----------------------------------------------------------------*
      * Common defintions                                              *
      *----------------------------------------------------------------*
      * Run time (debug) infomation for this invocation
        01  WS-HEADER.
           03 WS-EYECATCHER            PIC X(16)
                                        VALUE 'LGIPDB01------WS'.
           03 WS-TRANSID               PIC X(4).
           03 WS-TERMID                PIC X(4).
           03 WS-TASKNUM               PIC 9(7).
           03 WS-FILLER                PIC X.
           03 WS-ADDR-DFHCOMMAREA      USAGE is POINTER.
           03 WS-CALEN                 PIC S9(4) COMP.

      * Variables for time/date processing
       01  ABS-TIME                    PIC S9(8) COMP VALUE +0.
       01  TIME1                       PIC X(8)  VALUE SPACES.
       01  DATE1                       PIC X(10) VALUE SPACES.

      * Error Message structure
       01  ERROR-MSG.
           03 EM-DATE                  PIC X(8)  VALUE SPACES.
           03 FILLER                   PIC X     VALUE SPACES.
           03 EM-TIME                  PIC X(6)  VALUE SPACES.
           03 FILLER                   PIC X(9)  VALUE ' LGIPDB01'.
           03 EM-VARIABLE.
             05 FILLER                 PIC X(6)  VALUE ' CNUM='.
             05 EM-CUSNUM              PIC X(10)  VALUE SPACES.
             05 FILLER                 PIC X(6)  VALUE ' PNUM='.
             05 EM-POLNUM              PIC X(10)  VALUE SPACES.
             05 EM-SQLREQ              PIC X(16) VALUE SPACES.
             05 FILLER                 PIC X(9)  VALUE ' SQLCODE='.
             05 EM-SQLRC               PIC +9(5) USAGE DISPLAY.

       01  CA-ERROR-MSG.
           03 FILLER                   PIC X(9)  VALUE 'COMMAREA='.
           03 CA-DATA                  PIC X(90) VALUE SPACES.
      *----------------------------------------------------------------*

      *----------------------------------------------------------------*
      * Definitions required for data manipulation                     *
      *----------------------------------------------------------------*
       01 MINUS-ONE                    PIC S9(4) COMP VALUE -1.

       01  WS-COMMAREA-LENGTHS.
           03 WS-CA-HEADERTRAILER-LEN  PIC S9(4) COMP VALUE +33.
           03 WS-REQUIRED-CA-LEN       PIC S9(4)      VALUE +0.

      * Position of first char in commarea after end of policy data
      * for use when placing end of data indicator
       01  END-POLICY-POS              PIC S9(4) COMP VALUE +1.

       01  ICOM-Record-Count           PIC S9(4) COMP.
       01  WS-Request-ID               Pic X(6).

      *----------------------------------------------------------------*
      *    DB2 CONTROL
      *----------------------------------------------------------------*
      *----------------------------------------------------------------*
      * Declare Cursors
      *----------------------------------------------------------------*
      * GnuCOBOL: EXEC SQL DECLARE Cust_Cursor removed (no cursors)
      *----------------------------------------------------------------*
      * GnuCOBOL: EXEC SQL DECLARE Zip_Cursor removed (no cursors)

      * SQLCA DB2 communications area
           COPY SQLCA.


      *----------------------------------------------------------------*
      * Definitions required by SQL statement                          *
      *   DB2 datatypes to COBOL equivalents                           *
      *     SMALLINT    :   PIC S9(4) COMP                             *
      *     INTEGER     :   PIC S9(9) COMP                             *
      *     DATE        :   PIC X(10)                                  *
      *     TIMESTAMP   :   PIC X(26)                                  *
      *----------------------------------------------------------------*
      * Host variables for input to DB2 integer types
       01 DB2-IN-INTEGERS.
           03 DB2-CUSTOMERNUM-INT      PIC S9(9) COMP VALUE +0.
           03 DB2-POLICYNUM-INT        PIC S9(9) COMP VALUE +0.
           03 DB2-CLAIMNUM-INT         PIC S9(9) COMP VALUE +0.
       01 DB2-OUT-INTEGERS.
           03 DB2-BROKERID-INT         PIC S9(9) COMP.
           03 DB2-PAYMENT-INT          PIC S9(9) COMP.
           03 DB2-E-TERM-SINT          PIC S9(4) COMP.
           03 DB2-E-SUMASSURED-INT     PIC S9(9) COMP.
           03 DB2-E-PADDING-LEN        PIC S9(9) COMP.
           03 DB2-H-BEDROOMS-SINT      PIC S9(4) COMP.
           03 DB2-H-VALUE-INT          PIC S9(9) COMP.
           03 DB2-M-VALUE-INT          PIC S9(9) COMP.
           03 DB2-M-CC-SINT            PIC S9(4) COMP.
           03 DB2-M-PREMIUM-INT        PIC S9(9) COMP.
           03 DB2-M-ACCIDENTS-INT      PIC S9(9) COMP.
           03 DB2-B-FirePeril-Int      PIC S9(4) COMP.
           03 DB2-B-FirePremium-Int    PIC S9(9) COMP.
           03 DB2-B-CrimePeril-Int     PIC S9(4) COMP.
           03 DB2-B-CrimePremium-Int   PIC S9(9) COMP.
           03 DB2-B-FloodPeril-Int     PIC S9(4) COMP.
           03 DB2-B-FloodPremium-Int   PIC S9(9) COMP.
           03 DB2-B-WeatherPeril-Int   PIC S9(4) COMP.
           03 DB2-B-WeatherPremium-Int PIC S9(9) COMP.
           03 DB2-B-Status-Int         PIC S9(4) COMP.
           03 DB2-C-Paid-int           PIC S9(9) COMP.
           03 DB2-C-Value-int          PIC S9(9) COMP.

      * Policy descriptions to receive output from DB2
      * Must be an SQL INCLUDE so available to SQL pre-compiler
           COPY LGPOLICY.

      * Indicator variables for columns which could return nulls
      *   if these are not specified and SQL FETCH tries to return
      *   null value for a listed column it will FAIL with SQLCODE=-305
       77  IND-BROKERID                PIC S9(4) COMP.
       77  IND-BROKERSREF              PIC S9(4) COMP.
       77  IND-PAYMENT                 PIC S9(4) COMP.
       77  IND-E-PADDINGDATA           PIC S9(4) COMP.
       77  IND-E-PADDINGDATAL          PIC S9(4) COMP.

      *----------------------------------------------------------------*


      ******************************************************************
      *    L I N K A G E     S E C T I O N
      ******************************************************************
       LINKAGE SECTION.

       01  DFHCOMMAREA.
           COPY LGCMAREA.

       01  ICOM-Record                 Pic X(1202).

      ******************************************************************
      *    P R O C E D U R E S
      ******************************************************************
       PROCEDURE DIVISION USING DFHCOMMAREA.

      *----------------------------------------------------------------*
       MAINLINE SECTION.

      *----------------------------------------------------------------*
      * Common code                                                    *
      *----------------------------------------------------------------*
      * initialize working storage variables
           INITIALIZE WS-HEADER.
      * set up general variable
           MOVE EIBTRNID TO WS-TRANSID.
           MOVE EIBTRMID TO WS-TERMID.
           MOVE EIBTASKN TO WS-TASKNUM.
      *----------------------------------------------------------------*
      * initialize DB2 host variables
           INITIALIZE DB2-IN-INTEGERS.
           INITIALIZE DB2-OUT-INTEGERS.
           INITIALIZE DB2-POLICY.

      *---------------------------------------------------------------*
      * Check commarea and obtain required details                    *
      *---------------------------------------------------------------*
      * If NO commarea received issue an ABEND
           IF EIBCALEN IS EQUAL TO ZERO
             MOVE ' NO COMMAREA RECEIVED' TO EM-VARIABLE
             PERFORM WRITE-ERROR-MESSAGE
             DISPLAY 'STUB EXEC CICS ABEND ABCODE(LGCA)'
             MOVE 'Y' TO ABEND-FLAG
             GOBACK
           END-IF

      * initialize commarea return code to zero
           MOVE '00' TO CA-RETURN-CODE
           MOVE EIBCALEN TO WS-CALEN
           SET WS-ADDR-DFHCOMMAREA TO ADDRESS OF DFHCOMMAREA

      * Convert commarea customer & policy nums to DB2 integer format
           MOVE CA-CUSTOMER-NUM TO DB2-CUSTOMERNUM-INT
           MOVE CA-POLICY-NUM   TO DB2-POLICYNUM-INT
      * and save in error msg field incase required
           MOVE CA-CUSTOMER-NUM TO EM-CUSNUM
           MOVE CA-POLICY-NUM   TO EM-POLNUM

      *----------------------------------------------------------------*
      * Check which policy type is being requested                     *
      * This is not actually required whilst only endowment policy     *
      * inquires are supported, but will make future expansion simpler *
      *----------------------------------------------------------------*
      * Upper case value passed in Request Id field                    *
           MOVE FUNCTION UPPER-CASE(CA-REQUEST-ID) TO WS-REQUEST-ID

           EVALUATE WS-REQUEST-ID

             WHEN '01IEND'
               INITIALIZE DB2-ENDOWMENT
               PERFORM GET-ENDOW-DB2-INFO

             WHEN '01IHOU'
               INITIALIZE DB2-HOUSE
               PERFORM GET-HOUSE-DB2-INFO

             WHEN '01IMOT'
               INITIALIZE DB2-MOTOR
               PERFORM GET-MOTOR-DB2-INFO

             WHEN '01ICOM'
               INITIALIZE DB2-COMMERCIAL
               PERFORM GET-COMMERCIAL-DB2-INFO-1

             WHEN '02ICOM'
               INITIALIZE DB2-COMMERCIAL
               PERFORM GET-COMMERCIAL-DB2-INFO-2

             WHEN '03ICOM'
               INITIALIZE DB2-COMMERCIAL
               PERFORM GET-COMMERCIAL-DB2-INFO-3

             WHEN '05ICOM'
               INITIALIZE DB2-COMMERCIAL
               PERFORM GET-COMMERCIAL-DB2-INFO-5

             WHEN OTHER
               MOVE '99' TO CA-RETURN-CODE

           END-EVALUATE.

       End-Program.
           GOBACK.

       MAINLINE-EXIT.
           EXIT.
      *----------------------------------------------------------------*


      *================================================================*
      * Use Select on join of Policy and Endowment tables to obtain    *
      * single row that matches customer and policy number requested.  *
      * NOTE: because we do not know length of varchar yet - we must   *
      *      put output of DB2 SELECT into working storage to avoid    *
      *      risk of overwriting CICS storage beyond commarea.         *
      *================================================================*
       GET-ENDOW-DB2-INFO.

           MOVE ' SELECT ENDOW ' TO EM-SQLREQ
           DISPLAY 'STUB EXEC SQL SELECT FROM POLICY,ENDOWMENT'
           CALL 'DB2STUB' USING SQLCA

           IF SQLCODE = 0
      *      Select was successful

      *      Calculate size of commarea required to return all data
             ADD WS-CA-HEADERTRAILER-LEN TO WS-REQUIRED-CA-LEN
             ADD WS-FULL-ENDOW-LEN       TO WS-REQUIRED-CA-LEN

      *----------------------------------------------------------------*
      *      Specific code to allow for length of VACHAR data
      *      check whether PADDINGDATA field is non-null
      *        and calculate length of endowment policy
      *        and position of free space in commarea after policy data
      *----------------------------------------------------------------*
             IF IND-E-PADDINGDATAL NOT EQUAL MINUS-ONE
               ADD DB2-E-PADDING-LEN TO WS-REQUIRED-CA-LEN
               ADD DB2-E-PADDING-LEN TO END-POLICY-POS
             END-IF

      *      if commarea received is not large enough ...
      *        set error return code and return to caller
             IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
               MOVE '98' TO CA-RETURN-CODE
               GOBACK
             ELSE
      *        Length is sufficent so move data to commarea
      *        Move Integer fields to required length numerics
      *        Don't move null fields
               IF IND-BROKERID NOT EQUAL MINUS-ONE
                 MOVE DB2-BROKERID-INT    TO DB2-BROKERID
               END-IF
               IF IND-PAYMENT NOT EQUAL MINUS-ONE
                 MOVE DB2-PAYMENT-INT TO DB2-PAYMENT
               END-IF
      *----------------------------------------------------------------*
               MOVE DB2-E-TERM-SINT       TO DB2-E-TERM
               MOVE DB2-E-SUMASSURED-INT  TO DB2-E-SUMASSURED

               MOVE DB2-POLICY-COMMON     TO CA-POLICY-COMMON
               MOVE DB2-ENDOW-FIXED
                   TO CA-ENDOWMENT(1:WS-ENDOW-LEN)
               IF IND-E-PADDINGDATA NOT EQUAL MINUS-ONE
                 MOVE DB2-E-PADDINGDATA TO
                     CA-E-PADDING-DATA(1:DB2-E-PADDING-LEN)
               END-IF
             END-IF

      *      Mark the end of the policy data
             MOVE 'FINAL' TO CA-E-PADDING-DATA(END-POLICY-POS:5)

           ELSE
      *      Non-zero SQLCODE from first SQL FETCH statement
             IF SQLCODE EQUAL 100
      *        No rows found - invalid customer / policy number
               MOVE '01' TO CA-RETURN-CODE
             ELSE
      *        something has gone wrong
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF

           END-IF.
           EXIT.

      *================================================================*
      * Use Select on join of Policy and House tables to obtain        *
      * single row that matches customer and policy number requested.  *
      * NOTE: because we do not know length of varchar yet - we must   *
      *      put output of DB2 SELECT into working storage to avoid    *
      *      risk of overwriting CICS storage beyond commarea.         *
      *================================================================*
       GET-HOUSE-DB2-INFO.

           MOVE ' SELECT HOUSE ' TO EM-SQLREQ
           DISPLAY 'STUB EXEC SQL SELECT FROM POLICY,HOUSE'
           CALL 'DB2STUB' USING SQLCA

           IF SQLCODE = 0
      *      Select was successful

      *      Calculate size of commarea required to return all data
             ADD WS-CA-HEADERTRAILER-LEN TO WS-REQUIRED-CA-LEN
             ADD WS-FULL-HOUSE-LEN       TO WS-REQUIRED-CA-LEN

      *      if commarea received is not large enough ...
      *        set error return code and return to caller
             IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
               MOVE '98' TO CA-RETURN-CODE
               GOBACK
             ELSE
      *        Length is sufficent so move data to commarea
      *        Move Integer fields to required length numerics
      *        Don't move null fields
               IF IND-BROKERID NOT EQUAL MINUS-ONE
                 MOVE DB2-BROKERID-INT  TO DB2-BROKERID
               END-IF
               IF IND-PAYMENT NOT EQUAL MINUS-ONE
                 MOVE DB2-PAYMENT-INT TO DB2-PAYMENT
               END-IF
               MOVE DB2-H-BEDROOMS-SINT TO DB2-H-BEDROOMS
               MOVE DB2-H-VALUE-INT     TO DB2-H-VALUE

               MOVE DB2-POLICY-COMMON   TO CA-POLICY-COMMON
               MOVE DB2-HOUSE           TO CA-HOUSE(1:WS-HOUSE-LEN)
             END-IF

      *      Mark the end of the policy data
             MOVE 'FINAL' TO CA-H-FILLER(1:5)

           ELSE
      *      Non-zero SQLCODE from first SQL FETCH statement
             IF SQLCODE EQUAL 100
      *        No rows found - invalid customer / policy number
               MOVE '01' TO CA-RETURN-CODE
             ELSE
      *        something has gone wrong
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF

           END-IF.
           EXIT.

      *================================================================*
      * Use Select on join of Policy and Motor tables to obtain        *
      * single row that matches customer and policy number requested.  *
      *================================================================*
       GET-MOTOR-DB2-INFO.

           MOVE ' SELECT MOTOR ' TO EM-SQLREQ
           DISPLAY 'STUB EXEC SQL SELECT FROM POLICY,MOTOR'
           CALL 'DB2STUB' USING SQLCA

           IF SQLCODE = 0
      *      Select was successful

      *      Calculate size of commarea required to return all data
             ADD WS-CA-HEADERTRAILER-LEN TO WS-REQUIRED-CA-LEN
             ADD WS-FULL-MOTOR-LEN       TO WS-REQUIRED-CA-LEN

      *      if commarea received is not large enough ...
      *        set error return code and return to caller
             IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
               MOVE '98' TO CA-RETURN-CODE
               GOBACK
             ELSE
      *        Length is sufficent so move data to commarea
      *        Move Integer fields to required length numerics
      *        Don't move null fields
               IF IND-BROKERID NOT EQUAL MINUS-ONE
                 MOVE DB2-BROKERID-INT TO DB2-BROKERID
               END-IF
               IF IND-PAYMENT NOT EQUAL MINUS-ONE
                 MOVE DB2-PAYMENT-INT    TO DB2-PAYMENT
               END-IF
               MOVE DB2-M-CC-SINT      TO DB2-M-CC
               MOVE DB2-M-VALUE-INT    TO DB2-M-VALUE
               MOVE DB2-M-PREMIUM-INT  TO DB2-M-PREMIUM
               MOVE DB2-M-ACCIDENTS-INT TO DB2-M-ACCIDENTS
               MOVE DB2-M-PREMIUM-INT  TO CA-M-PREMIUM
               MOVE DB2-M-ACCIDENTS-INT TO CA-M-ACCIDENTS

               MOVE DB2-POLICY-COMMON  TO CA-POLICY-COMMON
               MOVE DB2-MOTOR          TO CA-MOTOR(1:WS-MOTOR-LEN)
             END-IF

      *      Mark the end of the policy data
             MOVE 'FINAL' TO CA-M-FILLER(1:5)

           ELSE
      *      Non-zero SQLCODE from first SQL FETCH statement
             IF SQLCODE EQUAL 100
      *        No rows found - invalid customer / policy number
               MOVE '01' TO CA-RETURN-CODE
             ELSE
      *        something has gone wrong
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF

           END-IF.
           EXIT.

      *================================================================*
      * Use Select on join of Policy and Commer table to obtain        *
      * single row that matches customer and policy number requested.  *
      *================================================================*
       GET-Commercial-DB2-INFO-1.

           MOVE ' SELECT Commercial ' TO EM-SQLREQ

           DISPLAY 'STUB EXEC SQL SELECT FROM POLICY,COMMERCIAL'
           CALL 'DB2STUB' USING SQLCA

           IF SQLCODE = 0
      *      Select was successful

      *      Calculate size of commarea required to return all data
             ADD WS-CA-HEADERTRAILER-LEN TO WS-REQUIRED-CA-LEN
             ADD WS-FULL-COMM-LEN        TO WS-REQUIRED-CA-LEN

      *      if commarea received is not large enough ...
      *        set error return code and return to caller
             IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
               MOVE '98' TO CA-RETURN-CODE
               GOBACK
             ELSE
               MOVE DB2-B-FirePeril-Int      TO DB2-B-FirePeril
               MOVE DB2-B-FirePremium-Int    TO DB2-B-FirePremium
               MOVE DB2-B-CrimePeril-Int     TO DB2-B-CrimePeril
               MOVE DB2-B-CrimePremium-Int   TO DB2-B-CrimePremium
               MOVE DB2-B-FloodPeril-Int     TO DB2-B-FloodPeril
               MOVE DB2-B-FloodPremium-Int   TO DB2-B-FloodPremium
               MOVE DB2-B-WeatherPeril-Int   TO DB2-B-WeatherPeril
               MOVE DB2-B-WeatherPremium-Int TO DB2-B-WeatherPremium
               MOVE DB2-B-Status-Int         TO DB2-B-Status
      *
               MOVE DB2-POLICY-COMMON  TO CA-POLICY-COMMON
               MOVE DB2-COMMERCIAL     TO CA-COMMERCIAL(1:WS-COMM-LEN)
             END-IF

      *      Mark the end of the policy data
             MOVE 'FINAL' TO CA-B-FILLER(1:5)

           ELSE
      *      Non-zero SQLCODE from first SQL FETCH statement
             IF SQLCODE EQUAL 100
      *        No rows found - invalid customer / policy number
               MOVE '01' TO CA-RETURN-CODE
             ELSE
      *        something has gone wrong
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF
           END-IF.


           EXIT.

      *================================================================*
      * Use Select on Policy number only to obtain                     *
      * single row that matches the requested ploicy.                  *
      *================================================================*
       GET-Commercial-DB2-INFO-2.

           MOVE ' SELECT Commercial ' TO EM-SQLREQ

           DISPLAY 'STUB EXEC SQL SELECT FROM POLICY,COMMERCIAL'
           CALL 'DB2STUB' USING SQLCA

           IF SQLCODE = 0
      *      Select was successful

      *      Calculate size of commarea required to return all data
             ADD WS-CA-HEADERTRAILER-LEN TO WS-REQUIRED-CA-LEN
             ADD WS-FULL-COMM-LEN        TO WS-REQUIRED-CA-LEN

      *      if commarea received is not large enough ...
      *        set error return code and return to caller
             IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
               MOVE '98' TO CA-RETURN-CODE
               GOBACK
             ELSE
               MOVE DB2-CustomerNum-Int      TO CA-Customer-Num
               MOVE DB2-B-FirePeril-Int      TO DB2-B-FirePeril
               MOVE DB2-B-FirePremium-Int    TO DB2-B-FirePremium
               MOVE DB2-B-CrimePeril-Int     TO DB2-B-CrimePeril
               MOVE DB2-B-CrimePremium-Int   TO DB2-B-CrimePremium
               MOVE DB2-B-FloodPeril-Int     TO DB2-B-FloodPeril
               MOVE DB2-B-FloodPremium-Int   TO DB2-B-FloodPremium
               MOVE DB2-B-WeatherPeril-Int   TO DB2-B-WeatherPeril
               MOVE DB2-B-WeatherPremium-Int TO DB2-B-WeatherPremium
               MOVE DB2-B-Status-Int         TO DB2-B-Status
      *
               MOVE DB2-POLICY-COMMON  TO CA-POLICY-COMMON
               MOVE DB2-COMMERCIAL     TO CA-COMMERCIAL(1:WS-COMM-LEN)
             END-IF

      *      Mark the end of the policy data
             MOVE 'FINAL' TO CA-B-FILLER(1:5)

           ELSE
      *      Non-zero SQLCODE from first SQL FETCH statement
             IF SQLCODE EQUAL 100
      *        No rows found - invalid customer / policy number
               MOVE '01' TO CA-RETURN-CODE
             ELSE
      *        something has gone wrong
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF
           END-IF.


           EXIT.

      *================================================================*
      * Use Cursor to obtain matching customer policy records          *
      * from commercial table                                          *
      *================================================================*
       GET-Commercial-DB2-INFO-3.

           MOVE ' SELECT Commercial ' TO EM-SQLREQ
           Move 0 To ICOM-Record-Count

           DISPLAY 'STUB EXEC SQL OPEN Cust_Cursor'
           CALL 'DB2STUB' USING SQLCA.
           IF SQLCODE NOT EQUAL 0
             MOVE '89' TO CA-RETURN-CODE
             PERFORM WRITE-ERROR-MESSAGE
             PERFORM END-PROGRAM
           END-IF.

           Perform GET-Commercial-DB2-INFO-3-Cur
                     With Test after Until SQLCODE > 0

           DISPLAY 'STUB EXEC SQL CLOSE Cust_Cursor'
           CALL 'DB2STUB' USING SQLCA.
           IF SQLCODE NOT EQUAL 0
             MOVE '88' TO CA-RETURN-CODE
             PERFORM WRITE-ERROR-MESSAGE
             PERFORM END-PROGRAM
           END-IF.

           Exit.

       GET-Commercial-DB2-INFO-3-Cur.

           DISPLAY 'STUB EXEC SQL FETCH Cust_Cursor'
           CALL 'DB2STUB' USING SQLCA

           If SQLCODE = 0
             MOVE DB2-B-FirePeril-Int      TO DB2-B-FirePeril
             MOVE DB2-B-FirePremium-Int    TO DB2-B-FirePremium
             MOVE DB2-B-CrimePeril-Int     TO DB2-B-CrimePeril
             MOVE DB2-B-CrimePremium-Int   TO DB2-B-CrimePremium
             MOVE DB2-B-FloodPeril-Int     TO DB2-B-FloodPeril
             MOVE DB2-B-FloodPremium-Int   TO DB2-B-FloodPremium
             MOVE DB2-B-WeatherPeril-Int   TO DB2-B-WeatherPeril
             MOVE DB2-B-WeatherPremium-Int TO DB2-B-WeatherPremium
             MOVE DB2-B-Status-Int         TO DB2-B-Status
             MOVE DB2-CustomerNum-Int      TO CA-CUSTOMER-NUM
             MOVE DB2-PolicyNum-Int        TO CA-POLICY-NUM
             MOVE DB2-POLICY-COMMON        TO CA-POLICY-COMMON
             MOVE DB2-COMMERCIAL     TO CA-COMMERCIAL(1:WS-COMM-LEN)

             Add 1 To ICOM-Record-Count
             If ICOM-Record-Count > 20
               Move 17 To SQLCODE
             End-If
           End-If


           EXIT.

      *================================================================*
      * Use Cursor to obtain matching rows                             *
      * from commercial table                                          *
      *================================================================*
       GET-Commercial-DB2-INFO-5.

           MOVE ' SELECT Commercial ' TO EM-SQLREQ

           DISPLAY 'STUB EXEC SQL OPEN Zip_Cursor'
           CALL 'DB2STUB' USING SQLCA.
           IF SQLCODE NOT EQUAL 0
             MOVE '89' TO CA-RETURN-CODE
             PERFORM WRITE-ERROR-MESSAGE
             PERFORM END-PROGRAM
           END-IF.

           Perform GET-Commercial-DB2-INFO-5-Cur
                     With Test after Until SQLCODE > 0

           DISPLAY 'STUB EXEC SQL CLOSE Zip_Cursor'
           CALL 'DB2STUB' USING SQLCA.
           IF SQLCODE NOT EQUAL 0
             MOVE '88' TO CA-RETURN-CODE
             PERFORM WRITE-ERROR-MESSAGE
             PERFORM END-PROGRAM
           END-IF.

           Exit.

       GET-Commercial-DB2-INFO-5-Cur.
           DISPLAY 'STUB EXEC SQL FETCH Zip_Cursor'
           CALL 'DB2STUB' USING SQLCA

           If SQLCODE = 0
             MOVE DB2-B-FirePeril-Int      TO DB2-B-FirePeril
             MOVE DB2-B-FirePremium-Int    TO DB2-B-FirePremium
             MOVE DB2-B-CrimePeril-Int     TO DB2-B-CrimePeril
             MOVE DB2-B-CrimePremium-Int   TO DB2-B-CrimePremium
             MOVE DB2-B-FloodPeril-Int     TO DB2-B-FloodPeril
             MOVE DB2-B-FloodPremium-Int   TO DB2-B-FloodPremium
             MOVE DB2-B-WeatherPeril-Int   TO DB2-B-WeatherPeril
             MOVE DB2-B-WeatherPremium-Int TO DB2-B-WeatherPremium
             MOVE DB2-B-Status-Int         TO DB2-B-Status
             MOVE DB2-CustomerNum-Int      TO CA-CUSTOMER-NUM
             MOVE DB2-PolicyNum-Int        TO CA-POLICY-NUM
             MOVE DB2-POLICY-COMMON        TO CA-POLICY-COMMON
             MOVE DB2-COMMERCIAL     TO CA-COMMERCIAL(1:WS-COMM-LEN)
           End-If


           EXIT.

      *================================================================*
      * Procedure to write error message to TD QUEUE(CSMT)             *
      *   message will include Date, Time, Program Name, Customer      *
      *   Number, Policy Number and SQLCODE.                           *
      *================================================================*
       WRITE-ERROR-MESSAGE.
      * Save SQLCODE in message
           MOVE SQLCODE TO EM-SQLRC
      * Obtain and format current time and date
           DISPLAY 'STUB EXEC CICS ASKTIME ABSTIME(ABS-TIME)'
           DISPLAY 'STUB EXEC CICS FORMATTIME ABSTIME(ABS-TIME)'
           MOVE DATE1 TO EM-DATE
           MOVE TIME1 TO EM-TIME
      * Write output message to TDQ
           CALL 'LGSTSQ' USING ERROR-MSG
                     BY CONTENT LENGTH OF ERROR-MSG.
      * Write 90 bytes or as much as we have of commarea to TDQ
           IF EIBCALEN > 0 THEN
             IF EIBCALEN < 91 THEN
               MOVE DFHCOMMAREA(1:EIBCALEN) TO CA-DATA
               CALL 'LGSTSQ' USING CA-ERROR-MSG
                         BY CONTENT LENGTH OF CA-ERROR-MSG
             ELSE
               MOVE DFHCOMMAREA(1:90) TO CA-DATA
               CALL 'LGSTSQ' USING CA-ERROR-MSG
                         BY CONTENT LENGTH OF CA-ERROR-MSG
             END-IF
           END-IF.
           EXIT.
