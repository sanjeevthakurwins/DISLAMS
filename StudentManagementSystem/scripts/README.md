# Seed Scripts

This folder contains SQL and helper scripts to populate test data into the DISLAMS Student Management database.

Files:
- `seed_data.sql` - SQL script that inserts sample data for Actors, Students, Courses, AttendanceRecords, AuditLogs, AttendanceExceptions, ReopenRequests. Uses fixed GUIDs so the data is repeatable.
- `run-seed.ps1` - PowerShell helper to run the SQL script. Tries `Invoke-Sqlcmd` if available; otherwise falls back to `sqlcmd.exe`.

How to run:
1. Ensure the database `DISLAMS_StudentManagement` exists on your server (migration step should have created it).
2. Open PowerShell with an account that can connect to SQL Server.
3. Run `./run-seed.ps1` (or `./run-seed.ps1 -Server "YourServerName" -Database "DISLAMS_StudentManagement"`).

Or you can run SQL directly:

sqlcmd -S Ditsdev346 -d DISLAMS_StudentManagement -i .\scripts\seed_data.sql

Notes:
- The script uses fixed GUIDs for IDs to make re-running easier for tests. If you re-run without clearing data you may get primary key or unique constraint errors. Consider dropping and recreating the database if you want a clean run.
- The seed data includes examples of Draft, Submitted, Approved, Published, Locked and Corrected attendance records, plus exceptions and reopen requests for testing.
