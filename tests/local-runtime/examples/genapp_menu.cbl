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
      *   For each of lgacdb01, lgacdb02, lgicdb01, lgucdb01, lgapdb01 (EXEC SQL -
      *   lgapdb01.cbl needs DB2-M-PREMIUM-int/DB2-M-ACCIDENTS-int uppercased to
      *   -INT in the working copy first, gixpp's host-var lookup is case-sensitive
      *   and the original source is inconsistent there - see local-cics-runtime-plan.md):
      *     gixpp -e -S -I <copy dir> -i base/src/X.cbl -o X.sql.cbl
      *     preprocess_cics.py X.sql.cbl X.pp.cbl
      *   For each of lgacus01, lgacvs01, lgicus01, lgucus01, lgucvs01, lgapol01,
      *   lgapvs01 (CICS only):
      *     preprocess_cics.py base/src/X.cbl X.pp.cbl
      *   cobc -c -o X.o X.pp.cbl -I <copy dir>                  for all twelve
      *   cobc -c -o lgstsq_stub.o lgstsq_stub.cbl
      *   cobc -c -x -o menu.o genapp_menu.cbl -I <copy dir>
      *   cobc -x -o genapp_menu.exe menu.o lgacus01.o lgacdb01.o lgacdb02.o
      *        lgacvs01.o lgicus01.o lgicdb01.o lgucus01.o lgucdb01.o lgucvs01.o
      *        lgapol01.o lgapdb01.o lgapvs01.o
      *        lgstsq_stub.o genapp_sqlstub.o genapp_vsam_stub.o
      *        -L <postgres lib dir> -lpq -lstdc++
      * Run with DATASRC/DATASRC_USR/DATASRC_PWD and GENAPP_VSAM_DIR set, then
      * just type at the menu - no piped input needed for interactive use.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GENAPPM1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COMM-AREA.
          COPY LGCMAREA.
       01 WS-OPTION        PIC X(2).
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
                   WHEN '3' PERFORM DO-ADD-MOTOR
                   WHEN '4' PERFORM DO-UPDATE
                   WHEN '5' PERFORM DO-INQUIRY-MOTOR
                   WHEN '6' PERFORM DO-DELETE-MOTOR
                   WHEN '7' PERFORM DO-UPDATE-MOTOR
                   WHEN '8' PERFORM DO-ADD-HOUSE
                   WHEN '9' PERFORM DO-INQUIRY-HOUSE
                   WHEN '10' PERFORM DO-DELETE-HOUSE
                   WHEN '11' PERFORM DO-UPDATE-HOUSE
                   WHEN '12' PERFORM DO-ADD-ENDOW
                   WHEN '13' PERFORM DO-INQUIRY-ENDOW
                   WHEN '14' PERFORM DO-DELETE-ENDOW
                   WHEN '15' PERFORM DO-UPDATE-ENDOW
                   WHEN '16' PERFORM DO-ADD-COMM
                   WHEN '17' PERFORM DO-INQUIRY-COMM
                   WHEN '18' PERFORM DO-DELETE-COMM
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
           DISPLAY "  3. Motor Policy Add".
           DISPLAY "  4. Cust Update".
           DISPLAY "  5. Motor Policy Inquiry".
           DISPLAY "  6. Motor Policy Delete".
           DISPLAY "  7. Motor Policy Update".
           DISPLAY "  8. House Policy Add".
           DISPLAY "  9. House Policy Inquiry".
           DISPLAY " 10. House Policy Delete".
           DISPLAY " 11. House Policy Update".
           DISPLAY " 12. Endowment Policy Add".
           DISPLAY " 13. Endowment Policy Inquiry".
           DISPLAY " 14. Endowment Policy Delete".
           DISPLAY " 15. Endowment Policy Update".
           DISPLAY " 16. Commercial Policy Add".
           DISPLAY " 17. Commercial Policy Inquiry".
           DISPLAY " 18. Commercial Policy Delete".
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

       DO-ADD-MOTOR.
           INITIALIZE COMM-AREA
           MOVE '01AMOT' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Issue date   (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-ISSUE-DATE
           DISPLAY "Expiry date  (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-EXPIRY-DATE
           DISPLAY "Broker ID                   : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-BROKERID
           DISPLAY "Broker's Reference          : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-BROKERSREF
           DISPLAY "Payment                     : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-PAYMENT
           DISPLAY "Car Make                    : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-M-MAKE
           DISPLAY "Car Model                   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-M-MODEL
           DISPLAY "Car Value                   : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-M-VALUE
           DISPLAY "Registration                : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-M-REGNUMBER
           DISPLAY "Car Colour                  : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-M-COLOUR
           DISPLAY "CC                          : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-M-CC
           DISPLAY "Manufacture Date(yyyy-mm-dd): " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-M-MANUFACTURED
           DISPLAY "Premium                     : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-M-PREMIUM
           DISPLAY "Accidents                   : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-M-ACCIDENTS
           CALL "LGAPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY "New Policy Inserted, Policy Number = "
                       CA-POLICY-NUM
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-DELETE-MOTOR.
           INITIALIZE COMM-AREA
           MOVE '01DMOT' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Policy Number                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-POLICY-NUM
           CALL "LGDPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY "Policy deleted"
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-INQUIRY-MOTOR.
           INITIALIZE COMM-AREA
           MOVE '01IMOT' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Policy Number                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-POLICY-NUM
           CALL "LGIPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY " "
               DISPLAY "Issue date  : " CA-ISSUE-DATE
               DISPLAY "Expiry date : " CA-EXPIRY-DATE
               DISPLAY "Broker ID   : " CA-BROKERID
               DISPLAY "Broker Ref  : " CA-BROKERSREF
               DISPLAY "Payment     : " CA-PAYMENT
               DISPLAY "Car Make    : " CA-M-MAKE
               DISPLAY "Car Model   : " CA-M-MODEL
               DISPLAY "Car Value   : " CA-M-VALUE
               DISPLAY "Registration: " CA-M-REGNUMBER
               DISPLAY "Car Colour  : " CA-M-COLOUR
               DISPLAY "CC          : " CA-M-CC
               DISPLAY "Manufactured: " CA-M-MANUFACTURED
               DISPLAY "Premium     : " CA-M-PREMIUM
               DISPLAY "Accidents   : " CA-M-ACCIDENTS
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-UPDATE-MOTOR.
           INITIALIZE COMM-AREA
           MOVE '01IMOT' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Policy Number                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-POLICY-NUM
      *    Fetch current values first - also gets the LASTCHANGED
      *    timestamp LGUPOL01 needs for its optimistic-lock check
      *    (CA-REQUEST-ID gets overwritten below, CA-LASTCHANGED isn't
      *    touched again so it carries straight through unmodified).
           CALL "LGIPOL01" USING COMM-AREA
           IF CA-RETURN-CODE NOT = '00'
             DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           ELSE
           DISPLAY " "
           DISPLAY "Current values shown - enter new values below."
           MOVE '01UMOT' TO CA-REQUEST-ID
           DISPLAY "Issue date   (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-ISSUE-DATE
           DISPLAY "Expiry date  (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-EXPIRY-DATE
           DISPLAY "Broker ID                   : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-BROKERID
           DISPLAY "Broker's Reference          : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-BROKERSREF
           DISPLAY "Payment                     : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-PAYMENT
           DISPLAY "Car Make                    : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-M-MAKE
           DISPLAY "Car Model                   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-M-MODEL
           DISPLAY "Car Value                   : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-M-VALUE
           DISPLAY "Registration                : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-M-REGNUMBER
           DISPLAY "Car Colour                  : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-M-COLOUR
           DISPLAY "CC                          : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-M-CC
           DISPLAY "Manufacture Date(yyyy-mm-dd): " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-M-MANUFACTURED
           DISPLAY "Premium                     : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-M-PREMIUM
           DISPLAY "Accidents                   : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-M-ACCIDENTS
           CALL "LGUPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
             DISPLAY "Policy updated"
           ELSE
             DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF
           END-IF.

       DO-ADD-HOUSE.
           INITIALIZE COMM-AREA
           MOVE '01AHOU' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Issue date   (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-ISSUE-DATE
           DISPLAY "Expiry date  (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-EXPIRY-DATE
           DISPLAY "Broker ID                   : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-BROKERID
           DISPLAY "Broker's Reference          : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-BROKERSREF
           DISPLAY "Payment                     : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-PAYMENT
           DISPLAY "Property Type                : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-H-PROPERTY-TYPE
           DISPLAY "Bedrooms                    : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-H-BEDROOMS
           DISPLAY "Value                        : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-H-VALUE
           DISPLAY "House Name                   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-H-HOUSE-NAME
           DISPLAY "House Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-H-HOUSE-NUMBER
           DISPLAY "Postcode                     : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-H-POSTCODE
           CALL "LGAPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY "New Policy Inserted, Policy Number = "
                       CA-POLICY-NUM
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-INQUIRY-HOUSE.
           INITIALIZE COMM-AREA
           MOVE '01IHOU' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Policy Number                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-POLICY-NUM
           CALL "LGIPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY " "
               DISPLAY "Issue date   : " CA-ISSUE-DATE
               DISPLAY "Expiry date  : " CA-EXPIRY-DATE
               DISPLAY "Broker ID    : " CA-BROKERID
               DISPLAY "Broker Ref   : " CA-BROKERSREF
               DISPLAY "Payment      : " CA-PAYMENT
               DISPLAY "Property Type: " CA-H-PROPERTY-TYPE
               DISPLAY "Bedrooms     : " CA-H-BEDROOMS
               DISPLAY "Value        : " CA-H-VALUE
               DISPLAY "House Name   : " CA-H-HOUSE-NAME
               DISPLAY "House Number : " CA-H-HOUSE-NUMBER
               DISPLAY "Postcode     : " CA-H-POSTCODE
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-DELETE-HOUSE.
           INITIALIZE COMM-AREA
           MOVE '01DHOU' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Policy Number                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-POLICY-NUM
           CALL "LGDPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY "Policy deleted"
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-UPDATE-HOUSE.
           INITIALIZE COMM-AREA
           MOVE '01IHOU' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Policy Number                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-POLICY-NUM
           CALL "LGIPOL01" USING COMM-AREA
           IF CA-RETURN-CODE NOT = '00'
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           ELSE
           DISPLAY " "
           DISPLAY "Current values shown - enter new values below."
           MOVE '01UHOU' TO CA-REQUEST-ID
           DISPLAY "Issue date   (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-ISSUE-DATE
           DISPLAY "Expiry date  (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-EXPIRY-DATE
           DISPLAY "Broker ID                   : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-BROKERID
           DISPLAY "Broker's Reference          : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-BROKERSREF
           DISPLAY "Property Type                : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-H-PROPERTY-TYPE
           DISPLAY "Bedrooms                    : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-H-BEDROOMS
           DISPLAY "Value                        : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-H-VALUE
           DISPLAY "House Name                   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-H-HOUSE-NAME
           DISPLAY "House Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-H-HOUSE-NUMBER
           DISPLAY "Postcode                     : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-H-POSTCODE
           CALL "LGUPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
             DISPLAY "Policy updated"
           ELSE
             DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF
           END-IF.

       DO-ADD-ENDOW.
           INITIALIZE COMM-AREA
           MOVE '01AEND' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Issue date   (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-ISSUE-DATE
           DISPLAY "Expiry date  (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-EXPIRY-DATE
           DISPLAY "Broker ID                   : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-BROKERID
           DISPLAY "Broker's Reference          : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-BROKERSREF
           DISPLAY "Payment                     : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-PAYMENT
           DISPLAY "With Profits (Y/N)           : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-E-WITH-PROFITS
           DISPLAY "Equities (Y/N)               : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-E-EQUITIES
           DISPLAY "Managed Fund (Y/N)           : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-E-MANAGED-FUND
           DISPLAY "Fund Name                    : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-E-FUND-NAME
           DISPLAY "Term (years)                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-E-TERM
           DISPLAY "Sum Assured                  : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-E-SUM-ASSURED
           DISPLAY "Life Assured                 : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-E-LIFE-ASSURED
           CALL "LGAPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY "New Policy Inserted, Policy Number = "
                       CA-POLICY-NUM
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-INQUIRY-ENDOW.
           INITIALIZE COMM-AREA
           MOVE '01IEND' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Policy Number                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-POLICY-NUM
           CALL "LGIPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY " "
               DISPLAY "Issue date  : " CA-ISSUE-DATE
               DISPLAY "Expiry date : " CA-EXPIRY-DATE
               DISPLAY "Broker ID   : " CA-BROKERID
               DISPLAY "Broker Ref  : " CA-BROKERSREF
               DISPLAY "Payment     : " CA-PAYMENT
               DISPLAY "WithProfits : " CA-E-WITH-PROFITS
               DISPLAY "Equities    : " CA-E-EQUITIES
               DISPLAY "ManagedFund : " CA-E-MANAGED-FUND
               DISPLAY "Fund Name   : " CA-E-FUND-NAME
               DISPLAY "Term        : " CA-E-TERM
               DISPLAY "Sum Assured : " CA-E-SUM-ASSURED
               DISPLAY "Life Assured: " CA-E-LIFE-ASSURED
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-DELETE-ENDOW.
           INITIALIZE COMM-AREA
           MOVE '01DEND' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Policy Number                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-POLICY-NUM
           CALL "LGDPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY "Policy deleted"
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-UPDATE-ENDOW.
           INITIALIZE COMM-AREA
           MOVE '01IEND' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Policy Number                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-POLICY-NUM
           CALL "LGIPOL01" USING COMM-AREA
           IF CA-RETURN-CODE NOT = '00'
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           ELSE
           DISPLAY " "
           DISPLAY "Current values shown - enter new values below."
           MOVE '01UEND' TO CA-REQUEST-ID
           DISPLAY "Issue date   (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-ISSUE-DATE
           DISPLAY "Expiry date  (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-EXPIRY-DATE
           DISPLAY "Broker ID                   : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-BROKERID
           DISPLAY "Broker's Reference          : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-BROKERSREF
           DISPLAY "With Profits (Y/N)           : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-E-WITH-PROFITS
           DISPLAY "Equities (Y/N)               : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-E-EQUITIES
           DISPLAY "Managed Fund (Y/N)           : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-E-MANAGED-FUND
           DISPLAY "Fund Name                    : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-E-FUND-NAME
           DISPLAY "Term (years)                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-E-TERM
           DISPLAY "Sum Assured                  : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-E-SUM-ASSURED
           DISPLAY "Life Assured                 : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-E-LIFE-ASSURED
           CALL "LGUPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
             DISPLAY "Policy updated"
           ELSE
             DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF
           END-IF.

       DO-ADD-COMM.
           INITIALIZE COMM-AREA
           MOVE '01ACOM' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Issue date   (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-ISSUE-DATE
           DISPLAY "Expiry date  (yyyy-mm-dd)   : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-EXPIRY-DATE
           DISPLAY "Broker ID                   : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-BROKERID
           DISPLAY "Broker's Reference          : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-BROKERSREF
           DISPLAY "Payment                     : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-PAYMENT
           DISPLAY "Address                      : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-B-Address
           DISPLAY "Postcode                     : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-B-Postcode
           DISPLAY "Latitude                     : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-B-Latitude
           DISPLAY "Longitude                    : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-B-Longitude
           DISPLAY "Customer (business name)     : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-B-Customer
           DISPLAY "Property Type                : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-B-PropType
           DISPLAY "Fire Peril                   : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-B-FirePeril
           DISPLAY "Fire Premium                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-B-FirePremium
           DISPLAY "Crime Peril                  : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-B-CrimePeril
           DISPLAY "Crime Premium                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-B-CrimePremium
           DISPLAY "Flood Peril                  : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-B-FloodPeril
           DISPLAY "Flood Premium                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-B-FloodPremium
           DISPLAY "Weather Peril                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-B-WeatherPeril
           DISPLAY "Weather Premium              : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-B-WeatherPremium
           DISPLAY "Status                       : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-B-Status
           DISPLAY "Reject Reason                : " WITH NO ADVANCING
           ACCEPT WS-IN-TEXT
           MOVE WS-IN-TEXT TO CA-B-RejectReason
           CALL "LGAPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY "New Policy Inserted, Policy Number = "
                       CA-POLICY-NUM
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-INQUIRY-COMM.
           INITIALIZE COMM-AREA
           MOVE '01ICOM' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Policy Number                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-POLICY-NUM
           CALL "LGIPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY " "
               DISPLAY "Issue date (StartDate)  : " CA-ISSUE-DATE
               DISPLAY "Expiry date (RenewDate) : " CA-EXPIRY-DATE
               DISPLAY "Address                 : " CA-B-Address
               DISPLAY "Postcode                : " CA-B-Postcode
               DISPLAY "Latitude                : " CA-B-Latitude
               DISPLAY "Longitude               : " CA-B-Longitude
               DISPLAY "Customer                : " CA-B-Customer
               DISPLAY "Property Type           : " CA-B-PropType
               DISPLAY "Fire Peril              : " CA-B-FirePeril
               DISPLAY "Fire Premium            : " CA-B-FirePremium
               DISPLAY "Crime Peril             : " CA-B-CrimePeril
               DISPLAY "Crime Premium           : " CA-B-CrimePremium
               DISPLAY "Flood Peril             : " CA-B-FloodPeril
               DISPLAY "Flood Premium           : " CA-B-FloodPremium
               DISPLAY "Weather Peril           : " CA-B-WeatherPeril
               DISPLAY "Weather Premium         : " CA-B-WeatherPremium
               DISPLAY "Status                  : " CA-B-Status
               DISPLAY "Reject Reason           : " CA-B-RejectReason
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.

       DO-DELETE-COMM.
           INITIALIZE COMM-AREA
           MOVE '01DCOM' TO CA-REQUEST-ID
           DISPLAY "Cust Number                 : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-CUSTOMER-NUM
           DISPLAY "Policy Number                : " WITH NO ADVANCING
           ACCEPT WS-IN-NUM
           MOVE WS-IN-NUM TO CA-POLICY-NUM
           CALL "LGDPOL01" USING COMM-AREA
           IF CA-RETURN-CODE = '00'
               DISPLAY "Policy deleted"
           ELSE
               DISPLAY "Error - CA-RETURN-CODE=" CA-RETURN-CODE
           END-IF.
