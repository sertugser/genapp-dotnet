      * Batch replacement for the 3270 menu (lgtestc1.cbl) that would
      * normally drive LGACUS01 - fills the COMMAREA directly and LINKs in
      * (via a plain COBOL CALL once preprocessed), the same contract a real
      * 3270 "add customer" screen submission would use. Proven end-to-end
      * against the real lgacus01/lgacdb01/lgacdb02/lgacvs01 chain - see
      * docs/local-cics-runtime-plan.md, Step 4. Template for driving the
      * other 17 operations the same way.
      *
      * Build (from this directory, after setup.ps1 + setup-sql-bridge.ps1 +
      * setup-cics-stub.ps1 have been run once):
      *   gixpp -e -S -I <copy dir> -i base/src/lgacdb01.cbl -o lgacdb01.sql.cbl
      *   gixpp -e -S -I <copy dir> -i base/src/lgacdb02.cbl -o lgacdb02.sql.cbl
      *   preprocess_cics.py lgacdb01.sql.cbl lgacdb01.pp.cbl   (and lgacdb02, lgacus01.cbl, lgacvs01.cbl directly)
      *   cobc -c -o X.o X.pp.cbl -I <copy dir>                 for each of the four
      *   cobc -c -x -o driver.o add_customer_driver.cbl -I <copy dir>
      *   cobc -x -o add_customer.exe driver.o lgacus01.o lgacdb01.o lgacdb02.o
      *        lgacvs01.o lgstsq_stub.o genapp_sqlstub.o genapp_vsam_stub.o
      *        -L <postgres lib dir> -lpq -lstdc++
      * Run with DATASRC/DATASRC_USR/DATASRC_PWD and GENAPP_VSAM_DIR set (see
      * setup-sql-bridge.ps1 / setup-cics-stub.ps1 output for the exact values).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DRIVER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COMM-AREA.
          COPY LGCMAREA.
       PROCEDURE DIVISION.
           MOVE '01ACUS' TO CA-REQUEST-ID
           MOVE 0 TO CA-CUSTOMER-NUM
           MOVE 'JOHN      ' TO CA-FIRST-NAME
           MOVE 'SMITH               ' TO CA-LAST-NAME
           MOVE '1975-05-20' TO CA-DOB
           MOVE 'ROSE COTTAGE        ' TO CA-HOUSE-NAME
           MOVE '7   ' TO CA-HOUSE-NUM
           MOVE 'SW1A 1AA' TO CA-POSTCODE
           MOVE '07700900123         ' TO CA-PHONE-MOBILE
           MOVE '02079460000         ' TO CA-PHONE-HOME
           MOVE 'john.smith@example.com'
                                        TO CA-EMAIL-ADDRESS
           DISPLAY "Calling LGACUS01 (Add Customer)..."
           CALL "LGACUS01" USING COMM-AREA
           DISPLAY "CA-RETURN-CODE=" CA-RETURN-CODE
           DISPLAY "CA-CUSTOMER-NUM=" CA-CUSTOMER-NUM
           STOP RUN.
