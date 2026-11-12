FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY MockSnowflake/ MockSnowflake/
RUN dotnet publish MockSnowflake -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8088
ENTRYPOINT ["dotnet", "MockSnowflake.dll"]
