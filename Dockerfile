# Etapa base: runtime de ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Etapa de build: SDK de .NET
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos todo el proyecto
COPY . .

# Restauramos dependencias
RUN dotnet restore ProyectoTFG/ProyectoTFG.csproj

# Publicamos el proyecto
RUN dotnet publish ProyectoTFG/ProyectoTFG.csproj -c Release -o /app/publish

# Etapa final: runtime con los archivos publicados
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Ejecutamos la app
ENTRYPOINT ["dotnet", "ProyectoTFG.dll"]
