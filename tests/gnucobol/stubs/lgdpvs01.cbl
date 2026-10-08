      ******************************************************************
      * STUB for LGDPVS01 (VSAM layer of DELETE policy).               *
      * Stands in for EXEC CICS LINK PROGRAM(LGDPVS01). Does not       *
      * touch the COMMAREA.                                            *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGDPVS01.
       DATA DIVISION.
       LINKAGE SECTION.
       01  DFHCOMMAREA.
             Copy LGCMAREA.
       PROCEDURE DIVISION USING DFHCOMMAREA.
           DISPLAY 'STUB LGDPVS01 reached, CA-REQUEST-ID='
                   CA-REQUEST-ID ' CA-RETURN-CODE=' CA-RETURN-CODE
           GOBACK.
