      ******************************************************************
      * STUB for the Db2 statements the rig does not support: the      *
      * SELECTs of the endowment, house and commercial branches and    *
      * the cursor statements (OPEN, FETCH, CLOSE) of LGIPDB01. The    *
      * 05-xx predictions cover the motor branch only, so no case      *
      * reaches them. If one does, this stub says so and stops the     *
      * process with return code 12: the case is not valid. It does    *
      * not set SQLCODE or fill anything, so it cannot make up a       *
      * result.                                                        *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DB2UNSUP.
       DATA DIVISION.
       LINKAGE SECTION.
           COPY SQLCA.
       PROCEDURE DIVISION USING SQLCA.
           DISPLAY 'RIG ERROR: DB2UNSUP reached. The rig does not'
                   ' support this Db2 statement; the case is invalid.'
           MOVE 12 TO RETURN-CODE
           STOP RUN.
