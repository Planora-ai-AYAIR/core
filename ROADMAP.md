# Planora Project Roadmap

This document outlines the high-level roadmap for the Planora platform. It is a living document and will be updated as the project evolves and priorities shift.

## Phase 1: MVP & Core Infrastructure (Completed - Release v1.0.0)

Our initial goal was to establish a robust, scalable architecture and deliver the core parcel management workflow.

* **Architecture:** Implementation of Clean Architecture in the .NET backend.
* **Infrastructure:** Dockerization of all services, automated CI/CD pipelines via GitHub Actions, and deployment to AWS EKS.
* **Authentication:** JWT-based user authentication and authorization with secure OTP verification.
* **Core Application:** Angular-based frontend with an interactive map (MapLibre) and dashboard interfaces.
* **Parcel Management:** GeoJSON upload, validation, and PostGIS storage using NetTopologySuite.
* **AI Analysis Integration:** Event-driven architecture utilizing Hangfire and PostgreSQL for processing complex AI analysis jobs (Topography, Soil, Risk).
* **Real-time Feedback:** SignalR implementation to push analysis results and status updates to connected clients immediately.

## Phase 2: Enhanced Analysis & User Experience (Next 3-6 Months)

Focus on expanding the AI capabilities and refining the user interface based on initial feedback.

* **Advanced Analysis Models:** Integration of more complex predictive models for geological risk assessment and environmental impact.
* **Batch Processing:** Allow users to upload and process multiple GeoJSON files or entire regions simultaneously.
* **Interactive Reporting:** Upgrade the automated PDF reports into dynamic, interactive web-based reports that users can explore in detail.
* **Data Export:** Provide comprehensive data export functionalities (CSV, Shapefile, KML) for analysis results.
* **User Roles & Permissions:** Implement fine-grained Role-Based Access Control (RBAC) to allow organizations to manage team access levels.
* **Performance Optimization:** Implement advanced database indexing for PostGIS queries and expand Redis caching strategies to reduce load times for heavy map renders.

## Phase 3: Integrations & Scalability (6-12 Months)

Focus on making Planora an extensible platform that fits into broader enterprise workflows.

* **Public API:** Release a documented, versioned REST/GraphQL API for third-party integrations.
* **Webhooks:** Allow users to subscribe to webhook events (e.g., when a parcel analysis completes) to trigger actions in external systems.
* **Marketplace Integrations:** Pre-built connectors for popular GIS software (ArcGIS, QGIS) and enterprise ERP systems.
* **Multi-region Support:** Deploy infrastructure across multiple AWS regions for lower latency and data residency compliance.
* **Cost Management Dashboards:** Provide organizations with visibility into their AI compute usage and associated costs.

## Phase 4: Long-Term Vision (12+ Months)

* **Predictive City Planning:** Moving beyond single parcels to analyze entire city grids for optimal infrastructure placement.
* **Real-time Sensor Integration:** Ingesting live IoT data (e.g., soil moisture, seismic activity) into the analysis models.
* **Advanced Collaboration:** Real-time multi-user editing and annotation on the same map view.

## How to Influence the Roadmap

We highly value community and user feedback. If you have feature requests or suggestions, please open an issue in our GitHub repository and tag it with `enhancement`. Major architectural changes or new product features will be discussed openly in the issue tracker.
