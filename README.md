# TowerBridge API

A small Web API to test out [HtmlAgilityPack](https://html-agility-pack.net/). The idea of creating the API was suggested by a tutor I once had in that the tower bridge can affect commutes but they offer no API to retrieve the data. They do however post of table of bridge lifts which the API screen scrapes

## Description

The API loads the HTML table from Tower Bridge [lift times](https://www.towerbridge.org.uk/lift-times) and then builds an array of bridge lift times. Microsoft's in-memory caching has also been used to improve performance.

## Docker

``` Bash
docker run -d -e TOWERBRIDGE__CACHINGEXPIRATION=01:00:00 -p 8080:80 milkyjoe93:towerbridge/api
```

## Configuration

|Environment Variable|Type|Default|Description|
|-|-|-|-|
|TOWERBRIDGE__CACHINGEXPIRATION|Timespan|01:00:00|How long to cache the bridge lift timetable|

## Logging

For logging, the [Serilog](https://serilog.net/) framework has been used to allow pushing logging events to various **sinks**. The current installed sinks are:

- [Console](https://github.com/serilog/serilog-sinks-console)
- [File](https://github.com/serilog/serilog-sinks-file)
- [Seq](https://github.com/datalust/serilog-sinks-seq)

[Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration/) has also been added to allow configuring serilog from appsettings.json or environment variables. By default the console and file sinks are enabled with the file logs being written to daily rolling log file at **/logs/log.txt**

## References

- [Serilog](https://serilog.net/)
- [HtmlAgilityPack](https://html-agility-pack.net/)