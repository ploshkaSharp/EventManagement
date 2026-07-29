FROM ubuntu:26.04  AS build
RUN apt-get update && apt-get install -y dotnet-sdk-10.0
WORKDIR /src

# Copy Shared project
COPY Shared/EventManagement.Shared.csproj Shared/EventManagement.Shared/

# Copy Events service projects
COPY Services/Events/Domain/EventManagement.Events.Domain.csproj Services/Events/Domain/
COPY Services/Events/Application/EventManagement.Events.Application.csproj Services/Events/Application/
COPY Services/Events/Infrastructure/EventManagement.Events.Infrastructure.csproj Services/Events/Infrastructure/
COPY Services/Events/Presentation/EventManagment.Events.Presentation.csproj Services/Events/Presentation/

# Copy all source code
COPY . .

# Restore dependencies
RUN dotnet restore Services/Events/Presentation/EventManagment.Events.Presentation.csproj


# Build and publish
WORKDIR /src/Services/Events/Presentation
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published files
COPY --from=build /app/publish .

# Set environment variable for port
ENV ASPNETCORE_URLS="http://+:8080"

ENTRYPOINT ["dotnet", "EventManagement.Events.Presentation.dll"]