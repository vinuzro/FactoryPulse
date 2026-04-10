# FACTORYPULSE

Factory monitoring + inspection system to track equipment status, log inspections, and generate reports. Built mainly to explore microservices, realtime updates, and multi-stack backend design.

## Features

- Role-based access (Admin / Engineer / Viewer)
- Inspection logging with audit trail
- Live status board using SignalR
- Mobile app for quick inspection entry
- PDF / Excel report generation

## Architecture

- 3 services:
  - Auth (login, roles)
  - Inspection (core data, logs)
  - Reporting (reports)
- API Gateway in front of services
- Each service has its own DB schema

## Tech

- Spring Boot, .NET 8, ASP.NET MVC
- C (used for some data handling)
- React Native (mobile)
- SQL Server
- SignalR (realtime)
- Docker Compose

## Notes

Started small, mainly to try realtime updates and service separation. Managing data flow between services was the hardest part. Some parts are still rough but overall system works as expected.