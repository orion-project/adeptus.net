# SQLite database CRDT synchronization

## LiteSync

[LiteSync](https://litesync.io/) allows your applications to easily keep their SQLite databases synchronized (CRDT = Conflict-free Replicated Data Type).To keep the LiteSync Primary Node running 24/7 on your VPS, you should use systemd. This ensures that if the process crashes or the VPS reboots, the synchronization listener starts back up automatically.

### Create the Service

Run the following command to create a new service file (you’ll need sudo):

```bash
sudo nano /etc/systemd/system/litesync.service
```

Paste the following content into the file. Adjust the paths and the port to match your setup:

```ini
[Unit]
Description=LiteSync Primary Node Service
After=network.target

[Service]
# Replace 'youruser' with your actual VPS username
User=youruser
Group=youruser

# The directory where your master database is located
WorkingDirectory=/home/youruser/db_sync/

# Command to run the LiteSync primary node
# - Replace 'master.db' with your filename
# - Replace '59231' with your random port
# - The 'key' must match your Qt app's connection string
ExecStart=/usr/local/bin/litesync /home/youruser/db_sync/master.db \
    "node=primary&bind=tcp://0.0.0.0:59231&key=YourLongRandomSecretKey123!"

# Restart logic
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

### Enable and Start the Service

Run these commands to tell the system about the new service and start it:

```bash
# Reload systemd to recognize the new file
sudo systemctl daemon-reload

# Enable it so it starts on boot
sudo systemctl enable litesync

# Start the service now
sudo systemctl start litesync

# Check the status to ensure it's running correctly
sudo systemctl status litesync
```

### Important Considerations for Your Workflow

**The Master File:** The `master.db` on your VPS will become the "Source of Truth." When you first start, make sure this file has the correct table structure.

**The key Security:** Since this file is on your VPS disk, ensure only your user has read/write permissions to the db_sync folder (chmod 700).

**Qt Driver Deployment:** When you build your Qt app, remember that you must link against the LiteSync-enabled SQLite library, not the default one, otherwise the node=primary part of the command string will be ignored as an "unknown parameter."

## LiteSync-Enabled Driver C++

To integrate LiteSync into your Qt project, you must recompile the QSQLITE driver so that it uses the LiteSync-modified SQLite engine instead of the standard one.

### Build the LiteSync-Enabled Driver

You must perform this step once on your development machine to generate the custom plugin.

1. Install Qt Sources: Use the Qt Maintenance Tool to ensure "Sources" are installed for your version.

2. Prepare LiteSync: Download the LiteSync binaries and place the header files in an include folder and the .lib/.dll in a lib folder. Crucial: Rename `litesync.lib` to `sqlite3.lib` in your local folder so the build script finds it.

3. Run the Build: Open the Qt Terminal and navigate to the driver source directory:

```bash
cd %QTDIR%\qtbase\src\plugins\sqldrivers
# For Qt 5/6 with qmake:
qmake -- -system-sqlite SQLITE_PREFIX=C:/Path/To/LiteSync
make
make install
```

This creates a new `qsqlite.dll` (or `.so`) that is internally linked to LiteSync.

### Qt Project Configuration

Once the driver is built, you can use it in your project by adding the SQL module.

**In your .pro file (qmake):**

```qmake
QT += sql
# No special flags needed here, as the driver is now LiteSync-aware
```

**In your CMakeLists.txt (CMake):**

```cmake
find_package(Qt6 REQUIRED COMPONENTS Sql)
target_link_libraries(my_app PRIVATE Qt6::Sql)
```

### Usage in C++ Code

Because the driver is now "LiteSync-aware," you simply use the standard `QSqlDatabase` class but pass the specialized URI parameters in the database name.

```cpp
#include <QSqlDatabase>
#include <QSqlQuery>

// ...

QSqlDatabase db = QSqlDatabase::addDatabase("QSQLITE");

// URI string containing your VPS IP, random port, and secret key
QString connectionString = "file:local_app.db?"
                           "node=secondary&"
                           "connect=tcp://your-vps-ip:59231&"
                           "cipher=chacha20&"
                           "key=YourSuperSecretKey123!";

db.setDatabaseName(connectionString);

if (db.open()) {
    // Standard Qt SQL code works normally
    QSqlQuery query;
    query.exec("CREATE TABLE IF NOT EXISTS notes (id INTEGER PRIMARY KEY, content TEXT)");
}
```

### Distribution

When you distribute your app, you must include the newly compiled LiteSync-enabled `qsqlite` plugin in the `sqldrivers` folder of your release package.

## Monitoring Sync Status

Since LiteSync operates as an extension to SQLite, you don't use a separate network API. Instead, you query a special internal table called `litesync_status` using standard `QSqlQuery`.

### Monitoring Sync Status in C++

You can create a timer or a refresh function in your Qt app to check if the local instance is successfully talking to your VPS.

```cpp
#include <QSqlQuery>
#include <QSqlRecord>
#include <QVariant>
#include <QDebug>

void checkSyncStatus() {
    QSqlQuery query("SELECT * FROM litesync_status");

    if (query.next()) {
        // 'connected' returns 1 if linked to the VPS, 0 otherwise
        bool isConnected = query.value("connected").toBool();

        // 'sync_status' tells you if data is currently being transferred
        // 0 = in sync, 1 = syncing
        int syncStatus = query.value("sync_status").toInt();

        // 'db_is_ready' is true once the initial sync is complete
        bool isReady = query.value("db_is_ready").toBool();

        if (isConnected) {
            qDebug() << "Connected to VPS. Sync state:" << (syncStatus == 0 ? "Idle" : "Syncing...");
        } else {
            qWarning() << "Disconnected from VPS. Changes are being saved locally only.";
        }
    }
}
```

The litesync_status table provides several helpful fields you can display in your app's UI (like a status bar icon):

- **connected:** Is the TCP connection to your VPS active?
- **sync_status:** Are there pending changes being uploaded or downloaded?
- **local_changes:** How many rows have been changed locally but haven't reached the VPS yet?
- **remote_changes:** How many rows have been received from the VPS but not yet applied locally?

### Handling First-Time Sync

Because SQLite is "local-first," your `db.open()` call will return true immediately even if the VPS is offline.

**Pro-Tip:** If your app requires the very latest data before starting (e.g., a shared task list), check `db_is_ready` before allowing the user to edit. If it's 0, show a "Downloading latest data..." overlay.

## LiteSync-Enabled Driver C#

To get your C# app talking to the same LiteSync VPS as your Qt app, you'll follow a nearly identical pattern. Since C# typically uses Microsoft.Data.Sqlite or System.Data.SQLite, you just need to swap the underlying engine.

### The C# "Driver" Strategy

Just like in Qt, the standard C# SQLite NuGet packages won't "speak" the LiteSync protocol (TCP/Sync) out of the box. Use the LiteSync .NET wrapper. LiteSync provides a `litesync.dll` (the engine) and a C# wrapper that implements the standard `IDbConnection` interface. This allows you to use standard ADO.NET patterns or even Entity Framework with minimal changes.

For .NET 10, you don't use the standard Microsoft.Data.Sqlite NuGet package because it bundles its own "vanilla" SQLite. Instead add the LiteSync Native DLL:
- Download the 64-bit LiteSync.dll from their portal.
- Right-click your project in *Solution Explorer -> Add -> Existing Item*.
- Select `litesync.dll`.
- In the *Properties* window for that file, set *Copy to Output Directory* to *Copy if newer*.
- Add a reference to LiteSync.NET.dll (the managed wrapper).

### Connection String in C#

Your C# connection string will look exactly like the one in your Qt app. You pass the URI parameters (IP, Port, Key) directly into the constructor.

```csharp
using System.Data;
using LiteSync; // Assuming you've added the LiteSync.NET reference

// ...

string connString = "file:local_csharp_app.db?" +
                    "node=secondary&" +
                    "connect=tcp://your-vps-ip:59231&" +
                    "cipher=chacha20&" +
                    "key=YourSuperSecretKey123!";

using (var connection = new LiteSyncConnection(connString))
{
    connection.Open();

    // Now you can run standard SQL commands
    var command = connection.CreateCommand();
    command.CommandText = "SELECT content FROM notes";

    using (var reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            Console.WriteLine(reader.GetString(0));
        }
    }
}
```

### Monitoring Sync in C#

To check if your C# instance is connected to the VPS, you query that same `litesync_status` table:

```csharp
var statusCmd = connection.CreateCommand();
statusCmd.CommandText = "SELECT connected, sync_status FROM litesync_status";
using (var reader = statusCmd.ExecuteReader())
{
    if (reader.Read())
    {
        bool isConnected = reader.GetInt32(0) == 1;
        int syncStatus = reader.GetInt32(1); // 0 = idle, 1 = syncing
        // Update your UI accordingly
    }
}
```

### Key Differences to Note

- **Deployment:** When you publish your C# app, you must include the native litesync.dll (the C++ core) in the same folder as your .exe. C# calls into this unmanaged DLL via P/Invoke.

- **Architecture:** Ensure you match the "Bitness." If your C# app is 64-bit, you must use the 64-bit LiteSync DLL.

- **Shared Data:** Since both the Qt and C# apps connect to the same `master.db` on your VPS, they will see each other's changes in near real-time.

### Handling .NET 10 "Publish"

When you use Publish in Visual Studio (to create your GitHub Release), .NET 10 might try to "trim" unused code.

- **Self-Contained:** If you publish as "Self-Contained," make sure the litesync.dll is included in the root folder.

- **Native AOT:** If you are using Native AOT (popular in .NET 10), you must ensure the LiteSync wrapper is compatible with AOT (no heavy reflection). LiteSync's direct P/Invoke approach is generally AOT-friendly.

### Using with EF Core

To use Entity Framework Core with LiteSync in .NET 10, you leverage the `Microsoft.EntityFrameworkCore.Sqlite` provider. Since EF Core sits on top of the ADO.NET layer, you simply swap the underlying connection with a `LiteSyncConnection`. In Visual Studio, ensure you have the following NuGet packages `Microsoft.EntityFrameworkCore.Sqlite` and `LiteSync.NET` (The wrapper).

The key is to pass the LiteSync Connection String into the `UseSqlite` method in your `OnConfiguring` override:

```csharp
using Microsoft.EntityFrameworkCore;
using LiteSync;

