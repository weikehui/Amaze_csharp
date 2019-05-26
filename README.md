# Amaze_csharp

A C# version solver for Amaze.

## Steup

Need Mono support if run on Linux or MacOS.

To Install Mono on Aws Linux server, run the script:

> sudo ./setup_mono.sh

## Build

To build the project, in project root directory, execute:

> xbuild /p:Configuration=Release

The executable file "amaze.exe" be generated in "bin/Release".

## Run

After build, you can run the "amaze.exe", there is two parameters:

> mono amaze.exe folder-path|file-path [--unoptimized]

|Parameter|Description|
|--|--|
|folder-path|the foler of all level data files.|
|file-path|single level data file.|
|--unoptimized|optional, disable optimized solve way for debug.|


You can also run it quickly in project root directory:

* all embedded levels

> ./run_all.sh

* all embedded levels use unoptimized way

> ./run_all_unoptimized.sh

* custom level(s)

> ./run.sh folder-path|file-path [--unoptimized]

## Result

|File|Description|
|--|--|
|result.txt|optimized solutions, more shorter :)|
|result_unoptimized.txt|un-optimized solutions, for debug.|

