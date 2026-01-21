# SS14.Admin

SS14.Admin is the web-based admin panel intended to be used with Space Station 14.

## Configuration

Example config file:

```yml
Serilog:
    Using: [ "Serilog.Sinks.Console" ]
    MinimumLevel:
        Default: Information
        Override:
            SS14: Debug
            Microsoft: "Warning"
            Microsoft.Hosting.Lifetime: "Information"
            Microsoft.AspNetCore: Warning
            IdentityServer4: Warning
    WriteTo:
        - Name: Console
          Args:
              OutputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}"

    Enrich: [ "FromLogContext" ]

    #Loki:
    #    Address: "http://localhost:3102"
    #    Name: "centcomm"

ConnectionStrings:
    # Connects to the same postgres database as the game server
    DefaultConnection: "Server=127.0.0.1;Port=5432;Database=ss14;User Id=ss14-admin;Password=foobar"

AllowedHosts: "central.spacestation14.io"

urls: "http://localhost:27689/"

PathBase: "/admin"

WebRootPath: "/opt/ss14_admin/bin/wwwroot"

ForwardProxies:
    - 127.0.0.1
    - 172.16.0.0/12  # Supports CIDR notation for subnets  (Docker)

Auth:
    Authority: "https://central.spacestation14.io/web/"
    ClientId: "9e2ce26f-28ba-4232-b4d9-8cc08993b33e"
    ClientSecret: "foobar"

authServer: "https://central.spacestation14.io/auth"
```

When registering an OAuth app against our auth server, use `/signin-oidc` as redirect URI (relative to whatever path your SS14.Admin thing is at, so for us it's `https://central.spacestation14.io/admin/signin-oidc`).
