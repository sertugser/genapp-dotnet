      ******************************************************************
      * Minimal SQLCA for GnuCOBOL runs (stands in for                 *
      * EXEC SQL INCLUDE SQLCA). Same field names and types as the     *
      * Db2 SQLCA; only the fields the GenApp programs use are filled  *
      * in by the stub. No VALUE clauses, so it can also be used in    *
      * a LINKAGE SECTION.                                             *
      ******************************************************************
       01  SQLCA.
           05 SQLCAID                  PIC X(8).
           05 SQLCABC                  PIC S9(9) COMP-5.
           05 SQLCODE                  PIC S9(9) COMP-5.
           05 SQLERRM.
              49 SQLERRML              PIC S9(4) COMP-5.
              49 SQLERRMC              PIC X(70).
           05 SQLERRP                  PIC X(8).
           05 SQLERRD                  OCCURS 6 TIMES
                                       PIC S9(9) COMP-5.
           05 SQLWARN                  PIC X(11).
           05 SQLSTATE                 PIC X(5).
