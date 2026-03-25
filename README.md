# 🫀 ECG-Vision 🫀

**ECG-Vision** integrates an ASP.NET Core backend with Python signal-processing scripts to automate the processing, storage, and analysis of ECG biometric data within a unified Docker environment.

## 🚀 Key Features

* Interop between .NET 10 and Python signal processing.
* JWT-based identity, resource-based authorization, and DataAnnotation validation.
* S3-compatible storage provider for durable medical signal hosting.
* Full OpenAPI documentation powered by Scalar.

## ⚙️ Getting Started
### 1. Prerequisites

* Docker
* .NET 10 SDK (for local development)
* An S3-compatible bucket (e.g., Minio, AWS)

### 2. Configuration

Create a `.env` file in the root directory (refer to `.env_example` for required keys) and run the following command to start the API and Database:

```
docker-compose up -d --build
```
The API will be available at http://localhost:5000.

To watch the logs as they happen:
```
docker logs -f ecg-vision-web
```
To see the last 100 lines of history and then continue following the live stream:
```
docker logs --tail 100 -f ecg-vision-web
```

## 📖 API Documentation

Once the application is running in Development mode, you can access the interactive API reference:

* Scalar UI: http://localhost:5000/scalar/v1
* OpenAPI Spec: http://localhost:5000/openapi/v1.json
