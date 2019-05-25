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

> mono amaze.exe folder-path|file-path [--optimized]

* folder-path  - the foler of all level data files.
* file-path    - single level data file.
* --optimized  - optional, enable optimized solve way. **Note: there is bug when there is some pipes cross with itself in some levels.**

You can also run it quickly in project root directory:

* custom level(s)

> ./run.sh folder-path|file-path [--optimized]

* all embedded levels

> ./run_all.sh

* all embedded levels use optimized way

> ./run_all_optimized.sh

## Result

result.txt - un-optimized solutions, no shortest, but ensure all levels are resolved.

result_optimized.txt - optimized solutions, more shorter, but there are problem in some levels.

