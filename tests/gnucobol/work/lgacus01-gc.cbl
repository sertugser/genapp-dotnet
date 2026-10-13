      ******************************************************************
      *                                                                *
      * (C) Copyright IBM Corp. 2011, 2020                             *
      *                                                                *
      *                    ADD Customer                                *
      *                                                                *
      *   Business logic for adding a new customer                     *
      *                                                                *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGACUS01.
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
       01  WS-SAVE-EIBCALEN            PIC S9(4) COMP-5.

      *----------------------------------------------------------------*
      * Common defintions                                              *
      *----------------------------------------------------------------*
      * Run time (debug) infomation for this invocation
        01  WS-HEADER.
           03 WS-EYECATCHER            PIC X(16)
                                        VALUE 'LGACUS01------WS'.
           03 WS-TRANSID               PIC X(4).
           03 WS-TERMID                PIC X(4).
           03 WS-TASKNUM               PIC 9(7).
           03 WS-FILLER                PIC X.
           03 WS-ADDR-DFHCOMMAREA      USAGE is POINTER.
           03 WS-CALEN                 PIC S9(4) COMP.

      * Variables for time/date processing
       01  WS-ABSTIME                  PIC S9(8) COMP VALUE +0.
       01  WS-TIME                     PIC X(8)  VALUE SPACES.
       01  WS-DATE                     PIC X(10) VALUE SPACES.

      * Error Message structure
       01  ERROR-MSG.
           03 EM-DATE                  PIC X(8)  VALUE SPACES.
           03 FILLER                   PIC X     VALUE SPACES.
           03 EM-TIME                  PIC X(6)  VALUE SPACES.
           03 FILLER                   PIC X(9)  VALUE ' LGACUS01'.
           03 EM-VARIABLE              PIC X(21) VALUE SPACES.

       01  CA-ERROR-MSG.
           03 FILLER                   PIC X(9)  VALUE 'COMMAREA='.
           03 CA-DATA                  PIC X(90) VALUE SPACES.
      *----------------------------------------------------------------*

      *----------------------------------------------------------------*
      * Definitions required for data manipulation                     *
      *----------------------------------------------------------------*
      * Fields to be used to check that commarea is correct length
       01  WS-COMMAREA-LENGTHS.
           03 WS-CA-HEADER-LEN         PIC S9(4) COMP VALUE +18.
           03 WS-REQUIRED-CA-LEN       PIC S9(4)      VALUE +0.
       77  LGACDB01                    PIC X(8)       VALUE 'LGACDB01'.

      *    Include copybook for defintion of customer details length
           COPY LGPOLICY.
      *----------------------------------------------------------------*

      ******************************************************************
      *    L I N K A G E     S E C T I O N
      ******************************************************************
       LINKAGE SECTION.

       01  DFHCOMMAREA.
             COPY LGCMAREA.

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

      *----------------------------------------------------------------*
      * Process incoming commarea                                      *
      *----------------------------------------------------------------*
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
           MOVE '00' TO CA-NUM-POLICIES
           MOVE EIBCALEN TO WS-CALEN.
           SET WS-ADDR-DFHCOMMAREA TO ADDRESS OF DFHCOMMAREA.

      * check commarea length
           ADD WS-CA-HEADER-LEN TO WS-REQUIRED-CA-LEN
           ADD WS-CUSTOMER-LEN  TO WS-REQUIRED-CA-LEN

      * if less set error return code and return to caller
           IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
             MOVE '98' TO CA-RETURN-CODE
             GOBACK
           END-IF

      *----------------------------------------------------------------*
      * Call routine to Insert row in DB2 Customer table               *
           PERFORM INSERT-CUSTOMER.
      
      *----------------------------------------------------------------*
      *
           GOBACK.

       MAINLINE-EXIT.
           EXIT.
      *----------------------------------------------------------------*

      *----------------------------------------------------------------*
      * DB2                                                            *
      *----------------------------------------------------------------*
       INSERT-CUSTOMER.

           DISPLAY 'STUB EXEC CICS LINK PROGRAM(LGACDB01) LENGTH(32500)'
           MOVE EIBCALEN TO WS-SAVE-EIBCALEN
           MOVE 32500 TO EIBCALEN
           CALL LGACDB01 USING DFHCOMMAREA
           MOVE WS-SAVE-EIBCALEN TO EIBCALEN
           IF ABEND-FLAG = 'Y'
               GOBACK
           END-IF.
      **********

           EXIT.

      *================================================================*
      * Procedure to write error message to Queues                     *
      *   message will include Date, Time, Program Name, Customer      *
      *   Number, Policy Number and SQLCODE.                           *
      *================================================================*
       WRITE-ERROR-MESSAGE.
      * Save SQLCODE in message
      * Obtain and format current time and date
           DISPLAY 'STUB EXEC CICS ASKTIME ABSTIME(WS-ABSTIME)'
           DISPLAY 'STUB EXEC CICS FORMATTIME ABSTIME(WS-ABSTIME)'
           MOVE WS-DATE TO EM-DATE
           MOVE WS-TIME TO EM-TIME
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
