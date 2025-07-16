# IdentityService

IdentityService is a microservice responsible for user authentication and authorization, including token generation, user management, and role-based access control.

## Tech Stack

- **.NET 8 / 9** (ASP.NET Core)
- **Entity Framework Core 9**
- **PostgreSQL** (Neon Database)
- **JWT Authentication**
- **Refresh Token Support**

## Features

- User registration and login
- Role management
- JWT token generation and validation
- Refresh token support
- Secure password hashing
- Basic CRUD operations for users

## Folder Structure

- `Controllers/` – API endpoints
- `Models/` – Data models
- `Services/` – Business logic and authentication services
- `Data/` – EF Core DbContext and Migrations
- `Middlewares/` – Custom middleware (if any)
- `Configurations/` – JWT and DB configuration

## Setup Instructions

1. Clone the repository:
   ```bash
   git clone <your-repo-url>
   cd IdentityService
