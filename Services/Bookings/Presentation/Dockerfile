FROM ubuntu:26.04  AS build
RUN apt-get update && apt-get install -y dotnet-sdk-10.0

WORKDIR /src

# Copy Shared project
COPY Shared/EventManagement.Shared.csproj Shared/EventManagement.Shared/

# Copy Bookings service projects
COPY Services/Bookings/Domain/EventManagement.Bookings.Domain.csproj Services/Bookings/Domain/
COPY Services/Bookings/Application/EventManagement.Bookings.Application.csproj Services/Bookings/Application/
COPY Services/Bookings/Infrastructure/EventManagement.Bookings.Infrastructure.csproj Services/Bookings/Infrastructure/
COPY Services/Bookings/Presentation/EventManagment.Bookings.Presentation.csproj Services/Bookings/Presentation/

# Copy all source code
COPY . .

# Restore dependencies
RUN dotnet restore Services/Bookings/Presentation/EventManagment.Bookings.Presentation.csproj



# Build and publish
WORKDIR /src/Services/Bookings/Presentation
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime image
# FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
FROM ubuntu:26.04  AS runtime
RUN apt-get update && apt-get install -y dotnet-runtime-10.0
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published files
COPY --from=build /app/publish .

# Set environment variable for port
ENV ASPNETCORE_URLS="http://+:8080"

ENTRYPOINT ["dotnet", "EventManagment.Bookings.Presentation.dll"]