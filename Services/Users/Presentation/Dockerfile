FROM ubuntu:26.04  AS build
RUN apt-get update && apt-get install -y dotnet-sdk-10.0
WORKDIR /src

# Copy csproj files
COPY Shared/EventManagement.Shared.csproj Shared/EventManagement.Shared/
COPY Services/Users/Domain/EventManagement.Users.Domain.csproj Services/Users/Domain/
COPY Services/Users/Application/EventManagement.Users.Application.csproj Services/Users/Application/
COPY Services/Users/Infrastructure/EventManagement.Users.Infrastructure.csproj Services/Users/Infrastructure/
COPY Services/Users/Presentation/EventManagment.Users.Presentation.csproj Services/Users/Presentation/


# Copy all source code
COPY . .

RUN dotnet --version
RUN dotnet restore Services/Users/Presentation/EventManagment.Users.Presentation.csproj


WORKDIR /src/Services/Users/Presentation
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventManagement.Users.Presentation.dll"]