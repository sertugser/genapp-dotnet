      ******************************************************************
      * STUB for LGAPDB01 (data layer of ADD policy).                  *
      * Stands in for EXEC CICS LINK PROGRAM(LGAPDB01). It does no     *
      * inserts and does not touch the COMMAREA: it only reports that  *
      * it was reached and what it received.                           *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGAPDB01.
       DATA DIVISION.
       LINKAGE SECTION.
       01  DFHCOMMAREA.
             Copy LGCMAREA.
       PROCEDURE DIVISION USING DFHCOMMAREA.
           DISPLAY 'STUB LGAPDB01 reached, CA-REQUEST-ID='
                   CA-REQUEST-ID ' CA-RETURN-CODE=' CA-RETURN-CODE
           GOBACK.
