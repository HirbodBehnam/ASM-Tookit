# ASM Toolkit

A .Net library and CLI tool to create and simulate ASM charts. These charts can be converted to Verilog code.

## Features

* .NET 6 library. So it can be used in any .NET project
* Uses postfix statements. So everything can be evaluated.
* Unlimited register width
* Saving/Loading ASM charts as json
* Simulation of ASM charts with step by step debugging
* Generate Verilog code from ASM chart

### Limitations

* No non-blocking assignment in ASM statemnets
* No chained decision boxes

## CLI Tool

This project also contains a CLI tool to create, edit and simulate ASM charts.
It's not very powerful or has nice UI but it gets the job done (kinda).

### Download

You can download the compile binaries from [releases](https://github.com/HirbodBehnam/ASM-Tookit/releases).
You need .NET 6 to run it.

#### Simple ASM Chart

The zip file in releases contains a sample ASM chart which does the multiplication based on addition.