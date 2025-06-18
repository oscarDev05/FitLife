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
RUN dotnet publish ProyectoTFG/ProyectoTFG.csproj -c Release -o /app/out

# Etapa final: copiamos archivos de publicación
FROM base AS final
WORKDIR /app
COPY --from=build /app/out .

# Ejecutamos la app
ENTRYPOINT ["dotnet", "ProyectoTFG.dll"]
