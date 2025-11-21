FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["vigia-del-rio-procesamiento.csproj", "."]
RUN dotnet restore "./vigia-del-rio-procesamiento.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "vigia-del-rio-procesamiento.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "vigia-del-rio-procesamiento.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "vigia-del-rio-procesamiento.dll"]
