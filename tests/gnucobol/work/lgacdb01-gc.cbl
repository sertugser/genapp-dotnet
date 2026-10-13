      ******************************************************************
      *                                                                *
      * (C) Copyright IBM Corp. 2011, 2021                             *
      *                                                                *
      *                    ADD Customer Details                        *
      *                                                                *
      *   To add customer's name, address and date of birth to the     *
      *  DB2 customer table creating a new customer entry.             *
      *                                                                *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGACDB01.
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
                                        VALUE 'LGACDB01------WS'.
           03 WS-TRANSID               PIC X(4).
           03 WS-TERMID                PIC X(4).
           03 WS-TASKNUM               PIC 9(7).
           03 WS-FILLER                PIC X.
           03 WS-ADDR-DFHCOMMAREA      USAGE is POINTER.
           03 WS-CALEN                 PIC S9(4) COMP.

      *
       01  WS-RESP                   PIC S9(8) COMP.
       01  LastCustNum               PIC S9(8) COMP.
       01  GENAcount                 PIC X(16) Value 'GENACUSTNUM'.
       01  GENApool                  PIC X(8)  Value 'GENA'.
      * Variables for time/date processing
       01  WS-ABSTIME                  PIC S9(8) COMP VALUE +0.
       01  WS-TIME                     PIC X(8)  VALUE SPACES.
       01  WS-DATE                     PIC X(10) VALUE SPACES.

      * Error Message structure
       01  ERROR-MSG.
           03 EM-DATE                  PIC X(8)  VALUE SPACES.
           03 FILLER                   PIC X     VALUE SPACES.
           03 EM-TIME                  PIC X(6)  VALUE SPACES.
           03 FILLER                   PIC X(9)  VALUE ' LGACDB01'.
           03 EM-VARIABLE.
             05 FILLER                 PIC X(6)  VALUE ' CNUM='.
             05 EM-CUSNUM              PIC X(10)  VALUE SPACES.
             05 EM-SQLREQ              PIC X(16) VALUE SPACES.
             05 FILLER                 PIC X(9)  VALUE ' SQLCODE='.
             05 EM-SQLRC               PIC +9(5) USAGE DISPLAY.

       01  CDB2AREA.
           03 D2-REQUEST-ID            PIC X(6).
           03 D2-RETURN-CODE           PIC 9(2).
           03 D2-CUSTOMER-NUM          PIC 9(10).
           03 D2-CUSTSECR-PASS         PIC X(32).
           03 D2-CUSTSECR-COUNT        PIC X(4).
           03 D2-CUSTSECR-STATE        PIC X.
           03 D2-CUSTSECR-DATA         PIC X(32445).

       01  CA-ERROR-MSG.
           03 FILLER                   PIC X(9)  VALUE 'COMMAREA='.
           03 CA-DATA                  PIC X(90) VALUE SPACES.
      *----------------------------------------------------------------*
       77 LGACDB02                     PIC X(8)  VALUE 'LGACDB02'.
       77 LGACVS01                     PIC X(8)  VALUE 'LGACVS01'.
       77 LGAC-NCS                     PIC X(2)  VALUE 'ON'.
       77 WS-CS-PASSWORD               PIC X(16) Value 'NewPass'.
       77 WS-CS-STATE                  PIC X     VALUE 'N'.
       77 WS-CA-COUNT                  PIC S9(9) COMP  Value 0.

      *----------------------------------------------------------------*
      * Definitions required for data manipulation                     *
      *----------------------------------------------------------------*
      * Fields to be used to check that commarea is correct length
       01  WS-COMMAREA-LENGTHS.
           03 WS-CA-HEADER-LEN         PIC S9(4) COMP VALUE +18.
           03 WS-REQUIRED-CA-LEN       PIC S9(4)      VALUE +0.


      *    Include copybook for defintion of customer details length
           COPY LGPOLICY.
      *----------------------------------------------------------------*

      *----------------------------------------------------------------*
      * Definitions required by SQL statement                          *
      *   DB2 datatypes to COBOL equivalents                           *
      *     SMALLINT    :   PIC S9(4) COMP                             *
      *     INTEGER     :   PIC S9(9) COMP                             *
      *     DATE        :   PIC X(10)                                  *
      *     TIMESTAMP   :   PIC X(26)                                  *
      *----------------------------------------------------------------*
      * Host variables for output from DB2 integer types
       01  DB2-OUT-INTEGERS.
           03 DB2-CUSTOMERNUM-INT   PIC S9(9) COMP.
      *----------------------------------------------------------------*

      *----------------------------------------------------------------*
      *    DB2 CONTROL
      *----------------------------------------------------------------*
      * SQLCA DB2 communications area
           COPY SQLCA.

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


      * initialize DB2 host variables
           INITIALIZE DB2-OUT-INTEGERS.

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

      * Call routine to Insert row in Customer table                   *
           PERFORM Obtain-CUSTOMER-Number.
           PERFORM INSERT-CUSTOMER.

           DISPLAY 'STUB EXEC CICS LINK PROGRAM(LGACVS01) LENGTH(225)'
           MOVE EIBCALEN TO WS-SAVE-EIBCALEN
           MOVE 225 TO EIBCALEN
           CALL LGACVS01 USING DFHCOMMAREA
           MOVE WS-SAVE-EIBCALEN TO EIBCALEN
           IF ABEND-FLAG = 'Y'
               GOBACK
           END-IF.

           MOVE DB2-CUSTOMERNUM-INT TO D2-CUSTOMER-NUM.
           Move '02ACUS'     To  D2-REQUEST-ID.
           move '5732fec825535eeafb8fac50fee3a8aa'
                             To  D2-CUSTSECR-PASS.
           Move '0000'       To  D2-CUSTSECR-COUNT.
           Move 'N'          To  D2-CUSTSECR-STATE.

           DISPLAY 'STUB EXEC CICS LINK PROGRAM(LGACDB02) LENGTH(32500)'
           MOVE EIBCALEN TO WS-SAVE-EIBCALEN
           MOVE 32500 TO EIBCALEN
           CALL LGACDB02 USING CDB2AREA
           MOVE WS-SAVE-EIBCALEN TO EIBCALEN
           IF ABEND-FLAG = 'Y'
               GOBACK
           END-IF.

      *    Return to caller
           GOBACK.

       MAINLINE-EXIT.
           EXIT.
      *----------------------------------------------------------------*


       Obtain-CUSTOMER-Number.

           DISPLAY
               'STUB EXEC CICS GET COUNTER(GENAcount) POOL(GENApool)'
           CALL 'CICSCNT' USING WS-RESP LastCustNum.
           If WS-RESP Not = 0
             MOVE 'NO' TO LGAC-NCS
             Initialize DB2-CUSTOMERNUM-INT
           ELSE
             Move LastCustNum  To DB2-CUSTOMERNUM-INT
           End-If.


      *================================================================*
       INSERT-CUSTOMER.
      *================================================================*
      * Insert row into Customer table based on customer number        *
      *================================================================*
           MOVE ' INSERT CUSTOMER' TO EM-SQLREQ
      *================================================================*
           IF LGAC-NCS = 'ON'
             DISPLAY 'STUB EXEC SQL INSERT INTO CUSTOMER'
             CALL 'DB2INSC' USING SQLCA DB2-CUSTOMERNUM-INT
                 CA-FIRST-NAME CA-LAST-NAME CA-DOB CA-HOUSE-NAME
                 CA-HOUSE-NUM CA-POSTCODE CA-PHONE-MOBILE CA-PHONE-HOME
                 CA-EMAIL-ADDRESS
             IF SQLCODE NOT EQUAL 0
               MOVE '90' TO CA-RETURN-CODE
               PERFORM WRITE-ERROR-MESSAGE
               GOBACK
             END-IF
           ELSE
             DISPLAY 'STUB EXEC SQL INSERT INTO CUSTOMER (DEFAULT)'
             CALL 'DB2INSD' USING SQLCA CA-FIRST-NAME CA-LAST-NAME
                 CA-DOB CA-HOUSE-NAME CA-HOUSE-NUM CA-POSTCODE
                 CA-PHONE-MOBILE CA-PHONE-HOME CA-EMAIL-ADDRESS
             IF SQLCODE NOT EQUAL 0
               MOVE '90' TO CA-RETURN-CODE
               PERFORM WRITE-ERROR-MESSAGE
               GOBACK
             END-IF
      *    get value of assigned customer number
               DISPLAY
                   'STUB EXEC SQL SET :DB2-CUSTOMERNUM-INT = IDENTITY_V'
                   'AL_LOCAL()'
               CALL 'DB2IDENT' USING SQLCA DB2-CUSTOMERNUM-INT
           END-IF.

           MOVE DB2-CUSTOMERNUM-INT TO CA-CUSTOMER-NUM.

           EXIT.
      *================================================================*

      *================================================================*
      * Procedure to write error message to Queues                     *
      *   message will include Date, Time, Program Name, Customer      *
      *   Number, Policy Number and SQLCODE.                           *
      *================================================================*
       WRITE-ERROR-MESSAGE.
      * Save SQLCODE in message
           MOVE SQLCODE TO EM-SQLRC
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
