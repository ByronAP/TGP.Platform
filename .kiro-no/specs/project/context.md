# Project Context

## Project Vision
(To be defined based on further exploration, but currently appears to be a device management and monitoring system with a strong focus on security and scalability).

## Current Status
- **Date**: 2025-12-26
- **Phase**: Development / Architecture Consolidation
- **Key Activities**:
  - Familiarization with codebase.
  - Establishing project steering documentation.

## Key Observations
- **Monorepo Structure**: The project is structured as a monorepo with separate solutions for microservices.
- **Duplication**: `TGP.Data` seems to be duplicated in `TGP.Microservices.DeviceGateway/src/TGP.Data`. This needs to be addressed to ensure a single source of truth.
- **Modern Tech Stack**: Uses .NET 10, which is cutting edge (assuming future date or preview).

## Immediate Goals
1.  Establish a clear understanding of the system.
2.  Verify the build and test health.
3.  Address architectural anomalies (like the code duplication).
