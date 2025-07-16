# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the source code
COPY . ./

# Build the application
RUN dotnet publish -c Release -o out

# Use the ASP.NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/out .

# Set environment variables (optional but recommended)
ENV ASPNETCORE_URLS=http://+:5000

# Expose port 5000 for the container
EXPOSE 5000

# Define the entry point to run the app
ENTRYPOINT ["dotnet", "IdentityService.dll"]
