# Simple TPA
Add 4 new command to the unturned server
- /tpa playername : request a teleport for the desired player
- /tpa accept : accept the teleport request
- /tpa deny : denied the request received
- /tpa abort : abort accepted request

### Configurations
- TickrateToExpire: time to a tpa request expire, calculation: Seconds * ServerTickrate
- TickrateToTeleport: time to player teleport to other player
- ServerTickrate: Actual server tickrate from ``Rocket.config``, used to calculate tickrate seconds

# Building

*Windows*: The project uses dotnet 4.8, consider installing into your machine, you need visual studio, simple open the solution file open the Build section and hit the build button (ctrl + shift + b) or you can do into powershell the command dotnet build -c Debug if you have installed dotnet 4.8.

*Linux*: Install dotnet-sdk from your distro package manager, open the root folder of this project and type ``dotnet build -c Debug``.

FTM License.
