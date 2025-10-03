# Learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging at https://aka.ms/customizecontainer.

# This stage is used when run by VS in fast mode (default for Debug configuration).
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
USER $APP_UID
WORKDIR /app

# This stage is used to build the service project.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

RUN apk add --no-cache libc6-compat

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Sendlix.Smpt.Relay.csproj", "."]
RUN dotnet restore "./Sendlix.Smpt.Relay.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Sendlix.Smpt.Relay.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied into the final stage.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Sendlix.Smpt.Relay.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when run by VS in regular mode (default when Debug configuration is not used).
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 587

ENV ListenAddress=0.0.0.0

ENTRYPOINT ["dotnet", "Sendlix.Smpt.Relay.dll"]