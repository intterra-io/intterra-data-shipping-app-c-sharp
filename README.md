# Intterra Data Shipping App (DSA)

Lightweight .net 4.5 app intended to ease custom Intterra integrations with CAD, AVL, and RMS systems.

# Details

While intended to be easy to use, there may be a few gotchyas or poweruser configurations you'll want to be aware of:

- __Queries__ accept a `{{LASTUPDATEDDATETIME}}` placeholder which will fetch the most recent timestamp from the data stored in Intterra's data center for your organization based on the data type selected. This can be a great way to send only the latest changes if your data contains a last modified timestamp, like so:

```
SELECT * 
FROM dbo.incidents 
WHERE last_updated_rms > '{{LASTUPDATEDDATETIME}}' 
ORDER BY incident_datetime;
```

- __ODBC__ connections are a great way to connect to relational databases other than MSSQL, but will probably require some additional configuration at the system level to install the appropriate ODBC driver. See documentation specific to your RDBMS. Here's an example of a query string that works for a __MySQL__ ODBC connection:

```
Driver={MySQL ODBC 5.3 Unicode Driver};Server=127.0.0.1;Port=3306;Database=test;UID=root
```

- __Windows Task Scheduler__ is used to manage background running of the application. This should be configured through the app. By default, the identity used to run the task is the `SYSTEM` user. If this undesirable for your environment, it's possible to change this directly in the Windows Task Scheduler.

- __Local Storage__ is used by the app to persist configurations and runtime logs. These files are located at `<SYSTEM>\ProgramData\Intterra\DSA`

# Release notes

## 3.x

#### New Features
- Smart hashing (optional) implemented to filter out data which is duplicated based on the previous run of the profile. SHA256 hashes ensure no data leakage into file system. 
- Allow for unlimited profiles.
- Separated background process into `DSA.exe`, a new executable, which may be run as a stand-alone CLI.
- Auto-save disabled. Users forced to explicitly save. Users prompted when attempted to close with unsaved changes.
- Periodic runtime logging to Intterra (optional).
- Allow setting the `agency` on profiles.
- Scheduled task automatically created after user accepts admin mode restart

#### Bug Fixes
- No longer sending batches when queries return 0 records.
- Stripping `\r` and `\n` characters from CSVs