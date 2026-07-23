# Local Development Run

Use these scripts when you want one-command local startup.

Start Docker infrastructure and all three API services:

```powershell
.\scripts\start-dev.ps1
```

If PowerShell execution policy blocks `.ps1`, use the command wrapper:

```powershell
.\scripts\start-dev.cmd
```

Swagger URLs:

- Users: http://localhost:5202/swagger
- Events: http://localhost:5203/swagger
- Bookings: http://localhost:5075/swagger

Stop only API services:

```powershell
.\scripts\stop-dev.ps1
```

Stop API services and Docker infrastructure:

```powershell
.\scripts\stop-dev.ps1 -WithDocker
```

Logs are written to:

```text
.run/logs
```
