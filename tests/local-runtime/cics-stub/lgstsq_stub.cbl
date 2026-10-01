      * No-op stand-in for base/src/lgstsq.cbl, the error-logging helper
      * every WRITE-ERROR-MESSAGE paragraph across base/src LINKs to on
      * failure paths (writes to a TDQ we're not emulating). Only needed so
      * programs *link*; real CICS resolves CALL "literal" dynamically at
      * runtime, so this is never required to compile/link a program, only
      * to run one down an error path without crashing on "module not found".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGSTSQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       LINKAGE SECTION.
       01 DUMMY PIC X(90).
       PROCEDURE DIVISION USING DUMMY.
           GOBACK.
