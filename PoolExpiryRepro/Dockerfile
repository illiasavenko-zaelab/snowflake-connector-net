FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SNOWFLAKE_DATA_VERSION=6.5.0
WORKDIR /src
COPY PoolExpiryRepro/ PoolExpiryRepro/
RUN dotnet publish PoolExpiryRepro -c Release -o /app -p:SnowflakeDataVersion=${SNOWFLAKE_DATA_VERSION}

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "PoolExpiryRepro.dll"]
