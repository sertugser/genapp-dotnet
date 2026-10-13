      ******************************************************************
      *                                                                *
      * (C) Copyright IBM Corp. 2011, 2020                             *
      *                                                                *
      *                    ADD Customer                                *
      *                                                                *
      * VSAM KSDS Customer record ADD                                  *
      *                                                                *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGACVS01.
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
       01  EIBRESP2                    PIC S9(8) COMP-5 IS EXTERNAL.

       01  WS-RESP                   PIC S9(8) COMP.
       01  WS-RESP2                  PIC S9(8) COMP.
       01  WS-Comm-Len               PIC S9(8) COMP.
       01  WS-STARTCODE              PIC XX Value spaces.
       01  WS-SYSID                  PIC X(4) Value spaces.
       01  WS-Commarea-Len           PIC S9(4) COMP.
      ******************************
      * Variables for time/date processing
       01  WS-ABSTIME                  PIC S9(8) COMP VALUE +0.
       01  WS-TIME                     PIC X(8)  VALUE SPACES.
       01  WS-DATE                     PIC X(10) VALUE SPACES.
      * Error Message structure
       01  ERROR-MSG.
           03 EM-DATE                  PIC X(8)  VALUE SPACES.
           03 FILLER                   PIC X     VALUE SPACES.
           03 EM-TIME                  PIC X(6)  VALUE SPACES.
           03 FILLER                   PIC X(9)  VALUE ' LGACVS01'.
           03 EM-VARIABLE.
             05 FILLER                 PIC X(6)  VALUE ' CNUM='.
             05 EM-CUSNUM              PIC X(10)  VALUE SPACES.
             05 FILLER                 PIC X(20)
                                        Value ' Write file KSDSCUST'.
             05 FILLER                 PIC X(6)  VALUE ' RESP='.
             05 EM-RESPRC              PIC +9(5) USAGE DISPLAY.
             05 FILLER                 PIC X(7)  VALUE ' RESP2='.
             05 EM-RESP2RC             PIC +9(5) USAGE DISPLAY.

       01  CA-ERROR-MSG.
           03 FILLER                   PIC X(9)  VALUE 'COMMAREA='.
           03 CA-DATA                  PIC X(90) VALUE SPACES.
       
       01  CUSTOMER-RECORD-SIZE        PIC S9(4) BINARY VALUE 0225.

      *****************************************************************
      *    L I N K A G E     S E C T I O N
      *****************************************************************
       LINKAGE SECTION.
       01  DFHCOMMAREA.
         Copy LGCMAREA.

      *----------------------------------------------------------------*
      *****************************************************************
       PROCEDURE DIVISION USING DFHCOMMAREA.

      *---------------------------------------------------------------*
       MAINLINE SECTION.
      *
      *---------------------------------------------------------------*
           Move EIBCALEN To WS-Commarea-Len.
      *---------------------------------------------------------------*
           DISPLAY 'STUB EXEC CICS WRITE FILE(KSDSCUST)'
           CALL 'CICSWRF' USING CA-Customer-Num CUSTOMER-RECORD-SIZE
               WS-RESP.
           If WS-RESP Not = 0
             Move EIBRESP2 To WS-RESP2
             MOVE '80' TO CA-RETURN-CODE
             PERFORM WRITE-ERROR-MESSAGE
             DISPLAY 'STUB EXEC CICS ABEND ABCODE(LGV0)'
             MOVE 'Y' TO ABEND-FLAG
             GOBACK
             GOBACK
           End-If.

      *---------------------------------------------------------------*

       A-EXIT.
           EXIT.
           GOBACK.
      *---------------------------------------------------------------*
       WRITE-ERROR-MESSAGE.
           DISPLAY 'STUB EXEC CICS ASKTIME ABSTIME(WS-ABSTIME)'
           DISPLAY 'STUB EXEC CICS FORMATTIME ABSTIME(WS-ABSTIME)'
      *
           MOVE WS-DATE TO EM-DATE
           MOVE WS-TIME TO EM-TIME
           Move CA-Customer-Num To EM-Cusnum
           Move WS-RESP         To EM-RespRC
           Move WS-RESP2        To EM-Resp2RC
           CALL 'LGSTSQ' USING ERROR-MSG
                     BY CONTENT LENGTH OF ERROR-MSG.
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
