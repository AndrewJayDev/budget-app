FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY BucketBudget.Domain/BucketBudget.Domain.csproj BucketBudget.Domain/
COPY BucketBudget.Application/BucketBudget.Application.csproj BucketBudget.Application/
COPY BucketBudget.Infrastructure/BucketBudget.Infrastructure.csproj BucketBudget.Infrastructure/
COPY BucketBudget.Client/BucketBudget.Client.csproj BucketBudget.Client/
COPY BucketBudget.Api/BucketBudget.Api.csproj BucketBudget.Api/
RUN dotnet restore BucketBudget.Api/BucketBudget.Api.csproj

# Copy everything and build
COPY . .
RUN dotnet publish BucketBudget.Api/BucketBudget.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "BucketBudget.Api.dll"]
