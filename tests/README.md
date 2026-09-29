# tests

Planned test projects:

- `GenApp.EquivalenceTests` – tests derived from the COBOL logic that capture the legacy behaviour: return codes
  (00, 01, 02, 70, 80, 81, 82, 88, 89, 90, 98, 99), optimistic locking with LASTCHANGED, and input checks.
  Selected programs are also run in GnuCOBOL, with CICS and Db2 calls replaced by stubs, to check these tests.
- `GenApp.IntegrationTests` – API + PostgreSQL tests
- Mutation testing with Stryker.NET measures how many planted bugs the tests catch
