FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=password

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["samples/Service/Host/Host.csproj", "samples/Service/Host/"]
COPY ["src/UFX.Relay/UFX.Relay.csproj", "src/UFX.Relay/"]
RUN dotnet restore "samples/Service/Host/Host.csproj"
COPY . .
WORKDIR "/src/samples/Service/Host"
RUN dotnet build "Host.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Host.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
RUN dotnet dev-certs https -ep /app/aspnetapp.pfx -p password

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /app/aspnetapp.pfx ./aspnetapp.pfx
USER root
RUN chmod 777 ./aspnetapp.pfx
USER $APP_UID

ENTRYPOINT ["dotnet", "Host.dll"]