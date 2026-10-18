      ******************************************************************
      *                                                                *
      * (C) Copyright IBM Corp. 2011, 2020                             *
      *                                                                *
      *                    Inquire Policy                              *
      *                                                                *
      *  Business logic for policy inquire                             *
      *   To obtain full details of an individual policy:              *
      *     Endowment, House or Motor                                  *
      *                                                                *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGIPOL01.
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
                                        VALUE 'LGIPOL01------WS'.
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
           03 FILLER                   PIC X(9)  VALUE ' LGIPOL01'.
           03 EM-VARIABLE              PIC X(21) VALUE SPACES.

       01  CA-ERROR-MSG.
           03 FILLER                   PIC X(9)  VALUE 'COMMAREA='.
           03 CA-DATA                  PIC X(90) VALUE SPACES.
      *----------------------------------------------------------------*

       01 LGIPDB01                     PIC x(8) Value 'LGIPDB01'.
      *----------------------------------------------------------------*
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
      *
           INITIALIZE WS-HEADER.
      *
           MOVE EIBTRNID TO WS-TRANSID.
           MOVE EIBTRMID TO WS-TERMID.
           MOVE EIBTASKN TO WS-TASKNUM.
      *
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
           MOVE EIBCALEN TO WS-CALEN.
           SET WS-ADDR-DFHCOMMAREA TO ADDRESS OF DFHCOMMAREA.
      *

           DISPLAY 'STUB EXEC CICS LINK PROGRAM(LGIPDB01) LENGTH(32500)'
           MOVE EIBCALEN TO WS-SAVE-EIBCALEN
           MOVE 32500 TO EIBCALEN
           CALL LGIPDB01 USING DFHCOMMAREA
           MOVE WS-SAVE-EIBCALEN TO EIBCALEN
           IF ABEND-FLAG = 'Y'
               GOBACK
           END-IF.

           GOBACK.

       MAINLINE-EXIT.
           EXIT.
      *----------------------------------------------------------------*

      *================================================================*
      * Procedure to write error message to Queues                     *
      *   message will include Date, Time, Program Name, Customer      *
      *   Number, Policy Number and SQLCODE.                           *
      *================================================================*
       WRITE-ERROR-MESSAGE.
      * Save SQLCODE in message
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
