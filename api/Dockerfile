FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY src/NoAzul.Api/NoAzul.Api.csproj                 src/NoAzul.Api/
COPY src/NoAzul.Application/NoAzul.Application.csproj src/NoAzul.Application/
COPY src/NoAzul.Domain/NoAzul.Domain.csproj           src/NoAzul.Domain/
COPY src/NoAzul.Infra.Data/NoAzul.Infra.Data.csproj   src/NoAzul.Infra.Data/
COPY src/NoAzul.Infra.IoC/NoAzul.Infra.IoC.csproj     src/NoAzul.Infra.IoC/
COPY src/NoAzul.Workers/NoAzul.Workers.csproj         src/NoAzul.Workers/
RUN dotnet restore src/NoAzul.Api/NoAzul.Api.csproj

COPY src/ src/
RUN dotnet publish src/NoAzul.Api/NoAzul.Api.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 tzdata \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV TZ=America/Sao_Paulo
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "NoAzul.Api.dll"]
