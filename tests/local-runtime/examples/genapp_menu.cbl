      * Text-mode stand-in for the SSMAPC1 3270 screen (base/src/ssmap.bms)
      * - same menu options, same fields, same order - driving the real
      * LGACUS01/LGICUS01/LGUCUS01 business chain through plain terminal
      * DISPLAY/ACCEPT instead of EXEC CICS SEND MAP/RECEIVE MAP. Not a
      * 3270 protocol reproduction (see docs/local-cics-runtime-plan.md,
      * "old screen" discussion) - a functional interim UI until the real
      * ASP.NET Core frontend exists. All three customer operations proven
      * working through this menu (Inquiry/Add/Update), each against the
      * real base/src programs, unmodified.
      *
      * Build (after setup.ps1 + setup-sql-bridge.ps1 + setup-cics-stub.ps1):
      *   For each of lgacdb01, lgacdb02, lgicdb01, lgucdb01 (EXEC SQL):
      *     gixpp -e -S -I <copy dir> -i base/src/X.cbl -o X.sql.cbl
      *     preprocess_cics.py X.sql.cbl X.pp.cbl
      *   For each of lgacus01, lgacvs01, lgicus01, lgucus01, lgucvs01 (CICS only):
      *     preprocess_cics.py base/src/X.cbl X.pp.cbl
      *   cobc -c -o X.o X.pp.cbl -I <copy dir>                  for all nine
      *   cobc -c -o lgstsq_stub.o lgstsq_stub.cbl
      *   cobc -c -x -o menu.o genapp_menu.cbl -I <copy dir>
      *   cobc -x -o genapp_menu.exe menu.o lgacus01.o lgacdb01.o lgacdb02.o
      *        lgacvs01.o lgicus01.o lgicdb01.o lgucus01.o lgucdb01.o
      *        lgucvs01.o lgstsq_stub.o genapp_sqlstub.o genapp_vsam_stub.o
      *        -L <postgres lib dir> -lpq -lstdc++
      * Run with DATASRC/DATASRC_USR/DATASRC_PWD and GENAPP_VSAM_DIR set, then
      * just type at the menu - no piped input needed for interactive use.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GENAPPM1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COMM-AREA.
          COPY LGCMAREA.
       01 WS-OPTION        PIC X.
       01 WS-IN-NUM        PIC X(10).
       01 WS-IN-TEXT       PIC X(100).
       01 WS-DONE          PIC X VALUE 'N'.

       PROCEDURE DIVISION.
       MAIN-LOOP.
           PERFORM UNTIL WS-DONE = 'Y'
               PERFORM SHOW-MENU
               ACCEPT WS-OPTION
               EVALUATE WS-OPTION
                   WHEN '1' PERFORM DO-INQUIRY
                   WHEN '2' PERFORM DO-ADD
                   WHEN '4' PERFORM DO-UPDATE
                   WHEN '0' MOVE 'Y' TO WS-DONE
                   WHEN OTHER
                       DISPLAY "Please enter a valid option"
               END-EVALUATE
           END-PERFORM
           STOP RUN.

       SHOW-MENU.
           DISPLAY " ".
           DISPLAY "SSC1   General Insurance Customer Menu".
           DISPLAY " ".
           DISPLAY "  1. Cust Inquiry".
           DISPLAY "  2. Cust Add".
           DISPLAY "  4. Cust Update".
           DISPLAY "  0. Exit".
           DISPLAY " ".
           DISPLAY "Select Option: " WITH NO ADVANCING.

       PROMPT-CUSTOMER-FIELDS.
           DISPLAY "Cust Name :First           : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-FIRST-NAME
           DISPLAY "          :Last             : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-LAST-NAME
           DISPLAY "DOB          (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-DOB
           DISPLAY "House Name                  : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-HOUSE-NAME
           DISPLAY "House Number                : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-HOUSE-NUM
           DISPLAY "Postcode                    : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-POSTCODE
           DISPLAY "Phone: Home                 : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-PHONE-HOME
           DISPLAY "Phone: Mob                  : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-PHONE-MOBILE
           DISPLAY "Email  Addr                 : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-EMAIL-ADDRESS.

       DO-INQUIRY.
           INITIALIZE COMM-AREA
           MOVE '01ICUS' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           CALL "LGICUS01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY " "
               DISPLAY "Cust Number : " CA-CUSTOMER-NUM
               DISPLAY "First Name  : " CA-FIRST-NAME
               DISPLAY "Last Name   : " CA-LAST-NAME
               DISPLAY "DOB         : " CA-DOB
               DISPLAY "House Name  : " CA-HOUSE-NAME
               DISPLAY "House Number: " CA-HOUSE-NUM
               DISPLAY "Postcode    : " CA-POSTCODE
               DISPLAY "Phone Home  : " CA-PHONE-HOME
               DISPLAY "Phone Mobile: " CA-PHONE-MOBILE
               DISPLAY "Email       : " CA-EMAIL-ADDRESS
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-ADD.
           INITIALIZE COMM-AREA
           MOVE '01ACUS' TO CA-REQUEST-ID
           MOVE 0 TO CA-CUSTOMER-NUM
           PERFORM PROMPT-CUSTOMER-FIELDS
           CALL "LGACUS01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY "New Customer Inserted, Cust Number = "
                       CA-CUSTOMER-NUM
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-UPDATE.
           INITIALIZE COMM-AREA
           MOVE '01UCUS' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           PERFORM PROMPT-CUSTOMER-FIELDS
           CALL "LGUCUS01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY "Customer details updated"
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.