public class MyAppContext : DbContext
{
    // Your Data Tables (Entities)
    public DbSet<Note> Notes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // 1. Define your Sync Connection String (same as Qt app)
        string connString = "file:local_ef_core.db?" +
                            "node=secondary&" +
                            "connect=tcp://your-vps-ip:59231&" +
                            "cipher=chacha20&" +
                            "key=YourSuperSecretKey123!";

        // 2. Tell EF Core to use a LiteSyncConnection instead of a standard one
        var connection = new LiteSyncConnection(connString);

        // This is the "magic" - EF Core uses the LiteSync engine
        // but keeps all its LINQ and Mapping features.
        optionsBuilder.UseSqlite(connection);
    }
}

// Simple Entity
public class Note
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
}
```

Now you can use standard LINQ queries. LiteSync will handle the background synchronization automatically.

```csharp
using var db = new MyAppContext();

// Ensure the database exists (this triggers the sync setup)
db.Database.EnsureCreated();

// Adding data (will sync to VPS and then to your Qt app)
db.Notes.Add(new Note { Content = "Hello from .NET 10!" });
db.SaveChanges();

// Reading data (includes changes made in the Qt app)
var allNotes = db.Notes.ToList();
foreach (var note in allNotes)
{
    Console.WriteLine(note.Content);
}
```

#### Handling Migrations (Schema Changes)

When using EF Core, you typically use Migrations to manage schema changes.

- **The Workflow:** Run `Add-Migration` in Visual Studio.
- **The Sync:** When you run `db.Database.Migrate()` on one machine, it executes the `ALTER TABLE` commands. LiteSync detects these and automatically pushes the new schema to your VPS and other machines (including the Qt one).

#### The Sync Worker Service

Because EF Core opens and closes connections frequently, you might want to keep one "Global" LiteSync connection open for the lifetime of the app. This ensures the sync engine stays connected to the VPS in the background even when you aren't actively querying the database.

- **Persistent Sync:** If you add a record in your Qt app, this background service will receive the change immediately, even if your C# UI is just sitting idle.

- **Performance:** EF Core won't have to perform the "Handshake" with your VPS every time you call SaveChanges(); the engine is already "warm."

- **Reliability:** If the VPS goes down, the BackgroundService can handle reconnection logic without crashing your main UI thread.

In .NET 10, the cleanest way to keep your sync engine alive is using a BackgroundService. This ensures the TCP connection to your VPS stays active even when your EF Core context isn't being used.

Create a service that holds a "master" connection. This prevents the sync engine from "sleeping" or disconnecting between UI actions.

```csharp
using LiteSync;
using Microsoft.Extensions.Hosting;

public class LiteSyncWorker(string connectionString) : BackgroundService
{
    private LiteSyncConnection? _keepAliveConnection;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Initialize the master connection to keep the sync engine running
            _keepAliveConnection = new LiteSyncConnection(connectionString);
            _keepAliveConnection.Open();

            Console.WriteLine("LiteSync: Master connection established. Syncing in background...");

            // Keep the service alive until the app shuts down
            while (!stoppingToken.IsCancellationRequested)
            {
                // Optional: Periodically log sync status from the litesync_status table here
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LiteSync Error: {ex.Message}");
        }
        finally
        {
            _keepAliveConnection?.Close();
        }
    }
}
```

In your .NET 10 entry point, register this service. It will start automatically when your app launches.

```csharp
var builder = Host.CreateApplicationBuilder(args);

const string connString = "file:local_ef_core.db?node=secondary&connect=tcp://your-vps-ip:59231&key=YourKey!";

// Register the worker to keep the connection open
builder.Services.AddHostedService(_ => new LiteSyncWorker(connString));

// Register your DbContext
builder.Services.AddDbContext<MyAppContext>();

using IHost host = builder.Build();
await host.RunAsync();
```
