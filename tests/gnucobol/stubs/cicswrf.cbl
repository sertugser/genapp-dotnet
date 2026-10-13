      ******************************************************************
      * STUB for EXEC CICS WRITE FILE('KSDSCUST') FROM(CA-Customer-Num)*
      *   LENGTH(CUSTOMER-RECORD-SIZE) RIDFLD(CA-Customer-Num)         *
      *   KEYLENGTH(10) RESP(WS-RESP) in LGACVS01.                     *
      * There is no VSAM file. The stub shows what the program asked   *
      * CICS to write: the key (first 10 bytes of the FROM area) and   *
      * the record (the first LENGTH bytes of the FROM area).          *
      * The test case decides the answer (precondition B4):            *
      *   - WRF-RESP not 0: the write fails with that RESP, and        *
      *     EIBRESP2 gets WRF-RESP2.                                   *
      *   - otherwise RESP = 0 (NORMAL).                               *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CICSWRF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WRF-RESP                    PIC S9(9) COMP-5 IS EXTERNAL.
       01  WRF-RESP2                   PIC S9(9) COMP-5 IS EXTERNAL.
       01  EIBRESP2                    PIC S9(8) COMP-5 IS EXTERNAL.
       01  DSP-LEN                     PIC -(4)9.
       01  DSP-RESP                    PIC -(8)9.
       LINKAGE SECTION.
       01  LK-REC                      PIC X(32500).
       01  LK-LEN                      PIC S9(4) BINARY.
       01  LK-RESP                     PIC S9(8) COMP.
       PROCEDURE DIVISION USING LK-REC LK-LEN LK-RESP.
           MOVE LK-LEN TO DSP-LEN
           DISPLAY 'STUB CICSWRF Write File KSDSCUST length=' DSP-LEN
           DISPLAY 'STUB CICSWRF key=[' LK-REC(1:10) ']'
           DISPLAY 'STUB CICSWRF record=[' LK-REC(1:LK-LEN) ']'
           IF WRF-RESP NOT = 0
               MOVE WRF-RESP TO LK-RESP
               MOVE WRF-RESP2 TO EIBRESP2
           ELSE
               MOVE 0 TO LK-RESP
               MOVE 0 TO EIBRESP2
           END-IF
           MOVE LK-RESP TO DSP-RESP
           DISPLAY 'STUB CICSWRF answers RESP=' DSP-RESP
           GOBACK.
