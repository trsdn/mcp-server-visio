# VisioMcp.ComInterop

Low-level COM interop utilities for Visio automation.

## Overview

This library provides the session, batching, threading, retry, and COM cleanup primitives that the rest of VisioMcp builds on. The implementation still uses some historical `Ppt*` type names internally, but the public intent of this layer is Visio automation on Windows.

## Responsibilities

- **STA threading management** for Office COM automation
- **Session and batch execution** for repeated Visio operations
- **COM object lifecycle helpers** for reliable cleanup
- **OLE message filtering and retry behavior** for busy COM calls
- **Resilient shutdown helpers** for closing Office automation cleanly

## Key Types

- **`PptSession`** — session lifecycle helper used by the higher Visio layers
- **`PptBatch`** — groups multiple operations into one automation session
- **`ComUtilities`** — safe COM cleanup and helper methods
- **`OleMessageFilter`** — retries rejected/busy COM calls

## Requirements

- Windows
- .NET 10.0 or later
- Microsoft Visio desktop installed

## Platform Support

- Windows x64
- Windows ARM64
- Linux: not supported
- macOS: not supported
