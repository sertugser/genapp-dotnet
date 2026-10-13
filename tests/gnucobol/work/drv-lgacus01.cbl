      ******************************************************************
      * Driver for LGACUS01 (and LGACDB01, LGACDB02, LGACVS01 called   *
      * directly). Plays CICS, the counter server and Db2 for the 01-xx*
      * predictions (tests/predictions/01-customer-add.md),            *
      * preconditions:                                                 *
      *   B1  the program always gets the address of one 32500-byte    *
      *       buffer. EIBCALEN is only the length the caller reports.  *
      *       Buffer bytes beyond EIBCALEN are spaces before the call. *
      *   B2  SQLCODE starts at 0 (SQLCA copybook has no VALUE; this   *
      *       is checked by case D0116).                               *
      *   B4  the driver sets the error codes and the data state       *
      *       (EXTERNAL fields below).                                 *
      *   B5  WORKING-STORAGE fields without VALUE start as spaces     *
      *       (GnuCOBOL default; seen in case C0106).                  *
      *   B6  first identity value 1000001 (IDENT-NEXT).               *
      *   B7  the counter answers 11 (CNTR-VALUE).                     *
      * Usage: drv-lgacus01 <CASE>   (cases are listed in EVALUATE)    *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DRVACUS1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EIBCALEN                    PIC S9(4) COMP-5 IS EXTERNAL.
       01  EIBTRNID                    PIC X(4)  IS EXTERNAL.
       01  EIBTRMID                    PIC X(4)  IS EXTERNAL.
       01  EIBTASKN                    PIC S9(7) COMP-3 IS EXTERNAL.
       01  EIBRESP2                    PIC S9(8) COMP-5 IS EXTERNAL.
       01  ABEND-FLAG                  PIC X     IS EXTERNAL.
       01  CNTR-RESP                   PIC S9(9) COMP-5 IS EXTERNAL.
       01  CNTR-VALUE                  PIC S9(9) COMP-5 IS EXTERNAL.
       01  IDENT-NEXT                  PIC S9(9) COMP-5 IS EXTERNAL.
       01  IDENT-LOCAL                 PIC S9(9) COMP-5 IS EXTERNAL.
       01  FORCE-INSC                  PIC S9(9) COMP-5 IS EXTERNAL.
       01  FORCE-INSD                  PIC S9(9) COMP-5 IS EXTERNAL.
       01  FORCE-INSS                  PIC S9(9) COMP-5 IS EXTERNAL.
       01  FORCE-IDENT                 PIC S9(9) COMP-5 IS EXTERNAL.
       01  WRF-RESP                    PIC S9(9) COMP-5 IS EXTERNAL.
       01  WRF-RESP2                   PIC S9(9) COMP-5 IS EXTERNAL.

       01  DRV-CASE                    PIC X(8) VALUE SPACES.
       01  DRV-PROGRAM                 PIC X(8) VALUE 'LGACUS01'.
       01  DRV-REQID                   PIC X(6) VALUE '01ACUS'.
       01  DRV-CUSTNUM                 PIC 9(10) VALUE 0.
       01  DRV-NONAME                  PIC X VALUE 'N'.
       01  DRV-DOB-BLANK               PIC X VALUE 'N'.
       01  DRV-D2                      PIC X VALUE 'N'.
       01  DRV-D2-REQID                PIC X(6) VALUE '02ACUS'.
       01  DRV-LEN                     PIC S9(9) COMP-5 VALUE 32500.
       01  DSP-LEN                     PIC -(5)9.
       01  DSP-A                       PIC -(9)9.
       01  DSP-B                       PIC -(9)9.
       01  DSP-C                       PIC -(9)9.
       01  DSP-D                       PIC -(9)9.

      * The one 32500-byte buffer (B1), customer layout
       01  DRV-BUFFER.
             Copy LGCMAREA.
      * The same buffer seen through the LGACDB02 layout (D2-*)
       01  DRV-BUFFER-D2 REDEFINES DRV-BUFFER.
           03 DRV-D2-REQUEST-ID        PIC X(6).
           03 DRV-D2-RETURN-CODE       PIC 9(2).
           03 DRV-D2-CUSTOMER-NUM      PIC 9(10).
           03 DRV-D2-PASS              PIC X(32).
           03 DRV-D2-COUNT             PIC X(4).
           03 DRV-D2-STATE             PIC X.
           03 DRV-D2-DATA              PIC X(32445).

       PROCEDURE DIVISION.
           ACCEPT DRV-CASE FROM COMMAND-LINE
           MOVE FUNCTION UPPER-CASE(DRV-CASE) TO DRV-CASE
           MOVE 'CGPL' TO EIBTRNID
           MOVE 'T001' TO EIBTRMID
           MOVE 1      TO EIBTASKN
           MOVE 0      TO EIBRESP2
           MOVE 'N'    TO ABEND-FLAG
      *    counter server (B7) and identity column (B6)
           MOVE 0       TO CNTR-RESP
           MOVE 11      TO CNTR-VALUE
           MOVE 1000001 TO IDENT-NEXT
           MOVE 0       TO IDENT-LOCAL
      *    Db2 and VSAM answers (B4): 0 = no forced error
           MOVE 0 TO FORCE-INSC FORCE-INSD FORCE-INSS FORCE-IDENT
           MOVE 0 TO WRF-RESP WRF-RESP2
           EVALUATE DRV-CASE
      *      --- chain: driver -> LGACUS01 -> LGACDB01 -> ... ---
             WHEN 'C0101'
               CONTINUE
             WHEN 'C0102'
               MOVE 16 TO CNTR-RESP
             WHEN 'C0102B'
               MOVE 16 TO CNTR-RESP
               MOVE -204 TO FORCE-IDENT
             WHEN 'C0103'
               MOVE 1 TO CNTR-VALUE
             WHEN 'C0104'
               MOVE 16 TO CNTR-RESP
               MOVE -204 TO FORCE-INSD
             WHEN 'C0105'
               MOVE 14 TO WRF-RESP
               MOVE 2  TO WRF-RESP2
             WHEN 'C0106'
               MOVE -204 TO FORCE-INSS
             WHEN 'C0107'
               MOVE 1 TO DRV-CUSTNUM
             WHEN 'C0108'
               MOVE 'Y' TO DRV-NONAME
             WHEN 'C0109'
               MOVE 'Y' TO DRV-DOB-BLANK
             WHEN 'C0110'
               MOVE 0 TO DRV-LEN
             WHEN 'C0111A'
               MOVE 89 TO DRV-LEN
             WHEN 'C0111B'
               MOVE 8 TO DRV-LEN
             WHEN 'C0112A'
               MOVE 7 TO DRV-LEN
             WHEN 'C0112B'
               MOVE 1 TO DRV-LEN
             WHEN 'C0113'
               MOVE 90 TO DRV-LEN
             WHEN 'C0114'
               MOVE SPACES TO DRV-REQID
      *      --- LGACDB01 called directly ---
             WHEN 'D0116'
               MOVE 'LGACDB01' TO DRV-PROGRAM
               MOVE 0 TO DRV-LEN
             WHEN 'D0117A'
               MOVE 'LGACDB01' TO DRV-PROGRAM
               MOVE 89 TO DRV-LEN
             WHEN 'D0117B'
               MOVE 'LGACDB01' TO DRV-PROGRAM
               MOVE 8 TO DRV-LEN
             WHEN 'D0117C'
               MOVE 'LGACDB01' TO DRV-PROGRAM
               MOVE 7 TO DRV-LEN
             WHEN 'D0117D'
               MOVE 'LGACDB01' TO DRV-PROGRAM
               MOVE 1 TO DRV-LEN
             WHEN 'D0118'
               MOVE 'LGACDB01' TO DRV-PROGRAM
               MOVE 90 TO DRV-LEN
               MOVE 1 TO CNTR-VALUE
             WHEN 'D0119'
               MOVE 'LGACDB01' TO DRV-PROGRAM
      *      --- LGACDB02 called directly (D2 layout) ---
             WHEN 'D0120'
               MOVE 'LGACDB02' TO DRV-PROGRAM
               MOVE 'Y' TO DRV-D2
               MOVE 0 TO DRV-LEN
             WHEN 'D0121'
               MOVE 'LGACDB02' TO DRV-PROGRAM
               MOVE 'Y' TO DRV-D2
               MOVE SPACES TO DRV-D2-REQID
             WHEN 'D0122A'
               MOVE 'LGACDB02' TO DRV-PROGRAM
               MOVE 'Y' TO DRV-D2
               MOVE 55 TO DRV-LEN
             WHEN 'D0122B'
               MOVE 'LGACDB02' TO DRV-PROGRAM
               MOVE 'Y' TO DRV-D2
               MOVE 90 TO DRV-LEN
      *      --- LGACVS01 called directly ---
             WHEN 'D0125A'
               MOVE 'LGACVS01' TO DRV-PROGRAM
               MOVE 18 TO DRV-LEN
               MOVE 11 TO DRV-CUSTNUM
               MOVE 14 TO WRF-RESP
               MOVE 2  TO WRF-RESP2
             WHEN 'D0125B'
               MOVE 'LGACVS01' TO DRV-PROGRAM
               MOVE 90 TO DRV-LEN
               MOVE 11 TO DRV-CUSTNUM
               MOVE 14 TO WRF-RESP
               MOVE 2  TO WRF-RESP2
             WHEN OTHER
               DISPLAY 'DRIVER unknown case [' DRV-CASE ']'
               MOVE 8 TO RETURN-CODE
               STOP RUN
           END-EVALUATE

      * Build the caller's data, then blank everything after EIBCALEN
           MOVE SPACES TO DRV-BUFFER
           IF DRV-D2 = 'Y'
               MOVE DRV-D2-REQID TO DRV-D2-REQUEST-ID
               MOVE 99 TO DRV-D2-RETURN-CODE
               MOVE 1  TO DRV-D2-CUSTOMER-NUM
               MOVE '5732fec825535eeafb8fac50fee3a8aa' TO DRV-D2-PASS
               MOVE '0000' TO DRV-D2-COUNT
               MOVE 'N' TO DRV-D2-STATE
           ELSE
               MOVE DRV-REQID TO CA-REQUEST-ID
               MOVE 99        TO CA-RETURN-CODE
               MOVE DRV-CUSTNUM TO CA-CUSTOMER-NUM
               IF DRV-NONAME = 'N'
                   MOVE 'Andrew' TO CA-FIRST-NAME
                   MOVE 'Pandy'  TO CA-LAST-NAME
               END-IF
               IF DRV-DOB-BLANK = 'N'
                   MOVE '1950-07-11' TO CA-DOB
               END-IF
               MOVE '34'      TO CA-HOUSE-NUM
               MOVE 'PI101OO' TO CA-POSTCODE
               MOVE 777       TO CA-NUM-POLICIES
               MOVE '07799 123456' TO CA-PHONE-MOBILE
               MOVE '01962 811234' TO CA-PHONE-HOME
               MOVE 'A.Pandy@beebhouse.com' TO CA-EMAIL-ADDRESS
           END-IF
           MOVE DRV-LEN TO EIBCALEN
           IF DRV-LEN < 32500
               MOVE SPACES TO DRV-BUFFER(DRV-LEN + 1:)
           END-IF

           MOVE EIBCALEN TO DSP-LEN
           DISPLAY 'DRIVER case=' DRV-CASE ' program=' DRV-PROGRAM
                   ' EIBCALEN=' DSP-LEN
           MOVE CNTR-RESP TO DSP-A
           MOVE CNTR-VALUE TO DSP-B
           DISPLAY 'DRIVER counter RESP=' DSP-A ' VALUE=' DSP-B
           MOVE FORCE-INSC TO DSP-A
           MOVE FORCE-INSD TO DSP-B
           MOVE FORCE-INSS TO DSP-C
           MOVE FORCE-IDENT TO DSP-D
           DISPLAY 'DRIVER forced SQLCODE INSC=' DSP-A ' INSD=' DSP-B
                   ' INSS=' DSP-C ' IDENT=' DSP-D
           MOVE WRF-RESP TO DSP-A
           MOVE WRF-RESP2 TO DSP-B
           DISPLAY 'DRIVER write RESP=' DSP-A ' RESP2=' DSP-B
           DISPLAY 'BEFORE bytes 1-6      =[' DRV-BUFFER(1:6) ']'
           DISPLAY 'BEFORE bytes 7-8      =[' DRV-BUFFER(7:2) ']'
           DISPLAY 'BEFORE bytes 9-18     =[' DRV-BUFFER(9:10) ']'
           DISPLAY 'BEFORE bytes 91-93    =[' DRV-BUFFER(91:3) ']'

           CALL DRV-PROGRAM USING DRV-BUFFER

           IF DRV-D2 = 'Y'
               PERFORM SHOW-D2
           ELSE
               PERFORM SHOW-CUSTOMER
           END-IF
           DISPLAY 'AFTER  ABEND-FLAG=[' ABEND-FLAG ']'
           STOP RUN.

       SHOW-CUSTOMER.
           DISPLAY 'AFTER  bytes 1-6   REQUEST-ID  =[' DRV-BUFFER(1:6)
                   ']'
           DISPLAY 'AFTER  bytes 7-8   RETURN-CODE =[' DRV-BUFFER(7:2)
                   ']'
           DISPLAY 'AFTER  bytes 9-18  CUSTOMER-NUM=[' DRV-BUFFER(9:10)
                   ']'
           DISPLAY 'AFTER  bytes 19-28 FIRST-NAME  =[' DRV-BUFFER(19:10)
                   ']'
           DISPLAY 'AFTER  bytes 29-48 LAST-NAME   =[' DRV-BUFFER(29:20)
                   ']'
           DISPLAY 'AFTER  bytes 49-58 DOB         =[' DRV-BUFFER(49:10)
                   ']'
           DISPLAY 'AFTER  bytes 59-78 HOUSE-NAME  =[' DRV-BUFFER(59:20)
                   ']'
           DISPLAY 'AFTER  bytes 79-82 HOUSE-NUM   =[' DRV-BUFFER(79:4)
                   ']'
           DISPLAY 'AFTER  bytes 83-90 POSTCODE    =[' DRV-BUFFER(83:8)
                   ']'
           DISPLAY 'AFTER  bytes 91-93 NUM-POLICIES=[' DRV-BUFFER(91:3)
                   ']'
           DISPLAY 'AFTER  bytes 94-113 PHONE-MOBILE=['
                   DRV-BUFFER(94:20) ']'
           DISPLAY 'AFTER  bytes 114-133 PHONE-HOME =['
                   DRV-BUFFER(114:20) ']'
           DISPLAY 'AFTER  bytes 134-233 EMAIL      =['
                   DRV-BUFFER(134:100) ']'
           IF DRV-BUFFER(234:32267) = SPACES
               DISPLAY 'AFTER  bytes 234-32500 POLICY-DATA = all spaces'
           ELSE
               DISPLAY 'AFTER  bytes 234-32500 POLICY-DATA = CHANGED'
           END-IF.

       SHOW-D2.
           DISPLAY 'AFTER  bytes 1-6   D2-REQUEST-ID =['
                   DRV-BUFFER(1:6) ']'
           DISPLAY 'AFTER  bytes 7-8   D2-RETURN-CODE=['
                   DRV-BUFFER(7:2) ']'
           DISPLAY 'AFTER  bytes 9-18  D2-CUSTOMER-NUM=['
                   DRV-BUFFER(9:10) ']'
           DISPLAY 'AFTER  bytes 19-50 D2-CUSTSECR-PASS=['
                   DRV-BUFFER(19:32) ']'
           DISPLAY 'AFTER  bytes 51-54 D2-CUSTSECR-COUNT=['
                   DRV-BUFFER(51:4) ']'
           DISPLAY 'AFTER  bytes 55    D2-CUSTSECR-STATE=['
                   DRV-BUFFER(55:1) ']'
           IF DRV-BUFFER(56:32445) = SPACES
               DISPLAY 'AFTER  bytes 56-32500 D2-CUSTSECR-DATA = '
                       'all spaces'
           ELSE
               DISPLAY 'AFTER  bytes 56-32500 D2-CUSTSECR-DATA = '
                       'CHANGED'
           END-IF.
