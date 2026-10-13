      ******************************************************************
      * STUB for EXEC CICS GET COUNTER(GENACUSTNUM) POOL(GENA)         *
      *   VALUE(LastCustNum) RESP(WS-RESP) in LGACDB01.                *
      * There is no named counter server. The test case decides the    *
      * answer (prediction 01-customer-add.md, precondition B7 and B4):*
      *   - CNTR-RESP not 0: the command fails with that RESP, the     *
      *     VALUE field is not touched.                                *
      *   - otherwise RESP = 0 (NORMAL) and VALUE = CNTR-VALUE         *
      *     (11 by default: the prediction's K, B7).                   *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CICSCNT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CNTR-RESP                   PIC S9(9) COMP-5 IS EXTERNAL.
       01  CNTR-VALUE                  PIC S9(9) COMP-5 IS EXTERNAL.
       01  DSP-RESP                    PIC -(8)9.
       01  DSP-VALUE                   PIC +9(8).
       LINKAGE SECTION.
       01  LK-RESP                     PIC S9(8) COMP.
       01  LK-VALUE                    PIC S9(8) COMP.
       PROCEDURE DIVISION USING LK-RESP LK-VALUE.
           IF CNTR-RESP NOT = 0
               MOVE CNTR-RESP TO LK-RESP
           ELSE
               MOVE 0 TO LK-RESP
               MOVE CNTR-VALUE TO LK-VALUE
           END-IF
           MOVE LK-RESP TO DSP-RESP
           IF LK-RESP = 0
               MOVE LK-VALUE TO DSP-VALUE
               DISPLAY 'STUB CICSCNT answers RESP=' DSP-RESP
                       ' VALUE=' DSP-VALUE
           ELSE
               DISPLAY 'STUB CICSCNT answers RESP=' DSP-RESP
                       ' (VALUE not touched)'
           END-IF
           GOBACK.
