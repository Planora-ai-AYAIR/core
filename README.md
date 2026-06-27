# Planora

Welcome to the Planora platform repository. Planora is a comprehensive, AI-driven platform for geospatial analysis, parcel management, and geological risk assessment.

## Accessing the Project

* **Production Environment:** The application is deployed to AWS EKS. The primary frontend interface can be accessed at `https://app.planora.ai` (ensure you have the necessary credentials or have registered an account to log in).
* **API Documentation:** The backend OpenAPI (Swagger) documentation is available at `https://api.planora.ai/swagger` when the environment is live.

## Repository Structure

This repository is structured as a monorepo containing the following core components:

* `apps/api-dotnet/`: The core backend API built with .NET 8 using Clean Architecture principles. It handles authentication, data persistence, and orchestration.
* `apps/frontend/`: The client-facing Angular application featuring interactive MapLibre maps and data dashboards.
* `apps/ai-python/`: The Python-based AI microservice responsible for heavy geospatial processing and generating analysis models.
* `infra/`: Infrastructure as Code (IaC) and Kubernetes deployment manifests.
* `docs/`: Additional architectural and technical documentation.

## Core Features

* **Advanced Parcel Management:** Upload, validate, and store complex GeoJSON boundaries. Geographic data is handled natively using PostGIS and NetTopologySuite.
* **AI Analysis Workflows:** Automated event-driven processing for Topography, Soil composition, and Risk assessment using Python-backed AI models.
* **Real-time Notifications:** WebSockets (SignalR) integration provides immediate feedback on long-running analysis jobs directly to the frontend.
* **Robust Infrastructure:** Built on a highly scalable stack utilizing SQL Server/PostgreSQL, Redis for caching, and Hangfire for resilient background job processing.
* **Secure Authentication:** Complete JWT-based authentication system with secure OTP verification.

## Prerequisites for Local Development

To run this project locally, ensure you have the following installed:

* Docker Desktop
* Node.js (v22+)
* .NET 8 SDK
* Python 3.14

## Local Setup & Installation

We provide a streamlined Docker Compose setup to run the necessary infrastructure and services locally.

1. **Clone the repository:**
   `git clone https://github.com/Planora-ai-AYAIR/core.git`
   `cd core`

2. **Start the Infrastructure Containers:**
   From the root directory, start the required databases and caching layers.
   `docker-compose up -d sqlserver redis postgres`

3. **Run the Backend API:**
   Navigate to the API source directory and run the application. Ensure you have applied the initial Entity Framework migrations.
   `cd apps/api-dotnet/src/Planora.API`
   `dotnet run`

4. **Run the Frontend Application:**
   Navigate to the frontend directory, install dependencies, and start the development server.
   `cd apps/frontend`
   `npm install`
   `npm run start`

5. **Run the AI Service:**
   Navigate to the AI service directory, install the Python environment, and start the local server.
   `cd apps/ai-python`
   `pip install -e .[dev]`
   `python -m uvicorn main:app --reload`

## Project Documentation

For more detailed information on contributing to the project, our community standards, and our future plans, please refer to the following documents:

* [Contributing Guidelines](CONTRIBUTING.md)
* [Code of Conduct](CODE_OF_CONDUCT.md)
* [Project Roadmap](ROADMAP.md)
