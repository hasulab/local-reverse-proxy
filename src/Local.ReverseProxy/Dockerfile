#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Local.ReverseProxy/Local.ReverseProxy.csproj", "src/Local.ReverseProxy/"]
RUN dotnet restore "src/Local.ReverseProxy/Local.ReverseProxy.csproj"
COPY . .
WORKDIR "/src/src/Local.ReverseProxy"
RUN dotnet build "Local.ReverseProxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Local.ReverseProxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Local.ReverseProxy.dll"]